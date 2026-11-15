using Dapper;
using GimDeks.Core.Interfaces;

namespace GimDeks.Data.Repositories;

public sealed class DriveLinkRepository : IDriveLinkRepository
{
    private readonly SqliteConnectionFactory _factory;

    public DriveLinkRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyDictionary<long, string>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT storage_id, drive_letter FROM drive_link WHERE drive_letter IS NOT NULL;", cancellationToken: ct));
        var result = new Dictionary<long, string>();
        foreach (var row in rows)
        {
            result[(long)row.storage_id] = (string)row.drive_letter;
        }

        return result;
    }

    public async Task SetAsync(long storageId, string? driveLetter, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        if (driveLetter is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM drive_link WHERE storage_id = @StorageId;", new { StorageId = storageId }, cancellationToken: ct));
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO drive_link (storage_id, drive_letter) VALUES (@StorageId, @DriveLetter)
            ON CONFLICT(storage_id) DO UPDATE SET drive_letter = excluded.drive_letter;
            """,
            new { StorageId = storageId, DriveLetter = driveLetter }, cancellationToken: ct));
    }
}
