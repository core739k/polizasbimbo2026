namespace PolizasBimbo.Web.Endpoints.V1.Polizas;

public static class PolizasV1Endpoints
{
    public const string CorsPolicyName = "angular";

    public static IEndpointRouteBuilder MapPolizasV1Endpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/polizas")
                          .RequireCors(CorsPolicyName);

        group.MapPost("/buscar", BuscarEndpoint.HandleAsync)
             .RequireRateLimiting("search");

        group.MapGet("/descargar/{token}", DescargarEndpoint.HandleAsync);

        return routes;
    }
}
