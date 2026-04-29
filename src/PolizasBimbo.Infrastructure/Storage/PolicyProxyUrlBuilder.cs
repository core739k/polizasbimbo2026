using Microsoft.Extensions.Options;
using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Infrastructure.Storage;

public sealed class PolicyProxyUrlBuilder : IPolicyDownloadUrlBuilder
{
    private readonly string _baseUrl;

    public PolicyProxyUrlBuilder(IOptions<PolicyProxyOptions> options)
    {
        var raw = options.Value.BaseUrl ?? string.Empty;
        _baseUrl = raw.TrimEnd('/') + "/";
    }

    public string Build(string fileName) => _baseUrl + Uri.EscapeDataString(fileName);
}
