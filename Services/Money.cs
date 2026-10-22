using System.Globalization;

namespace Staybnb.Services;

public static class Money
{
    // South African style: "R 1 780" or "R 1 780,00"
    private static readonly NumberFormatInfo SouthAfrica = new()
    {
        NumberGroupSeparator = "\u00A0",
        NumberDecimalSeparator = ","
    };

    public static string Rand(decimal amount, bool cents = false) =>
        "R\u00A0" + amount.ToString(cents ? "N2" : "N0", SouthAfrica);
}