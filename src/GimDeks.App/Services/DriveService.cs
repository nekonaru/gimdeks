using System.IO;
using GimDeks.Core.Interfaces;

namespace GimDeks.App.Services;

/// <summary>Lists ready drives and reports free space for the storage links.</summary>
public sealed class DriveService
{
    private readonly IDriveLinkRepository _links;

    public DriveService(IDriveLinkRepository links) => _links = links;

    public sealed record DriveLinkInfo(string Letter, string Label, ulong FreeBytes, ulong TotalBytes)
    {
        public string DisplayText => $"{Letter} ({Label})";
        public string FreeText
        {
            get
            {
                var gb = FreeBytes / 1024.0 / 1024.0 / 1024.0;
                return gb >= 1024 ? $"Kosong {gb / 1024:0.0} TB" : $"Kosong {gb:0.0} GB";
            }
        }
    }

    public IReadOnlyList<DriveLinkInfo> GetReadyDrives()
    {
        return System.IO.DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType is DriveType.Fixed or DriveType.Removable)
            .Select(ToInfo)
            .ToList();
    }

    public DriveLinkInfo? FindDrive(string? letter)
    {
        if (string.IsNullOrWhiteSpace(letter))
        {
            return null;
        }

        var drive = System.IO.DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady && string.Equals(d.Name.TrimEnd('\\', ':'), letter, StringComparison.OrdinalIgnoreCase));
        return drive is null ? null : ToInfo(drive);
    }

    private static DriveLinkInfo ToInfo(System.IO.DriveInfo drive)
    {
        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Tanpa label" : drive.VolumeLabel;
        return new DriveLinkInfo(drive.Name.TrimEnd('\\', ':'), label,
            (ulong)drive.AvailableFreeSpace, (ulong)drive.TotalSize);
    }
}
