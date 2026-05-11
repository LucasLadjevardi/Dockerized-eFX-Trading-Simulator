using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class PositionService
{
    private readonly IRedisStore _redis;

    private static readonly string[] SupportedPairs =
    {
        "EURUSD",
        "GBPUSD",
        "USDJPY",
        "EURGBP"
    };

    public PositionService(IRedisStore redis)
    {
        _redis = redis;
    }

    public async Task<PositionUpdateResult> ApplyTradeAsync(Trade trade)
    {
        var portfolioId = PortfolioIds.Normalize(trade.PortfolioId);
        var positionKey = RedisKeys.Position(portfolioId, trade.Pair);
        var existingPosition = await _redis.GetJsonAsync<Position>(positionKey);

        var signedBaseAmount = trade.Side == "BUY"
            ? trade.BaseAmount
            : -trade.BaseAmount;

        var oldNetBaseAmount = existingPosition?.NetBaseAmount ?? 0m;
        var oldAveragePrice = existingPosition?.AveragePrice ?? 0m;
        var oldRealizedPnl = existingPosition?.RealizedPnl ?? 0m;

        var newNetBaseAmount = oldNetBaseAmount + signedBaseAmount;
        var realizedPnl = CalculateRealizedPnl(
            oldNetBaseAmount,
            oldAveragePrice,
            signedBaseAmount,
            trade.Price);

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
            PortfolioId = portfolioId,
            Pair = trade.Pair,
            BaseCurrency = trade.BaseCurrency,
            QuoteCurrency = trade.QuoteCurrency,
            NetBaseAmount = newNetBaseAmount,
            AveragePrice = newAveragePrice,
            CurrentPrice = currentPrice,
            UnrealizedPnl = unrealizedPnl,
            RealizedPnl = oldRealizedPnl + realizedPnl,
            PnlCurrency = trade.QuoteCurrency,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _redis.SetJsonAsync(positionKey, newPosition);

        return new PositionUpdateResult
        {
            Position = newPosition,
            RealizedPnl = realizedPnl,
            PnlCurrency = trade.QuoteCurrency
        };
    }

    public async Task<List<Position>> GetAllPositionsAsync(
        string? portfolioId = null)
    {
        var normalizedPortfolioId = PortfolioIds.Normalize(portfolioId);
        var positions = new List<Position>();

        foreach (var pair in SupportedPairs)
        {
            var position = await GetPositionAsync(pair, normalizedPortfolioId);

            if (position is not null)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    public async Task<Position?> GetPositionAsync(string pair)
    {
        return await GetPositionAsync(pair, PortfolioIds.Default);
    }

    public async Task<Position?> GetPositionAsync(
        string pair,
        string? portfolioId)
    {
        pair = pair.ToUpperInvariant();
        var normalizedPortfolioId = PortfolioIds.Normalize(portfolioId);

        var position = await _redis.GetJsonAsync<Position>(
            RedisKeys.Position(normalizedPortfolioId, pair));

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
            PortfolioId = normalizedPortfolioId,
            Pair = position.Pair,
            BaseCurrency = position.BaseCurrency,
            QuoteCurrency = position.QuoteCurrency,
            NetBaseAmount = position.NetBaseAmount,
            AveragePrice = position.AveragePrice,
            CurrentPrice = currentPrice,
            UnrealizedPnl = unrealizedPnl,
            RealizedPnl = position.RealizedPnl,
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

    private static decimal CalculateRealizedPnl(
        decimal oldNetBaseAmount,
        decimal oldAveragePrice,
        decimal signedBaseAmount,
        decimal tradePrice)
    {
        if (oldNetBaseAmount == 0m ||
            oldAveragePrice == 0m ||
            Math.Sign(oldNetBaseAmount) == Math.Sign(signedBaseAmount))
        {
            return 0m;
        }

        var closedBaseAmount = Math.Min(
            Math.Abs(oldNetBaseAmount),
            Math.Abs(signedBaseAmount));

        return closedBaseAmount *
            (tradePrice - oldAveragePrice) *
            Math.Sign(oldNetBaseAmount);
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
