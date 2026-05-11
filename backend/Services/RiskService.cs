using EfxSimulator.Api.Infrastructure;
using EfxSimulator.Api.Models;

namespace EfxSimulator.Api.Services;

public sealed class RiskService
{
    private readonly RedisStore _redis;
    private readonly IReadOnlyList<IPreTradeRiskRule> _rules;

    private const decimal MaxSingleTradeSize = 2_000_000m;
    private const decimal MaxPairNetExposure = 5_000_000m;
    private const decimal MaxCurrencyExposureUsd = 7_500_000m;
    private const decimal MaxGrossNotionalUsd = 15_000_000m;
    private const decimal MaxUnrealizedLossUsd = 100_000m;

    private static readonly TimeSpan MaxMarketPriceAge = TimeSpan.FromSeconds(5);

    private static readonly string[] SupportedPairs =
    {
        "EURUSD",
        "GBPUSD",
        "USDJPY",
        "EURGBP"
    };

    public RiskService(RedisStore redis)
    {
        _redis = redis;
        _rules = new IPreTradeRiskRule[]
        {
            new SingleTradeSizeRule(MaxSingleTradeSize),
            new MarketDataFreshnessRule(MaxMarketPriceAge),
            new PairExposureRule(MaxPairNetExposure),
            new CurrencyExposureRule(MaxCurrencyExposureUsd),
            new GrossNotionalRule(MaxGrossNotionalUsd),
            new LossLimitRule(MaxUnrealizedLossUsd)
        };
    }

    public async Task<RiskResult> CheckTradeAsync(Quote quote)
    {
        var context = await BuildRiskContextAsync(quote);

        foreach (var rule in _rules)
        {
            var result = rule.Check(context);

            if (!result.IsApproved)
            {
                return result;
            }
        }

        return RiskResult.Approved();
    }

    private async Task<RiskCheckContext> BuildRiskContextAsync(Quote quote)
    {
        var pricesByPair = await LoadPricesAsync();
        var positionsByPair = await LoadPositionsAsync();
        var projectedPairNetBaseAmounts = BuildProjectedPairNetBaseAmounts(
            positionsByPair,
            quote);

        var currencyExposures = CalculateCurrencyExposures(
            projectedPairNetBaseAmounts,
            pricesByPair);

        var currencyExposureUsdResult = ConvertCurrencyExposuresToUsd(
            currencyExposures,
            pricesByPair);

        var projectedPnlUsdResult = CalculateProjectedUnrealizedPnlUsd(
            projectedPairNetBaseAmounts,
            positionsByPair,
            pricesByPair,
            quote);

        var missingUsdConversionCurrencies = new HashSet<string>(
            currencyExposureUsdResult.MissingConversionCurrencies,
            StringComparer.OrdinalIgnoreCase);

        missingUsdConversionCurrencies.UnionWith(
            projectedPnlUsdResult.MissingConversionCurrencies);

        return new RiskCheckContext
        {
            Quote = quote,
            CheckedAtUtc = DateTime.UtcNow,
            PricesByPair = pricesByPair,
            RequiredPricePairs = DetermineRequiredPricePairs(
                quote,
                projectedPairNetBaseAmounts,
                currencyExposures),
            ProjectedPairNetBaseAmounts = projectedPairNetBaseAmounts,
            ProjectedCurrencyExposures = currencyExposures,
            ProjectedCurrencyExposuresUsd =
                currencyExposureUsdResult.CurrencyExposuresUsd,
            GrossNotionalUsd = currencyExposureUsdResult.GrossNotionalUsd,
            ProjectedUnrealizedPnlUsd =
                projectedPnlUsdResult.ProjectedUnrealizedPnlUsd,
            MissingUsdConversionCurrencies = missingUsdConversionCurrencies
        };
    }

    private async Task<Dictionary<string, FxPrice>> LoadPricesAsync()
    {
        var prices = new Dictionary<string, FxPrice>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in SupportedPairs)
        {
            var price = await _redis.GetJsonAsync<FxPrice>(RedisKeys.Price(pair));

            if (price is not null)
            {
                prices[pair] = price;
            }
        }

        return prices;
    }

    private async Task<Dictionary<string, Position>> LoadPositionsAsync()
    {
        var positions = new Dictionary<string, Position>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in SupportedPairs)
        {
            var position = await _redis.GetJsonAsync<Position>(
                RedisKeys.Position(pair));

            if (position is not null)
            {
                positions[pair] = position;
            }
        }

        return positions;
    }

    private static Dictionary<string, decimal> BuildProjectedPairNetBaseAmounts(
        IReadOnlyDictionary<string, Position> positionsByPair,
        Quote quote)
    {
        var projectedPairNetBaseAmounts = new Dictionary<string, decimal>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var pair in SupportedPairs)
        {
            projectedPairNetBaseAmounts[pair] = positionsByPair.TryGetValue(
                pair,
                out var position)
                ? position.NetBaseAmount
                : 0m;
        }

        if (!projectedPairNetBaseAmounts.ContainsKey(quote.Pair))
        {
            projectedPairNetBaseAmounts[quote.Pair] = 0m;
        }

        projectedPairNetBaseAmounts[quote.Pair] += GetSignedBaseAmount(quote);

        return projectedPairNetBaseAmounts;
    }

    private static Dictionary<string, decimal> CalculateCurrencyExposures(
        IReadOnlyDictionary<string, decimal> projectedPairNetBaseAmounts,
        IReadOnlyDictionary<string, FxPrice> pricesByPair)
    {
        var exposures = new Dictionary<string, decimal>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var (pair, netBaseAmount) in projectedPairNetBaseAmounts)
        {
            if (netBaseAmount == 0m)
            {
                continue;
            }

            AddExposure(
                exposures,
                CurrencyPairHelper.GetBaseCurrency(pair),
                netBaseAmount);

            if (!pricesByPair.TryGetValue(pair, out var price))
            {
                continue;
            }

            AddExposure(
                exposures,
                CurrencyPairHelper.GetQuoteCurrency(pair),
                -netBaseAmount * price.Mid);
        }

        return exposures;
    }

    private static CurrencyExposureUsdResult ConvertCurrencyExposuresToUsd(
        IReadOnlyDictionary<string, decimal> currencyExposures,
        IReadOnlyDictionary<string, FxPrice> pricesByPair)
    {
        var exposuresUsd = new Dictionary<string, decimal>(
            StringComparer.OrdinalIgnoreCase);
        var missingConversions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var grossNotionalUsd = 0m;

        foreach (var (currency, amount) in currencyExposures)
        {
            if (!TryConvertToUsd(currency, amount, pricesByPair, out var usdAmount))
            {
                missingConversions.Add(currency);
                continue;
            }

            exposuresUsd[currency] = usdAmount;
            grossNotionalUsd += Math.Abs(usdAmount);
        }

        return new CurrencyExposureUsdResult(
            exposuresUsd,
            grossNotionalUsd,
            missingConversions);
    }

    private static ProjectedPnlUsdResult CalculateProjectedUnrealizedPnlUsd(
        IReadOnlyDictionary<string, decimal> projectedPairNetBaseAmounts,
        IReadOnlyDictionary<string, Position> positionsByPair,
        IReadOnlyDictionary<string, FxPrice> pricesByPair,
        Quote quote)
    {
        var projectedUnrealizedPnlUsd = 0m;
        var missingConversions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (pair, projectedNetBaseAmount) in projectedPairNetBaseAmounts)
        {
            if (projectedNetBaseAmount == 0m)
            {
                continue;
            }

            if (!pricesByPair.TryGetValue(pair, out var price))
            {
                continue;
            }

            positionsByPair.TryGetValue(pair, out var existingPosition);

            var projectedAveragePrice = existingPosition?.AveragePrice ?? 0m;

            if (pair.Equals(quote.Pair, StringComparison.OrdinalIgnoreCase))
            {
                projectedAveragePrice = CalculateAveragePrice(
                    existingPosition?.NetBaseAmount ?? 0m,
                    existingPosition?.AveragePrice ?? 0m,
                    GetSignedBaseAmount(quote),
                    quote.Price);
            }

            var pnl = CalculateUnrealizedPnl(
                projectedNetBaseAmount,
                projectedAveragePrice,
                price.Mid);

            var pnlCurrency = CurrencyPairHelper.GetQuoteCurrency(pair);

            if (!TryConvertToUsd(pnlCurrency, pnl, pricesByPair, out var pnlUsd))
            {
                missingConversions.Add(pnlCurrency);
                continue;
            }

            projectedUnrealizedPnlUsd += pnlUsd;
        }

        return new ProjectedPnlUsdResult(
            projectedUnrealizedPnlUsd,
            missingConversions);
    }

    private static HashSet<string> DetermineRequiredPricePairs(
        Quote quote,
        IReadOnlyDictionary<string, decimal> projectedPairNetBaseAmounts,
        IReadOnlyDictionary<string, decimal> currencyExposures)
    {
        var requiredPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            quote.Pair
        };

        foreach (var (pair, netBaseAmount) in projectedPairNetBaseAmounts)
        {
            if (netBaseAmount != 0m)
            {
                requiredPairs.Add(pair);
            }
        }

        foreach (var currency in currencyExposures.Keys)
        {
            AddUsdConversionPricePair(currency, requiredPairs);
        }

        return requiredPairs;
    }

    private static void AddExposure(
        IDictionary<string, decimal> exposures,
        string currency,
        decimal amount)
    {
        exposures.TryGetValue(currency, out var currentAmount);
        exposures[currency] = currentAmount + amount;
    }

    private static decimal GetSignedBaseAmount(Quote quote)
    {
        return quote.Side == "BUY"
            ? quote.Amount
            : -quote.Amount;
    }

    private static bool TryConvertToUsd(
        string currency,
        decimal amount,
        IReadOnlyDictionary<string, FxPrice> pricesByPair,
        out decimal usdAmount)
    {
        if (currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            usdAmount = amount;
            return true;
        }

        if (currency.Equals("EUR", StringComparison.OrdinalIgnoreCase))
        {
            return TryMultiplyByUsdPair("EURUSD", amount, pricesByPair, out usdAmount);
        }

        if (currency.Equals("GBP", StringComparison.OrdinalIgnoreCase))
        {
            return TryMultiplyByUsdPair("GBPUSD", amount, pricesByPair, out usdAmount);
        }

        if (currency.Equals("JPY", StringComparison.OrdinalIgnoreCase))
        {
            if (pricesByPair.TryGetValue("USDJPY", out var usdJpy) &&
                usdJpy.Mid > 0m)
            {
                usdAmount = amount / usdJpy.Mid;
                return true;
            }
        }

        usdAmount = 0m;
        return false;
    }

    private static bool TryMultiplyByUsdPair(
        string pair,
        decimal amount,
        IReadOnlyDictionary<string, FxPrice> pricesByPair,
        out decimal usdAmount)
    {
        if (pricesByPair.TryGetValue(pair, out var price) &&
            price.Mid > 0m)
        {
            usdAmount = amount * price.Mid;
            return true;
        }

        usdAmount = 0m;
        return false;
    }

    private static void AddUsdConversionPricePair(
        string currency,
        ISet<string> requiredPairs)
    {
        if (currency.Equals("EUR", StringComparison.OrdinalIgnoreCase))
        {
            requiredPairs.Add("EURUSD");
            return;
        }

        if (currency.Equals("GBP", StringComparison.OrdinalIgnoreCase))
        {
            requiredPairs.Add("GBPUSD");
            return;
        }

        if (currency.Equals("JPY", StringComparison.OrdinalIgnoreCase))
        {
            requiredPairs.Add("USDJPY");
        }
    }

    private static decimal CalculateAveragePrice(
        decimal oldNetBaseAmount,
        decimal oldAveragePrice,
        decimal signedBaseAmount,
        decimal tradePrice)
    {
        var newNetBaseAmount = oldNetBaseAmount + signedBaseAmount;

        if (newNetBaseAmount == 0m)
        {
            return 0m;
        }

        if (oldNetBaseAmount == 0m ||
            Math.Sign(oldNetBaseAmount) == Math.Sign(signedBaseAmount))
        {
            var oldNotional = Math.Abs(oldNetBaseAmount) * oldAveragePrice;
            var tradeNotional = Math.Abs(signedBaseAmount) * tradePrice;

            return (oldNotional + tradeNotional) / Math.Abs(newNetBaseAmount);
        }

        if (Math.Sign(oldNetBaseAmount) != Math.Sign(newNetBaseAmount))
        {
            return tradePrice;
        }

        return oldAveragePrice;
    }

    private static decimal CalculateUnrealizedPnl(
        decimal netBaseAmount,
        decimal averagePrice,
        decimal currentPrice)
    {
        if (netBaseAmount == 0m || averagePrice == 0m || currentPrice == 0m)
        {
            return 0m;
        }

        return netBaseAmount * (currentPrice - averagePrice);
    }

    private interface IPreTradeRiskRule
    {
        RiskResult Check(RiskCheckContext context);
    }

    private sealed class SingleTradeSizeRule : IPreTradeRiskRule
    {
        private readonly decimal _maxSingleTradeSize;

        public SingleTradeSizeRule(decimal maxSingleTradeSize)
        {
            _maxSingleTradeSize = maxSingleTradeSize;
        }

        public RiskResult Check(RiskCheckContext context)
        {
            return context.Quote.Amount > _maxSingleTradeSize
                ? RiskResult.Rejected(
                    $"Trade amount exceeds max single trade size of {_maxSingleTradeSize:N0}")
                : RiskResult.Approved();
        }
    }

    private sealed class MarketDataFreshnessRule : IPreTradeRiskRule
    {
        private readonly TimeSpan _maxMarketPriceAge;

        public MarketDataFreshnessRule(TimeSpan maxMarketPriceAge)
        {
            _maxMarketPriceAge = maxMarketPriceAge;
        }

        public RiskResult Check(RiskCheckContext context)
        {
            foreach (var pair in context.RequiredPricePairs.OrderBy(pair => pair))
            {
                if (!context.PricesByPair.TryGetValue(pair, out var price))
                {
                    return RiskResult.Rejected(
                        $"Missing market price for {pair}");
                }

                var age = context.CheckedAtUtc - price.TimestampUtc;

                if (age > _maxMarketPriceAge)
                {
                    return RiskResult.Rejected(
                        $"Market price for {pair} is stale ({age.TotalSeconds:N1}s old)");
                }
            }

            return RiskResult.Approved();
        }
    }

    private sealed class PairExposureRule : IPreTradeRiskRule
    {
        private readonly decimal _maxPairNetExposure;

        public PairExposureRule(decimal maxPairNetExposure)
        {
            _maxPairNetExposure = maxPairNetExposure;
        }

        public RiskResult Check(RiskCheckContext context)
        {
            foreach (var (pair, netBaseAmount) in
                     context.ProjectedPairNetBaseAmounts.OrderBy(item => item.Key))
            {
                if (Math.Abs(netBaseAmount) > _maxPairNetExposure)
                {
                    return RiskResult.Rejected(
                        $"Projected net exposure for {pair} exceeds {_maxPairNetExposure:N0} base units");
                }
            }

            return RiskResult.Approved();
        }
    }

    private sealed class CurrencyExposureRule : IPreTradeRiskRule
    {
        private readonly decimal _maxCurrencyExposureUsd;

        public CurrencyExposureRule(decimal maxCurrencyExposureUsd)
        {
            _maxCurrencyExposureUsd = maxCurrencyExposureUsd;
        }

        public RiskResult Check(RiskCheckContext context)
        {
            if (context.MissingUsdConversionCurrencies.Count > 0)
            {
                return RiskResult.Rejected(
                    $"Missing USD conversion price for {FormatCurrencies(context.MissingUsdConversionCurrencies)}");
            }

            foreach (var (currency, exposureUsd) in
                     context.ProjectedCurrencyExposuresUsd.OrderBy(item => item.Key))
            {
                if (Math.Abs(exposureUsd) > _maxCurrencyExposureUsd)
                {
                    return RiskResult.Rejected(
                        $"Projected {currency} exposure exceeds {_maxCurrencyExposureUsd:N0} USD equivalent");
                }
            }

            return RiskResult.Approved();
        }
    }

    private sealed class GrossNotionalRule : IPreTradeRiskRule
    {
        private readonly decimal _maxGrossNotionalUsd;

        public GrossNotionalRule(decimal maxGrossNotionalUsd)
        {
            _maxGrossNotionalUsd = maxGrossNotionalUsd;
        }

        public RiskResult Check(RiskCheckContext context)
        {
            return context.GrossNotionalUsd > _maxGrossNotionalUsd
                ? RiskResult.Rejected(
                    $"Projected gross notional exceeds {_maxGrossNotionalUsd:N0} USD")
                : RiskResult.Approved();
        }
    }

    private sealed class LossLimitRule : IPreTradeRiskRule
    {
        private readonly decimal _maxUnrealizedLossUsd;

        public LossLimitRule(decimal maxUnrealizedLossUsd)
        {
            _maxUnrealizedLossUsd = maxUnrealizedLossUsd;
        }

        public RiskResult Check(RiskCheckContext context)
        {
            return context.ProjectedUnrealizedPnlUsd < -_maxUnrealizedLossUsd
                ? RiskResult.Rejected(
                    $"Projected unrealized loss exceeds {_maxUnrealizedLossUsd:N0} USD")
                : RiskResult.Approved();
        }
    }

    private sealed class RiskCheckContext
    {
        public required Quote Quote { get; init; }

        public DateTime CheckedAtUtc { get; init; }

        public required IReadOnlyDictionary<string, FxPrice> PricesByPair { get; init; }

        public required IReadOnlyCollection<string> RequiredPricePairs { get; init; }

        public required IReadOnlyDictionary<string, decimal>
            ProjectedPairNetBaseAmounts { get; init; }

        public required IReadOnlyDictionary<string, decimal>
            ProjectedCurrencyExposures { get; init; }

        public required IReadOnlyDictionary<string, decimal>
            ProjectedCurrencyExposuresUsd { get; init; }

        public decimal GrossNotionalUsd { get; init; }

        public decimal ProjectedUnrealizedPnlUsd { get; init; }

        public required IReadOnlySet<string> MissingUsdConversionCurrencies { get; init; }
    }

    private sealed record CurrencyExposureUsdResult(
        IReadOnlyDictionary<string, decimal> CurrencyExposuresUsd,
        decimal GrossNotionalUsd,
        IReadOnlySet<string> MissingConversionCurrencies);

    private sealed record ProjectedPnlUsdResult(
        decimal ProjectedUnrealizedPnlUsd,
        IReadOnlySet<string> MissingConversionCurrencies);

    private static string FormatCurrencies(IEnumerable<string> currencies)
    {
        return string.Join(", ", currencies.OrderBy(currency => currency));
    }
}
