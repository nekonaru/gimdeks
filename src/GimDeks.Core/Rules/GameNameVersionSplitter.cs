using System.Text.RegularExpressions;

namespace GimDeks.Core.Rules;

/// <summary>
/// Splits a version-like token from the end of a game name.
/// Example: "God of War Ragnarok v1.7" becomes name "God of War Ragnarok", version "1.7".
/// Returns the input unchanged with a null version when no version-like token is found.
/// </summary>
public static partial class GameNameVersionSplitter
{
    public static (string Name, string? Versi) Split(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (string.Empty, null);
        }

        var match = TrailingVersion().Match(name);
        if (!match.Success)
        {
            return (name.Trim(), null);
        }

        var version = match.Groups["ver"].Value.TrimStart('v', 'V');
        var cleaned = name[..match.Index].Trim().TrimEnd('-', ' ');
        if (cleaned.Length == 0)
        {
            return (name.Trim(), null);
        }

        return (cleaned, version);
    }

    [GeneratedRegex(@"\s*\b(?<ver>v?\d+([.,]\d+){1,3})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TrailingVersion();
}
