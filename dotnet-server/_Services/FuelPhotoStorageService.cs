using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace dotnet_server.Application.Services;

public class FuelPhotoStorageService(IOptions<BlobStorageOptions> options)
{
    private readonly BlobStorageOptions _options = options.Value;

    public async Task<string> UploadAsync(Stream fileStream, string contentType, string blobName, CancellationToken cancellationToken = default)
    {
        var connectionString = _options.BuildConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Blob storage connection settings are not configured.");

        var containerClient = new BlobContainerClient(connectionString, _options.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);

        return blobClient.Uri.ToString();
    }
}
