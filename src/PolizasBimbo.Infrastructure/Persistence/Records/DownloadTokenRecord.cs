namespace PolizasBimbo.Infrastructure.Persistence.Records;

public sealed class DownloadTokenRecord
{
    public Guid Jti { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int IdColaborador { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
