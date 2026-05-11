using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class PositionService
{
    private readonly RedisStore _redis;

    private static readonly string[] SupportedPairs =
    {
        "EURUSD",
        "GBPUSD",
        "USDJPY",
        "EURGBP"
    };

    public PositionService(RedisStore redis)
    {
        _redis = redis;
    }

    public async Task<Position> ApplyTradeAsync(Trade trade)
    {
        var positionKey = RedisKeys.Position(trade.Pair);
        var existingPosition = await _redis.GetJsonAsync<Position>(positionKey);

        var signedBaseAmount = trade.Side == "BUY"
            ? trade.BaseAmount
            : -trade.BaseAmount;

        var oldNetBaseAmount = existingPosition?.NetBaseAmount ?? 0m;
        var oldAveragePrice = existingPosition?.AveragePrice ?? 0m;

        var newNetBaseAmount = oldNetBaseAmount + signedBaseAmount;

        var newAveragePrice = CalculateAveragePrice(
            oldNetBaseAmount,
            oldAveragePrice,
            signedBaseAmount,
            trade.Price);

        var currentPrice = await GetCurrentMidPriceAsync(trade.Pair);

        var unrealizedPnl = CalculateUnrealizedPnl(
            newNetBaseAmount,
            newAveragePrice,
            currentPrice);

        var newPosition = new Position
        {
            Pair = trade.Pair,
            BaseCurrency = trade.BaseCurrency,
            QuoteCurrency = trade.QuoteCurrency,
            NetBaseAmount = newNetBaseAmount,
            AveragePrice = newAveragePrice,
            CurrentPrice = currentPrice,
            UnrealizedPnl = unrealizedPnl,
            PnlCurrency = trade.QuoteCurrency,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _redis.SetJsonAsync(positionKey, newPosition);

        return newPosition;
    }

    public async Task<List<Position>> GetAllPositionsAsync()
    {
        var positions = new List<Position>();

        foreach (var pair in SupportedPairs)
        {
            var position = await GetPositionAsync(pair);

            if (position is not null)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    public async Task<Position?> GetPositionAsync(string pair)
    {
        pair = pair.ToUpperInvariant();

        var position = await _redis.GetJsonAsync<Position>(
            RedisKeys.Position(pair));

        if (position is null)
        {
            return null;
        }

        var currentPrice = await GetCurrentMidPriceAsync(pair);

        var unrealizedPnl = CalculateUnrealizedPnl(
            position.NetBaseAmount,
            position.AveragePrice,
            currentPrice);

        var refreshedPosition = new Position
        {
            Pair = position.Pair,
            BaseCurrency = position.BaseCurrency,
            QuoteCurrency = position.QuoteCurrency,
            NetBaseAmount = position.NetBaseAmount,
            AveragePrice = position.AveragePrice,
            CurrentPrice = currentPrice,
            UnrealizedPnl = unrealizedPnl,
            PnlCurrency = position.QuoteCurrency,
            UpdatedAtUtc = DateTime.UtcNow
        };

        return refreshedPosition;
    }

    private async Task<decimal> GetCurrentMidPriceAsync(string pair)
    {
        var price = await _redis.GetJsonAsync<FxPrice>(RedisKeys.Price(pair));

        return price?.Mid ?? 0m;
    }

    private static decimal CalculateAveragePrice(
        decimal oldNetBaseAmount,
        decimal oldAveragePrice,
        decimal signedBaseAmount,
        decimal tradePrice)
    {
        var newNetBaseAmount = oldNetBaseAmount + signedBaseAmount;

        if (newNetBaseAmount == 0)
        {
            return 0m;
        }

        if (oldNetBaseAmount == 0 || Math.Sign(oldNetBaseAmount) == Math.Sign(signedBaseAmount))
        {
            var oldNotional = Math.Abs(oldNetBaseAmount) * oldAveragePrice;
            var tradeNotional = Math.Abs(signedBaseAmount) * tradePrice;

            return (oldNotional + tradeNotional) / Math.Abs(newNetBaseAmount);
        }

        if (Math.Sign(oldNetBaseAmount) != Math.Sign(newNetBaseAmount))
        {
            return tradePrice;
        }

        return oldAveragePrice;
    }

    private static decimal CalculateUnrealizedPnl(
        decimal netBaseAmount,
        decimal averagePrice,
        decimal currentPrice)
    {
        if (netBaseAmount == 0 || averagePrice == 0 || currentPrice == 0)
        {
            return 0m;
        }

        return netBaseAmount * (currentPrice - averagePrice);
    }
}
