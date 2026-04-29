namespace PolizasBimbo.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
}
