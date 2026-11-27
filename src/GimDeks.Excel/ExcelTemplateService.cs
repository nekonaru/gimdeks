using GimDeks.Core.Models;

namespace GimDeks.Excel;

/// <summary>Creates an empty workbook in the exact layout GimDeks imports.</summary>
public static class ExcelTemplateService
{
    public static void Create(string filePath)
    {
        var storages = new List<Storage>
        {
            new()
            {
                Nama = "HDD Eksternal 1",
                Tipe = StorageType.HddEksternal,
                KapasitasGb = 1000,
                BatasGb = 900,
                Urutan = 0,
            },
        };
        new ExcelExporter().Export(filePath, storages, Array.Empty<Game>());
    }
}
