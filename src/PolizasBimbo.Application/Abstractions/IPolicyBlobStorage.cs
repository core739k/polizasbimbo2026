namespace PolizasBimbo.Application.Abstractions;

public interface IPolicyBlobStorage
{
    Task<BlobDownload?> OpenReadAsync(string fileName, CancellationToken ct);
}

public sealed record BlobDownload(Stream Content, string ContentType, long? Length);
