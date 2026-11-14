using Dapper;

namespace GimDeks.Data.Repositories;

public sealed class ReplaceAllService
{
    private readonly SqliteConnectionFactory _factory;

    public ReplaceAllService(SqliteConnectionFactory factory) => _factory = factory;

    /// <summary>Deletes every game (cascades to riwayat) and every storage, inside one transaction.</summary>
    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM game;", transaction: transaction, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM storage;", transaction: transaction, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM riwayat;", transaction: transaction, cancellationToken: ct));
        transaction.Commit();
    }
}

