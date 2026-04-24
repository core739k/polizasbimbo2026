using System.Text;
using FluentAssertions;
using PolizasBimbo.Infrastructure.Padron;

namespace PolizasBimbo.Application.Tests.Infrastructure;

public class CsvPadronLoaderTests
{
    [Fact]
    public void Parse_WithThreeColumnsUtf8NoHeader_ReturnsPolicies()
    {
        var csv = "10010067,JUAN PEREZ LOPEZ,10010067_0000001.pdf\n" +
                  "10010079,MARIA GARCIA HERNANDEZ,10010079_0000002.pdf\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var loader = new CsvPadronLoader();
        var rows = loader.Parse(ms).ToList();

        rows.Should().HaveCount(2);
        rows[0].NumColaborador.Should().Be(10010067);
        rows[0].FullName.Should().Be("JUAN PEREZ LOPEZ");
        rows[0].FileName.Should().Be("10010067_0000001.pdf");
    }

    [Fact]
    public void Parse_SkipsBlankLines()
    {
        var csv = "10010067,JUAN,a.pdf\n\n10010079,MARIA,b.pdf\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        new CsvPadronLoader().Parse(ms).Should().HaveCount(2);
    }
}
