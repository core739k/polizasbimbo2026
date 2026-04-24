using FluentAssertions;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Domain.Tests.ValueObjects;

public class PhoneTests
{
    [Theory]
    [InlineData("5551234567", "5551234567")]
    [InlineData("(55) 5123-4567", "5551234567")]
    [InlineData(" 55 5123 4567 ", "5551234567")]
    public void Create_NormalizesTo10DigitNumbers(string input, string expected)
    {
        var phone = Phone.Create(input);
        phone.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901")]
    [InlineData("abcd")]
    [InlineData("")]
    public void Create_WithInvalid_Throws(string input)
    {
        var act = () => Phone.Create(input);
        act.Should().Throw<ArgumentException>();
    }
}
