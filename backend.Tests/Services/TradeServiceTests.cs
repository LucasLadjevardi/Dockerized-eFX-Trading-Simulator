using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using EfxSimulator.Api.Tests.Infrastructure;

namespace EfxSimulator.Api.Tests.Services;

public sealed class TradeServiceTests
{
    [Fact]
    public async Task GetLatestTradesAsync_ReturnsNewestRecordedTradesFirst()
    {
        var tradeStore = new InMemoryTradeStore();
        var service = new TradeService(tradeStore);

        var firstTrade = CreateTrade("t-1", "q-1", "EURUSD");
        var secondTrade = CreateTrade("t-2", "q-2", "GBPUSD");

        await service.RecordTradeAsync(firstTrade);
        await service.RecordTradeAsync(secondTrade);

        var trades = await service.GetLatestTradesAsync();

        Assert.Collection(
            trades,
            trade => Assert.Equal("t-2", trade.TradeId),
            trade => Assert.Equal("t-1", trade.TradeId));
    }

    [Fact]
    public async Task GetLatestTradesAsync_RespectsRequestedCount()
    {
        var tradeStore = new InMemoryTradeStore();
        var service = new TradeService(tradeStore);

        await service.RecordTradeAsync(CreateTrade("t-1", "q-1", "EURUSD"));
        await service.RecordTradeAsync(CreateTrade("t-2", "q-2", "EURUSD"));
        await service.RecordTradeAsync(CreateTrade("t-3", "q-3", "EURUSD"));

        var trades = await service.GetLatestTradesAsync(count: 2);

        Assert.Collection(
            trades,
            trade => Assert.Equal("t-3", trade.TradeId),
            trade => Assert.Equal("t-2", trade.TradeId));
    }

    [Fact]
    public async Task GetLatestTradesAsync_ReturnsOnlyRequestedPortfolio()
    {
        var tradeStore = new InMemoryTradeStore();
        var service = new TradeService(tradeStore);

        await service.RecordTradeAsync(CreateTrade(
            "t-alpha",
            "q-alpha",
            "EURUSD",
            portfolioId: "alpha"));
        await service.RecordTradeAsync(CreateTrade(
            "t-beta",
            "q-beta",
            "EURUSD",
            portfolioId: "beta"));

        var trades = await service.GetLatestTradesAsync("alpha");

        var trade = Assert.Single(trades);
        Assert.Equal("t-alpha", trade.TradeId);
        Assert.Equal("alpha", trade.PortfolioId);
    }

    private static Trade CreateTrade(
        string tradeId,
        string quoteId,
        string pair,
        string portfolioId = PortfolioIds.Default)
    {
        return new Trade
        {
            TradeId = tradeId,
            PortfolioId = portfolioId,
            QuoteId = quoteId,
            Pair = pair,
            Side = "BUY",
            BaseCurrency = pair[..3],
            QuoteCurrency = pair[3..],
            BaseAmount = 1_000_000m,
            QuoteAmount = 1_100_000m,
            Price = 1.1m,
            Status = "FILLED",
            ExecutedAtUtc = DateTime.UtcNow
        };
    }
}
