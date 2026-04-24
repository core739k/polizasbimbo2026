using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Infrastructure.Persistence.Records;

namespace PolizasBimbo.Infrastructure.Persistence.Repositories;

public sealed class SqlDownloadAuditRepository : IDownloadAuditRepository
{
    private readonly AppDbContext _db;
    public SqlDownloadAuditRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(DownloadAudit audit, CancellationToken ct)
    {
        _db.Audits.Add(new DownloadAuditRecord
        {
            PolicyId = audit.PolicyId,
            NumColaborador = audit.NumColaborador,
            Email = audit.Email,
            Phone = audit.Phone,
            FileName = audit.FileName,
            Country = audit.Origin.Country,
            City = audit.Origin.City,
            CreatedAt = audit.CreatedAt
        });
        await _db.SaveChangesAsync(ct);
    }
}
