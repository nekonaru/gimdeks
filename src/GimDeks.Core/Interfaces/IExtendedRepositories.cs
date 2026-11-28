using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public sealed record PlaySession(long Id, long GameId, string Mulai, string? Selesai, int DurasiDetik, string Sumber);
public sealed record GameNote(long Id, long GameId, string Isi, string DibuatPada);
public sealed record GameImage(long Id, long GameId, string NamaFile, int Urutan);
public sealed record FilterPreset(long Id, string Nama, string DefinisiJson);
public sealed record ImportHistory(long Id, string Path, string Mode, string HasilJson, string Waktu);
public sealed record ActivityEntry(long Id, string Waktu, string Aksi, long? GameId, string Detail);
public sealed record LaunchProfile(long Id, long GameId, string Nama, string PathExe, string? Argumen, int Urutan);
public sealed record CustomField(long Id, string Nama, string Tipe, int Urutan);
public sealed record CustomValue(long GameId, long FieldId, string? Nilai);
public sealed record AutomationRule(long Id, string Nama, string Field, string Operator, string Nilai, string Aksi, string? Argumen, bool Aktif, int Urutan);
public sealed record SaveGameEntry(long Id, long GameId, string Path, string DibuatPada);
public sealed record ModEntry(long Id, long GameId, string Nama, string Path, bool Aktif, string? Catatan);
public sealed record CapacitySnapshot(long Id, long StorageId, string Tanggal, int JumlahGame, double TotalGb);

public interface IPlaySessionRepository
{
    Task<IReadOnlyList<PlaySession>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PlaySession>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<long> StartAsync(long gameId, string source, CancellationToken ct = default);
    Task EndAsync(long sessionId, int durasiDetik, CancellationToken ct = default);
    Task CloseOpenSessionsAsync(CancellationToken ct = default);
    Task<Dictionary<long, int>> GetTotalsAsync(CancellationToken ct = default);
}

public interface IGameNoteRepository
{
    Task<IReadOnlyList<GameNote>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<Dictionary<long, string>> GetAllTextByGameAsync(CancellationToken ct = default);
    Task<long> AddAsync(long gameId, string isi, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IGameImageRepository
{
    Task<IReadOnlyList<GameImage>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<long> AddAsync(long gameId, string namaFile, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IFilterPresetRepository
{
    Task<IReadOnlyList<FilterPreset>> GetAllAsync(CancellationToken ct = default);
    Task<long> SaveAsync(string nama, string definisiJson, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IImportHistoryRepository
{
    Task<IReadOnlyList<ImportHistory>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(string path, string mode, string hasilJson, CancellationToken ct = default);
}

public interface IAutomationRuleRepository
{
    Task<IReadOnlyList<AutomationRule>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(AutomationRule rule, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool active, CancellationToken ct = default);
}

public interface ISaveGameRepository
{
    Task<IReadOnlyList<SaveGameEntry>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<long> AddAsync(long gameId, string path, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IModRepository
{
    Task<IReadOnlyList<ModEntry>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<long> AddAsync(long gameId, string nama, string path, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool active, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IActivityRepository
{
    Task<IReadOnlyList<ActivityEntry>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<long> AddAsync(string aksi, long? gameId, string detail, CancellationToken ct = default);
}

public interface ILaunchProfileRepository
{
    Task<IReadOnlyList<LaunchProfile>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<long> AddAsync(long gameId, string nama, string pathExe, string? argumen, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ICustomFieldRepository
{
    Task<IReadOnlyList<CustomField>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(string nama, string tipe, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, string?>> GetValuesAsync(long gameId, CancellationToken ct = default);
    Task SetValuesAsync(long gameId, IReadOnlyDictionary<long, string?> values, CancellationToken ct = default);
}

public interface ICapacitySnapshotRepository
{
    Task<long> AddAsync(long storageId, int jumlahGame, double totalGb, CancellationToken ct = default);
    Task AddBatchAsync(IReadOnlyList<(long StorageId, int JumlahGame, double TotalGb)> items, CancellationToken ct = default);
    Task<IReadOnlyList<CapacitySnapshot>> GetAllAsync(CancellationToken ct = default);
}
