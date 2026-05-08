using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PolizasBimbo.Application.UseCases.DownloadPolicy;
using PolizasBimbo.Application.UseCases.SearchPolicies;
using PolizasBimbo.Infrastructure;
using PolizasBimbo.Web.Endpoints.V1.Polizas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<SearchPoliciesHandler>();
builder.Services.AddScoped<DownloadPolicyHandler>();
builder.Services.AddAntiforgery();

const string AngularCorsPolicy = "angular";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("search", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors(AngularCorsPolicy);
app.UseRateLimiter();
app.UseAntiforgery();
app.UseAuthorization();
app.MapRazorPages();
app.MapPolizasV1Endpoints();

app.Run();

public partial class Program { }
