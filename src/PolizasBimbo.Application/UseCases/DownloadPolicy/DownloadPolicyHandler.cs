using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Application.UseCases.DownloadPolicy;

public sealed record DownloadPolicyRequest(
    string Token,
    string Email,
    string Phone,
    string? Country,
    string? City);

public abstract record DownloadPolicyResponse
{
    public sealed record Ok(BlobDownload Blob, string FileName) : DownloadPolicyResponse;
    public sealed record InvalidToken : DownloadPolicyResponse;
    public sealed record Expired : DownloadPolicyResponse;
    public sealed record AlreadyUsed : DownloadPolicyResponse;
    public sealed record NotFound : DownloadPolicyResponse;
}

public sealed class DownloadPolicyHandler
{
    private readonly ITokenSigner _signer;
    private readonly IDownloadTokenRepository _tokens;
    private readonly IPolicyRepository _policies;
    private readonly IPolicyBlobStorage _blob;
    private readonly IDownloadAuditRepository _audit;
    private readonly IClock _clock;

    public DownloadPolicyHandler(
        ITokenSigner signer,
        IDownloadTokenRepository tokens,
        IPolicyRepository policies,
        IPolicyBlobStorage blob,
        IDownloadAuditRepository audit,
        IClock clock)
    {
        _signer = signer;
        _tokens = tokens;
        _policies = policies;
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

        var policy = await _policies.GetByIdAsync(payload.PolicyId, ct);
        if (policy is null) return new DownloadPolicyResponse.NotFound();

        var blob = await _blob.OpenReadAsync(policy.FileName, ct);
        if (blob is null) return new DownloadPolicyResponse.NotFound();

        var email = Email.Create(request.Email);
        var phone = Phone.Create(request.Phone);
        var geo = GeoOrigin.Create(request.Country, request.City);

        await _tokens.MarkConsumedAsync(record.Jti, now, ct);
        await _policies.UpdateContactAsync(policy.NumColaborador, email.Value, phone.Value, now, ct);
        await _audit.AddAsync(DownloadAudit.Record(policy, email, phone, geo, now), ct);

        return new DownloadPolicyResponse.Ok(blob, policy.FileName);
    }
}
