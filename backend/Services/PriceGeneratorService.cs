using System.Text.Json;
using EfxSimulator.Api.Models;
using StackExchange.Redis;

namespace EfxSimulator.Api.Services;

public sealed class PriceGeneratorService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<PriceGeneratorService> _logger;
    private readonly Random _random = new();

    private readonly Dictionary<string, decimal> _midPrices = new()
    {
        ["EURUSD"] = 1.0850m,
        ["GBPUSD"] = 1.2650m,
        ["USDJPY"] = 155.50m,
        ["EURGBP"] = 0.8570m
    };

    public PriceGeneratorService(
        IConnectionMultiplexer redis,
        ILogger<PriceGeneratorService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        _logger.LogInformation("Price generator service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var pair in _midPrices.Keys.ToList())
            {
                var price = GenerateNextPrice(pair);

                var redisKey = $"price:{pair}";
                var json = JsonSerializer.Serialize(price);

                await db.StringSetAsync(redisKey, json);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("Price generator service stopped.");
    }

    private FxPrice GenerateNextPrice(string pair)
    {
        var currentMid = _midPrices[pair];

        var randomMove = GetRandomMove(pair);

        var newMid = currentMid + randomMove;

        if (newMid <= 0)
        {
            newMid = currentMid;
        }

        _midPrices[pair] = newMid;

        var spread = GetSpread(pair);

        return new FxPrice
        {
            Pair = pair,
            Bid = decimal.Round(newMid - spread / 2, GetDecimalPlaces(pair)),
            Ask = decimal.Round(newMid + spread / 2, GetDecimalPlaces(pair)),
            TimestampUtc = DateTime.UtcNow
        };
    }

    private decimal GetRandomMove(string pair)
    {
        var direction = _random.Next(0, 2) == 0 ? -1 : 1;

        var magnitude = pair.EndsWith("JPY")
            ? (decimal)_random.NextDouble() * 0.03m
            : (decimal)_random.NextDouble() * 0.0003m;

        return direction * magnitude;
    }

    private static decimal GetSpread(string pair)
    {
        return pair.EndsWith("JPY")
            ? 0.02m
            : 0.0002m;
    }

    private static int GetDecimalPlaces(string pair)
    {
        return pair.EndsWith("JPY")
            ? 3
            : 5;
    }
}