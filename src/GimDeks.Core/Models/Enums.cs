namespace GimDeks.Core.Models;

/// <summary>Storage type values persisted in the database.</summary>
public enum StorageType
{
    HddEksternal,
    LaptopHdd,
    LaptopSsd,
    Virtual,
}

/// <summary>Game status values persisted in the database.</summary>
public enum GameStatus
{
    BelumDownload,
    SudahAda,
    Dikecualikan,
    Dieliminasi,
}

public static class EnumDatabaseNames
{
    public static string ToDatabase(this StorageType type) => type switch
    {
        StorageType.HddEksternal => "HDD_EKSTERNAL",
        StorageType.LaptopHdd => "LAPTOP_HDD",
        StorageType.LaptopSsd => "LAPTOP_SSD",
        StorageType.Virtual => "VIRTUAL",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static StorageType StorageTypeFromDatabase(string value) => value switch
    {
        "HDD_EKSTERNAL" => StorageType.HddEksternal,
        "LAPTOP_HDD" => StorageType.LaptopHdd,
        "LAPTOP_SSD" => StorageType.LaptopSsd,
        "VIRTUAL" => StorageType.Virtual,
        _ => throw new FormatException($"Unknown storage type: {value}"),
    };

    public static string ToDatabase(this GameStatus status) => status switch
    {
        GameStatus.BelumDownload => "BELUM_DOWNLOAD",
        GameStatus.SudahAda => "SUDAH_ADA",
        GameStatus.Dikecualikan => "DIKECUALIKAN",
        GameStatus.Dieliminasi => "DIELIMINASI",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static GameStatus GameStatusFromDatabase(string value) => value switch
    {
        "BELUM_DOWNLOAD" => GameStatus.BelumDownload,
        "SUDAH_ADA" => GameStatus.SudahAda,
        "DIKECUALIKAN" => GameStatus.Dikecualikan,
        "DIELIMINASI" => GameStatus.Dieliminasi,
        _ => throw new FormatException($"Unknown game status: {value}"),
    };
}
