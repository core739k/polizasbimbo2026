using PolizasBimbo.Application.UseCases.SearchPolicies;

namespace PolizasBimbo.Web.Endpoints.V1.Polizas;

public static class BuscarEndpoint
{
    public static async Task<IResult> HandleAsync(
        BuscarRequest request,
        SearchPoliciesHandler handler,
        CancellationToken ct)
    {
        try
        {
            var response = await handler.HandleAsync(
                new SearchPoliciesRequest(request.IdColaborador, request.Email, request.Telefono),
                ct);

            var resultados = response.Results
                .Select(r => new BuscarResultado(r.FileName, r.DisplayName, r.DownloadToken))
                .ToArray();

            return Results.Ok(resultados);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
