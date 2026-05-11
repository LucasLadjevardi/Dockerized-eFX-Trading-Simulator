using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using EfxSimulator.Api.Options;
using EfxSimulator.Api.Services;
using EfxSimulator.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace EfxSimulator.Api.Tests.Services;

public sealed class ExecutionServiceTests
{
    [Fact]
    public async Task ExecuteTradeAsync_ConsumesQuoteAndRejectsSecondExecution()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var quote = new Quote
        {
            QuoteId = "q-1",
            Pair = "EURUSD",
            Side = "BUY",
            Amount = 1_000_000m,
            Price = 1.1002m,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(5)
        };

        await redis.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);

        var service = CreateService(redis);

        var trade = await service.ExecuteTradeAsync(new TradeRequest
        {
            QuoteId = quote.QuoteId
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteTradeAsync(new TradeRequest
            {
                QuoteId = quote.QuoteId
            }));

        Assert.Equal("FILLED", trade.Status);
        Assert.Equal(quote.QuoteId, trade.QuoteId);
        Assert.Null(await redis.GetJsonAsync<Quote>(RedisKeys.Quote(quote.QuoteId)));
    }

    [Fact]
    public async Task ExecuteTradeAsync_RejectsExpiredQuoteWithoutRecordingTrade()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var quote = new Quote
        {
            QuoteId = "q-expired",
            Pair = "EURUSD",
            Side = "BUY",
            Amount = 1_000_000m,
            Price = 1.1002m,
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1)
        };

        await redis.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);
        var tradeStore = new InMemoryTradeStore();
        var service = CreateService(redis, tradeStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteTradeAsync(new TradeRequest
            {
                QuoteId = quote.QuoteId
            }));

        Assert.Empty(await new TradeService(tradeStore).GetLatestTradesAsync());
    }

    [Fact]
    public async Task ExecuteTradeAsync_RejectedRiskDoesNotRecordTradeOrPosition()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var quote = CreateQuote("q-risk", "EURUSD", "BUY", 1_000_000m);
        await redis.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);

        var tradeStore = new InMemoryTradeStore();
        var service = CreateService(redis, tradeStore, new RiskLimitsOptions
        {
            MaxSingleTradeSize = 500_000m
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteTradeAsync(new TradeRequest
            {
                QuoteId = quote.QuoteId
            }));

        Assert.Empty(await new TradeService(tradeStore).GetLatestTradesAsync());
        Assert.Null(await new PositionService(redis).GetPositionAsync("EURUSD"));
    }

    [Fact]
    public async Task ExecuteTradeAsync_RejectsQuoteFromDifferentPortfolio()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var quote = CreateQuote("q-alpha", "EURUSD", "BUY", 1_000_000m, "alpha");
        await redis.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);

        var tradeStore = new InMemoryTradeStore();
        var service = CreateService(redis, tradeStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteTradeAsync(
                new TradeRequest
                {
                    QuoteId = quote.QuoteId
                },
                "beta"));

        Assert.Empty(await new TradeService(tradeStore).GetLatestTradesAsync("alpha"));
        Assert.Null(await new PositionService(redis).GetPositionAsync("EURUSD", "alpha"));
    }

    [Fact]
    public async Task ExecuteTradeAsync_ConcurrentExecutionsForSameQuoteFillOnlyOnce()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var quote = CreateQuote("q-concurrent", "EURUSD", "BUY", 1_000_000m);
        await redis.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);

        var tradeStore = new InMemoryTradeStore();
        var service = CreateService(redis, tradeStore);
        var requests = Enumerable
            .Range(0, 10)
            .Select(_ => ExecuteIgnoringRejectionAsync(service, quote.QuoteId))
            .ToArray();

        var results = await Task.WhenAll(requests);

        Assert.Single(results, trade => trade is not null);
        Assert.Single(await new TradeService(tradeStore).GetLatestTradesAsync());
    }

    [Fact]
    public async Task ExecuteTradeAsync_ConcurrentSamePairQuotesAccumulatePosition()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        for (var index = 0; index < 5; index++)
        {
            var quote = CreateQuote($"q-{index}", "EURUSD", "BUY", 100_000m);
            await redis.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);
        }

        var service = CreateService(redis);
        await Task.WhenAll(
            Enumerable
                .Range(0, 5)
                .Select(index => service.ExecuteTradeAsync(new TradeRequest
                {
                    QuoteId = $"q-{index}"
                })));

        var position = await new PositionService(redis).GetPositionAsync("EURUSD");

        Assert.NotNull(position);
        Assert.Equal(500_000m, position.NetBaseAmount);
    }

    [Fact]
    public async Task ExecuteTradeAsync_RecordsRealizedPnlWhenReducingPosition()
    {
        var redis = new InMemoryRedisStore();
        await SeedFreshPricesAsync(redis);

        var buyQuote = CreateQuote("q-buy", "EURUSD", "BUY", 1_000_000m);
        var sellQuote = CreateQuote("q-sell", "EURUSD", "SELL", 250_000m);
        sellQuote = new Quote
        {
            QuoteId = sellQuote.QuoteId,
            PortfolioId = sellQuote.PortfolioId,
            Pair = sellQuote.Pair,
            Side = sellQuote.Side,
            Amount = sellQuote.Amount,
            Price = 1.1100m,
            CreatedAtUtc = sellQuote.CreatedAtUtc,
            ExpiresAtUtc = sellQuote.ExpiresAtUtc
        };

        await redis.SetJsonAsync(RedisKeys.Quote(buyQuote.QuoteId), buyQuote);
        await redis.SetJsonAsync(RedisKeys.Quote(sellQuote.QuoteId), sellQuote);

        var tradeStore = new InMemoryTradeStore();
        var service = CreateService(redis, tradeStore);

        await service.ExecuteTradeAsync(new TradeRequest
        {
            QuoteId = buyQuote.QuoteId
        });
        var trade = await service.ExecuteTradeAsync(new TradeRequest
        {
            QuoteId = sellQuote.QuoteId
        });

        var position = await new PositionService(redis).GetPositionAsync("EURUSD");

        Assert.Equal(2_450m, trade.RealizedPnl);
        Assert.NotNull(position);
        Assert.Equal(2_450m, position.RealizedPnl);
    }

    private static async Task<Trade?> ExecuteIgnoringRejectionAsync(
        ExecutionService service,
        string quoteId)
    {
        try
        {
            return await service.ExecuteTradeAsync(new TradeRequest
            {
                QuoteId = quoteId
            });
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static ExecutionService CreateService(
        InMemoryRedisStore redis,
        InMemoryTradeStore? tradeStore = null,
        RiskLimitsOptions? limits = null)
    {
        tradeStore ??= new InMemoryTradeStore();

        var tradeService = new TradeService(tradeStore);
        var positionService = new PositionService(redis);
        var riskService = new RiskService(
            redis,
            Microsoft.Extensions.Options.Options.Create(
                limits ?? new RiskLimitsOptions()));

        return new ExecutionService(
            redis,
            tradeService,
            positionService,
            riskService);
    }

    private static Quote CreateQuote(
        string quoteId,
        string pair,
        string side,
        decimal amount,
        string portfolioId = PortfolioIds.Default)
    {
        return new Quote
        {
            QuoteId = quoteId,
            PortfolioId = portfolioId,
            Pair = pair,
            Side = side,
            Amount = amount,
            Price = pair == "USDJPY" ? 155.50m : 1.1002m,
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
