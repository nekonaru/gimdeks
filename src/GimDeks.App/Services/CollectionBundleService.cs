using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using GimDeks.Data;

namespace GimDeks.App.Services;

/// <summary>
/// Exports the whole offline collection as one zip with a checksum manifest,
/// and imports it back after verifying every entry. No network involved.
/// </summary>
public sealed class CollectionBundleService
{
    private const string ManifestName = "manifest.txt";

    public string Export(string destination)
    {
        var source = DatabasePaths.AppDataDirectory;
        var entries = new List<(string Name, string Path)>();
        var database = Path.Combine(source, "gimdeks.db");
        var settings = DatabasePaths.ResolveSettingsPath();
        if (File.Exists(database))
        {
            entries.Add(("gimdeks.db", database));
        }
        if (File.Exists(settings))
        {
            entries.Add(("settings.json", settings));
        }
        var covers = Path.Combine(source, "covers");
        if (Directory.Exists(covers))
        {
            foreach (var file in Directory.GetFiles(covers, "*", SearchOption.AllDirectories))
            {
                var name = "covers/" + Path.GetRelativePath(covers, file).Replace('\\', '/');
                entries.Add((name, file));
            }
        }

        var manifest = new List<string>();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        using var zip = ZipFile.Open(destination, ZipArchiveMode.Create);
        foreach (var entry in entries)
        {
            using var stream = File.OpenRead(entry.Path);
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            manifest.Add($"{hash}  {entry.Name}");
            var archiveEntry = zip.CreateEntry(entry.Name, CompressionLevel.Optimal);
            using var output = archiveEntry.Open();
            stream.Position = 0;
            stream.CopyTo(output);
        }
        var manifestEntry = zip.CreateEntry(ManifestName);
        using var writer = new StreamWriter(manifestEntry.Open());
        foreach (var line in manifest)
        {
            writer.WriteLine(line);
        }
        return destination;
    }

    public (bool Ok, string Message) Verify(string bundlePath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(bundlePath);
            var manifestEntry = zip.GetEntry(ManifestName);
            if (manifestEntry is null)
            {
                return (false, Loc.Get("Ui_Bundle_NoManifest"));
            }
            using var reader = new StreamReader(manifestEntry.Open());
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                var parts = line.Split("  ", 2);
                if (parts.Length != 2)
                {
                    return (false, Loc.Get("Ui_Bundle_BadManifest"));
                }
                var entry = zip.GetEntry(parts[1]);
                if (entry is null)
                {
                    return (false, Loc.Format("Ui_Bundle_Missing", parts[1]));
                }
                using var stream = entry.Open();
                var hash = Convert.ToHexString(SHA256.HashData(stream));
                if (!string.Equals(hash, parts[0], StringComparison.OrdinalIgnoreCase))
                {
                    return (false, Loc.Format("Ui_Bundle_HashMismatch", parts[1]));
                }
            }
            return (true, Loc.Get("Ui_Bundle_Verified"));
        }
        catch (Exception exception)
        {
            return (false, exception.Message);
        }
    }

    public string Import(string bundlePath)
    {
        var verification = Verify(bundlePath);
        if (!verification.Ok)
        {
            throw new InvalidDataException(verification.Message);
        }

        var target = DatabasePaths.AppDataDirectory;
        Directory.CreateDirectory(target);
        using var zip = ZipFile.OpenRead(bundlePath);
        foreach (var entry in zip.Entries)
        {
            if (entry.FullName == ManifestName || string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }
            var destination = Path.Combine(target, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var source = entry.Open();
            using var output = File.Create(destination);
            source.CopyTo(output);
        }
        return target;
    }
}
