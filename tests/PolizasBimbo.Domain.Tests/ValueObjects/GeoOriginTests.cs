using FluentAssertions;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Tests.ValueObjects;

public class GeoOriginTests
{
    [Fact]
    public void Create_WithCountryAndCity_Trims()
    {
        var geo = GeoOrigin.Create("  Mexico  ", "  Ciudad de Mexico ");
        geo.Country.Should().Be("Mexico");
        geo.City.Should().Be("Ciudad de Mexico");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Create_WithEmpty_FallsBackToUnknown(string? country, string? city)
    {
        var geo = GeoOrigin.Create(country, city);
        geo.Country.Should().Be("Desconocido");
        geo.City.Should().Be("Desconocido");
    }

    [Fact]
    public void Create_TruncatesTo100Chars()
    {
        var longValue = new string('A', 150);
        var geo = GeoOrigin.Create(longValue, longValue);
        geo.Country.Length.Should().Be(100);
        geo.City.Length.Should().Be(100);
    }
}
