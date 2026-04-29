using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Entities;

public sealed class DownloadAudit
{
    public int Id { get; private set; }
    public int NumColaborador { get; }
    public string Email { get; }
    public string Phone { get; }
    public string FileName { get; }
    public DateTime CreatedAt { get; }

    private DownloadAudit(int numColaborador, string email, string phone, string fileName, DateTime createdAt)
    {
        NumColaborador = numColaborador;
        Email = email;
        Phone = phone;
        FileName = fileName;
        CreatedAt = createdAt;
    }

    public static DownloadAudit Record(int idColaborador, string fileName, Email email, Phone phone, DateTime utcNow)
        => new(idColaborador, email.Value, phone.Value, fileName, utcNow);
}
