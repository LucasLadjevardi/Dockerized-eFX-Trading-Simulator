using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using EfxSimulator.Api.Tests.Infrastructure;

namespace EfxSimulator.Api.Tests.Services;

public sealed class PositionServiceTests
{
    [Fact]
    public async Task ApplyTradeAsync_CreatesLongPositionAndCalculatesPnl()
    {
        var redis = new InMemoryRedisStore();
        await SeedPriceAsync(redis, "EURUSD", bid: 1.1098m, ask: 1.1102m);

        var service = new PositionService(redis);

        var result = await service.ApplyTradeAsync(new Trade
        {
            TradeId = "t-1",
            QuoteId = "q-1",
            Pair = "EURUSD",
            Side = "BUY",
            BaseCurrency = "EUR",
            QuoteCurrency = "USD",
            BaseAmount = 1_000_000m,
            QuoteAmount = 1_100_000m,
            Price = 1.1000m,
            Status = "FILLED",
            ExecutedAtUtc = DateTime.UtcNow
        });
        var position = result.Position;

        Assert.Equal(1_000_000m, position.NetBaseAmount);
        Assert.Equal(1.1000m, position.AveragePrice);
        Assert.Equal(1.1100m, position.CurrentPrice);
        Assert.Equal(10_000m, position.UnrealizedPnl);
        Assert.Equal(0m, position.RealizedPnl);
    }

    [Fact]
    public async Task ApplyTradeAsync_ReducesPositionWithoutChangingAveragePrice()
    {
        var redis = new InMemoryRedisStore();
        await SeedPriceAsync(redis, "EURUSD", bid: 1.1098m, ask: 1.1102m);

        var service = new PositionService(redis);

        await service.ApplyTradeAsync(CreateTrade("BUY", 1_000_000m, 1.1000m));
        var result = await service.ApplyTradeAsync(CreateTrade("SELL", 250_000m, 1.1100m));
        var position = result.Position;

        Assert.Equal(750_000m, position.NetBaseAmount);
        Assert.Equal(1.1000m, position.AveragePrice);
        Assert.Equal(2_500m, result.RealizedPnl);
        Assert.Equal(2_500m, position.RealizedPnl);
    }

    [Fact]
    public async Task ApplyTradeAsync_FlipsLongPositionToShortAtTradePrice()
    {
        var redis = new InMemoryRedisStore();
        await SeedPriceAsync(redis, "EURUSD", bid: 1.0898m, ask: 1.0902m);

        var service = new PositionService(redis);

        await service.ApplyTradeAsync(CreateTrade("BUY", 1_000_000m, 1.1000m));
        var result = await service.ApplyTradeAsync(CreateTrade("SELL", 1_500_000m, 1.0900m));
        var position = result.Position;

        Assert.Equal(-500_000m, position.NetBaseAmount);
        Assert.Equal(1.0900m, position.AveragePrice);
        Assert.Equal(-10_000m, result.RealizedPnl);
        Assert.Equal(-10_000m, position.RealizedPnl);
    }

    [Fact]
    public async Task ApplyTradeAsync_ClosingPositionResetsAveragePriceAndPnl()
    {
        var redis = new InMemoryRedisStore();
        await SeedPriceAsync(redis, "EURUSD", bid: 1.1098m, ask: 1.1102m);

        var service = new PositionService(redis);

        await service.ApplyTradeAsync(CreateTrade("BUY", 1_000_000m, 1.1000m));
        var result = await service.ApplyTradeAsync(CreateTrade("SELL", 1_000_000m, 1.1100m));
        var position = result.Position;

        Assert.Equal(0m, position.NetBaseAmount);
        Assert.Equal(0m, position.AveragePrice);
        Assert.Equal(0m, position.UnrealizedPnl);
        Assert.Equal(10_000m, position.RealizedPnl);
    }

    [Fact]
    public async Task ApplyTradeAsync_KeepsPortfoliosIsolated()
    {
        var redis = new InMemoryRedisStore();
        await SeedPriceAsync(redis, "EURUSD", bid: 1.1098m, ask: 1.1102m);

        var service = new PositionService(redis);

        await service.ApplyTradeAsync(CreateTrade(
            side: "BUY",
            amount: 1_000_000m,
            price: 1.1000m,
            portfolioId: "alpha"));

        await service.ApplyTradeAsync(CreateTrade(
            side: "SELL",
            amount: 400_000m,
            price: 1.1000m,
            portfolioId: "beta"));

        var alphaPosition = await service.GetPositionAsync("EURUSD", "alpha");
        var betaPosition = await service.GetPositionAsync("EURUSD", "beta");

        Assert.NotNull(alphaPosition);
        Assert.NotNull(betaPosition);
        Assert.Equal(1_000_000m, alphaPosition.NetBaseAmount);
        Assert.Equal(-400_000m, betaPosition.NetBaseAmount);
    }

    private static Trade CreateTrade(
        string side,
        decimal amount,
        decimal price,
        string portfolioId = PortfolioIds.Default)
    {
        return new Trade
        {
            TradeId = $"t-{Guid.NewGuid():N}",
            PortfolioId = portfolioId,
            QuoteId = $"q-{Guid.NewGuid():N}",
            Pair = "EURUSD",
            Side = side,
            BaseCurrency = "EUR",
            QuoteCurrency = "USD",
            BaseAmount = amount,
            QuoteAmount = amount * price,
            Price = price,
            Status = "FILLED",
            ExecutedAtUtc = DateTime.UtcNow
        };
    }

    private static Task SeedPriceAsync(
        InMemoryRedisStore redis,
        string pair,
        decimal bid,
        decimal ask)
    {
        return redis.SetJsonAsync(RedisKeys.Price(pair), new FxPrice
        {
            Pair = pair,
            Bid = bid,
            Ask = ask,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
