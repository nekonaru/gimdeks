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

        using var connection = _factory.CreateConnection();
        connection.Open();

        var version = GetUserVersion(connection);
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
