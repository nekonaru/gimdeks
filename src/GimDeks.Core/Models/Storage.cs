namespace GimDeks.Core.Models;

public sealed class Storage
{
    public long Id { get; set; }
    public required string Nama { get; set; }
    public StorageType Tipe { get; set; }
    public double? KapasitasGb { get; set; }
    public double? BatasGb { get; set; }
    public int Urutan { get; set; }
}
