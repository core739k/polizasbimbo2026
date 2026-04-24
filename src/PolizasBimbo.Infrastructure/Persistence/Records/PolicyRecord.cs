namespace PolizasBimbo.Infrastructure.Persistence.Records;

public sealed class PolicyRecord
{
    public int Id { get; set; }
    public int NumColaborador { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
