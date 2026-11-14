using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public interface IStorageRepository
{
    Task<IReadOnlyList<Storage>> GetAllAsync(CancellationToken ct = default);
    Task<Storage?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<long> AddAsync(Storage storage, CancellationToken ct = default);
    Task UpdateAsync(Storage storage, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string nama, long? excludeId = null, CancellationToken ct = default);
}
