using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class TradeService
{
    private readonly RedisStore _redis;

    public TradeService(RedisStore redis)
    {
        _redis = redis;
    }

    public async Task RecordTradeAsync(Trade trade)
    {
        await _redis.SetJsonAsync(RedisKeys.Trade(trade.TradeId), trade);
        await _redis.ListLeftPushAsync(RedisKeys.Trades, trade.TradeId);
    }

    public async Task<List<Trade>> GetLatestTradesAsync(int count = 50)
    {
        var tradeIds = await _redis.ListRangeAsync(RedisKeys.Trades, 0, count - 1);
        var trades = new List<Trade>();

        foreach (var tradeId in tradeIds)
        {
            var trade = await _redis.GetJsonAsync<Trade>(RedisKeys.Trade(tradeId));

            if (trade is not null)
            {
                trades.Add(trade);
            }
        }

        return trades;
    }
}
