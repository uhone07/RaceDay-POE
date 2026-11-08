using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

// Uploads images to Azure Blob Storage and returns the public URL.
public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;

    public BlobStorageService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        var connectionString = _config["AzureBlob:ConnectionString"];
        var containerName = _config["AzureBlob:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}