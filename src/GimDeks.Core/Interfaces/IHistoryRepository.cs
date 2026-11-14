using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public interface IHistoryRepository
{
    Task<IReadOnlyList<RiwayatEntry>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RiwayatEntry>> GetByGameAsync(long gameId, CancellationToken ct = default);
    Task<IReadOnlyList<RiwayatEntry>> GetByStorageAsync(long storageId, CancellationToken ct = default);
    Task AddAsync(RiwayatEntry entry, CancellationToken ct = default);
}
