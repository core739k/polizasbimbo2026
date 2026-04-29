using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Infrastructure.Storage;

public sealed class AzureBlobPolicyStorage : IPolicyBlobStorage
{
    private readonly BlobContainerClient _container;
    private readonly string _prefix;
    private readonly ILogger<AzureBlobPolicyStorage> _log;

    public AzureBlobPolicyStorage(IOptions<BlobStorageOptions> options, ILogger<AzureBlobPolicyStorage> log)
    {
        var opt = options.Value;
        _container = new BlobContainerClient(opt.ConnectionString, opt.Container);
        _prefix = string.IsNullOrEmpty(opt.Prefix) ? string.Empty : opt.Prefix;
        _log = log;
        _log.LogInformation(
            "AzureBlobPolicyStorage initialized. AccountName={Account} Container={Container} Prefix='{Prefix}'",
            _container.AccountName, _container.Name, _prefix);
    }

    public async Task<IReadOnlyList<string>> ListByCollaboratorAsync(int idColaborador, CancellationToken ct)
    {
        var prefix = $"{_prefix}{idColaborador}_";
        _log.LogInformation(
            "ListByCollaboratorAsync: Account={Account} Container={Container} Prefix='{Prefix}'",
            _container.AccountName, _container.Name, prefix);

        var names = new List<string>();
        try
        {
            await foreach (var item in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct))
            {
                _log.LogInformation("ListByCollaboratorAsync: hit Name='{Name}' Size={Size}", item.Name, item.Properties?.ContentLength);
                names.Add(item.Name);
            }
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex,
                "ListByCollaboratorAsync FAILED. Status={Status} ErrorCode={ErrorCode} Prefix='{Prefix}'",
                ex.Status, ex.ErrorCode, prefix);
            throw;
        }

        _log.LogInformation("ListByCollaboratorAsync: prefix='{Prefix}' matched {Count} blob(s)", prefix, names.Count);
        return names;
    }

    public async Task<BlobDownload?> OpenReadAsync(string fileName, CancellationToken ct)
    {
        var blob = _container.GetBlobClient(fileName);
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
