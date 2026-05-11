using EfxSimulator.Api.Models;
using System.Text.Json;
using StackExchange.Redis;

namespace EfxSimulator.Api.Infrastructure;

public interface IRedisStore
{
    Task<T?> GetJsonAsync<T>(string key);

    Task<T?> GetAndDeleteJsonAsync<T>(string key);

    Task<bool> SetJsonAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null);

    Task<List<T>> ListRangeJsonAsync<T>(
        string key,
        long start = 0,
        long stop = -1);

    Task<List<string>> ListRangeAsync(
        string key,
        long start = 0,
        long stop = -1);

    Task<long> ListLeftPushAsync(string key, string value);

    Task<long> ListRightPushJsonAsync<T>(string key, T value);

    Task ListTrimAsync(string key, long start, long stop);

    Task<bool> TryAcquireLockAsync(
        string key,
        string lockValue,
        TimeSpan expiry);

    Task<bool> ReleaseLockAsync(string key, string lockValue);
}

public sealed class RedisStore : IRedisStore
{
    private readonly IDatabase _database;

    public RedisStore(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetJsonAsync<T>(string key)
    {
        var json = await _database.StringGetAsync(key);

        return json.HasValue
            ? JsonSerializer.Deserialize<T>(json!)
            : default;
    }

    public async Task<T?> GetAndDeleteJsonAsync<T>(string key)
    {
        var json = await _database.StringGetDeleteAsync(key);

        return json.HasValue
            ? JsonSerializer.Deserialize<T>(json!)
            : default;
    }

    public Task<bool> SetJsonAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        return _database.StringSetAsync(key, json, expiry);
    }

    public async Task<List<T>> ListRangeJsonAsync<T>(
        string key,
        long start = 0,
        long stop = -1)
    {
        var values = await _database.ListRangeAsync(key, start, stop);
        var items = new List<T>();

        foreach (var value in values)
        {
            if (!value.HasValue)
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<T>(value!);

            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<List<string>> ListRangeAsync(
        string key,
        long start = 0,
        long stop = -1)
    {
        var values = await _database.ListRangeAsync(key, start, stop);
        var items = new List<string>();

        foreach (var value in values)
        {
            if (value.HasValue)
            {
                items.Add(value.ToString());
            }
        }

        return items;
    }

    public Task<long> ListLeftPushAsync(string key, string value)
    {
        return _database.ListLeftPushAsync(key, value);
    }

    public Task<long> ListRightPushJsonAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        return _database.ListRightPushAsync(key, json);
    }

    public Task ListTrimAsync(string key, long start, long stop)
    {
        return _database.ListTrimAsync(key, start, stop);
    }

    public Task<bool> TryAcquireLockAsync(
        string key,
        string lockValue,
        TimeSpan expiry)
    {
        return _database.LockTakeAsync(key, lockValue, expiry);
    }

    public Task<bool> ReleaseLockAsync(string key, string lockValue)
    {
        return _database.LockReleaseAsync(key, lockValue);
    }
}

public static class RedisKeys
{
    public static string Price(string pair) => $"price:{pair}";

    public static string PriceHistory(string pair) => $"pricehistory:{pair}";

    public static string Quote(string quoteId) => $"quote:{quoteId}";

    public static string Position(string pair) =>
        Position(PortfolioIds.Default, pair);

    public static string Position(string portfolioId, string pair) =>
        $"portfolio:{PortfolioIds.Normalize(portfolioId)}:position:{pair}";

    public static string PairExecutionLock(string pair) =>
        PairExecutionLock(PortfolioIds.Default, pair);

    public static string PairExecutionLock(string portfolioId, string pair) =>
        $"lock:execution:portfolio:{PortfolioIds.Normalize(portfolioId)}:pair:{pair}";

    public static string PortfolioRiskLock() =>
        PortfolioRiskLock(PortfolioIds.Default);

    public static string PortfolioRiskLock(string portfolioId) =>
        $"lock:execution:portfolio:{PortfolioIds.Normalize(portfolioId)}:risk";
}
