using System.Text.Json;
using EfxSimulator.Api.Models;
using StackExchange.Redis;

namespace EfxSimulator.Api.Services;

public sealed class TradeService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly PositionService _positionService;
    private readonly RiskService _riskService;

    public TradeService(
        IConnectionMultiplexer redis,
        PositionService positionService,
        RiskService riskService)
    {
        _redis = redis;
        _positionService = positionService;
        _riskService = riskService;
    }

    public async Task<Trade> ExecuteTradeAsync(TradeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Trade request is required");
        }

        if (string.IsNullOrWhiteSpace(request.QuoteId))
        {
            throw new ArgumentException("QuoteId is required");
        }

        var db = _redis.GetDatabase();

        var quoteKey = $"quote:{request.QuoteId}";
        var quoteJson = await db.StringGetAsync(quoteKey);

        if (!quoteJson.HasValue)
        {
            throw new InvalidOperationException("Quote expired or does not exist");
        }

        var quote = JsonSerializer.Deserialize<Quote>(quoteJson!);

        if (quote is null)
        {
            throw new InvalidOperationException("Failed to deserialize quote");
        }

        var riskResult = await _riskService.CheckTradeAsync(quote);

        if (!riskResult.IsApproved)
        {
            throw new InvalidOperationException(riskResult.Reason);
        }

        var baseCurrency = CurrencyPairHelper.GetBaseCurrency(quote.Pair);
        var quoteCurrency = CurrencyPairHelper.GetQuoteCurrency(quote.Pair);

        var baseAmount = quote.Amount;
        var quoteAmount = quote.Amount * quote.Price;

        var trade = new Trade
        {
            TradeId = $"t-{Guid.NewGuid():N}",
            QuoteId = quote.QuoteId,
            Pair = quote.Pair,
            Side = quote.Side,
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency,
            BaseAmount = baseAmount,
            QuoteAmount = quoteAmount,
            Price = quote.Price,
            Status = "FILLED",
            ExecutedAtUtc = DateTime.UtcNow
        };

        var tradeJson = JsonSerializer.Serialize(trade);

        await db.StringSetAsync($"trade:{trade.TradeId}", tradeJson);
        await db.ListLeftPushAsync("trades", trade.TradeId);

        // Prevent same quote being executed twice.
        await db.KeyDeleteAsync(quoteKey);

        await _positionService.ApplyTradeAsync(trade);

        return trade;
    }

    public async Task<List<Trade>> GetLatestTradesAsync(int count = 50)
    {
        var db = _redis.GetDatabase();

        var tradeIds = await db.ListRangeAsync("trades", 0, count - 1);

        var trades = new List<Trade>();

        foreach (var tradeId in tradeIds)
        {
            var tradeJson = await db.StringGetAsync($"trade:{tradeId}");

            if (!tradeJson.HasValue)
            {
                continue;
            }

            var trade = JsonSerializer.Deserialize<Trade>(tradeJson!);

            if (trade is not null)
            {
                trades.Add(trade);
            }
        }

        return trades;
    }
}