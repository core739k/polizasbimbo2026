using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Entities;

public sealed class DownloadAudit
{
    public long Id { get; private set; }
    public int PolicyId { get; }
    public int NumColaborador { get; }
    public string Email { get; }
    public string Phone { get; }
    public string FileName { get; }
    public GeoOrigin Origin { get; }
    public DateTime CreatedAt { get; }

    private DownloadAudit(int policyId, int numColaborador, string email, string phone, string fileName, GeoOrigin origin, DateTime createdAt)
    {
        PolicyId = policyId;
        NumColaborador = numColaborador;
        Email = email;
        Phone = phone;
        FileName = fileName;
        Origin = origin;
        CreatedAt = createdAt;
    }

    public static DownloadAudit Record(Policy policy, Email email, Phone phone, GeoOrigin origin, DateTime utcNow)
        => new(policy.Id, policy.NumColaborador, email.Value, phone.Value, policy.FileName, origin, utcNow);
}
