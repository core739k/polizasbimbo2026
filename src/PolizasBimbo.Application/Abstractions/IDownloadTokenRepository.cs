using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Application.Abstractions;

public interface IDownloadTokenRepository
{
    Task AddAsync(DownloadToken token, CancellationToken ct);
    Task<DownloadToken?> GetAsync(Guid jti, CancellationToken ct);
    Task MarkConsumedAsync(Guid jti, DateTime utcNow, CancellationToken ct);
}
