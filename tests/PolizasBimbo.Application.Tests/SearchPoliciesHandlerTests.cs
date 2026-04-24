using FluentAssertions;
using Moq;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Application.Tests.Helpers;
using PolizasBimbo.Application.UseCases.SearchPolicies;
using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Application.Tests;

public class SearchPoliciesHandlerTests
{
    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly Mock<IDownloadTokenRepository> _tokens = new();
    private readonly Mock<ITokenSigner> _signer = new();
    private readonly FixedClock _clock = new(new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc));

    private SearchPoliciesHandler CreateSut() => new(_policies.Object, _tokens.Object, _signer.Object, _clock);

    [Fact]
    public async Task Search_WithTermTooShort_Throws()
    {
        var sut = CreateSut();
        var act = () => sut.HandleAsync(new SearchPoliciesRequest("ana"), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Search_WithResults_IssuesTokenPerPolicy()
    {
        var matches = new[]
        {
            Policy.Create(1, 10010067, "JUAN PEREZ LOPEZ", "10010067_0000001.pdf"),
            Policy.Create(2, 10010067, "JUAN PEREZ LOPEZ", "10010067_0000002.pdf")
        };

        _policies.Setup(r => r.SearchByNameAsync(It.IsAny<SearchTerm>(), 5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(matches);
        _signer.Setup(s => s.Issue(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
               .Returns<Guid, int, DateTime, TimeSpan>((jti, _, _, _) => $"jwt-{jti}");

        var sut = CreateSut();
        var response = await sut.HandleAsync(new SearchPoliciesRequest("Perez"), CancellationToken.None);

        response.Results.Should().HaveCount(2);
        response.Results[0].FileName.Should().Be("10010067_0000001.pdf");
        response.Results[0].DownloadToken.Should().StartWith("jwt-");
        _tokens.Verify(t => t.AddAsync(It.IsAny<DownloadToken>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmpty()
    {
        _policies.Setup(r => r.SearchByNameAsync(It.IsAny<SearchTerm>(), 5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Array.Empty<Policy>());
        var sut = CreateSut();
        var response = await sut.HandleAsync(new SearchPoliciesRequest("Zzzzz"), CancellationToken.None);
        response.Results.Should().BeEmpty();
        _tokens.Verify(t => t.AddAsync(It.IsAny<DownloadToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
