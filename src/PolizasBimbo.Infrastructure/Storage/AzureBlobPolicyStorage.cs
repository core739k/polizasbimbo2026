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
        var marker = $"{idColaborador}_";
        _log.LogInformation(
            "ListByCollaboratorAsync: Account={Account} Container={Container} RootPrefix='{Prefix}' Marker='{Marker}'",
            _container.AccountName, _container.Name, _prefix, marker);

        var names = new List<string>();
        try
        {
            await foreach (var item in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, _prefix, ct))
            {
                if (!MatchesCollaborator(item.Name, marker)) continue;
                _log.LogInformation("ListByCollaboratorAsync: hit Name='{Name}' Size={Size}", item.Name, item.Properties?.ContentLength);
                names.Add(item.Name);
            }
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex,
                "ListByCollaboratorAsync FAILED. Status={Status} ErrorCode={ErrorCode} RootPrefix='{Prefix}' Marker='{Marker}'",
                ex.Status, ex.ErrorCode, _prefix, marker);
            throw;
        }

        _log.LogInformation("ListByCollaboratorAsync: marker='{Marker}' matched {Count} blob(s)", marker, names.Count);
        return names;
    }

    internal static bool MatchesCollaborator(string blobName, string marker)
    {
        var slash = blobName.LastIndexOf('/');
        var leaf = slash >= 0 ? blobName[(slash + 1)..] : blobName;
        return leaf.StartsWith(marker, StringComparison.Ordinal);
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
