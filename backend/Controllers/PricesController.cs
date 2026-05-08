using System.Text.Json;
using EfxSimulator.Api.Models;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace EfxSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PricesController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;

    public PricesController(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, FxPrice?>>> GetPrices()
    {
        var db = _redis.GetDatabase();

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
            var json = await db.StringGetAsync($"price:{pair}");

            prices[pair] = json.HasValue
                ? JsonSerializer.Deserialize<FxPrice>(json!)
                : null;
        }

        return Ok(prices);
    }
    
    [HttpGet("history/{pair}")]
    public async Task<ActionResult<List<FxPrice>>> GetPriceHistory(string pair)
    {
        var db = _redis.GetDatabase();

        pair = pair.ToUpperInvariant();

        var values = await db.ListRangeAsync($"pricehistory:{pair}", 0, -1);

        var history = new List<FxPrice>();

        foreach (var value in values)
        {
            var item = JsonSerializer.Deserialize<FxPrice>(value!);

            if (item is not null)
            {
                history.Add(item);
            }
        }

        return Ok(history);
    }
}

