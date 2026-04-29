using Microsoft.EntityFrameworkCore;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Infrastructure.Persistence.Records;

namespace PolizasBimbo.Infrastructure.Persistence.Repositories;

public sealed class SqlDownloadTokenRepository : IDownloadTokenRepository
{
    private readonly AppDbContext _db;
    public SqlDownloadTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(DownloadToken token, CancellationToken ct)
    {
        _db.DownloadTokens.Add(new DownloadTokenRecord
        {
            Jti = token.Jti,
            FileName = token.FileName,
            IdColaborador = token.IdColaborador,
            Email = token.Email,
            Phone = token.Phone,
            IssuedAt = token.IssuedAt,
            ConsumedAt = token.ConsumedAt
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DownloadToken?> GetAsync(Guid jti, CancellationToken ct)
    {
        var row = await _db.DownloadTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Jti == jti, ct);
        return row is null
            ? null
            : DownloadToken.Rehydrate(row.Jti, row.FileName, row.IdColaborador, row.Email, row.Phone, row.IssuedAt, row.ConsumedAt);
    }

    public async Task MarkConsumedAsync(Guid jti, DateTime utcNow, CancellationToken ct)
    {
        await _db.DownloadTokens
            .Where(t => t.Jti == jti && t.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, utcNow), ct);
    }
}
