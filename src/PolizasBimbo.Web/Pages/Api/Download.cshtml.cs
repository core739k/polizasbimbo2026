using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolizasBimbo.Application.UseCases.DownloadPolicy;

namespace PolizasBimbo.Web.Pages.Api;

public class DownloadModel : PageModel
{
    private readonly DownloadPolicyHandler _handler;
    public DownloadModel(DownloadPolicyHandler handler) => _handler = handler;

    public async Task<IActionResult> OnGetAsync(string token, CancellationToken ct)
    {
        var result = await _handler.HandleAsync(new DownloadPolicyRequest(token), ct);

        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        return result switch
        {
            DownloadPolicyResponse.Ok ok => new FileStreamResult(ok.Blob.Content, ok.Blob.ContentType)
            {
                FileDownloadName = ok.DisplayFileName
            },
            DownloadPolicyResponse.InvalidToken => StatusCode(StatusCodes.Status401Unauthorized, new { error = "Token inválido." }),
            DownloadPolicyResponse.Expired => StatusCode(StatusCodes.Status410Gone, new { error = "El enlace expiró. Realiza una nueva búsqueda." }),
            DownloadPolicyResponse.AlreadyUsed => StatusCode(StatusCodes.Status410Gone, new { error = "Este enlace ya fue utilizado." }),
            DownloadPolicyResponse.NotFound => NotFound(new { error = "Archivo no encontrado." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
