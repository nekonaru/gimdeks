using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;
using GimDeks.Data;
using GimDeks.Data.Repositories;

namespace GimDeks.Excel;

/// <summary>
/// Orchestrates a full import: parse, automatic backup, then write.
/// Destructive only when the caller picks GantiSemua; the backup always
/// happens first so the previous state can be restored.
/// </summary>
public sealed class ExcelImportService
{
    private readonly ExcelImporter _importer;
    private readonly IGameRepository _gameRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly ReplaceAllService _replaceAll;
    private readonly DatabaseBackupService _backup;

    public ExcelImportService(
        ExcelImporter importer,
        IGameRepository gameRepository,
        IStorageRepository storageRepository,
        ReplaceAllService replaceAll,
        DatabaseBackupService backup)
    {
        _importer = importer;
        _gameRepository = gameRepository;
        _storageRepository = storageRepository;
        _replaceAll = replaceAll;
        _backup = backup;
    }

    public ImportPreview Preview(string filePath) => _importer.Preview(filePath);

    public async Task<ImportResult> ImportAsync(string filePath, ImportMode mode, CancellationToken ct = default)
    {
        var preview = _importer.Preview(filePath);

        // Never write over the user's data without a safety copy first.
        var backupPath = await _backup.BackupAsync(ct);

        if (mode == ImportMode.GantiSemua)
        {
            await _replaceAll.ClearAllAsync(ct);
        }

        var storageIdBySheet = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var storagesAdded = 0;
        if (mode == ImportMode.Gabung)
        {
            var existing = await _storageRepository.GetAllAsync(ct);
            storageIdBySheet = existing.ToDictionary(s => s.Nama, s => s.Id, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var storagePreview in preview.Storages)
        {
            if (storageIdBySheet.TryGetValue(storagePreview.Storage.Nama, out var existingId))
            {
                // Gabung: refresh capacity metadata, keep identity stable.
                var merged = storagePreview.Storage;
                merged.Id = existingId;
                await _storageRepository.UpdateAsync(merged, ct);
                continue;
            }

            var id = await _storageRepository.AddAsync(storagePreview.Storage, ct);
            storageIdBySheet[storagePreview.Storage.Nama] = id;
            storagesAdded++;
        }

        var gamesAdded = 0;
        var gamesSkipped = 0;
        foreach (var gamePreview in preview.Games)
        {
            var game = gamePreview.Game;
            if (storageIdBySheet.TryGetValue(gamePreview.SheetName, out var storageId))
            {
                game.StorageId = storageId;
            }

            if (mode == ImportMode.Gabung &&
                await _gameRepository.ExistsByNameInStorageAsync(game.Nama, game.StorageId, ct))
            {
                gamesSkipped++;
                continue;
            }

            await _gameRepository.AddAsync(game, ct);
            gamesAdded++;
        }

        return new ImportResult
        {
            StoragesAdded = storagesAdded,
            GamesAdded = gamesAdded,
            GamesSkippedAsDuplicate = gamesSkipped,
            Ringkasan = preview.Ringkasan,
            BackupPath = backupPath,
        };
    }
}
