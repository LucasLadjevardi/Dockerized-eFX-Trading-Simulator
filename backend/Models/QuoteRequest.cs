namespace EfxSimulator.Api.Models;

public sealed class QuoteRequest
{
    public required string Pair { get; init; }

    public required string Side { get; init; }

    public decimal Amount { get; init; }
}