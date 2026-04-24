namespace PolizasBimbo.Application.Abstractions;

public interface ITokenSigner
{
    string Issue(Guid jti, int policyId, DateTime issuedAt, TimeSpan ttl);
    TokenPayload? Validate(string token, DateTime utcNow);
}

public sealed record TokenPayload(Guid Jti, int PolicyId, DateTime ExpiresAt);
