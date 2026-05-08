using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Integration.Tests.Infrastructure;

namespace PolizasBimbo.Integration.Tests.V1.Polizas;

public class DescargarEndpointTests : IClassFixture<PolizasApiFactory>
{
    private readonly PolizasApiFactory _factory;

    public DescargarEndpointTests(PolizasApiFactory factory) => _factory = factory;

    private void ResetMocks()
    {
        _factory.Blob.Reset();
        _factory.Tokens.Reset();
        _factory.Audit.Reset();
        _factory.Signer.Reset();
    }

    [Fact]
    public async Task Descargar_TokenValido_Returns200ConPdfYConsumeYAudita()
    {
        ResetMocks();

        var jti = Guid.NewGuid();
        var fileName = "bimbo/renovacion-2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf";
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-fake-bytes");

        _factory.Signer.Setup(s => s.Validate("jwt-valido", _factory.UtcNow))
            .Returns(new TokenPayload(jti, _factory.UtcNow.AddMinutes(5)));
        _factory.Tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DownloadToken.Rehydrate(jti, fileName, 10010081, "user@example.com", "5551234567", _factory.UtcNow.AddMinutes(-1), null));
        _factory.Blob.Setup(b => b.OpenReadAsync(fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobDownload(new MemoryStream(pdfBytes), "application/pdf", pdfBytes.Length));

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/polizas/descargar/jwt-valido");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var body = await response.Content.ReadAsByteArrayAsync();
        body.Should().Equal(pdfBytes);

        _factory.Tokens.Verify(t => t.MarkConsumedAsync(jti, _factory.UtcNow, It.IsAny<CancellationToken>()), Times.Once);
        _factory.Audit.Verify(a => a.AddAsync(
            It.Is<DownloadAudit>(da =>
                da.NumColaborador == 10010081 &&
                da.Email == "user@example.com" &&
                da.Phone == "5551234567" &&
                da.FileName == fileName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Descargar_TokenInvalido_Returns401()
    {
        ResetMocks();
        _factory.Signer.Setup(s => s.Validate(It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns((TokenPayload?)null);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/polizas/descargar/jwt-roto");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.Audit.Verify(a => a.AddAsync(It.IsAny<DownloadAudit>(), It.IsAny<CancellationToken>()), Times.Never);
        _factory.Tokens.Verify(t => t.MarkConsumedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Descargar_TokenExpirado_Returns410()
    {
        ResetMocks();
        var jti = Guid.NewGuid();
        _factory.Signer.Setup(s => s.Validate("jwt-vencido", _factory.UtcNow))
            .Returns(new TokenPayload(jti, _factory.UtcNow.AddMinutes(-1)));

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/polizas/descargar/jwt-vencido");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        _factory.Tokens.Verify(t => t.MarkConsumedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Descargar_TokenYaUsado_Returns410()
    {
        ResetMocks();
        var jti = Guid.NewGuid();
        var fileName = "bimbo/renovacion-2026/10010081_X.pdf";
        _factory.Signer.Setup(s => s.Validate("jwt-usado", _factory.UtcNow))
            .Returns(new TokenPayload(jti, _factory.UtcNow.AddMinutes(5)));
        _factory.Tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DownloadToken.Rehydrate(jti, fileName, 10010081, "u@e.com", "5551234567",
                _factory.UtcNow.AddMinutes(-5), _factory.UtcNow.AddMinutes(-2)));

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/polizas/descargar/jwt-usado");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        _factory.Audit.Verify(a => a.AddAsync(It.IsAny<DownloadAudit>(), It.IsAny<CancellationToken>()), Times.Never);
        _factory.Blob.Verify(b => b.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Descargar_BlobNoExiste_Returns404()
    {
        ResetMocks();
        var jti = Guid.NewGuid();
        var fileName = "bimbo/renovacion-2026/10010081_borrado.pdf";
        _factory.Signer.Setup(s => s.Validate("jwt-sin-blob", _factory.UtcNow))
            .Returns(new TokenPayload(jti, _factory.UtcNow.AddMinutes(5)));
        _factory.Tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DownloadToken.Rehydrate(jti, fileName, 10010081, "u@e.com", "5551234567",
                _factory.UtcNow.AddMinutes(-1), null));
        _factory.Blob.Setup(b => b.OpenReadAsync(fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobDownload?)null);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/polizas/descargar/jwt-sin-blob");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.Audit.Verify(a => a.AddAsync(It.IsAny<DownloadAudit>(), It.IsAny<CancellationToken>()), Times.Never);
        _factory.Tokens.Verify(t => t.MarkConsumedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Descargar_HappyPath_FijaHeadersCacheControlYContentDisposition()
    {
        ResetMocks();
        var jti = Guid.NewGuid();
        var fileName = "bimbo/renovacion-2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf";
        var pdf = Encoding.UTF8.GetBytes("%PDF");

        _factory.Signer.Setup(s => s.Validate("jwt-headers", _factory.UtcNow))
            .Returns(new TokenPayload(jti, _factory.UtcNow.AddMinutes(5)));
        _factory.Tokens.Setup(t => t.GetAsync(jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DownloadToken.Rehydrate(jti, fileName, 10010081, "u@e.com", "5551234567",
                _factory.UtcNow.AddMinutes(-1), null));
        _factory.Blob.Setup(b => b.OpenReadAsync(fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobDownload(new MemoryStream(pdf), "application/pdf", pdf.Length));

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/polizas/descargar/jwt-headers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        response.Content.Headers.ContentDisposition?.FileName.Should().Contain("MARIA GUADALUPE ZEPEDA CASTILLO");
        response.Content.Headers.ContentDisposition?.FileName.Should().EndWith(".pdf");
    }
}
