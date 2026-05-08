namespace EfxSimulator.Api.Models;

public sealed class Quote
{
    public required string QuoteId { get; init; }

    public required string Pair { get; init; }

    public required string Side { get; init; }

    public decimal Amount { get; init; }

    public decimal Price { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; init; }
}