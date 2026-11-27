using System.Globalization;

namespace GimDeks.Core.Rules;

/// <summary>
/// Parses decimal numbers the way the source Excel files store them.
/// Thousands separators are not accepted; invariant culture (dot decimal)
/// first, then Indonesian (comma decimal). The current culture is never used
/// so a locale with comma thousands separators cannot misread "1,5" as 15.
/// Never throws; returns null on failure.
/// </summary>
public static class NumberParser
{
    private static readonly CultureInfo IndonesianCulture = CultureInfo.GetCultureInfo("id-ID");

    public static double? ParseGb(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariant) && double.IsFinite(invariant))
        {
            return invariant;
        }

        if (double.TryParse(text, NumberStyles.Float, IndonesianCulture, out var indonesian) && double.IsFinite(indonesian))
        {
            return indonesian;
        }

        return null;
    }
}
