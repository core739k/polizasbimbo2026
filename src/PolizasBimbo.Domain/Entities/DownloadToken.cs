namespace PolizasBimbo.Domain.Entities;

public sealed class DownloadToken
{
    public Guid Jti { get; }
    public string FileName { get; }
    public int IdColaborador { get; }
    public string Email { get; }
    public string Phone { get; }
    public DateTime IssuedAt { get; }
    public DateTime? ConsumedAt { get; private set; }

    private DownloadToken(Guid jti, string fileName, int idColaborador, string email, string phone, DateTime issuedAt, DateTime? consumedAt)
    {
        Jti = jti;
        FileName = fileName;
        IdColaborador = idColaborador;
        Email = email;
        Phone = phone;
        IssuedAt = issuedAt;
        ConsumedAt = consumedAt;
    }

    public static DownloadToken Issue(string fileName, int idColaborador, string email, string phone, DateTime utcNow)
        => new(Guid.NewGuid(), fileName, idColaborador, email, phone, utcNow, null);

    public static DownloadToken Rehydrate(Guid jti, string fileName, int idColaborador, string email, string phone, DateTime issuedAt, DateTime? consumedAt)
        => new(jti, fileName, idColaborador, email, phone, issuedAt, consumedAt);

    public bool IsConsumed => ConsumedAt.HasValue;

    public void MarkConsumed(DateTime utcNow)
    {
        if (ConsumedAt.HasValue)
            throw new InvalidOperationException("El token ya fue consumido.");
        ConsumedAt = utcNow;
    }
}
