using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string? Prefix { get; set; }
}

public sealed class AzureBlobPolicyStorage : IPolicyBlobStorage
{
    private readonly BlobContainerClient _container;
    private readonly string _prefix;

    public AzureBlobPolicyStorage(IOptions<BlobStorageOptions> options)
    {
        var opt = options.Value;
        _container = new BlobContainerClient(opt.ConnectionString, opt.Container);
        _prefix = string.IsNullOrWhiteSpace(opt.Prefix) ? string.Empty : opt.Prefix.TrimEnd('/') + "/";
    }

    public async Task<BlobDownload?> OpenReadAsync(string fileName, CancellationToken ct)
    {
        var blob = _container.GetBlobClient(_prefix + fileName);
        try
        {
            var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
            var details = response.Value.Details;
            return new BlobDownload(response.Value.Content, details.ContentType ?? "application/octet-stream", details.ContentLength);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
