using System.Text.Json;
using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnectionString =
    builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddHostedService<PriceGeneratorService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "EfxSimulator.Api",
    status = "running"
}));

app.MapGet("/health", async (IConnectionMultiplexer redis) =>
{
    try
    {
        var db = redis.GetDatabase();
        await db.PingAsync();

        return Results.Ok(new
        {
            status = "ok",
            redis = "connected"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Health check failed",
            detail: ex.Message,
            statusCode: 503
        );
    }
});

app.MapGet("/api/prices", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();

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

        if (json.HasValue)
        {
            var price = JsonSerializer.Deserialize<FxPrice>(json!);
            prices[pair] = price;
        }
        else
        {
            prices[pair] = null;
        }
    }

    return Results.Ok(prices);
});

app.Run();