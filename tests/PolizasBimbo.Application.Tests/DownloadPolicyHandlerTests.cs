using FluentAssertions;
using Moq;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Application.Tests.Helpers;
using PolizasBimbo.Application.UseCases.DownloadPolicy;
using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Application.Tests;

public class DownloadPolicyHandlerTests
{
    private readonly Mock<ITokenSigner> _signer = new();
    private readonly Mock<IDownloadTokenRepository> _tokens = new();
    private readonly Mock<IPolicyBlobStorage> _blob = new();
    private readonly Mock<IDownloadAuditRepository> _audit = new();
    private readonly FixedClock _clock = new(new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc));

    private DownloadPolicyHandler CreateSut() =>
        new(_signer.Object, _tokens.Object, _blob.Object, _audit.Object, _clock);

    private static DownloadToken FreshToken(Guid jti, DateTime issuedAt) =>
        DownloadToken.Rehydrate(
            jti,
            "bimbo/renovacion2026/10010081_MARIA GUADALUPE.pdf",
            10010081,
            "user@example.com",
            "5551234567",
            issuedAt,
            null);

    [Fact]
    public async Task Handle_InvalidToken_ReturnsInvalidToken()
    {
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns((TokenPayload?)null);
        var result = await CreateSut().HandleAsync(new DownloadPolicyRequest("jwt"), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.InvalidToken>();
    }

    [Fact]
    public async Task Handle_ExpiredPayload_ReturnsExpired()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, _clock.UtcNow.AddMinutes(-1)));
        var result = await CreateSut().HandleAsync(new DownloadPolicyRequest("jwt"), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.Expired>();
    }

    [Fact]
    public async Task Handle_ConsumedToken_ReturnsAlreadyUsed()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, _clock.UtcNow.AddMinutes(5)));

        var consumed = DownloadToken.Rehydrate(jti, "f.pdf", 1, "a@b.co", "5550000000", _clock.UtcNow.AddMinutes(-1), _clock.UtcNow);
        _tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>())).ReturnsAsync(consumed);

        var result = await CreateSut().HandleAsync(new DownloadPolicyRequest("jwt"), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.AlreadyUsed>();
    }

    [Fact]
    public async Task Handle_HappyPath_StreamsBlobConsumesTokenAndAudits()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, _clock.UtcNow.AddMinutes(5)));

        var fresh = FreshToken(jti, _clock.UtcNow.AddMinutes(-1));
        _tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>())).ReturnsAsync(fresh);

        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _blob.Setup(b => b.OpenReadAsync(fresh.FileName, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new BlobDownload(stream, "application/pdf", 3));

        var result = await CreateSut().HandleAsync(new DownloadPolicyRequest("jwt"), CancellationToken.None);

        var ok = result.Should().BeOfType<DownloadPolicyResponse.Ok>().Subject;
        ok.Blob.Content.Should().BeSameAs(stream);
        ok.DisplayFileName.Should().Be("MARIA GUADALUPE.pdf");

        _tokens.Verify(t => t.MarkConsumedAsync(jti, _clock.UtcNow, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.AddAsync(
            It.Is<DownloadAudit>(da =>
                da.NumColaborador == 10010081 &&
                da.Email == "user@example.com" &&
                da.Phone == "5551234567" &&
                da.FileName == fresh.FileName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BlobMissing_ReturnsNotFound()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, _clock.UtcNow.AddMinutes(5)));
        _tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>())).ReturnsAsync(FreshToken(jti, _clock.UtcNow));
        _blob.Setup(b => b.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((BlobDownload?)null);

        var result = await CreateSut().HandleAsync(new DownloadPolicyRequest("jwt"), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.NotFound>();
    }
}
