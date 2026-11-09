// Add to RaceDay.API (Part 2). The MVC project never talks to Azure directly;
// it posts the file to the API, and this service stores it in Azure Blob Storage.
//
// Package:  dotnet add RaceDay.API package Azure.Storage.Blobs
// Config:   AzureBlob:ConnectionString and AzureBlob:ContainerName in user secrets
//           or environment variables (never commit the connection string).
// Register: builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");
        var containerName = config["AzureBlob:ContainerName"] ?? "raceday-images";
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        }, ct);

        return blob.Uri.ToString();
    }
}
