using EfxSimulator.Api.Services;
using StackExchange.Redis;
using EfxSimulator.Api.Hubs;
using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Options;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.Configure<RiskLimitsOptions>(
    builder.Configuration.GetSection(RiskLimitsOptions.SectionName));

var redisConnectionString =
    builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
var postgresConnectionString =
    builder.Configuration["Postgres:ConnectionString"] ??
    "Host=localhost;Port=5432;Database=efx_simulator;Username=efx;Password=efx_dev_password";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IRedisStore, RedisStore>();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(postgresConnectionString));
builder.Services.AddScoped<ITradeStore, PostgresTradeStore>();

builder.Services.AddHostedService<PriceGeneratorService>();

builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddScoped<ExecutionService>();
builder.Services.AddSingleton<PositionService>();
builder.Services.AddScoped<RiskService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapHub<PriceHub>("/hubs/prices");

app.MapGet("/health", async (
    IConnectionMultiplexer redis,
    NpgsqlDataSource postgres) =>
{
    var db = redis.GetDatabase();
    await db.PingAsync();

    await using var command = postgres.CreateCommand("SELECT 1");
    await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        status = "ok",
        redis = "connected",
        postgres = "connected"
    });
});

app.Run();
