using PolizasBimbo.Domain.Entities;
using PolizasBimbo.Domain.ValueObjects;

namespace PolizasBimbo.Application.Abstractions;

public interface IPolicyRepository
{
    Task<IReadOnlyList<Policy>> SearchByNameAsync(SearchTerm term, int take, CancellationToken ct);
    Task<Policy?> GetByIdAsync(int id, CancellationToken ct);
    Task UpdateContactAsync(int numColaborador, string email, string phone, DateTime utcNow, CancellationToken ct);
    Task ReplaceAllAsync(IEnumerable<Policy> policies, CancellationToken ct);
}
