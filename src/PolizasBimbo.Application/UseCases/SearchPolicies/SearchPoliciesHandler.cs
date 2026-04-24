using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Application.UseCases.SearchPolicies;

public sealed record SearchPoliciesRequest(string Name);

public sealed record PolicySearchResult(int PolicyId, string FileName, string DownloadToken);

public sealed record SearchPoliciesResponse(IReadOnlyList<PolicySearchResult> Results);

public sealed class SearchPoliciesHandler
{
    public const int MaxResults = 5;
    public static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(10);

    private readonly IPolicyRepository _policies;
    private readonly IDownloadTokenRepository _tokens;
    private readonly ITokenSigner _signer;
    private readonly IClock _clock;

    public SearchPoliciesHandler(
        IPolicyRepository policies,
        IDownloadTokenRepository tokens,
        ITokenSigner signer,
        IClock clock)
    {
        _policies = policies;
        _tokens = tokens;
        _signer = signer;
        _clock = clock;
    }

    public async Task<SearchPoliciesResponse> HandleAsync(SearchPoliciesRequest request, CancellationToken ct)
    {
        var term = SearchTerm.Create(request.Name);
        var policies = await _policies.SearchByNameAsync(term, MaxResults, ct);

        var now = _clock.UtcNow;
        var results = new List<PolicySearchResult>(policies.Count);
        foreach (var policy in policies)
        {
            var token = DownloadToken.Issue(policy.Id, now);
            await _tokens.AddAsync(token, ct);
            var jwt = _signer.Issue(token.Jti, policy.Id, now, TokenTtl);
            results.Add(new PolicySearchResult(policy.Id, policy.FileName, jwt));
        }

        return new SearchPoliciesResponse(results);
    }
}
