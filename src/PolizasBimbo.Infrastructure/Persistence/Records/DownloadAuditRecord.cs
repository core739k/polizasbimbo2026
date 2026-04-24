namespace PolizasBimbo.Infrastructure.Persistence.Records;

public sealed class DownloadAuditRecord
{
    public long Id { get; set; }
    public int PolicyId { get; set; }
    public int NumColaborador { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
