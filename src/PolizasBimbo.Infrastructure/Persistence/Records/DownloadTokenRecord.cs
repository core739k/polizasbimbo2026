namespace PolizasBimbo.Infrastructure.Persistence.Records;

public sealed class DownloadTokenRecord
{
    public Guid Jti { get; set; }
    public int PolicyId { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
