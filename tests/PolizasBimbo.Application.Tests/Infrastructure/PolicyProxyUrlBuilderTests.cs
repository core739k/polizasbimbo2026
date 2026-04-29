using FluentAssertions;
using Microsoft.Extensions.Options;
using PolizasBimbo.Infrastructure.Storage;

namespace PolizasBimbo.Application.Tests.Infrastructure;

public class PolicyProxyUrlBuilderTests
{
    private static PolicyProxyUrlBuilder CreateSut(string baseUrl)
        => new(Options.Create(new PolicyProxyOptions { BaseUrl = baseUrl }));

    [Fact]
    public void Build_AppendsFileName_ToBaseUrl()
    {
        var sut = CreateSut("https://api.mcb.uno:8099/api/v1/blobs/");
        sut.Build("10010067_0000001.pdf")
           .Should().Be("https://api.mcb.uno:8099/api/v1/blobs/10010067_0000001.pdf");
    }

    [Fact]
    public void Build_NormalizesTrailingSlash_OnBaseUrl()
    {
        var sut = CreateSut("https://api.mcb.uno:8099/api/v1/blobs");
        sut.Build("file.pdf")
           .Should().Be("https://api.mcb.uno:8099/api/v1/blobs/file.pdf");
    }

    [Fact]
    public void Build_UrlEncodesFileName()
    {
        var sut = CreateSut("https://api.mcb.uno:8099/api/v1/blobs/");
        sut.Build("archivo con espacios y ñ.pdf")
           .Should().Be("https://api.mcb.uno:8099/api/v1/blobs/archivo%20con%20espacios%20y%20%C3%B1.pdf");
    }
}
