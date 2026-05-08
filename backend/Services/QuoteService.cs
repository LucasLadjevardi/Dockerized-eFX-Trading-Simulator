using System.Text.Json;
using EfxSimulator.Api.Models;
using StackExchange.Redis;

namespace EfxSimulator.Api.Services;

public sealed class QuoteService
{
    private readonly IConnectionMultiplexer _redis;

    public QuoteService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<Quote> CreateQuoteAsync(QuoteRequest request)
    {
        var db = _redis.GetDatabase();

        var pair = request.Pair.ToUpperInvariant();
        var side = request.Side.ToUpperInvariant();

        if (side is not "BUY" and not "SELL")
        {
            throw new ArgumentException("Side must be BUY or SELL");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero");
        }

        var priceJson = await db.StringGetAsync($"price:{pair}");

        if (!priceJson.HasValue)
        {
            throw new InvalidOperationException($"No price available for {pair}");
        }

        var price = JsonSerializer.Deserialize<FxPrice>(priceJson!);

        if (price is null)
        {
            throw new InvalidOperationException("Failed to deserialize price");
        }

        var executablePrice = side == "BUY"
            ? price.Ask
            : price.Bid;

        var quote = new Quote
        {
            QuoteId = $"q-{Guid.NewGuid():N}",
            Pair = pair,
            Side = side,
            Amount = request.Amount,
            Price = executablePrice,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(5)
        };

        var quoteJson = JsonSerializer.Serialize(quote);

        await db.StringSetAsync(
            $"quote:{quote.QuoteId}",
            quoteJson,
            expiry: TimeSpan.FromSeconds(20));

        return quote;
    }
}