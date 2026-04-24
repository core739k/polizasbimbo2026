using FluentAssertions;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Tests.ValueObjects;

public class SearchTermTests
{
    [Fact]
    public void Create_WithFewerThan5Characters_Throws()
    {
        var act = () => SearchTerm.Create("ana");
        act.Should().Throw<ArgumentException>().WithMessage("*5*");
    }

    [Fact]
    public void Create_WithNullOrWhitespace_Throws()
    {
        var act1 = () => SearchTerm.Create("");
        var act2 = () => SearchTerm.Create("     ");
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("  HeRnÁndez  ", "HERNANDEZ")]
    [InlineData("lópez garcía", "LOPEZ GARCIA")]
    [InlineData("Núñez", "NUNEZ")]
    public void Normalized_UppercasesAndRemovesAccentsAndTrims(string input, string expected)
    {
        var term = SearchTerm.Create(input);
        term.Normalized.Should().Be(expected);
    }

    [Fact]
    public void ToFullTextQuery_BuildsAndClauseOfPrefixTerms()
    {
        var term = SearchTerm.Create("Hernandez Lopez");
        term.ToFullTextQuery().Should().Be("\"HERNANDEZ*\" AND \"LOPEZ*\"");
    }

    [Fact]
    public void ToFullTextQuery_SingleWord_WrapsAsPrefix()
    {
        var term = SearchTerm.Create("Hernandez");
        term.ToFullTextQuery().Should().Be("\"HERNANDEZ*\"");
    }
}
