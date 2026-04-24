using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;
using PolizasBimbo.Infrastructure.Persistence.Records;

namespace PolizasBimbo.Infrastructure.Persistence.Repositories;

public sealed class SqlPolicyRepository : IPolicyRepository
{
    private readonly AppDbContext _db;
    public SqlPolicyRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Policy>> SearchByNameAsync(SearchTerm term, int take, CancellationToken ct)
    {
        var query = term.ToFullTextQuery();
        var sql = $@"
SELECT TOP ({take}) id AS Id, NumColaborador, NomArchivo AS FileName, NombreCompleto AS FullName, Email, Telefono AS Phone, UpdatedAt
FROM dbo.PolizasBimboTraspaso
WHERE CONTAINS(NombreCompleto, @p0)
ORDER BY NombreCompleto;";

        var rows = await _db.Policies
            .FromSqlRaw(sql, new SqlParameter("@p0", query))
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    public async Task<Policy?> GetByIdAsync(int id, CancellationToken ct)
    {
        var row = await _db.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return row is null ? null : Map(row);
    }

    public async Task UpdateContactAsync(int numColaborador, string email, string phone, DateTime utcNow, CancellationToken ct)
    {
        await _db.Policies
            .Where(p => p.NumColaborador == numColaborador)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Email, email)
                .SetProperty(p => p.Phone, phone)
                .SetProperty(p => p.UpdatedAt, utcNow), ct);
    }

    public async Task ReplaceAllAsync(IEnumerable<Policy> policies, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.PolizasBimboTraspaso;", ct);
        _db.Policies.AddRange(policies.Select(p => new PolicyRecord
        {
            NumColaborador = p.NumColaborador,
            FullName = p.FullName,
            FileName = p.FileName
        }));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static Policy Map(PolicyRecord r)
        => Policy.Create(r.Id, r.NumColaborador, r.FullName ?? string.Empty, r.FileName);
}
