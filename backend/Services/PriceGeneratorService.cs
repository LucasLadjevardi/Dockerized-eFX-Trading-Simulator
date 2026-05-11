using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using EfxSimulator.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EfxSimulator.Api.Services;

public sealed class PriceGeneratorService : BackgroundService // no other class can inherit. BackgroundService runs as a long-lived hosted service in ASP.NET
{
    private readonly IRedisStore _redis;
    private readonly ILogger<PriceGeneratorService> _logger;
    private readonly IHubContext<PriceHub> _priceHub;
    private readonly PositionService _positionService;
    private readonly Random _random = new();

    private readonly Dictionary<string, decimal> _midPrices = new()
    {
        ["EURUSD"] = 1.0850m,
        ["GBPUSD"] = 1.2650m,
        ["USDJPY"] = 155.50m,
        ["EURGBP"] = 0.8570m
    };

    public PriceGeneratorService(
        IRedisStore redis,
        ILogger<PriceGeneratorService> logger,
        IHubContext<PriceHub> priceHub,
        PositionService positionService)
    {
        _redis = redis;
        _logger = logger;
        _priceHub = priceHub;
        _positionService = positionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Price generator service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var prices = new Dictionary<string, FxPrice>();

            foreach (var pair in _midPrices.Keys.ToList())
            {
                var price = GenerateNextPrice(pair);

                await _redis.SetJsonAsync(RedisKeys.Price(pair), price);

                var historyKey = RedisKeys.PriceHistory(pair);
                await _redis.ListRightPushJsonAsync(historyKey, price);
                await _redis.ListTrimAsync(historyKey, -120, -1);

                prices[pair] = price;
            }

            await _priceHub.Clients.All.SendAsync(
                "pricesUpdated",
                prices,
                stoppingToken);
            
            var positions = await _positionService.GetAllPositionsAsync();

            if (positions.Count > 0)
            {
                await _priceHub.Clients.All.SendAsync(
                    "positionsUpdated",
                    positions,
                    stoppingToken);
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
