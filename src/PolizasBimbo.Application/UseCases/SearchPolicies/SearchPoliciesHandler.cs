using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Application.UseCases.SearchPolicies;

public sealed record SearchPoliciesRequest(int IdColaborador, string Email, string Telefono);

public sealed record PolicySearchResult(string FileName, string DisplayName, string DownloadToken);

public sealed record SearchPoliciesResponse(IReadOnlyList<PolicySearchResult> Results);

public sealed class SearchPoliciesHandler
{
    public static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(10);

    private readonly IPolicyBlobStorage _blob;
    private readonly IDownloadTokenRepository _tokens;
    private readonly ITokenSigner _signer;
    private readonly IClock _clock;

    public SearchPoliciesHandler(
        IPolicyBlobStorage blob,
        IDownloadTokenRepository tokens,
        ITokenSigner signer,
        IClock clock)
    {
        _blob = blob;
        _tokens = tokens;
        _signer = signer;
        _clock = clock;
    }

    public async Task<SearchPoliciesResponse> HandleAsync(SearchPoliciesRequest request, CancellationToken ct)
    {
        if (request.IdColaborador <= 0)
            throw new ArgumentException("ID de colaborador inválido.", nameof(request));

        var email = Email.Create(request.Email);
        var phone = Phone.Create(request.Telefono);

        var blobs = await _blob.ListByCollaboratorAsync(request.IdColaborador, ct);

        var now = _clock.UtcNow;
        var results = new List<PolicySearchResult>(blobs.Count);
        foreach (var fileName in blobs)
        {
            var token = DownloadToken.Issue(fileName, request.IdColaborador, email.Value, phone.Value, now);
            await _tokens.AddAsync(token, ct);
            var jwt = _signer.Issue(token.Jti, now, TokenTtl);
            results.Add(new PolicySearchResult(fileName, DeriveDisplayName(fileName), jwt));
        }

        return new SearchPoliciesResponse(results);
    }

    internal static string DeriveDisplayName(string fileName)
    {
        var slash = fileName.LastIndexOf('/');
        var leaf = slash >= 0 ? fileName[(slash + 1)..] : fileName;
        var underscore = leaf.IndexOf('_');
        if (underscore < 0) return leaf;
        var afterId = leaf[(underscore + 1)..];
        var dot = afterId.LastIndexOf('.');
        return dot >= 0 ? afterId[..dot] : afterId;
    }
}
