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
    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly Mock<IPolicyBlobStorage> _blob = new();
    private readonly Mock<IDownloadAuditRepository> _audit = new();
    private readonly FixedClock _clock = new(new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc));

    private DownloadPolicyHandler CreateSut() =>
        new(_signer.Object, _tokens.Object, _policies.Object, _blob.Object, _audit.Object, _clock);

    private static DownloadPolicyRequest ValidRequest(string token = "jwt") =>
        new(token, "user@example.com", "5551234567", "Mexico", "Ciudad de Mexico");

    [Fact]
    public async Task Handle_InvalidToken_ReturnsInvalidToken()
    {
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>())).Returns((TokenPayload?)null);
        var result = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.InvalidToken>();
    }

    [Fact]
    public async Task Handle_ExpiredPayload_ReturnsExpired()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, 1, _clock.UtcNow.AddMinutes(-1)));
        var result = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.Expired>();
    }

    [Fact]
    public async Task Handle_ConsumedToken_ReturnsAlreadyUsed()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, 1, _clock.UtcNow.AddMinutes(5)));

        var consumed = DownloadToken.Rehydrate(jti, 1, _clock.UtcNow.AddMinutes(-1), _clock.UtcNow);
        _tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>())).ReturnsAsync(consumed);

        var result = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.AlreadyUsed>();
    }

    [Fact]
    public async Task Handle_HappyPath_ConsumesTokenUpdatesContactAndLogsAudit()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, 1, _clock.UtcNow.AddMinutes(5)));

        var fresh = DownloadToken.Rehydrate(jti, 1, _clock.UtcNow.AddMinutes(-1), null);
        _tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>())).ReturnsAsync(fresh);

        var policy = Policy.Create(1, 10010067, "JUAN PEREZ", "10010067_0000001.pdf");
        _policies.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var contentStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _blob.Setup(b => b.OpenReadAsync(policy.FileName, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new BlobDownload(contentStream, "application/pdf", 3));

        var result = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);

        result.Should().BeOfType<DownloadPolicyResponse.Ok>();
        _tokens.Verify(t => t.MarkConsumedAsync(jti, _clock.UtcNow, It.IsAny<CancellationToken>()), Times.Once);
        _policies.Verify(p => p.UpdateContactAsync(10010067, "user@example.com", "5551234567", _clock.UtcNow, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.AddAsync(It.IsAny<DownloadAudit>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BlobMissing_ReturnsNotFound()
    {
        var jti = Guid.NewGuid();
        _signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
               .Returns(new TokenPayload(jti, 1, _clock.UtcNow.AddMinutes(5)));
        _tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>()))
               .ReturnsAsync(DownloadToken.Rehydrate(jti, 1, _clock.UtcNow, null));
        _policies.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Policy.Create(1, 123, "N", "missing.pdf"));
        _blob.Setup(b => b.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((BlobDownload?)null);

        var result = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);
        result.Should().BeOfType<DownloadPolicyResponse.NotFound>();
    }
}
