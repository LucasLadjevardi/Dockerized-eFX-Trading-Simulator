using System.Text.Json;
using EfxSimulator.Api.Models;
using StackExchange.Redis;

namespace EfxSimulator.Api.Services;

public sealed class RiskService
{
    private readonly IConnectionMultiplexer _redis;

    private const decimal MaxSingleTradeSize = 2_000_000m;
    private const decimal MaxNetExposure = 5_000_000m;

    public RiskService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<RiskResult> CheckTradeAsync(Quote quote)
    {
        if (quote.Amount > MaxSingleTradeSize)
        {
            return RiskResult.Rejected(
                $"Trade amount exceeds max single trade size of {MaxSingleTradeSize:N0}");
        }

        var db = _redis.GetDatabase();

        var positionJson = await db.StringGetAsync($"position:{quote.Pair}");

        var currentNetAmount = 0m;

        if (positionJson.HasValue)
        {
            var position = JsonSerializer.Deserialize<Position>(positionJson!);
            currentNetAmount = position?.NetBaseAmount ?? 0m;
        }

        var signedTradeAmount = quote.Side == "BUY"
            ? quote.Amount
            : -quote.Amount;

        var resultingNetAmount = currentNetAmount + signedTradeAmount;

        if (Math.Abs(resultingNetAmount) > MaxNetExposure)
        {
            return RiskResult.Rejected(
                $"Resulting net exposure exceeds max exposure of {MaxNetExposure:N0}");
        }

        return RiskResult.Approved();
    }
}