using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Infrastructure;

public interface ITradeStore
{
    Task RecordTradeAsync(Trade trade);

    Task<List<Trade>> GetLatestTradesAsync(
        string portfolioId = PortfolioIds.Default,
        int count = 50);
}
