using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using EfxSimulator.Api.Options;
using EfxSimulator.Api.Services;
using EfxSimulator.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace EfxSimulator.Api.Tests.Services;

public sealed class RiskServiceTests
{
    [Fact]
    public async Task CheckTradeAsync_ApprovesTradeInsideConfiguredLimits()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var service = CreateService(redis);

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1_000_000m));

        Assert.True(result.IsApproved);
    }

    [Fact]
    public async Task CheckTradeAsync_RejectsTradeAboveConfiguredSingleTradeLimit()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 500_000m
        });

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1_000_000m));

        Assert.False(result.IsApproved);
        Assert.Contains("max single trade size", result.Reason);
    }

    [Fact]
    public async Task CheckTradeAsync_RejectsStaleMarketPrice()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);
        await redis.SetJsonAsync(RedisKeys.Price("EURUSD"), new FxPrice
        {
            Pair = "EURUSD",
            Bid = 1.0998m,
            Ask = 1.1002m,
            TimestampUtc = DateTime.UtcNow.AddSeconds(-30)
        });

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxMarketPriceAgeSeconds = 5
        });

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1_000_000m));

        Assert.False(result.IsApproved);
        Assert.Contains("stale", result.Reason);
    }

    [Fact]
    public async Task CheckTradeAsync_RejectsProjectedPairExposureBreach()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 2_000_000m,
            MaxPairNetExposure = 500_000m
        });

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1_000_000m));

        Assert.False(result.IsApproved);
        Assert.Contains("EURUSD", result.Reason);
    }

    [Fact]
    public async Task CheckTradeAsync_RejectsCurrencyExposureBreach()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 2_000_000m,
            MaxPairNetExposure = 5_000_000m,
            MaxCurrencyExposureUsd = 500_000m
        });

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1_000_000m));

        Assert.False(result.IsApproved);
        Assert.Contains("exposure", result.Reason);
    }

    [Fact]
    public async Task CheckTradeAsync_RejectsGrossNotionalBreach()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 2_000_000m,
            MaxPairNetExposure = 5_000_000m,
            MaxCurrencyExposureUsd = 10_000_000m,
            MaxGrossNotionalUsd = 1_500_000m
        });

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1_000_000m));

        Assert.False(result.IsApproved);
        Assert.Contains("gross notional", result.Reason);
    }

    [Fact]
    public async Task CheckTradeAsync_RejectsProjectedLossBreach()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);
        await redis.SetJsonAsync(RedisKeys.Position("EURUSD"), new Position
        {
            Pair = "EURUSD",
            BaseCurrency = "EUR",
            QuoteCurrency = "USD",
            NetBaseAmount = 1_000_000m,
            AveragePrice = 1.2000m,
            CurrentPrice = 1.1000m,
            UnrealizedPnl = -100_000m,
            PnlCurrency = "USD",
            UpdatedAtUtc = DateTime.UtcNow
        });

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 2_000_000m,
            MaxPairNetExposure = 5_000_000m,
            MaxCurrencyExposureUsd = 10_000_000m,
            MaxGrossNotionalUsd = 20_000_000m,
            MaxUnrealizedLossUsd = 25_000m
        });

        var result = await service.CheckTradeAsync(CreateQuote("EURUSD", "BUY", 1m));

        Assert.False(result.IsApproved);
        Assert.Contains("unrealized loss", result.Reason);
    }

    [Fact]
    public async Task CheckTradeAsync_UsesOnlyQuotePortfolioPositions()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);
        await redis.SetJsonAsync(RedisKeys.Position("other", "EURUSD"), new Position
        {
            PortfolioId = "other",
            Pair = "EURUSD",
            BaseCurrency = "EUR",
            QuoteCurrency = "USD",
            NetBaseAmount = 1_000_000m,
            AveragePrice = 1.1000m,
            CurrentPrice = 1.1000m,
            UnrealizedPnl = 0m,
            PnlCurrency = "USD",
            UpdatedAtUtc = DateTime.UtcNow
        });

        var service = CreateService(redis, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 2_000_000m,
            MaxPairNetExposure = 1_500_000m
        });

        var result = await service.CheckTradeAsync(
            CreateQuote("EURUSD", "BUY", 1_000_000m, "alpha"));

        Assert.True(result.IsApproved);
    }


    private static RiskService CreateService(
        InMemoryRedisStore redis,
        RiskLimitsOptions? limits = null)
    {
        return new RiskService(
            redis,
            Microsoft.Extensions.Options.Options.Create(
                limits ?? new RiskLimitsOptions()));
    }

    private static Quote CreateQuote(
        string pair,
        string side,
        decimal amount,
        string portfolioId = PortfolioIds.Default)
    {
        return new Quote
        {
            QuoteId = $"q-{Guid.NewGuid():N}",
            PortfolioId = portfolioId,
            Pair = pair,
            Side = side,
            Amount = amount,
            Price = pair == "USDJPY" ? 155.50m : 1.1000m,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(5)
        };
    }

    private static async Task SeedFreshPricesAsync(InMemoryRedisStore redis)
    {
        await SeedPriceAsync(redis, "EURUSD", 1.0998m, 1.1002m);
        await SeedPriceAsync(redis, "GBPUSD", 1.2598m, 1.2602m);
        await SeedPriceAsync(redis, "USDJPY", 155.49m, 155.51m);
        await SeedPriceAsync(redis, "EURGBP", 0.8578m, 0.8582m);
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
