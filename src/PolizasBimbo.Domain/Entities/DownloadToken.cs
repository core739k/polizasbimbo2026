namespace PolizasBimbo.Domain.Entities;

public sealed class DownloadToken
{
    public Guid Jti { get; }
    public int PolicyId { get; }
    public DateTime IssuedAt { get; }
    public DateTime? ConsumedAt { get; private set; }

    private DownloadToken(Guid jti, int policyId, DateTime issuedAt, DateTime? consumedAt)
    {
        Jti = jti;
        PolicyId = policyId;
        IssuedAt = issuedAt;
        ConsumedAt = consumedAt;
    }

    public static DownloadToken Issue(int policyId, DateTime utcNow)
        => new(Guid.NewGuid(), policyId, utcNow, null);

    public static DownloadToken Rehydrate(Guid jti, int policyId, DateTime issuedAt, DateTime? consumedAt)
        => new(jti, policyId, issuedAt, consumedAt);

    public bool IsConsumed => ConsumedAt.HasValue;

    public void MarkConsumed(DateTime utcNow)
    {
        if (ConsumedAt.HasValue)
            throw new InvalidOperationException("El token ya fue consumido.");
        ConsumedAt = utcNow;
    }
}
