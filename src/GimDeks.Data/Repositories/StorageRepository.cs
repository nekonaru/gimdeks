using Dapper;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.Data.Repositories;

public sealed class StorageRepository : IStorageRepository
{
    private readonly SqliteConnectionFactory _factory;

    public StorageRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Storage>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, nama, tipe, kapasitas_gb, batas_gb, urutan, grup
            FROM storage
            ORDER BY urutan, nama;
            """;
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Select(Map).ToList();
    }

    public async Task<Storage?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, nama, tipe, kapasitas_gb, batas_gb, urutan, grup FROM storage WHERE id = @Id;";
        using var connection = _factory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row is null ? null : Map(row);
    }

    public async Task<long> AddAsync(Storage storage, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO storage (nama, tipe, kapasitas_gb, batas_gb, urutan, grup)
            VALUES (@Nama, @Tipe, @KapasitasGb, @BatasGb, @Urutan, @Grup);
            SELECT last_insert_rowid();
            """;
        using var connection = _factory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            Nama = storage.Nama,
            Tipe = storage.Tipe.ToDatabase(),
            KapasitasGb = storage.KapasitasGb,
            BatasGb = storage.BatasGb,
            Urutan = storage.Urutan,
            Grup = storage.Grup,
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(Storage storage, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE storage
            SET nama = @Nama, tipe = @Tipe, kapasitas_gb = @KapasitasGb, batas_gb = @BatasGb, urutan = @Urutan, grup = @Grup
            WHERE id = @Id;
            """;
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Nama = storage.Nama,
            Tipe = storage.Tipe.ToDatabase(),
            KapasitasGb = storage.KapasitasGb,
            BatasGb = storage.BatasGb,
            Urutan = storage.Urutan,
            Grup = storage.Grup,
            Id = storage.Id,
        }, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM storage WHERE id = @Id;";
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExistsByNameAsync(string nama, long? excludeId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM storage
            WHERE lower(nama) = lower(@Nama) AND (@ExcludeId IS NULL OR id <> @ExcludeId);
            """;
        using var connection = _factory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, new { Nama = nama, ExcludeId = excludeId }, cancellationToken: ct));
        return count > 0;
    }

    private static Storage Map(dynamic row) => new()
    {
        Id = (long)row.id,
        Nama = (string)row.nama,
        Tipe = EnumDatabaseNames.StorageTypeFromDatabase((string)row.tipe),
        KapasitasGb = row.kapasitas_gb is null ? null : Convert.ToDouble(row.kapasitas_gb, System.Globalization.CultureInfo.InvariantCulture),
        BatasGb = row.batas_gb is null ? null : Convert.ToDouble(row.batas_gb, System.Globalization.CultureInfo.InvariantCulture),
        Urutan = (int)row.urutan,
        Grup = row.grup is null ? null : (string)row.grup,
    };
}
