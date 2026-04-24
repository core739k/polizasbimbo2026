using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PolizasBimbo.Application.UseCases.LoadPadron;

namespace PolizasBimbo.Web.Pages.Admin;

public sealed class AdminOptions
{
    public string ApiKey { get; set; } = string.Empty;
}

[IgnoreAntiforgeryToken]
public class LoadPadronModel : PageModel
{
    private readonly LoadPadronHandler _handler;
    private readonly string _expectedKey;

    public LoadPadronModel(LoadPadronHandler handler, IConfiguration config)
    {
        _handler = handler;
        _expectedKey = config["Admin:ApiKey"] ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file, [FromHeader(Name = "X-Admin-Key")] string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_expectedKey))
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Admin:ApiKey no configurado." });
        if (!string.Equals(apiKey, _expectedKey, StringComparison.Ordinal))
            return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Archivo vacío." });

        await using var stream = file.OpenReadStream();
        var result = await _handler.HandleAsync(new LoadPadronRequest(stream), ct);
        return new JsonResult(new { rowsLoaded = result.RowsLoaded });
    }
}
