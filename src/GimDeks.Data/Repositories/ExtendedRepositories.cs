using Dapper;
using GimDeks.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace GimDeks.Data.Repositories;

public sealed class PlaySessionRepository(SqliteConnectionFactory factory) : IPlaySessionRepository
{
    private static string Stamp(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

    public async Task<IReadOnlyList<PlaySession>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM sesi_main ORDER BY mulai DESC;", cancellationToken: ct));
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PlaySession>> GetByGameAsync(long gameId, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM sesi_main WHERE game_id = @GameId ORDER BY mulai DESC;", new { GameId = gameId }, cancellationToken: ct));
        return rows.Select(Map).ToList();
    }

    public async Task<long> StartAsync(long gameId, string source, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO sesi_main (game_id, mulai, sumber) VALUES (@GameId, @Mulai, @Sumber); SELECT last_insert_rowid();",
            new { GameId = gameId, Mulai = Stamp(DateTime.UtcNow), Sumber = source }, cancellationToken: ct));
    }

    public async Task EndAsync(long sessionId, int durasiDetik, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE sesi_main SET selesai = @Selesai, durasi_detik = @Durasi WHERE id = @Id;",
            new { Selesai = Stamp(DateTime.UtcNow), Durasi = durasiDetik, Id = sessionId }, cancellationToken: ct));
    }

    public async Task CloseOpenSessionsAsync(CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE sesi_main
            SET selesai = datetime('now'),
                durasi_detik = MAX(0, CAST(strftime('%s', datetime('now')) - strftime('%s', mulai) AS INTEGER))
            WHERE selesai IS NULL;
            """, cancellationToken: ct));
    }

    public async Task<Dictionary<long, int>> GetTotalsAsync(CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT game_id AS GameId, SUM(durasi_detik) AS Total FROM sesi_main GROUP BY game_id;", cancellationToken: ct));
        return rows.ToDictionary<dynamic, long, int>(
            r => (long)r.GameId,
            r => r.Total is null ? 0 : Convert.ToInt32((object)r.Total, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static PlaySession Map(dynamic row) => new(
        (long)row.id,
        (long)row.game_id,
        (string)row.mulai,
        row.selesai is null ? null : (string)row.selesai,
        row.durasi_detik is null ? 0 : Convert.ToInt32(row.durasi_detik, System.Globalization.CultureInfo.InvariantCulture),
        (string)row.sumber);
}

public sealed class GameNoteRepository(SqliteConnectionFactory factory) : IGameNoteRepository
{
    public async Task<IReadOnlyList<GameNote>> GetByGameAsync(long gameId, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM catatan_game WHERE game_id = @GameId ORDER BY id DESC;", new { GameId = gameId }, cancellationToken: ct));
        return rows.Select(r => new GameNote((long)r.id, (long)r.game_id, (string)r.isi, (string)r.dibuat_pada)).ToList();
    }

    public async Task<long> AddAsync(long gameId, string isi, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO catatan_game (game_id, isi) VALUES (@GameId, @Isi); SELECT last_insert_rowid();",
            new { GameId = gameId, Isi = isi }, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM catatan_game WHERE id = @Id;", new { Id = id }, cancellationToken: ct));
    }
}

public sealed class GameImageRepository(SqliteConnectionFactory factory) : IGameImageRepository
{
    public async Task<IReadOnlyList<GameImage>> GetByGameAsync(long gameId, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM gambar_game WHERE game_id = @GameId ORDER BY urutan, id;", new { GameId = gameId }, cancellationToken: ct));
        return rows.Select(r => new GameImage((long)r.id, (long)r.game_id, (string)r.nama_file,
            r.urutan is null ? 0 : Convert.ToInt32(r.urutan, System.Globalization.CultureInfo.InvariantCulture))).ToList();
    }

    public async Task<long> AddAsync(long gameId, string namaFile, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO gambar_game (game_id, nama_file, urutan) VALUES (@GameId, @NamaFile, (SELECT COALESCE(MAX(urutan), -1) + 1 FROM gambar_game WHERE game_id = @GameId)); SELECT last_insert_rowid();",
            new { GameId = gameId, NamaFile = namaFile }, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM gambar_game WHERE id = @Id;", new { Id = id }, cancellationToken: ct));
    }
}

public sealed class FilterPresetRepository(SqliteConnectionFactory factory) : IFilterPresetRepository
{
    public async Task<IReadOnlyList<FilterPreset>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM preset_filter ORDER BY nama;", cancellationToken: ct));
        return rows.Select(r => new FilterPreset((long)r.id, (string)r.nama, (string)r.definisi_json)).ToList();
    }

    public async Task<long> SaveAsync(string nama, string definisiJson, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO preset_filter (nama, definisi_json) VALUES (@Nama, @Json) ON CONFLICT(nama) DO UPDATE SET definisi_json = excluded.definisi_json; SELECT id FROM preset_filter WHERE nama = @Nama;",
            new { Nama = nama, Json = definisiJson }, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM preset_filter WHERE id = @Id;", new { Id = id }, cancellationToken: ct));
    }
}

public sealed class ImportHistoryRepository(SqliteConnectionFactory factory) : IImportHistoryRepository
{
    public async Task<IReadOnlyList<ImportHistory>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM histori_import ORDER BY waktu DESC;", cancellationToken: ct));
        return rows.Select(r => new ImportHistory((long)r.id, (string)r.path, (string)r.mode, (string)r.hasil_json, (string)r.waktu)).ToList();
    }

    public async Task<long> AddAsync(string path, string mode, string hasilJson, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO histori_import (path, mode, hasil_json) VALUES (@Path, @Mode, @Json); SELECT last_insert_rowid();",
            new { Path = path, Mode = mode, Json = hasilJson }, cancellationToken: ct));
    }
}

public sealed class CapacitySnapshotRepository(SqliteConnectionFactory factory) : ICapacitySnapshotRepository
{
    public async Task<long> AddAsync(long storageId, int jumlahGame, double totalGb, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO snapshot_kapasitas (storage_id, tanggal, jumlah_game, total_gb) VALUES (@StorageId, datetime('now'), @JumlahGame, @TotalGb); SELECT last_insert_rowid();",
            new { StorageId = storageId, JumlahGame = jumlahGame, TotalGb = totalGb }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CapacitySnapshot>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT * FROM snapshot_kapasitas ORDER BY tanggal DESC;", cancellationToken: ct));
        return rows.Select(r => new CapacitySnapshot((long)r.id, (long)r.storage_id, (string)r.tanggal,
            (int)r.jumlah_game, Convert.ToDouble(r.total_gb, System.Globalization.CultureInfo.InvariantCulture))).ToList();
    }
}

