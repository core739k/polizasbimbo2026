using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Application.Abstractions;

public interface IPadronLoader
{
    IEnumerable<Policy> Parse(Stream csvStream);
}
