using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class TradeService
{
    private readonly ITradeStore _tradeStore;

    public TradeService(ITradeStore tradeStore)
    {
        _tradeStore = tradeStore;
    }

    public async Task RecordTradeAsync(Trade trade)
    {
        await _tradeStore.RecordTradeAsync(trade);
    }

    public async Task<List<Trade>> GetLatestTradesAsync(
        string? portfolioId = null,
        int count = 50)
    {
        return await _tradeStore.GetLatestTradesAsync(
            PortfolioIds.Normalize(portfolioId),
            count);
    }
}
