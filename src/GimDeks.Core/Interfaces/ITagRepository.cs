using GimDeks.Core.Models;

namespace GimDeks.Core.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, List<Tag>>> GetTagsForGamesAsync(IReadOnlyList<long> gameIds, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetTagsForGameAsync(long gameId, CancellationToken ct = default);
    Task<long> EnsureAsync(string nama, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, long>> EnsureManyAsync(IEnumerable<string> names, CancellationToken ct = default);
    Task SetTagsForGameAsync(long gameId, IReadOnlyList<long> tagIds, CancellationToken ct = default);
    Task SetTagsForGamesBatchAsync(IReadOnlyList<(long GameId, long TagId)> pairs, CancellationToken ct = default);
    Task<IReadOnlyList<long>> GetGameIdsWithTagAsync(long tagId, CancellationToken ct = default);
}
