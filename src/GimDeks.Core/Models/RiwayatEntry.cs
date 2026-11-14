namespace GimDeks.Core.Models;

public sealed class RiwayatEntry
{
    public long Id { get; set; }
    public long GameId { get; set; }
    public long? DariStorageId { get; set; }
    public long? KeStorageId { get; set; }
    public DateTime Waktu { get; set; }
}
