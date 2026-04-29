using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Application.UseCases.SearchPolicies;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Application.UseCases.DownloadPolicy;

public sealed record DownloadPolicyRequest(string Token);

public abstract record DownloadPolicyResponse
{
    public sealed record Ok(BlobDownload Blob, string DisplayFileName) : DownloadPolicyResponse;
    public sealed record InvalidToken : DownloadPolicyResponse;
    public sealed record Expired : DownloadPolicyResponse;
    public sealed record AlreadyUsed : DownloadPolicyResponse;
    public sealed record NotFound : DownloadPolicyResponse;
}

public sealed class DownloadPolicyHandler
{
    private readonly ITokenSigner _signer;
    private readonly IDownloadTokenRepository _tokens;
    private readonly IPolicyBlobStorage _blob;
    private readonly IDownloadAuditRepository _audit;
    private readonly IClock _clock;

    public DownloadPolicyHandler(
        ITokenSigner signer,
        IDownloadTokenRepository tokens,
        IPolicyBlobStorage blob,
        IDownloadAuditRepository audit,
        IClock clock)
    {
        _signer = signer;
        _tokens = tokens;
        _blob = blob;
        _audit = audit;
        _clock = clock;
    }

    public async Task<DownloadPolicyResponse> HandleAsync(DownloadPolicyRequest request, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var payload = _signer.Validate(request.Token, now);
        if (payload is null)
            return new DownloadPolicyResponse.InvalidToken();

        if (payload.ExpiresAt <= now)
            return new DownloadPolicyResponse.Expired();

        var record = await _tokens.GetAsync(payload.Jti, ct);
        if (record is null) return new DownloadPolicyResponse.InvalidToken();
        if (record.IsConsumed) return new DownloadPolicyResponse.AlreadyUsed();

        var blob = await _blob.OpenReadAsync(record.FileName, ct);
        if (blob is null) return new DownloadPolicyResponse.NotFound();

        await _tokens.MarkConsumedAsync(record.Jti, now, ct);
        await _audit.AddAsync(
            DownloadAudit.Record(
                record.IdColaborador,
                record.FileName,
                Email.Create(record.Email),
                Phone.Create(record.Phone),
                now),
            ct);

        var display = SearchPoliciesHandler.DeriveDisplayName(record.FileName) + ".pdf";
        return new DownloadPolicyResponse.Ok(blob, display);
    }
}
