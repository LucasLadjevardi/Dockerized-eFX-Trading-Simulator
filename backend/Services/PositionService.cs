using System.Text.Json;
using EfxSimulator.Api.Models;
using StackExchange.Redis;

namespace EfxSimulator.Api.Services;

public sealed class PositionService
{
    private readonly IConnectionMultiplexer _redis;

    private static readonly string[] SupportedPairs =
    {
        "EURUSD",
        "GBPUSD",
        "USDJPY",
        "EURGBP"
    };

    public PositionService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<Position> ApplyTradeAsync(Trade trade)
    {
        var db = _redis.GetDatabase();

        var positionKey = $"position:{trade.Pair}";
        var existingPositionJson = await db.StringGetAsync(positionKey);

        Position? existingPosition = null;

        if (existingPositionJson.HasValue)
        {
            existingPosition = JsonSerializer.Deserialize<Position>(existingPositionJson!);
        }

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

        var newPositionJson = JsonSerializer.Serialize(newPosition);

        await db.StringSetAsync(positionKey, newPositionJson);

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
        var db = _redis.GetDatabase();

        pair = pair.ToUpperInvariant();

        var positionJson = await db.StringGetAsync($"position:{pair}");

        if (!positionJson.HasValue)
        {
            return null;
        }

        var position = JsonSerializer.Deserialize<Position>(positionJson!);

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
        var db = _redis.GetDatabase();

        var priceJson = await db.StringGetAsync($"price:{pair}");

        if (!priceJson.HasValue)
        {
            return 0m;
        }

        var price = JsonSerializer.Deserialize<FxPrice>(priceJson!);

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