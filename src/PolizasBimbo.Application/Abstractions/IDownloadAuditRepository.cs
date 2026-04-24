using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Application.Abstractions;

public interface IDownloadAuditRepository
{
    Task AddAsync(DownloadAudit audit, CancellationToken ct);
}
