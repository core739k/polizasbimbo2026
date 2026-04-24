using FluentAssertions;
using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Domain.Tests.Entities;

public class PolicyTests
{
    [Fact]
    public void Create_WithRequiredFields_SetsProperties()
    {
        var p = Policy.Create(id: 42, numColaborador: 10010067, fullName: "JUAN PEREZ LOPEZ", fileName: "10010067_0000001.pdf");
        p.Id.Should().Be(42);
        p.NumColaborador.Should().Be(10010067);
        p.FullName.Should().Be("JUAN PEREZ LOPEZ");
        p.FileName.Should().Be("10010067_0000001.pdf");
    }

    [Fact]
    public void Create_WithEmptyFileName_Throws()
    {
        var act = () => Policy.Create(1, 1, "X", "");
        act.Should().Throw<ArgumentException>();
    }
}
