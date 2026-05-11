using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EfxSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PricesController : ControllerBase
{
    private readonly IRedisStore _redis;

    public PricesController(IRedisStore redis)
    {
        _redis = redis;
    }

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, FxPrice?>>> GetPrices()
    {
        var pairs = new[]
        {
            "EURUSD",
            "GBPUSD",
            "USDJPY",
            "EURGBP"
        };

        var prices = new Dictionary<string, FxPrice?>();

        foreach (var pair in pairs)
        {
            prices[pair] = await _redis.GetJsonAsync<FxPrice>(
                RedisKeys.Price(pair));
        }

        return Ok(prices);
    }
    
    [HttpGet("history/{pair}")]
    public async Task<ActionResult<List<FxPrice>>> GetPriceHistory(string pair)
    {
        pair = pair.ToUpperInvariant();

        var history = await _redis.ListRangeJsonAsync<FxPrice>(
            RedisKeys.PriceHistory(pair));

        return Ok(history);
    }
}

