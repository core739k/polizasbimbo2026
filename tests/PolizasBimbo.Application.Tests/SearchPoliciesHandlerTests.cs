using FluentAssertions;
using Moq;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Application.Tests.Helpers;
using PolizasBimbo.Application.UseCases.SearchPolicies;
using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Application.Tests;

public class SearchPoliciesHandlerTests
{
    private readonly Mock<IPolicyBlobStorage> _blob = new();
    private readonly Mock<IDownloadTokenRepository> _tokens = new();
    private readonly Mock<ITokenSigner> _signer = new();
    private readonly FixedClock _clock = new(new DateTime(2026, 4, 24, 12, 0, 0, DateTimeKind.Utc));

    private SearchPoliciesHandler CreateSut() =>
        new(_blob.Object, _tokens.Object, _signer.Object, _clock);

    private static SearchPoliciesRequest ValidRequest(int id = 10010081) =>
        new(id, "user@example.com", "5551234567");

    [Fact]
    public async Task Handle_InvalidIdColaborador_Throws()
    {
        var act = () => CreateSut().HandleAsync(new SearchPoliciesRequest(0, "u@e.com", "5551234567"), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_InvalidEmail_Throws()
    {
        var act = () => CreateSut().HandleAsync(new SearchPoliciesRequest(1, "not-an-email", "5551234567"), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_InvalidPhone_Throws()
    {
        var act = () => CreateSut().HandleAsync(new SearchPoliciesRequest(1, "u@e.com", "123"), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_NoMatchingBlobs_ReturnsEmpty()
    {
        _blob.Setup(b => b.ListByCollaboratorAsync(10010081, It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<string>());

        var response = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);

        response.Results.Should().BeEmpty();
        _tokens.Verify(t => t.AddAsync(It.IsAny<DownloadToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HappyPath_IssuesTokensAndDerivesDisplayName()
    {
        _blob.Setup(b => b.ListByCollaboratorAsync(10010081, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[]
             {
                 "bimbo/renovacion2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf",
                 "bimbo/renovacion2026/10010081_JUAN PEREZ.pdf"
             });

        _signer.Setup(s => s.Issue(It.IsAny<Guid>(), _clock.UtcNow, It.IsAny<TimeSpan>())).Returns("jwt");

        var response = await CreateSut().HandleAsync(ValidRequest(), CancellationToken.None);

        response.Results.Should().HaveCount(2);
        response.Results[0].FileName.Should().Be("bimbo/renovacion2026/10010081_MARIA GUADALUPE ZEPEDA CASTILLO.pdf");
        response.Results[0].DisplayName.Should().Be("MARIA GUADALUPE ZEPEDA CASTILLO");
        response.Results[0].DownloadToken.Should().Be("jwt");
        response.Results[1].DisplayName.Should().Be("JUAN PEREZ");

        _tokens.Verify(t => t.AddAsync(
            It.Is<DownloadToken>(d =>
                d.IdColaborador == 10010081 &&
                d.Email == "user@example.com" &&
                d.Phone == "5551234567"),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
