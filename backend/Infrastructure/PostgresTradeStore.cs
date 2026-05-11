using EfxSimulator.Api.Models;
using Npgsql;

namespace EfxSimulator.Api.Infrastructure;

public sealed class PostgresTradeStore : ITradeStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly SemaphoreSlim _schemaGate = new(1, 1);
    private bool _schemaInitialized;

    public PostgresTradeStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task RecordTradeAsync(Trade trade)
    {
        await EnsureSchemaAsync();

        await using var command = _dataSource.CreateCommand("""
            INSERT INTO trades (
                trade_id,
                portfolio_id,
                quote_id,
                pair,
                side,
                base_currency,
                quote_currency,
                base_amount,
                quote_amount,
                price,
                realized_pnl,
                status,
                executed_at_utc
            )
            VALUES (
                @trade_id,
                @portfolio_id,
                @quote_id,
                @pair,
                @side,
                @base_currency,
                @quote_currency,
                @base_amount,
                @quote_amount,
                @price,
                @realized_pnl,
                @status,
                @executed_at_utc
            )
            ON CONFLICT (trade_id) DO NOTHING;
            """);

        command.Parameters.AddWithValue("trade_id", trade.TradeId);
        command.Parameters.AddWithValue(
            "portfolio_id",
            PortfolioIds.Normalize(trade.PortfolioId));
        command.Parameters.AddWithValue("quote_id", trade.QuoteId);
        command.Parameters.AddWithValue("pair", trade.Pair);
        command.Parameters.AddWithValue("side", trade.Side);
        command.Parameters.AddWithValue("base_currency", trade.BaseCurrency);
        command.Parameters.AddWithValue("quote_currency", trade.QuoteCurrency);
        command.Parameters.AddWithValue("base_amount", trade.BaseAmount);
        command.Parameters.AddWithValue("quote_amount", trade.QuoteAmount);
        command.Parameters.AddWithValue("price", trade.Price);
        command.Parameters.AddWithValue("realized_pnl", trade.RealizedPnl);
        command.Parameters.AddWithValue("status", trade.Status);
        command.Parameters.AddWithValue(
            "executed_at_utc",
            DateTime.SpecifyKind(trade.ExecutedAtUtc, DateTimeKind.Utc));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Trade>> GetLatestTradesAsync(
        string portfolioId = PortfolioIds.Default,
        int count = 50)
    {
        await EnsureSchemaAsync();

        await using var command = _dataSource.CreateCommand("""
            SELECT
                trade_id,
                portfolio_id,
                quote_id,
                pair,
                side,
                base_currency,
                quote_currency,
                base_amount,
                quote_amount,
                price,
                realized_pnl,
                status,
                executed_at_utc
            FROM trades
            WHERE portfolio_id = @portfolio_id
            ORDER BY executed_at_utc DESC, trade_id DESC
            LIMIT @count;
            """);

        command.Parameters.AddWithValue(
            "portfolio_id",
            PortfolioIds.Normalize(portfolioId));
        command.Parameters.AddWithValue("count", count);

        await using var reader = await command.ExecuteReaderAsync();
        var trades = new List<Trade>();

        while (await reader.ReadAsync())
        {
            trades.Add(new Trade
            {
                TradeId = reader.GetString(0),
                PortfolioId = reader.GetString(1),
                QuoteId = reader.GetString(2),
                Pair = reader.GetString(3),
                Side = reader.GetString(4),
                BaseCurrency = reader.GetString(5),
                QuoteCurrency = reader.GetString(6),
                BaseAmount = reader.GetDecimal(7),
                QuoteAmount = reader.GetDecimal(8),
                Price = reader.GetDecimal(9),
                RealizedPnl = reader.GetDecimal(10),
                Status = reader.GetString(11),
                ExecutedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(12), DateTimeKind.Utc)
            });
        }

        return trades;
    }

    private async Task EnsureSchemaAsync()
    {
        if (_schemaInitialized)
        {
            return;
        }

        await _schemaGate.WaitAsync();

        try
        {
            if (_schemaInitialized)
            {
                return;
            }

            await using var command = _dataSource.CreateCommand("""
                CREATE TABLE IF NOT EXISTS trades (
                    trade_id text PRIMARY KEY,
                    portfolio_id text NOT NULL DEFAULT 'default',
                    quote_id text NOT NULL,
                    pair text NOT NULL,
                    side text NOT NULL,
                    base_currency text NOT NULL,
                    quote_currency text NOT NULL,
                    base_amount numeric(20, 6) NOT NULL,
                    quote_amount numeric(20, 6) NOT NULL,
                    price numeric(20, 8) NOT NULL,
                    realized_pnl numeric(20, 6) NOT NULL DEFAULT 0,
                    status text NOT NULL,
                    executed_at_utc timestamptz NOT NULL
                );

                ALTER TABLE trades
                    ADD COLUMN IF NOT EXISTS portfolio_id text NOT NULL DEFAULT 'default';

                ALTER TABLE trades
                    ADD COLUMN IF NOT EXISTS realized_pnl numeric(20, 6) NOT NULL DEFAULT 0;

                CREATE INDEX IF NOT EXISTS ix_trades_executed_at_utc
                    ON trades (executed_at_utc DESC);

                CREATE INDEX IF NOT EXISTS ix_trades_portfolio_executed_at_utc
                    ON trades (portfolio_id, executed_at_utc DESC);
                """);

            await command.ExecuteNonQueryAsync();
            _schemaInitialized = true;
        }
        finally
        {
            _schemaGate.Release();
        }
    }
}
