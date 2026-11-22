using Dapper;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Data;

namespace GimDeks.Excel;

/// <summary>
/// Orchestrates a full import: parse, automatic backup, then write.
/// The whole write happens in one transaction: either everything lands,
/// or the previous state is left untouched (besides the pre-import backup).
/// </summary>
public sealed class ExcelImportService
{
    private readonly ExcelImporter _importer;
    private readonly SqliteConnectionFactory _factory;
    private readonly IStorageRepository _storageRepository;
    private readonly DatabaseBackupService _backup;
    private readonly object _previewLock = new();
    private string? _cachedPreviewPath;
    private long _cachedPreviewLength;
    private DateTime _cachedPreviewWriteUtc;
    private ImportPreview? _cachedPreview;

    public ExcelImportService(
        ExcelImporter importer,
        SqliteConnectionFactory factory,
        IStorageRepository storageRepository,
        DatabaseBackupService backup)
    {
        _importer = importer;
        _factory = factory;
        _storageRepository = storageRepository;
        _backup = backup;
    }

    public ImportPreview Preview(string filePath)
    {
        var info = new FileInfo(filePath);
        lock (_previewLock)
        {
            if (_cachedPreview is not null
                && string.Equals(_cachedPreviewPath, filePath, StringComparison.OrdinalIgnoreCase)
                && _cachedPreviewLength == info.Length
                && _cachedPreviewWriteUtc == info.LastWriteTimeUtc)
            {
                return _cachedPreview;
            }

            var preview = _importer.Preview(filePath);
            _cachedPreviewPath = filePath;
            _cachedPreviewLength = info.Length;
            _cachedPreviewWriteUtc = info.LastWriteTimeUtc;
            _cachedPreview = preview;
            return preview;
        }
    }

    public async Task<ImportResult> ImportAsync(string filePath, ImportMode mode, CancellationToken ct = default)
    {
        var preview = Preview(filePath);

        // Never write over the user's data without a safety copy first.
        var backupPath = await _backup.BackupAsync(ct);

        var storageIdBySheet = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var storagesAdded = 0;
        if (mode == ImportMode.Gabung)
        {
            var existing = await _storageRepository.GetAllAsync(ct);
            storageIdBySheet = existing.ToDictionary(s => s.Nama, s => s.Id, StringComparer.OrdinalIgnoreCase);
        }

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        try
        {
            if (mode == ImportMode.GantiSemua)
            {
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM game;", transaction: transaction, cancellationToken: ct));
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM storage;", transaction: transaction, cancellationToken: ct));
                await connection.ExecuteAsync(new CommandDefinition("DELETE FROM riwayat;", transaction: transaction, cancellationToken: ct));
            }
            else
            {
                // Gabung: refresh capacity metadata of existing storages.
                foreach (var storagePreview in preview.Storages)
                {
                    if (!storageIdBySheet.TryGetValue(storagePreview.Storage.Nama, out var id))
                    {
                        continue;
                    }

                    var merged = storagePreview.Storage;
                    await connection.ExecuteAsync(new CommandDefinition(
                        "UPDATE storage SET tipe = @Tipe, kapasitas_gb = @KapasitasGb, batas_gb = @BatasGb, urutan = @Urutan WHERE id = @Id;",
                        new { Tipe = merged.Tipe.ToDatabase(), KapasitasGb = merged.KapasitasGb, BatasGb = merged.BatasGb, Urutan = merged.Urutan, Id = id },
                        transaction,
                        cancellationToken: ct));
                }
            }

            foreach (var storagePreview in preview.Storages)
            {
                if (storageIdBySheet.ContainsKey(storagePreview.Storage.Nama))
                {
                    continue; // existing storage already refreshed above (Gabung)
                }

                var storage = storagePreview.Storage;
                var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                    """
                    INSERT INTO storage (nama, tipe, kapasitas_gb, batas_gb, urutan)
                    VALUES (@Nama, @Tipe, @KapasitasGb, @BatasGb, @Urutan);
                    SELECT last_insert_rowid();
                    """,
                    new
                    {
                        Nama = storage.Nama,
                        Tipe = storage.Tipe.ToDatabase(),
                        KapasitasGb = storage.KapasitasGb,
                        BatasGb = storage.BatasGb,
                        Urutan = storage.Urutan,
                    },
                    transaction,
                    cancellationToken: ct));
                storageIdBySheet[storage.Nama] = id;
                storagesAdded++;
            }

            var gamesAdded = 0;
            var gamesSkipped = 0;
            var existingKeys = mode == ImportMode.Gabung
                ? await LoadExistingGameKeysAsync(connection, transaction, ct)
                : new HashSet<(string Nama, long? StorageId)>();
            foreach (var gamePreview in preview.Games)
            {
                var game = gamePreview.Game;
                if (storageIdBySheet.TryGetValue(gamePreview.SheetName, out var storageId))
                {
                    game.StorageId = storageId;
                }

                var key = (game.Nama.ToLowerInvariant(), game.StorageId);
                if (mode == ImportMode.Gabung && !existingKeys.Add(key))
                {
                    gamesSkipped++;
                    continue;
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO game (nama, versi, ukuran_gb, status, storage_id, asal_storage, alasan, catatan, urutan)
                    VALUES (@Nama, @Versi, @UkuranGb, @Status, @StorageId, @AsalStorage, @Alasan, @Catatan, @Urutan);
                    """,
                    new
                    {
                        Nama = game.Nama,
                        Versi = game.Versi,
                        UkuranGb = game.UkuranGb,
                        Status = game.Status.ToDatabase(),
                        StorageId = game.StorageId,
                        AsalStorage = game.AsalStorage,
                        Alasan = game.Alasan,
                        Catatan = game.Catatan,
                        Urutan = game.Urutan,
                    },
                    transaction,
                    cancellationToken: ct));
                gamesAdded++;
            }

            transaction.Commit();

            return new ImportResult
            {
                StoragesAdded = storagesAdded,
                GamesAdded = gamesAdded,
                GamesSkippedAsDuplicate = gamesSkipped,
                Ringkasan = preview.Ringkasan,
                BackupPath = backupPath,
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task<HashSet<(string Nama, long? StorageId)>> LoadExistingGameKeysAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken ct)
    {
        var rows = await connection.QueryAsync(new CommandDefinition(
            "SELECT CAST(nama AS TEXT) AS Nama, storage_id AS StorageId FROM game;",
            transaction: transaction,
            cancellationToken: ct));
        var keys = new HashSet<(string Nama, long? StorageId)>();
        foreach (var row in rows)
        {
            var values = (IDictionary<string, object?>)row;
            var rawName = values["Nama"];
            var name = rawName is string text ? text
                : rawName is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes)
                : rawName?.ToString() ?? "";
            var rawStorage = values["StorageId"];
            long? storageId = rawStorage is long l ? l : rawStorage is int i ? i : null;
            keys.Add((name.ToLowerInvariant(), storageId));
        }

        return keys;
    }
}
