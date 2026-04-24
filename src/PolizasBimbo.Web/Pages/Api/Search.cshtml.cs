using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using PolizasBimbo.Application.UseCases.SearchPolicies;

namespace PolizasBimbo.Web.Pages.Api;

[EnableRateLimiting("search")]
[IgnoreAntiforgeryToken]
public class SearchModel : PageModel
{
    private readonly SearchPoliciesHandler _handler;
    public SearchModel(SearchPoliciesHandler handler) => _handler = handler;

    public class SearchInput
    {
        public string Nombre { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync([FromBody] SearchInput input, CancellationToken ct)
    {
        try
        {
            var response = await _handler.HandleAsync(new SearchPoliciesRequest(input.Nombre), ct);
            return new JsonResult(new
            {
                results = response.Results.Select(r => new { r.PolicyId, r.FileName, r.DownloadToken })
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
