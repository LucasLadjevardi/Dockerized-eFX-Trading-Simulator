using System.Text;

namespace EfxSimulator.Api.Models;

public static class PortfolioIds
{
    public const string Default = "default";

    public static string Normalize(string? portfolioId)
    {
        if (string.IsNullOrWhiteSpace(portfolioId))
        {
            return Default;
        }

        var normalized = new StringBuilder();

        foreach (var character in portfolioId.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) ||
                character is '-' or '_' or '.')
            {
                normalized.Append(character);
            }
        }

        return normalized.Length == 0
            ? Default
            : normalized.ToString();
    }
}
