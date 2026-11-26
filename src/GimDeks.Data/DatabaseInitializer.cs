using Microsoft.Data.Sqlite;

namespace GimDeks.Data;

/// <summary>
/// Applies the schema in order and tracks the applied version in PRAGMA user_version.
/// New migrations are appended to the list; existing ones never change.
/// </summary>
public sealed class DatabaseInitializer
{
    private static readonly string[] Migrations =
    {
        // 1: initial schema (storage, game, riwayat)
        """
        CREATE TABLE storage (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          nama TEXT NOT NULL UNIQUE,
          tipe TEXT NOT NULL,
          kapasitas_gb REAL,
          batas_gb REAL,
          urutan INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE game (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          nama TEXT NOT NULL,
          versi TEXT,
          ukuran_gb REAL NOT NULL DEFAULT 0,
          status TEXT NOT NULL,
          storage_id INTEGER REFERENCES storage(id),
          asal_storage TEXT,
          alasan TEXT,
          catatan TEXT,
          urutan INTEGER NOT NULL DEFAULT 0,
          dibuat_pada TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE INDEX ix_game_storage ON game(storage_id);
        CREATE INDEX ix_game_status ON game(status);
        CREATE INDEX ix_game_nama_norm ON game(lower(nama));

        CREATE TABLE riwayat (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          game_id INTEGER NOT NULL REFERENCES game(id) ON DELETE CASCADE,
          dari_storage_id INTEGER REFERENCES storage(id),
          ke_storage_id INTEGER REFERENCES storage(id),
          waktu TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE INDEX ix_riwayat_game ON riwayat(game_id);
        """,

        // 2: per-game completion flag and tags
        """
        ALTER TABLE game ADD COLUMN selesai INTEGER NOT NULL DEFAULT 0;

        CREATE TABLE tag (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          nama TEXT NOT NULL UNIQUE
        );

        CREATE TABLE game_tag (
          game_id INTEGER NOT NULL REFERENCES game(id) ON DELETE CASCADE,
          tag_id INTEGER NOT NULL REFERENCES tag(id) ON DELETE CASCADE,
          PRIMARY KEY (game_id, tag_id)
        );

        CREATE INDEX ix_game_tag_tag ON game_tag(tag_id);

        CREATE TABLE drive_link (
          storage_id INTEGER PRIMARY KEY REFERENCES storage(id) ON DELETE CASCADE,
          drive_letter TEXT
        );
        """,

        // 3: local launch path and play log
        """
        ALTER TABLE game ADD COLUMN path_exe TEXT;
        ALTER TABLE game ADD COLUMN dimainkan_pada TEXT;
        ALTER TABLE game ADD COLUMN jumlah_main INTEGER NOT NULL DEFAULT 0;
        """,

        // 4: favorites, rating, play sessions, notes, gallery, presets, history and snapshots
        """
        ALTER TABLE game ADD COLUMN favorit INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE game ADD COLUMN rating INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE game ADD COLUMN prioritas INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE game ADD COLUMN folder_path TEXT;
        ALTER TABLE game ADD COLUMN total_detik INTEGER NOT NULL DEFAULT 0;

        CREATE TABLE sesi_main (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          game_id INTEGER NOT NULL REFERENCES game(id) ON DELETE CASCADE,
          mulai TEXT NOT NULL,
          selesai TEXT,
          durasi_detik INTEGER NOT NULL DEFAULT 0,
          sumber TEXT NOT NULL DEFAULT 'launcher'
        );
        CREATE INDEX ix_sesi_main_game ON sesi_main(game_id);
        CREATE INDEX ix_sesi_main_mulai ON sesi_main(mulai);

        CREATE TABLE catatan_game (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          game_id INTEGER NOT NULL REFERENCES game(id) ON DELETE CASCADE,
          isi TEXT NOT NULL,
          dibuat_pada TEXT NOT NULL DEFAULT (datetime('now'))
        );
        CREATE INDEX ix_catatan_game_game ON catatan_game(game_id);

        CREATE TABLE gambar_game (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          game_id INTEGER NOT NULL REFERENCES game(id) ON DELETE CASCADE,
          nama_file TEXT NOT NULL,
          urutan INTEGER NOT NULL DEFAULT 0
        );
        CREATE INDEX ix_gambar_game_game ON gambar_game(game_id);

        CREATE TABLE preset_filter (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          nama TEXT NOT NULL UNIQUE,
          definisi_json TEXT NOT NULL
        );

        CREATE TABLE histori_import (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          path TEXT NOT NULL,
          mode TEXT NOT NULL,
          hasil_json TEXT NOT NULL,
          waktu TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE snapshot_kapasitas (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          storage_id INTEGER NOT NULL REFERENCES storage(id) ON DELETE CASCADE,
          tanggal TEXT NOT NULL,
          jumlah_game INTEGER NOT NULL,
          total_gb REAL NOT NULL
        );
        CREATE INDEX ix_snapshot_storage_tanggal ON snapshot_kapasitas(storage_id, tanggal);
        """,
    };

    private readonly SqliteConnectionFactory _factory;

    public DatabaseInitializer(SqliteConnectionFactory factory) => _factory = factory;

    public string DatabasePath => _factory.DatabasePath;

    public void EnsureCreated()
    {
        var directory = Path.GetDirectoryName(_factory.DatabasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var databaseExists = File.Exists(_factory.DatabasePath);
        using var connection = _factory.CreateConnection(); // already open; foreign keys enabled in the factory

        var version = GetUserVersion(connection);
        var preUpgradeBackup = databaseExists && version < Migrations.Length
            ? BackupBeforeMigration()
            : null;

        for (var index = version; index < Migrations.Length; index++)
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = Migrations[index];
            command.ExecuteNonQuery();
            SetUserVersion(connection, transaction, index + 1);
            transaction.Commit();
        }

        if (preUpgradeBackup is not null)
        {
            try
            {
                File.Delete(preUpgradeBackup);
            }
            catch
            {
                // Best effort: a leftover upgrade backup is harmless.
            }
        }
    }

    /// <summary>
    /// Copy the database via the SQLite online backup API before a pending migration runs,
    /// so a broken upgrade can never strand the last known-good schema. Best effort; a
    /// backup failure must not block startup.
    /// </summary>
    private string? BackupBeforeMigration()
    {
        try
        {
            var directory = Path.GetDirectoryName(_factory.DatabasePath);
            if (string.IsNullOrEmpty(directory))
            {
                return null;
            }

            var backupDir = Path.Combine(directory, "backups");
            Directory.CreateDirectory(backupDir);
            var destination = Path.Combine(
                backupDir,
                $"gimdeks-pre-upgrade-{DateTime.Now:yyyyMMdd-HHmmss}.db");

            using var source = _factory.CreateConnection();
            using var target = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = destination,
            }.ToString());
            target.Open();
            source.BackupDatabase(target);
            return destination;
        }
        catch
        {
            // A locked backups folder must never block app startup.
            return null;
        }
    }

    private static int GetUserVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void SetUserVersion(SqliteConnection connection, SqliteTransaction transaction, int version)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // PRAGMA does not accept bound parameters; the value is a small trusted integer.
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
    }
}
