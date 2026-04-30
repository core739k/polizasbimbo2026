using FluentAssertions;
using PolizasBimbo.Infrastructure.Storage;

namespace PolizasBimbo.Infrastructure.Tests.Storage;

public class AzureBlobPolicyStorageTests
{
    [Theory]
    [InlineData("bimbo/renovacion-2026/Barcel/12345_juan.pdf", "12345_", true)]
    [InlineData("bimbo/renovacion-2026/Bimbo/12345_juan.pdf", "12345_", true)]
    [InlineData("bimbo/renovacion-2026/Barcel/9999_otro.pdf", "12345_", false)]
    [InlineData("bimbo/renovacion-2026/Barcel/123456_juan.pdf", "12345_", false)]
    [InlineData("bimbo/renovacion-2026/12345_juan.pdf", "12345_", true)]
    [InlineData("bimbo/renovacion-2026/Barcel/sub/12345_x.pdf", "12345_", true)]
    [InlineData("12345_juan.pdf", "12345_", true)]
    public void MatchesCollaborator_returns_expected(string blobName, string marker, bool expected)
    {
        AzureBlobPolicyStorage.MatchesCollaborator(blobName, marker).Should().Be(expected);
    }
}
