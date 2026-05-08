using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Integration.Tests.Infrastructure;

public sealed class PolizasApiFactory : WebApplicationFactory<Program>
{
    public Mock<IPolicyBlobStorage> Blob { get; } = new();
    public Mock<IDownloadTokenRepository> Tokens { get; } = new();
    public Mock<IDownloadAuditRepository> Audit { get; } = new();
    public Mock<ITokenSigner> Signer { get; } = new();
    public Mock<IClock> Clock { get; } = new();

    public DateTime UtcNow { get; set; } = new(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc);

    public PolizasApiFactory()
    {
        Clock.SetupGet(c => c.UtcNow).Returns(() => UtcNow);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=test;Database=test;Integrated Security=true;TrustServerCertificate=true",
                ["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
                ["BlobStorage:Container"] = "archivos",
                ["BlobStorage:Prefix"] = "bimbo/renovacion-2026/",
                ["TokenSigner:SigningKey"] = "test-signing-key-with-at-least-32-characters",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            ReplaceSingleton(services, Blob.Object);
            ReplaceSingleton(services, Tokens.Object);
            ReplaceSingleton(services, Audit.Object);
            ReplaceSingleton(services, Signer.Object);
            ReplaceSingleton(services, Clock.Object);

            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
            services.Configure<RateLimiterOptions>(opt =>
            {
                opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opt.AddPolicy<string>("search", _ => RateLimitPartition.GetNoLimiter<string>("test"));
            });
        });
    }

    private static void ReplaceSingleton<TService>(IServiceCollection services, TService instance)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(instance);
    }
}
