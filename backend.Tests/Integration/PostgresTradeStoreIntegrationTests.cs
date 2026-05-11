using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using Npgsql;

namespace EfxSimulator.Api.Tests.Integration;

public sealed class PostgresTradeStoreIntegrationTests
{
    [Fact]
    public async Task PostgresTradeStore_RoundTripsTrades_WhenPostgresIntegrationIsEnabled()
    {
        await using var fixture = await PostgresIntegrationFixture.TryCreateAsync();

        if (fixture is null)
        {
            return;
        }

        var store = new PostgresTradeStore(fixture.DataSource);
        var firstTrade = CreateTrade("t-postgres-1", "q-1", "EURUSD", DateTime.UtcNow.AddSeconds(-1));
        var secondTrade = CreateTrade("t-postgres-2", "q-2", "GBPUSD", DateTime.UtcNow);

        await store.RecordTradeAsync(firstTrade);
        await store.RecordTradeAsync(secondTrade);

        var trades = await store.GetLatestTradesAsync(count: 2);

        Assert.Collection(
            trades,
            trade => Assert.Equal("t-postgres-2", trade.TradeId),
            trade => Assert.Equal("t-postgres-1", trade.TradeId));
    }

    private static Trade CreateTrade(
        string tradeId,
        string quoteId,
        string pair,
        DateTime executedAtUtc)
    {
        var price = pair == "GBPUSD" ? 1.2600m : 1.1000m;

        return new Trade
        {
            TradeId = tradeId,
            QuoteId = quoteId,
            Pair = pair,
            Side = "BUY",
            BaseCurrency = pair[..3],
            QuoteCurrency = pair[3..],
            BaseAmount = 1_000_000m,
            QuoteAmount = 1_000_000m * price,
            Price = price,
            Status = "FILLED",
            ExecutedAtUtc = executedAtUtc
        };
    }

    private sealed class PostgresIntegrationFixture : IAsyncDisposable
    {
        private PostgresIntegrationFixture(NpgsqlDataSource dataSource)
        {
            DataSource = dataSource;
        }

        public NpgsqlDataSource DataSource { get; }

        public static async Task<PostgresIntegrationFixture?> TryCreateAsync()
        {
            if (!string.Equals(
                    Environment.GetEnvironmentVariable("EFX_POSTGRES_INTEGRATION"),
                    "1",
                    StringComparison.Ordinal))
            {
                return null;
            }

            var connectionString =
                Environment.GetEnvironmentVariable("EFX_POSTGRES_CONNECTION_STRING") ??
                "Host=localhost;Port=5432;Database=efx_simulator;Username=efx;Password=efx_dev_password";
            var dataSource = NpgsqlDataSource.Create(connectionString);

            await using var command = dataSource.CreateCommand("DELETE FROM trades;");

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException exception) when (exception.SqlState == "42P01")
            {
                // The store creates the table lazily on first use.
            }

            return new PostgresIntegrationFixture(dataSource);
        }

        public async ValueTask DisposeAsync()
        {
            await using var command = DataSource.CreateCommand("DELETE FROM trades;");

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException exception) when (exception.SqlState == "42P01")
            {
            }

            await DataSource.DisposeAsync();
        }
    }
}
