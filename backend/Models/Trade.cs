namespace EfxSimulator.Api.Models;

public sealed class Trade
{
    public required string TradeId { get; init; }

    public required string QuoteId { get; init; }

    public required string Pair { get; init; }

    public required string Side { get; init; }

    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }

    public decimal BaseAmount { get; init; }

    public decimal QuoteAmount { get; init; }

    public decimal Price { get; init; }

    public required string Status { get; init; }

    public DateTime ExecutedAtUtc { get; init; } = DateTime.UtcNow;
}