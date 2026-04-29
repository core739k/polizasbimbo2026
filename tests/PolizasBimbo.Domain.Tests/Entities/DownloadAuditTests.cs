using FluentAssertions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Tests.Entities;

public class DownloadAuditTests
{
    [Fact]
    public void Record_BuildsAuditFromTokenContext()
    {
        var audit = DownloadAudit.Record(
            idColaborador: 10010081,
            fileName: "bimbo/renovacion2026/10010081_MARIA GUADALUPE.pdf",
            email: Email.Create("user@example.com"),
            phone: Phone.Create("5551234567"),
            utcNow: new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc));

        audit.NumColaborador.Should().Be(10010081);
        audit.FileName.Should().Be("bimbo/renovacion2026/10010081_MARIA GUADALUPE.pdf");
        audit.Email.Should().Be("user@example.com");
        audit.Phone.Should().Be("5551234567");
        audit.CreatedAt.Should().Be(new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc));
    }
}
