using Dapper;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.Data.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly SqliteConnectionFactory _factory;

    public TagRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT id, nama FROM tag ORDER BY lower(nama);", cancellationToken: ct));
        return rows.Select(r => new Tag { Id = (long)r.id, Nama = (string)r.nama }).ToList();
    }

    public async Task<IReadOnlyDictionary<long, List<Tag>>> GetTagsForGamesAsync(
        IReadOnlyList<long> gameIds, CancellationToken ct = default)
    {
        var result = new Dictionary<long, List<Tag>>();
        if (gameIds.Count == 0)
        {
            return result;
        }

        const string sql = """
            SELECT gt.game_id, t.id, t.nama
            FROM game_tag gt
            JOIN tag t ON t.id = gt.tag_id
            WHERE gt.game_id IN @GameIds
            ORDER BY lower(t.nama);
            """;
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { GameIds = gameIds }, cancellationToken: ct));
        foreach (var row in rows)
        {
            var gameId = (long)row.game_id;
            if (!result.TryGetValue(gameId, out var list))
            {
                list = new List<Tag>();
                result[gameId] = list;
            }

            list.Add(new Tag { Id = (long)row.id, Nama = (string)row.nama });
        }

        return result;
    }

    public async Task<IReadOnlyList<Tag>> GetTagsForGameAsync(long gameId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT t.id, t.nama
            FROM tag t
            JOIN game_tag gt ON gt.tag_id = t.id
            WHERE gt.game_id = @GameId
            ORDER BY lower(t.nama);
            """;
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { GameId = gameId }, cancellationToken: ct));
        return rows.Select(r => new Tag { Id = (long)r.id, Nama = (string)r.nama }).ToList();
    }

    public async Task<long> EnsureAsync(string nama, CancellationToken ct = default)
    {
        var normalized = nama.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Nama tag kosong.", nameof(nama));
        }

        using var connection = _factory.CreateConnection();
        var existing = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            "SELECT id FROM tag WHERE lower(nama) = lower(@Nama);", new { Nama = normalized }, cancellationToken: ct));
        if (existing is { } id)
        {
            return id;
        }

        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO tag (nama) VALUES (@Nama); SELECT last_insert_rowid();",
            new { Nama = normalized }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<long>> GetGameIdsWithTagAsync(long tagId, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync<long>(new CommandDefinition(
            "SELECT game_id FROM game_tag WHERE tag_id = @TagId;", new { TagId = tagId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task SetTagsForGameAsync(long gameId, IReadOnlyList<long> tagIds, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM game_tag WHERE game_id = @GameId;", new { GameId = gameId }, transaction, cancellationToken: ct));
        foreach (var tagId in tagIds.Distinct())
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT OR IGNORE INTO game_tag (game_id, tag_id) VALUES (@GameId, @TagId);",
                new { GameId = gameId, TagId = tagId }, transaction, cancellationToken: ct));
        }

        transaction.Commit();
    }
}
