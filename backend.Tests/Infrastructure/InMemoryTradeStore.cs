using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Tests.Infrastructure;

public sealed class InMemoryTradeStore : ITradeStore
{
    private readonly object _gate = new();
    private readonly List<Trade> _trades = new();

    public Task RecordTradeAsync(Trade trade)
    {
        lock (_gate)
        {
            if (_trades.All(existingTrade => existingTrade.TradeId != trade.TradeId))
            {
                _trades.Insert(0, trade);
            }

            return Task.CompletedTask;
        }
    }

    public Task<List<Trade>> GetLatestTradesAsync(
        string portfolioId = PortfolioIds.Default,
        int count = 50)
    {
        lock (_gate)
        {
            var normalizedPortfolioId = PortfolioIds.Normalize(portfolioId);

            return Task.FromResult(
                _trades
                    .Where(trade => PortfolioIds.Normalize(trade.PortfolioId)
                        == normalizedPortfolioId)
                    .Take(count)
                    .ToList());
        }
    }
}
