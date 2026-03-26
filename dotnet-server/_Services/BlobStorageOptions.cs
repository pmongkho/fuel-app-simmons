namespace dotnet_server.Application.Services;

public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "fuel-photos";

    public string BuildConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
            return ConnectionString;

        if (string.IsNullOrWhiteSpace(AccountName) || string.IsNullOrWhiteSpace(AccessKey))
            return string.Empty;

        return $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccessKey};EndpointSuffix=core.windows.net";
    }
}
