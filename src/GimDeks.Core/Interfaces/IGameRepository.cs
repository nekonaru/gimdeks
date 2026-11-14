using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public interface IGameRepository
{
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetByStorageAsync(long? storageId, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetByStatusAsync(GameStatus status, CancellationToken ct = default);
    Task<Game?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddAsync(Game game, CancellationToken ct = default);
    Task UpdateAsync(Game game, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task DeleteAllAsync(CancellationToken ct = default);
    Task MoveToStorageAsync(IReadOnlyList<long> gameIds, long? targetStorageId, CancellationToken ct = default);
    Task<int> CountByStorageAsync(long storageId, CancellationToken ct = default);
    Task<bool> ExistsByNameInStorageAsync(string nama, long? storageId, CancellationToken ct = default);
}
