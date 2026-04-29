namespace PolizasBimbo.Infrastructure.Persistence.Records;

public sealed class DownloadAuditRecord
{
    public int Id { get; set; }
    public int NumColaborador { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
