using FluentAssertions;
using Microsoft.Extensions.Options;
using PolizasBimbo.Infrastructure.Security;

namespace PolizasBimbo.Application.Tests.Infrastructure;

public class JwtTokenSignerTests
{
    private static JwtTokenSigner CreateSut(string key = "ThisIsA32ByteSigningKeyForTestsXX")
        => new(Options.Create(new TokenSignerOptions { SigningKey = key }));

    [Fact]
    public void RoundTrip_ValidatesIssuedToken()
    {
        var sut = CreateSut();
        var jti = Guid.NewGuid();
        var issuedAt = new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc);
        var token = sut.Issue(jti, 42, issuedAt, TimeSpan.FromMinutes(10));

        var payload = sut.Validate(token, issuedAt.AddMinutes(5));
        payload.Should().NotBeNull();
        payload!.Jti.Should().Be(jti);
        payload.PolicyId.Should().Be(42);
        payload.ExpiresAt.Should().BeCloseTo(issuedAt.AddMinutes(10), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Validate_WithTamperedSignature_ReturnsNull()
    {
        var sut = CreateSut();
        var token = sut.Issue(Guid.NewGuid(), 1, DateTime.UtcNow, TimeSpan.FromMinutes(10));
        var tampered = token[..^2] + "xx";
        sut.Validate(tampered, DateTime.UtcNow).Should().BeNull();
    }

    [Fact]
    public void Validate_WithMalformedToken_ReturnsNull()
    {
        var sut = CreateSut();
        sut.Validate("not.a.jwt.at.all", DateTime.UtcNow).Should().BeNull();
        sut.Validate("", DateTime.UtcNow).Should().BeNull();
    }

    [Fact]
    public void Ctor_WithShortKey_Throws()
    {
        var act = () => new JwtTokenSigner(Options.Create(new TokenSignerOptions { SigningKey = "short" }));
        act.Should().Throw<InvalidOperationException>();
    }
}
