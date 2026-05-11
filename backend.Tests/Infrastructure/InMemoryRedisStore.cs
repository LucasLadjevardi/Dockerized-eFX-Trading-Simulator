using EfxSimulator.Api.Infrastructure;

namespace EfxSimulator.Api.Tests.Infrastructure;

public sealed class InMemoryRedisStore : IRedisStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _lists = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _locks = new(StringComparer.Ordinal);

    public Task<T?> GetJsonAsync<T>(string key)
    {
        lock (_gate)
        {
            return Task.FromResult(
                _values.TryGetValue(key, out var value) && value is T item
                    ? item
                    : default);
        }
    }

    public Task<T?> GetAndDeleteJsonAsync<T>(string key)
    {
        lock (_gate)
        {
            if (!_values.TryGetValue(key, out var value) || value is not T item)
            {
                return Task.FromResult<T?>(default);
            }

            _values.Remove(key);
            return Task.FromResult<T?>(item);
        }
    }

    public Task<bool> SetJsonAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null)
    {
        lock (_gate)
        {
            _values[key] = value!;
            return Task.FromResult(true);
        }
    }

    public Task<List<T>> ListRangeJsonAsync<T>(
        string key,
        long start = 0,
        long stop = -1)
    {
        lock (_gate)
        {
            if (!_lists.TryGetValue(key, out var values))
            {
                return Task.FromResult(new List<T>());
            }

            return Task.FromResult(
                Slice(values, start, stop)
                    .Select(value => _values.TryGetValue(value, out var item) && item is T typed
                        ? typed
                        : default)
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .ToList());
        }
    }

    public Task<List<string>> ListRangeAsync(
        string key,
        long start = 0,
        long stop = -1)
    {
        lock (_gate)
        {
            return Task.FromResult(
                _lists.TryGetValue(key, out var values)
                    ? Slice(values, start, stop)
                    : new List<string>());
        }
    }

    public Task<long> ListLeftPushAsync(string key, string value)
    {
        lock (_gate)
        {
            var values = GetOrCreateList(key);
            values.Insert(0, value);
            return Task.FromResult((long)values.Count);
        }
    }

    public Task<long> ListRightPushJsonAsync<T>(string key, T value)
    {
        lock (_gate)
        {
            var valueKey = $"{key}:{Guid.NewGuid():N}";
            _values[valueKey] = value!;

            var values = GetOrCreateList(key);
            values.Add(valueKey);

            return Task.FromResult((long)values.Count);
        }
    }

    public Task ListTrimAsync(string key, long start, long stop)
    {
        lock (_gate)
        {
            if (_lists.TryGetValue(key, out var values))
            {
                _lists[key] = Slice(values, start, stop);
            }

            return Task.CompletedTask;
        }
    }

    public Task<bool> TryAcquireLockAsync(
        string key,
        string lockValue,
        TimeSpan expiry)
    {
        lock (_gate)
        {
            if (_locks.ContainsKey(key))
            {
                return Task.FromResult(false);
            }

            _locks[key] = lockValue;
            return Task.FromResult(true);
        }
    }

    public Task<bool> ReleaseLockAsync(string key, string lockValue)
    {
        lock (_gate)
        {
            if (!_locks.TryGetValue(key, out var currentValue) ||
                currentValue != lockValue)
            {
                return Task.FromResult(false);
            }

            _locks.Remove(key);
            return Task.FromResult(true);
        }
    }

    private List<string> GetOrCreateList(string key)
    {
        if (_lists.TryGetValue(key, out var values))
        {
            return values;
        }

        values = new List<string>();
        _lists[key] = values;
        return values;
    }

    private static List<string> Slice(
        IReadOnlyList<string> values,
        long start,
        long stop)
    {
        if (values.Count == 0)
        {
            return new List<string>();
        }

        var normalizedStart = NormalizeIndex(start, values.Count);
        var normalizedStop = stop == -1
            ? values.Count - 1
            : NormalizeIndex(stop, values.Count);

        if (normalizedStart > normalizedStop ||
            normalizedStart >= values.Count ||
            normalizedStop < 0)
        {
            return new List<string>();
        }

        normalizedStart = Math.Max(0, normalizedStart);
        normalizedStop = Math.Min(values.Count - 1, normalizedStop);

        return values
            .Skip(normalizedStart)
            .Take(normalizedStop - normalizedStart + 1)
            .ToList();
    }

    private static int NormalizeIndex(long index, int count)
    {
        return index < 0
            ? count + (int)index
            : (int)index;
    }
}
