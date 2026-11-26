using System.IO;
using System.Text.RegularExpressions;
using GimDeks.Core.Models;
using GimDeks.Core.Rules;

namespace GimDeks.App.Services;

/// <summary>
/// Reads local folders for .exe files. It never starts a process, never follows
/// reparse points, and stops at a fixed depth and file count so a large drive
/// cannot hang the UI.
/// </summary>
public static partial class InstalledGameScanner
{
    public const int MaxDepth = 5;
    public const int MaxFiles = 5000;

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "windows", "programdata", "$recycle.bin", "system volume information",
        "recovery", "perflogs", "inetpub", ".git", "node_modules",
    };

    public static bool IsValidRoot(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && Path.IsPathRooted(path)
        && Directory.Exists(path);

    public static async Task<IReadOnlyList<ScannedExecutable>> ScanAsync(string root, CancellationToken ct = default)
    {
        if (!IsValidRoot(root))
        {
            return Array.Empty<ScannedExecutable>();
        }

        return await Task.Run(() =>
        {
            var results = new List<ScannedExecutable>();
            var stack = new Stack<(string Path, int Depth)>();
            stack.Push((root, 0));

            while (stack.Count > 0 && results.Count < MaxFiles)
            {
                ct.ThrowIfCancellationRequested();
                var (folder, depth) = stack.Pop();

                try
                {
                    foreach (var file in Directory.EnumerateFiles(folder, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        ct.ThrowIfCancellationRequested();
                        var name = Path.GetFileNameWithoutExtension(file);
                        var normalized = GameNameNormalizer.Normalize(DeriveName(name));
                        if (normalized.Length > 0)
                        {
                            results.Add(new ScannedExecutable(file, name + ".exe", normalized));
                        }
                        if (results.Count >= MaxFiles)
                        {
                            break;
                        }
                    }

                    if (depth >= MaxDepth)
                    {
                        continue;
                    }

                    foreach (var child in Directory.EnumerateDirectories(folder))
                    {
                        ct.ThrowIfCancellationRequested();
                        var info = new DirectoryInfo(child);
                        if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }
                        if (SkippedFolders.Contains(info.Name))
                        {
                            continue;
                        }
                        stack.Push((child, depth + 1));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // A protected folder is normal on Windows. Skip it.
                }
                catch (DirectoryNotFoundException)
                {
                    // A removable drive may disappear during a scan. Skip it.
                }
                catch (IOException)
                {
                    // A locked or removed entry is not fatal for the scan.
                }
            }

            return (IReadOnlyList<ScannedExecutable>)results;
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Removes common launcher and platform tokens from an executable file name.</summary>
    public static string DeriveName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var spaced = TokenSeparator().Replace(fileName, " ");
        var cleaned = NoiseToken().Replace(spaced, " ");
        return Cleanup().Replace(cleaned, " ").Trim();
    }

    // Launcher, platform, installer and version-like words that are not part of a game name.
    [GeneratedRegex(@"\b(launcher|crashreportclient|unins\d*|setup|install|installer|updater|win64|win32|x64|x86|redistributable|vcredist|directx|dxsetup|redist|helper|legal|readme|patch|beta|demo)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoiseToken();

    [GeneratedRegex(@"[._\-]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenSeparator();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Cleanup();
}
