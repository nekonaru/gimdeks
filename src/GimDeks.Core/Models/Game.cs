namespace GimDeks.Core.Models;

public sealed class Game
{
    public long Id { get; set; }
    public required string Nama { get; set; }
    public string? Versi { get; set; }
    public double UkuranGb { get; set; }
    public GameStatus Status { get; set; }
    public long? StorageId { get; set; }
    public string? AsalStorage { get; set; }
    public string? Alasan { get; set; }
    public string? Catatan { get; set; }
    public int Urutan { get; set; }
    public DateTime DibuatPada { get; set; }
}
