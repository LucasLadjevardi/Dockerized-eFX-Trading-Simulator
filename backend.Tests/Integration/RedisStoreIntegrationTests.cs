using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;
using StackExchange.Redis;

namespace EfxSimulator.Api.Tests.Integration;

public sealed class RedisStoreIntegrationTests
{
    [Fact]
    public async Task RedisStore_RoundTripsJsonAndLists_WhenRedisIntegrationIsEnabled()
    {
        await using var fixture = await RedisIntegrationFixture.TryCreateAsync();

        if (fixture is null)
        {
            return;
        }

        var store = new RedisStore(fixture.Connection);

        var price = new FxPrice
        {
            Pair = "EURUSD",
            Bid = 1.0998m,
            Ask = 1.1002m,
            TimestampUtc = DateTime.UtcNow
        };

        await store.SetJsonAsync(RedisKeys.Price("EURUSD"), price);
        await store.ListRightPushJsonAsync(RedisKeys.PriceHistory("EURUSD"), price);

        var storedPrice = await store.GetJsonAsync<FxPrice>(RedisKeys.Price("EURUSD"));
        var history = await store.ListRangeJsonAsync<FxPrice>(
            RedisKeys.PriceHistory("EURUSD"));

        Assert.NotNull(storedPrice);
        Assert.Equal(price.Pair, storedPrice.Pair);
        Assert.Single(history);
        Assert.Equal(price.Bid, history[0].Bid);
    }

    [Fact]
    public async Task RedisStore_GetAndDeleteJsonIsSingleUse_WhenRedisIntegrationIsEnabled()
    {
        await using var fixture = await RedisIntegrationFixture.TryCreateAsync();

        if (fixture is null)
        {
            return;
        }

        var store = new RedisStore(fixture.Connection);
        var quote = new Quote
        {
            QuoteId = "q-integration",
            Pair = "EURUSD",
            Side = "BUY",
            Amount = 1_000_000m,
            Price = 1.1002m,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(5)
        };

        await store.SetJsonAsync(RedisKeys.Quote(quote.QuoteId), quote);

        var firstRead = await store.GetAndDeleteJsonAsync<Quote>(
            RedisKeys.Quote(quote.QuoteId));
        var secondRead = await store.GetAndDeleteJsonAsync<Quote>(
            RedisKeys.Quote(quote.QuoteId));

        Assert.NotNull(firstRead);
        Assert.Null(secondRead);
    }

    [Fact]
    public async Task RedisStore_LocksAreExclusive_WhenRedisIntegrationIsEnabled()
    {
        await using var fixture = await RedisIntegrationFixture.TryCreateAsync();

        if (fixture is null)
        {
            return;
        }

        var store = new RedisStore(fixture.Connection);
        var lockKey = RedisKeys.PairExecutionLock("EURUSD");

        var firstLock = await store.TryAcquireLockAsync(
            lockKey,
            "owner-1",
            TimeSpan.FromSeconds(10));
        var secondLock = await store.TryAcquireLockAsync(
            lockKey,
            "owner-2",
            TimeSpan.FromSeconds(10));
        var released = await store.ReleaseLockAsync(lockKey, "owner-1");
        var thirdLock = await store.TryAcquireLockAsync(
            lockKey,
            "owner-2",
            TimeSpan.FromSeconds(10));

        Assert.True(firstLock);
        Assert.False(secondLock);
        Assert.True(released);
        Assert.True(thirdLock);
    }

    private sealed class RedisIntegrationFixture : IAsyncDisposable
    {
        private RedisIntegrationFixture(IConnectionMultiplexer connection)
        {
            Connection = connection;
        }

        public IConnectionMultiplexer Connection { get; }

        public static async Task<RedisIntegrationFixture?> TryCreateAsync()
        {
            if (!string.Equals(
                    Environment.GetEnvironmentVariable("EFX_REDIS_INTEGRATION"),
                    "1",
                    StringComparison.Ordinal))
            {
                return null;
            }

            var connectionString =
                Environment.GetEnvironmentVariable("EFX_REDIS_CONNECTION_STRING") ??
                "localhost:6379";
            var options = ConfigurationOptions.Parse(connectionString);

            options.AbortOnConnectFail = false;
            options.DefaultDatabase ??= 15;

            var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var database = connection.GetDatabase();

            await database.ExecuteAsync("FLUSHDB");

            return new RedisIntegrationFixture(connection);
        }

        public async ValueTask DisposeAsync()
        {
            var database = Connection.GetDatabase();
            await database.ExecuteAsync("FLUSHDB");
            await Connection.CloseAsync();
            Connection.Dispose();
        }
    }
}
