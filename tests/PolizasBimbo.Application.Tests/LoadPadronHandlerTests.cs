using FluentAssertions;
using Moq;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Application.UseCases.LoadPadron;
using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Application.Tests;

public class LoadPadronHandlerTests
{
    [Fact]
    public async Task Handle_ParsesAndReplaces()
    {
        var loader = new Mock<IPadronLoader>();
        var policies = new Mock<IPolicyRepository>();

        var parsed = new[]
        {
            Policy.Create(0, 1, "A", "a.pdf"),
            Policy.Create(0, 2, "B", "b.pdf"),
        };
        loader.Setup(l => l.Parse(It.IsAny<Stream>())).Returns(parsed);

        var sut = new LoadPadronHandler(loader.Object, policies.Object);
        var response = await sut.HandleAsync(new LoadPadronRequest(Stream.Null), CancellationToken.None);

        response.RowsLoaded.Should().Be(2);
        policies.Verify(p => p.ReplaceAllAsync(It.IsAny<IEnumerable<Policy>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
