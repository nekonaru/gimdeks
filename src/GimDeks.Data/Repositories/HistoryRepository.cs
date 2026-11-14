using Dapper;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.Data.Repositories;

public sealed class HistoryRepository : IHistoryRepository
{
    private readonly SqliteConnectionFactory _factory;

    public HistoryRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<RiwayatEntry>> GetAllAsync(CancellationToken ct = default) =>
        await QueryAsync("SELECT * FROM riwayat ORDER BY waktu DESC, id DESC;", null, ct);

    public async Task<IReadOnlyList<RiwayatEntry>> GetByGameAsync(long gameId, CancellationToken ct = default) =>
        await QueryAsync("SELECT * FROM riwayat WHERE game_id = @GameId ORDER BY waktu DESC, id DESC;", new { GameId = gameId }, ct);

    public async Task<IReadOnlyList<RiwayatEntry>> GetByStorageAsync(long storageId, CancellationToken ct = default) =>
        await QueryAsync("SELECT * FROM riwayat WHERE dari_storage_id = @StorageId OR ke_storage_id = @StorageId ORDER BY waktu DESC, id DESC;", new { StorageId = storageId }, ct);

    public async Task AddAsync(RiwayatEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO riwayat (game_id, dari_storage_id, ke_storage_id)
            VALUES (@GameId, @DariStorageId, @KeStorageId);
            """;
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            GameId = entry.GameId,
            DariStorageId = entry.DariStorageId,
            KeStorageId = entry.KeStorageId,
        }, cancellationToken: ct));
    }

    private async Task<IReadOnlyList<RiwayatEntry>> QueryAsync(string sql, object? parameters, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.Select(Map).ToList();
    }

    private static RiwayatEntry Map(dynamic row) => new()
    {
        Id = (long)row.id,
        GameId = (long)row.game_id,
        DariStorageId = row.dari_storage_id is null ? null : (long)row.dari_storage_id,
        KeStorageId = row.ke_storage_id is null ? null : (long)row.ke_storage_id,
        Waktu = DateTime.TryParse((string)row.waktu, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var waktu) ? waktu : DateTime.UtcNow,
    };
}
