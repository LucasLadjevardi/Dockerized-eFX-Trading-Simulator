using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using EfxSimulator.Api.Tests.Infrastructure;

namespace EfxSimulator.Api.Tests.Services;

public sealed class QuoteServiceTests
{
    [Fact]
    public async Task CreateQuoteAsync_UsesAskForBuyAndStoresExecutableQuote()
    {
        var redis = new InMemoryRedisStore();
        await redis.SetJsonAsync(RedisKeys.Price("EURUSD"), new FxPrice
        {
            Pair = "EURUSD",
            Bid = 1.1000m,
            Ask = 1.1002m,
            TimestampUtc = DateTime.UtcNow
        });

        var service = new QuoteService(redis);

        var quote = await service.CreateQuoteAsync(new QuoteRequest
        {
            Pair = "eurusd",
            Side = "buy",
            Amount = 1_000_000m
        });

        var storedQuote = await redis.GetJsonAsync<Quote>(
            RedisKeys.Quote(quote.QuoteId));

        Assert.Equal("EURUSD", quote.Pair);
        Assert.Equal("BUY", quote.Side);
        Assert.Equal(1.1002m, quote.Price);
        Assert.NotNull(storedQuote);
        Assert.Equal(quote.QuoteId, storedQuote.QuoteId);
        Assert.InRange(
            (quote.ExpiresAtUtc - quote.CreatedAtUtc).TotalSeconds,
            4.5,
            5.5);
    }

    [Fact]
    public async Task CreateQuoteAsync_UsesBidForSell()
    {
        var redis = new InMemoryRedisStore();
        await redis.SetJsonAsync(RedisKeys.Price("EURUSD"), new FxPrice
        {
            Pair = "EURUSD",
            Bid = 1.1000m,
            Ask = 1.1002m,
            TimestampUtc = DateTime.UtcNow
        });

        var service = new QuoteService(redis);

        var quote = await service.CreateQuoteAsync(new QuoteRequest
        {
            Pair = "EURUSD",
            Side = "SELL",
            Amount = 1_000_000m
        });

        Assert.Equal(1.1000m, quote.Price);
        Assert.Equal("SELL", quote.Side);
    }

    [Fact]
    public async Task CreateQuoteAsync_RejectsMissingPrice()
    {
        var service = new QuoteService(new InMemoryRedisStore());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateQuoteAsync(new QuoteRequest
            {
                Pair = "EURUSD",
                Side = "BUY",
                Amount = 1_000_000m
            }));

        Assert.Equal("No price available for EURUSD", exception.Message);
    }

    [Fact]
    public async Task CreateQuoteAsync_RejectsNonPositiveAmount()
    {
        var service = new QuoteService(new InMemoryRedisStore());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateQuoteAsync(new QuoteRequest
            {
                Pair = "EURUSD",
                Side = "BUY",
                Amount = 0m
            }));

        Assert.Equal("Amount must be greater than zero", exception.Message);
    }

    [Fact]
    public async Task CreateQuoteAsync_RejectsInvalidSide()
    {
        var service = new QuoteService(new InMemoryRedisStore());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateQuoteAsync(new QuoteRequest
            {
                Pair = "EURUSD",
                Side = "HOLD",
                Amount = 1_000_000m
            }));

        Assert.Equal("Side must be BUY or SELL", exception.Message);
    }
}
