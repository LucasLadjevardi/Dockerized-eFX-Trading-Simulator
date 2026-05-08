using EfxSimulator.Api.Services;
using StackExchange.Redis;
using EfxSimulator.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var redisConnectionString =
    builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddHostedService<PriceGeneratorService>();

builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddSingleton<PositionService>();
builder.Services.AddScoped<RiskService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapHub<PriceHub>("/hubs/prices");

app.MapGet("/health", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    await db.PingAsync();

    return Results.Ok(new
    {
        status = "ok",
        redis = "connected"
    });
});

app.Run();