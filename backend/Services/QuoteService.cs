using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class QuoteService
{
    private static readonly TimeSpan QuoteLifetime = TimeSpan.FromSeconds(5);

    private readonly IRedisStore _redis;

    public QuoteService(IRedisStore redis)
    {
        _redis = redis;
    }

    public async Task<Quote> CreateQuoteAsync(
        QuoteRequest request,
        string? portfolioId = null)
    {
        if (request is null)
        {
            throw new ArgumentException("Quote request is required");
        }

        if (string.IsNullOrWhiteSpace(request.Pair))
        {
            throw new ArgumentException("Pair is required");
        }

        if (string.IsNullOrWhiteSpace(request.Side))
        {
            throw new ArgumentException("Side is required");
        }

        var pair = request.Pair.ToUpperInvariant();
        var side = request.Side.ToUpperInvariant();
        var normalizedPortfolioId = PortfolioIds.Normalize(portfolioId);

        if (side is not "BUY" and not "SELL")
        {
            throw new ArgumentException("Side must be BUY or SELL");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero");
        }

        var price = await _redis.GetJsonAsync<FxPrice>(RedisKeys.Price(pair));

        if (price is null)
        {
            throw new InvalidOperationException($"No price available for {pair}");
        }

        var executablePrice = side == "BUY"
            ? price.Ask
            : price.Bid;

        var now = DateTime.UtcNow;

        var quote = new Quote
        {
            QuoteId = $"q-{Guid.NewGuid():N}",
            PortfolioId = normalizedPortfolioId,
            Pair = pair,
            Side = side,
            Amount = request.Amount,
            Price = executablePrice,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(QuoteLifetime)
        };

        await _redis.SetJsonAsync(
            RedisKeys.Quote(quote.QuoteId),
            quote,
            QuoteLifetime);

        return quote;
    }
}
