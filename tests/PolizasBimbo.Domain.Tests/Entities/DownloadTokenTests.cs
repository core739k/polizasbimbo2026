using FluentAssertions;
using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Domain.Tests.Entities;

public class DownloadTokenTests
{
    private static readonly DateTime Now = new(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Issue_GeneratesFreshTokenWithProvidedContext()
    {
        var t = DownloadToken.Issue(
            fileName: "bimbo/renovacion2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf",
            idColaborador: 10010081,
            email: "user@example.com",
            phone: "5551234567",
            utcNow: Now);

        t.Jti.Should().NotBe(Guid.Empty);
        t.FileName.Should().Be("bimbo/renovacion2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf");
        t.IdColaborador.Should().Be(10010081);
        t.Email.Should().Be("user@example.com");
        t.Phone.Should().Be("5551234567");
        t.IssuedAt.Should().Be(Now);
        t.ConsumedAt.Should().BeNull();
        t.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void MarkConsumed_SetsConsumedAt()
    {
        var t = DownloadToken.Issue("x.pdf", 1, "a@b.co", "5550000000", Now);
        t.MarkConsumed(Now.AddMinutes(1));
        t.IsConsumed.Should().BeTrue();
        t.ConsumedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void MarkConsumed_Twice_Throws()
    {
        var t = DownloadToken.Issue("x.pdf", 1, "a@b.co", "5550000000", Now);
        t.MarkConsumed(Now);
        var act = () => t.MarkConsumed(Now.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rehydrate_PreservesAllFields()
    {
        var jti = Guid.NewGuid();
        var t = DownloadToken.Rehydrate(jti, "f.pdf", 7, "z@z.com", "5559876543", Now, Now.AddMinutes(2));
        t.Jti.Should().Be(jti);
        t.FileName.Should().Be("f.pdf");
        t.IdColaborador.Should().Be(7);
        t.Email.Should().Be("z@z.com");
        t.Phone.Should().Be("5559876543");
        t.IssuedAt.Should().Be(Now);
        t.ConsumedAt.Should().Be(Now.AddMinutes(2));
        t.IsConsumed.Should().BeTrue();
    }
}
