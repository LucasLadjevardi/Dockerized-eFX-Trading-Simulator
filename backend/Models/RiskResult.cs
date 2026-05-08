namespace EfxSimulator.Api.Models;

public sealed class RiskResult
{
    public bool IsApproved { get; init; }

    public required string Reason { get; init; }

    public static RiskResult Approved() => new()
    {
        IsApproved = true,
        Reason = "Approved"
    };

    public static RiskResult Rejected(string reason) => new()
    {
        IsApproved = false,
        Reason = reason
    };
}