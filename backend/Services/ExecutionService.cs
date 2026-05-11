using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class ExecutionService
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(25);
    private static readonly TimeSpan MaxLockWait = TimeSpan.FromSeconds(4);

    private readonly RedisStore _redis;
    private readonly TradeService _tradeService;
    private readonly PositionService _positionService;
    private readonly RiskService _riskService;

    public ExecutionService(
        RedisStore redis,
        TradeService tradeService,
        PositionService positionService,
        RiskService riskService)
    {
        _redis = redis;
        _tradeService = tradeService;
        _positionService = positionService;
        _riskService = riskService;
    }

    public async Task<Trade> ExecuteTradeAsync(TradeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Trade request is required");
        }

        if (string.IsNullOrWhiteSpace(request.QuoteId))
        {
            throw new ArgumentException("QuoteId is required");
        }

        var quoteKey = RedisKeys.Quote(request.QuoteId);
        var quotePreview = await _redis.GetJsonAsync<Quote>(quoteKey);

        if (quotePreview is null)
        {
            throw new InvalidOperationException(
                "Quote expired, already used, or does not exist");
        }

        if (quotePreview.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Quote expired");
        }

        var lockValue = Guid.NewGuid().ToString("N");
        var lockKeys = new[]
            {
                RedisKeys.PairExecutionLock(quotePreview.Pair),
                RedisKeys.PortfolioRiskLock()
            }
            .Order(StringComparer.Ordinal)
            .ToArray();

        var acquiredLocks = await AcquireLocksAsync(
            lockKeys,
            lockValue,
            quotePreview.ExpiresAtUtc);

        try
        {
            var quote = await _redis.GetAndDeleteJsonAsync<Quote>(quoteKey);

            if (quote is null)
            {
                throw new InvalidOperationException(
                    "Quote expired, already used, or does not exist");
            }

            if (quote.ExpiresAtUtc <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Quote expired");
            }

            var riskResult = await _riskService.CheckTradeAsync(quote);

            if (!riskResult.IsApproved)
            {
                throw new InvalidOperationException(riskResult.Reason);
            }

            var baseCurrency = CurrencyPairHelper.GetBaseCurrency(quote.Pair);
            var quoteCurrency = CurrencyPairHelper.GetQuoteCurrency(quote.Pair);

            var trade = new Trade
            {
                TradeId = $"t-{Guid.NewGuid():N}",
                QuoteId = quote.QuoteId,
                Pair = quote.Pair,
                Side = quote.Side,
                BaseCurrency = baseCurrency,
                QuoteCurrency = quoteCurrency,
                BaseAmount = quote.Amount,
                QuoteAmount = quote.Amount * quote.Price,
                Price = quote.Price,
                Status = "FILLED",
                ExecutedAtUtc = DateTime.UtcNow
            };

            await _tradeService.RecordTradeAsync(trade);
            await _positionService.ApplyTradeAsync(trade);

            return trade;
        }
        finally
        {
            await ReleaseLocksAsync(acquiredLocks, lockValue);
        }
    }

    private async Task<List<string>> AcquireLocksAsync(
        IReadOnlyCollection<string> lockKeys,
        string lockValue,
        DateTime quoteExpiresAtUtc)
    {
        var acquiredLocks = new List<string>();
        var waitDeadlineUtc = DateTime.UtcNow.Add(MaxLockWait);

        if (quoteExpiresAtUtc < waitDeadlineUtc)
        {
            waitDeadlineUtc = quoteExpiresAtUtc;
        }

        try
        {
            foreach (var lockKey in lockKeys)
            {
                var acquired = false;

                while (DateTime.UtcNow < waitDeadlineUtc)
                {
                    acquired = await _redis.TryAcquireLockAsync(
                        lockKey,
                        lockValue,
                        LockExpiry);

                    if (acquired)
                    {
                        acquiredLocks.Add(lockKey);
                        break;
                    }

                    await Task.Delay(LockRetryDelay);
                }

                if (!acquired)
                {
                    throw new InvalidOperationException(
                        "Could not acquire execution lock before quote expired");
                }
            }

            return acquiredLocks;
        }
        catch
        {
            await ReleaseLocksAsync(acquiredLocks, lockValue);
            throw;
        }
    }

    private async Task ReleaseLocksAsync(
        IReadOnlyList<string> lockKeys,
        string lockValue)
    {
        for (var i = lockKeys.Count - 1; i >= 0; i--)
        {
            await _redis.ReleaseLockAsync(lockKeys[i], lockValue);
        }
    }
}
