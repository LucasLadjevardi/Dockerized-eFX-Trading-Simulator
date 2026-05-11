namespace EfxSimulator.Api.Options;

public sealed class RiskLimitsOptions
{
    public const string SectionName = "RiskLimits";

    public decimal MaxSingleTradeSize { get; init; } = 2_000_000m;

    public decimal MaxPairNetExposure { get; init; } = 5_000_000m;

    public decimal MaxCurrencyExposureUsd { get; init; } = 7_500_000m;

    public decimal MaxGrossNotionalUsd { get; init; } = 15_000_000m;

    public decimal MaxUnrealizedLossUsd { get; init; } = 100_000m;

    public int MaxMarketPriceAgeSeconds { get; init; } = 5;
}
