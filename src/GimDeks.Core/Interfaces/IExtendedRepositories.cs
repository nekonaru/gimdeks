using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public sealed record PlaySession(long Id, long GameId, string Mulai, string? Selesai, int DurasiDetik, string Sumber);
public sealed record GameNote(long Id, long GameId, string Isi, string DibuatPada);
public sealed record GameImage(long Id, long GameId, string NamaFile, int Urutan);
public sealed record FilterPreset(long Id, string Nama, string DefinisiJson);
public sealed record ImportHistory(long Id, string Path, string Mode, string HasilJson, string Waktu);
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

public interface ICapacitySnapshotRepository
{
    Task<long> AddAsync(long storageId, int jumlahGame, double totalGb, CancellationToken ct = default);
    Task<IReadOnlyList<CapacitySnapshot>> GetAllAsync(CancellationToken ct = default);
}
