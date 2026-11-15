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
        return target;
    }

    /// <summary>Disable portable mode: move the portable data back to the user profile.</summary>
    public async Task<string> DisableAsync()
    {
        if (!IsPortable)
        {
            throw new InvalidOperationException(Services.Loc.Get("Ui_Portable_AlreadyOff"));
        }

        var source = PortableDirectory;
        var target = UserProfileDirectory;
        await CopyDirectoryAsync(source, target);
        Directory.Delete(source, recursive: true);
        return target;
    }

    private static async Task CopyDirectoryAsync(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            Directory.CreateDirectory(target);
            return;
        }

        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await Task.Run(() => File.Copy(file, destination, overwrite: true));
        }
    }
}
