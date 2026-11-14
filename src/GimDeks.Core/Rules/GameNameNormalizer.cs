using System.Text;
using System.Text.RegularExpressions;

namespace GimDeks.Core.Rules;

/// <summary>
/// Normalizes game names for duplicate detection: lowercase, no punctuation,
/// collapsed spaces, version-like tokens stripped.
/// </summary>
public static partial class GameNameNormalizer
{
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var withoutVersion = VersionToken().Replace(name, " ");
        var builder = new StringBuilder(withoutVersion.Length);
        foreach (var ch in withoutVersion.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        var collapsed = Whitespace().Replace(builder.ToString(), " ").Trim();
        return collapsed;
    }

    // Matches version-like tokens such as v1.7, v2,12, 2.810.0.0, build 1491.50.
    [GeneratedRegex(@"(v|build\s*)?\d+([.,]\d+){1,3}(?<![.,])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VersionToken();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
