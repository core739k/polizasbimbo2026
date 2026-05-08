using System.Net;
using FluentAssertions;
using PolizasBimbo.Integration.Tests.Infrastructure;

namespace PolizasBimbo.Integration.Tests.V1.Polizas;

public class CorsTests : IClassFixture<PolizasApiFactory>
{
    private readonly PolizasApiFactory _factory;

    public CorsTests(PolizasApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Preflight_DesdeOrigenPermitido_DevuelveHeadersCors()
    {
        var client = _factory.CreateClient();
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/polizas/buscar");
        preflight.Headers.Add("Origin", "http://localhost:4200");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().Contain("http://localhost:4200");
        response.Headers.GetValues("Access-Control-Allow-Methods")
            .SelectMany(v => v.Split(','))
            .Select(v => v.Trim().ToUpperInvariant())
            .Should().Contain("POST");
    }

    [Fact]
    public async Task Preflight_DesdeOrigenNoPermitido_NoIncluyeAllowOrigin()
    {
        var client = _factory.CreateClient();
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/polizas/buscar");
        preflight.Headers.Add("Origin", "http://attacker.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(preflight);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
