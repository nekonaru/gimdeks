using System.IO;
using GimDeks.Data;

namespace GimDeks.App.Services;

/// <summary>
/// Moves the app's data folder between the user profile (LOCALAPPDATA)
/// and a "data" folder next to the executable (portable mode), so the
/// catalog can travel with an external HDD.
/// </summary>
public sealed class PortableModeService
{
    public bool IsPortable => DatabasePaths.PortableRoot is not null;

    public string PortableDirectory
    {
        get
        {
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
            if (string.IsNullOrEmpty(exeDir))
            {
                throw new InvalidOperationException(Services.Loc.Get("Ui_Portable_ExeMissing"));
            }

            return Path.Combine(exeDir, "data");
        }
    }

    private static string UserProfileDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GimDeks");

    private string CurrentRoot => DatabasePaths.AppDataDirectory;

    /// <summary>Enable portable mode: copy the user-profile data next to the exe.</summary>
    public async Task<string> EnableAsync()
    {
        if (IsPortable)
        {
            throw new InvalidOperationException(Services.Loc.Get("Ui_Portable_AlreadyOn"));
        }

        var target = PortableDirectory;
        if (Directory.Exists(target) && Directory.GetFileSystemEntries(target).Length > 0)
        {
            throw new InvalidOperationException(Services.Loc.Get("Ui_Portable_TargetOccupied"));
        }

        await CopyDirectoryAsync(CurrentRoot, target);
        VerifyCopy(CurrentRoot, target);
        return target;
    }

    /// <summary>Disable portable mode: move the portable data back to the user profile.
    /// The existing profile copy is kept as a timestamped sibling backup until the copy succeeds.</summary>
    public async Task<string> DisableAsync()
    {
        if (!IsPortable)
        {
            throw new InvalidOperationException(Services.Loc.Get("Ui_Portable_AlreadyOff"));
        }

        var source = PortableDirectory;
        var target = UserProfileDirectory;
        string? backup = null;
        if (Directory.Exists(target) && Directory.GetFileSystemEntries(target).Length > 0)
        {
            backup = target + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            Directory.Move(target, backup);
        }

        try
        {
            await CopyDirectoryAsync(source, target);
            VerifyCopy(source, target);
            Directory.Delete(source, recursive: true);
            if (backup is not null)
            {
                TryDeleteDirectory(backup);
            }

            return target;
        }
        catch
        {
            try
            {
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                }
            }
            catch
            {
                // Best effort; the backup restore below is what matters.
            }

            if (backup is not null && Directory.Exists(backup))
            {
                try
                {
                    Directory.Move(backup, target);
                }
                catch
                {
                    // The backup remains on disk next to the profile folder.
                }
            }

            throw;
        }
    }

    private static void VerifyCopy(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        var sourceFiles = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        var targetFiles = Directory.Exists(target)
            ? Directory.GetFiles(target, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();
        if (sourceFiles.Length != targetFiles.Length)
        {
            throw new IOException($"Portable mode copy verification failed: expected {sourceFiles.Length} files, wrote {targetFiles.Length}.");
        }

        var sourceBytes = sourceFiles.Sum(file => new FileInfo(file).Length);
        var targetBytes = targetFiles.Sum(file => new FileInfo(file).Length);
        if (sourceBytes != targetBytes)
        {
            throw new IOException("Portable mode copy verification failed: file sizes differ.");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // A locked backup is better left behind than crashing the move.
        }
    }

    private static async Task CopyDirectoryAsync(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            Directory.CreateDirectory(target);
            return;
        }

        var sourceRoot = Path.GetFullPath(source);
        var targetRoot = Path.GetFullPath(target);
        Directory.CreateDirectory(targetRoot);

        var files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var fullFile = Path.GetFullPath(file);
            if (!fullFile.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Portable mode copy stopped: a path outside the source folder was found.");
            }

            var relative = Path.GetRelativePath(sourceRoot, fullFile);
            var destination = Path.GetFullPath(Path.Combine(targetRoot, relative));
            if (!destination.StartsWith(targetRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Portable mode copy stopped: a path outside the target folder was found.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await Task.Run(() => File.Copy(fullFile, destination, overwrite: true));
        }
    }
}
