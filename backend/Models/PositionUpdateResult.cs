namespace EfxSimulator.Api.Models;

public sealed class PositionUpdateResult
{
    public required Position Position { get; init; }

    public decimal RealizedPnl { get; init; }

    public required string PnlCurrency { get; init; }
}
