using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public interface IDriveLinkRepository
{
    Task<IReadOnlyDictionary<long, string>> GetAllAsync(CancellationToken ct = default);
    Task SetAsync(long storageId, string? driveLetter, CancellationToken ct = default);
}
