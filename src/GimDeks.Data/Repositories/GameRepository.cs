using Dapper;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.Data.Repositories;

public sealed class GameRepository : IGameRepository
{
    private readonly SqliteConnectionFactory _factory;

    public GameRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default) =>
        await QueryAsync("SELECT * FROM game ORDER BY urutan, lower(nama);", null, ct);

    public async Task<IReadOnlyList<Game>> GetByStorageAsync(long? storageId, CancellationToken ct = default) =>
        storageId is null
            ? await QueryAsync("SELECT * FROM game WHERE storage_id IS NULL ORDER BY urutan, lower(nama);", null, ct)
            : await QueryAsync("SELECT * FROM game WHERE storage_id = @StorageId ORDER BY urutan, lower(nama);", new { StorageId = storageId }, ct);

    public async Task<IReadOnlyList<Game>> GetByStatusAsync(GameStatus status, CancellationToken ct = default) =>
        await QueryAsync("SELECT * FROM game WHERE status = @Status ORDER BY urutan, lower(nama);", new { Status = status.ToDatabase() }, ct);

    public async Task<Game?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync(new CommandDefinition(
            "SELECT * FROM game WHERE id = @Id;", new { Id = id }, cancellationToken: ct));
        return row is null ? null : Map(row);
    }

    public async Task<long> AddAsync(Game game, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO game (nama, versi, ukuran_gb, status, storage_id, asal_storage, alasan, catatan, urutan, selesai, path_exe, dimainkan_pada, jumlah_main, favorit, rating, prioritas, folder_path, total_detik)
            VALUES (@Nama, @Versi, @UkuranGb, @Status, @StorageId, @AsalStorage, @Alasan, @Catatan, @Urutan, @SelesaiMain, @PathExe, @DimainkanPada, @JumlahMain, @Favorit, @Rating, @Prioritas, @FolderPath, @TotalDetik);
            SELECT last_insert_rowid();
            """;
        using var connection = _factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, Params(game), cancellationToken: ct));
    }

    public async Task<IReadOnlyList<long>> AddBatchAsync(IReadOnlyList<Game> games, CancellationToken ct = default)
    {
        if (games.Count == 0)
        {
            return Array.Empty<long>();
        }

        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var ids = new List<long>(games.Count);
        foreach (var game in games)
        {
            var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                """
                INSERT INTO game (nama, versi, ukuran_gb, status, storage_id, asal_storage, alasan, catatan, urutan, selesai, path_exe, dimainkan_pada, jumlah_main, favorit, rating, prioritas, folder_path, total_detik)
                VALUES (@Nama, @Versi, @UkuranGb, @Status, @StorageId, @AsalStorage, @Alasan, @Catatan, @Urutan, @SelesaiMain, @PathExe, @DimainkanPada, @JumlahMain, @Favorit, @Rating, @Prioritas, @FolderPath, @TotalDetik);
                SELECT last_insert_rowid();
                """,
                Params(game), transaction, cancellationToken: ct));
            ids.Add(id);
        }
        transaction.Commit();
        return ids;
    }

    public async Task UpdateSizesBatchAsync(IReadOnlyList<(long Id, double UkuranGb)> items, CancellationToken ct = default)
    {
        if (items.Count == 0)
        {
            return;
        }

        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var item in items)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE game SET ukuran_gb = @UkuranGb WHERE id = @Id;",
                new { item.Id, item.UkuranGb }, transaction, cancellationToken: ct));
        }
        transaction.Commit();
    }

    public async Task UpdateAsync(Game game, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE game SET nama = @Nama, versi = @Versi, ukuran_gb = @UkuranGb, status = @Status,
                storage_id = @StorageId, asal_storage = @AsalStorage, alasan = @Alasan, catatan = @Catatan, urutan = @Urutan, selesai = @SelesaiMain,
                path_exe = @PathExe, dimainkan_pada = @DimainkanPada, jumlah_main = @JumlahMain,
                favorit = @Favorit, rating = @Rating, prioritas = @Prioritas, folder_path = @FolderPath, total_detik = @TotalDetik
            WHERE id = @Id;
            """;
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, Params(game, game.Id), cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM game WHERE id = @Id;", new { Id = id }, cancellationToken: ct));
    }

    public async Task DeleteAllAsync(CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM game;", cancellationToken: ct));
    }

    public async Task MoveToStorageAsync(IReadOnlyList<long> gameIds, long? targetStorageId, CancellationToken ct = default)
    {
        if (gameIds.Count == 0)
        {
            return;
        }

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        try
        {
            var parameters = new { Ids = gameIds, Target = targetStorageId };
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO riwayat (game_id, dari_storage_id, ke_storage_id)
                SELECT id, storage_id, @Target FROM game WHERE id IN @Ids AND storage_id IS NOT @Target;
                """,
                parameters, transaction, cancellationToken: ct));
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE game SET storage_id = @Target WHERE id IN @Ids;",
                parameters, transaction, cancellationToken: ct));

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ExistsByNameInStorageAsync(string nama, long? storageId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM game
            WHERE lower(nama) = lower(@Nama) AND storage_id IS @StorageId;
            """;
        using var connection = _factory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, new { Nama = nama, StorageId = storageId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<int> CountByStorageAsync(long storageId, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM game WHERE storage_id = @StorageId;", new { StorageId = storageId }, cancellationToken: ct));
    }

    private async Task<IReadOnlyList<Game>> QueryAsync(string sql, object? parameters, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.Select(Map).ToList();
    }

    private static object Params(Game game, long? id = null) => new
    {
        Id = id,
        Nama = game.Nama,
        Versi = game.Versi,
        UkuranGb = game.UkuranGb,
        Status = game.Status.ToDatabase(),
        StorageId = game.StorageId,
        AsalStorage = game.AsalStorage,
        Alasan = game.Alasan,
        Catatan = game.Catatan,
        Urutan = game.Urutan,
        SelesaiMain = game.SelesaiMain ? 1 : 0,
        PathExe = game.PathExe,
        DimainkanPada = game.DimainkanPada?.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
        JumlahMain = game.JumlahMain,
        Favorit = game.Favorit ? 1 : 0,
        Rating = game.Rating,
        Prioritas = game.Prioritas,
        FolderPath = game.FolderPath,
        TotalDetik = game.TotalDetik,
    };

    private static Game Map(dynamic row) => new()
    {
        Id = (long)row.id,
        Nama = (string)row.nama,
        Versi = row.versi is null ? null : (string)row.versi,
        UkuranGb = Convert.ToDouble(row.ukuran_gb, System.Globalization.CultureInfo.InvariantCulture),
        Status = EnumDatabaseNames.GameStatusFromDatabase((string)row.status),
        StorageId = row.storage_id is null ? null : (long)row.storage_id,
        AsalStorage = row.asal_storage is null ? null : (string)row.asal_storage,
        Alasan = row.alasan is null ? null : (string)row.alasan,
        Catatan = row.catatan is null ? null : (string)row.catatan,
        Urutan = (int)row.urutan,
        SelesaiMain = Convert.ToInt64(row.selesai, System.Globalization.CultureInfo.InvariantCulture) != 0,
        DibuatPada = DateTime.TryParse((string)row.dibuat_pada, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var created) ? created : DateTime.UtcNow,
        PathExe = row.path_exe is null ? null : (string)row.path_exe,
        DimainkanPada = row.dimainkan_pada is not null && DateTime.TryParse(
            (string)row.dimainkan_pada, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var played) ? played.ToUniversalTime() : null,
        JumlahMain = row.jumlah_main is null ? 0 : Convert.ToInt32(row.jumlah_main, System.Globalization.CultureInfo.InvariantCulture),
        Favorit = row.favorit is not null && Convert.ToInt64(row.favorit, System.Globalization.CultureInfo.InvariantCulture) != 0,
        Rating = row.rating is null ? 0 : Convert.ToInt32(row.rating, System.Globalization.CultureInfo.InvariantCulture),
        Prioritas = row.prioritas is null ? 0 : Convert.ToInt32(row.prioritas, System.Globalization.CultureInfo.InvariantCulture),
        FolderPath = row.folder_path is null ? null : (string)row.folder_path,
        TotalDetik = row.total_detik is null ? 0 : Convert.ToInt32(row.total_detik, System.Globalization.CultureInfo.InvariantCulture),
    };
}
