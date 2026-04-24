using FluentAssertions;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("NAME.LAST+tag@sub.domain.mx")]
    public void Create_WithValidAddress_Lowercases(string input)
    {
        var email = Email.Create(input);
        email.Value.Should().Be(input.ToLowerInvariant());
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("")]
    public void Create_WithInvalid_Throws(string input)
    {
        var act = () => Email.Create(input);
        act.Should().Throw<ArgumentException>();
    }
}
