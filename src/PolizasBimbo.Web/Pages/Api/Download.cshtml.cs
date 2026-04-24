using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolizasBimbo.Application.UseCases.DownloadPolicy;

namespace PolizasBimbo.Web.Pages.Api;

[IgnoreAntiforgeryToken]
public class DownloadModel : PageModel
{
    private readonly DownloadPolicyHandler _handler;
    public DownloadModel(DownloadPolicyHandler handler) => _handler = handler;

    public async Task<IActionResult> OnPostAsync(
        string token,
        [FromForm] string email,
        [FromForm] string telefono,
        [FromForm] string? pais,
        [FromForm] string? ciudad,
        CancellationToken ct)
    {
        DownloadPolicyResponse result;
        try
        {
            result = await _handler.HandleAsync(
                new DownloadPolicyRequest(token, email, telefono, pais, ciudad), ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return result switch
        {
            DownloadPolicyResponse.Ok ok => File(
                ok.Blob.Content,
                ok.Blob.ContentType,
                fileDownloadName: ok.FileName),
            DownloadPolicyResponse.InvalidToken => StatusCode(StatusCodes.Status401Unauthorized, new { error = "Token inválido." }),
            DownloadPolicyResponse.Expired => StatusCode(StatusCodes.Status410Gone, new { error = "El enlace expiró. Realiza una nueva búsqueda." }),
            DownloadPolicyResponse.AlreadyUsed => StatusCode(StatusCodes.Status410Gone, new { error = "Este enlace ya fue utilizado." }),
            DownloadPolicyResponse.NotFound => NotFound(new { error = "Archivo no encontrado." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
