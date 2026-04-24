using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Application.UseCases.LoadPadron;

public sealed record LoadPadronRequest(Stream CsvStream);

public sealed record LoadPadronResponse(int RowsLoaded);

public sealed class LoadPadronHandler
{
    private readonly IPadronLoader _loader;
    private readonly IPolicyRepository _policies;

    public LoadPadronHandler(IPadronLoader loader, IPolicyRepository policies)
    {
        _loader = loader;
        _policies = policies;
    }

    public async Task<LoadPadronResponse> HandleAsync(LoadPadronRequest request, CancellationToken ct)
    {
        var policies = _loader.Parse(request.CsvStream).ToList();
        await _policies.ReplaceAllAsync(policies, ct);
        return new LoadPadronResponse(policies.Count);
    }
}
