namespace GimDeks.Core.Models;

/// <summary>
/// A local executable found by a drive scan. The path is always a file that
/// exists on disk at scan time; nothing is launched from this record.
/// </summary>
public sealed record ScannedExecutable(string Path, string FileName, string NormalizedName);
