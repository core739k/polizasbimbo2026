using Microsoft.AspNetCore.Http;
using PolizasBimbo.Application.UseCases.DownloadPolicy;

namespace PolizasBimbo.Web.Endpoints.V1.Polizas;

public static class DescargarEndpoint
{
    public static async Task<IResult> HandleAsync(
        string token,
        DownloadPolicyHandler handler,
        HttpResponse httpResponse,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DownloadPolicyRequest(token), ct);

        httpResponse.Headers.CacheControl = "no-store";
        httpResponse.Headers.Pragma = "no-cache";

        return result switch
        {
            DownloadPolicyResponse.Ok ok => Results.File(
                ok.Blob.Content,
                ok.Blob.ContentType,
                fileDownloadName: ok.DisplayFileName),
            DownloadPolicyResponse.InvalidToken =>
                Results.Json(new { error = "invalid_token" }, statusCode: StatusCodes.Status401Unauthorized),
            DownloadPolicyResponse.Expired =>
                Results.Json(new { error = "expired" }, statusCode: StatusCodes.Status410Gone),
            DownloadPolicyResponse.AlreadyUsed =>
                Results.Json(new { error = "already_used" }, statusCode: StatusCodes.Status410Gone),
            DownloadPolicyResponse.NotFound =>
                Results.Json(new { error = "not_found" }, statusCode: StatusCodes.Status404NotFound),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
