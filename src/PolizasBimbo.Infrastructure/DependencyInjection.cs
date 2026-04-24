using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Infrastructure.Padron;
using PolizasBimbo.Infrastructure.Persistence;
using PolizasBimbo.Infrastructure.Persistence.Repositories;
using PolizasBimbo.Infrastructure.Security;
using PolizasBimbo.Infrastructure.Storage;
using PolizasBimbo.Infrastructure.Time;

namespace PolizasBimbo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default")));

        services.Configure<BlobStorageOptions>(config.GetSection("BlobStorage"));
        services.Configure<TokenSignerOptions>(config.GetSection("TokenSigner"));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPolicyRepository, SqlPolicyRepository>();
        services.AddScoped<IDownloadAuditRepository, SqlDownloadAuditRepository>();
        services.AddScoped<IDownloadTokenRepository, SqlDownloadTokenRepository>();
        services.AddSingleton<IPolicyBlobStorage, AzureBlobPolicyStorage>();
        services.AddSingleton<ITokenSigner, JwtTokenSigner>();
        services.AddSingleton<IPadronLoader, CsvPadronLoader>();

        return services;
    }
}
