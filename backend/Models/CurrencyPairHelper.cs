namespace EfxSimulator.Api.Models;

public static class CurrencyPairHelper
{
    public static string GetBaseCurrency(string pair)
    {
        if (string.IsNullOrWhiteSpace(pair) || pair.Length != 6)
        {
            throw new ArgumentException("Currency pair must be a 6-character symbol like EURUSD");
        }

        return pair[..3];
    }

    public static string GetQuoteCurrency(string pair)
    {
        if (string.IsNullOrWhiteSpace(pair) || pair.Length != 6)
        {
            throw new ArgumentException("Currency pair must be a 6-character symbol like EURUSD");
        }

        return pair[3..6];
    }
}