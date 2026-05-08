using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Moq;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Integration.Tests.Infrastructure;

namespace PolizasBimbo.Integration.Tests.V1.Polizas;

public class BuscarEndpointTests : IClassFixture<PolizasApiFactory>
{
    private readonly PolizasApiFactory _factory;

    public BuscarEndpointTests(PolizasApiFactory factory) => _factory = factory;

    private static readonly object ValidRequest = new
    {
        idColaborador = 10010081,
        email = "user@example.com",
        telefono = "5551234567"
    };

    [Fact]
    public async Task Buscar_HappyPath_Returns200ConArrayDePolizas()
    {
        _factory.Blob.Reset();
        _factory.Tokens.Reset();
        _factory.Signer.Reset();

        _factory.Blob.Setup(b => b.ListByCollaboratorAsync(10010081, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                "bimbo/renovacion-2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf",
                "bimbo/renovacion-2026/10010081_JUAN PEREZ.pdf"
            });
        _factory.Signer.Setup(s => s.Issue(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
            .Returns<Guid, DateTime, TimeSpan>((jti, _, _) => $"jwt-{jti}");

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/polizas/buscar", ValidRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(2);

        _factory.Tokens.Verify(t => t.AddAsync(It.IsAny<DownloadToken>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Buscar_HappyPath_RespetaContratoJsonEnEspanol()
    {
        _factory.Blob.Reset();
        _factory.Tokens.Reset();
        _factory.Signer.Reset();

        _factory.Blob.Setup(b => b.ListByCollaboratorAsync(10010081, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "bimbo/renovacion-2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf" });
        _factory.Signer.Setup(s => s.Issue(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
            .Returns("jwt-firmado");

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/polizas/buscar", ValidRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await response.Content.ReadAsStringAsync();
        var array = JsonDocument.Parse(raw).RootElement;

        array.ValueKind.Should().Be(JsonValueKind.Array);
        array.GetArrayLength().Should().Be(1);

        var item = array[0];
        item.TryGetProperty("nombreArchivo", out var nombre).Should().BeTrue("contrato exige 'nombreArchivo' en camelCase");
        item.TryGetProperty("displayName", out var display).Should().BeTrue();
        item.TryGetProperty("tokenDescarga", out var token).Should().BeTrue("contrato exige 'tokenDescarga'");

        nombre.GetString().Should().Be("bimbo/renovacion-2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf");
        display.GetString().Should().Be("MARIA GUADALUPE ZEPEDA CASTILLO");
        token.GetString().Should().Be("jwt-firmado");
    }

    [Fact]
    public async Task Buscar_IdColaboradorInvalido_Returns400()
    {
        _factory.Blob.Reset();
        _factory.Tokens.Reset();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/polizas/buscar", new
        {
            idColaborador = 0,
            email = "user@example.com",
            telefono = "5551234567"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Tokens.Verify(t => t.AddAsync(It.IsAny<DownloadToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Buscar_EmailInvalido_Returns400()
    {
        _factory.Blob.Reset();
        _factory.Blob.Setup(b => b.ListByCollaboratorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/polizas/buscar", new
        {
            idColaborador = 10010081,
            email = "no-es-email",
            telefono = "5551234567"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Buscar_TelefonoInvalido_Returns400()
    {
        _factory.Blob.Reset();
        _factory.Blob.Setup(b => b.ListByCollaboratorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/polizas/buscar", new
        {
            idColaborador = 10010081,
            email = "user@example.com",
            telefono = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Buscar_SinCoincidencias_ReturnsArrayVacio()
    {
        _factory.Blob.Reset();
        _factory.Tokens.Reset();
        _factory.Blob.Setup(b => b.ListByCollaboratorAsync(99999999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/polizas/buscar", new
        {
            idColaborador = 99999999,
            email = "user@example.com",
            telefono = "5551234567"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var array = await response.Content.ReadFromJsonAsync<JsonElement>();
        array.ValueKind.Should().Be(JsonValueKind.Array);
        array.GetArrayLength().Should().Be(0);
        _factory.Tokens.Verify(t => t.AddAsync(It.IsAny<DownloadToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
