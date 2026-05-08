namespace EfxSimulator.Api.Models;

public sealed class Position
{
    public required string Pair { get; init; }

    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }

    public decimal NetBaseAmount { get; init; }

    public decimal AveragePrice { get; init; }

    public decimal CurrentPrice { get; init; }

    public decimal UnrealizedPnl { get; init; }

    public required string PnlCurrency { get; init; }

    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
}