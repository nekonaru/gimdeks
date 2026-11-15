namespace GimDeks.Core.Models;

public sealed class Tag
{
    public long Id { get; set; }
    public required string Nama { get; set; }
}

public sealed class DriveLink
{
    public long StorageId { get; set; }
    public string? DriveLetter { get; set; }
}
