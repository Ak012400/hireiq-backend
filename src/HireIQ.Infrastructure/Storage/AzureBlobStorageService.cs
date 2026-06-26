using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using HireIQ.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireIQ.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly AzureBlobSettings _settings;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient client,
        IOptions<AzureBlobSettings> settings,
        ILogger<AzureBlobStorageService> logger)
    {
        _client = client;
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task<BlobContainerClient> GetContainerAsync(string container, CancellationToken ct)
    {
        var c = _client.GetBlobContainerClient(container);
        if (_settings.CreateContainerIfMissing)
            await c.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        return c;
    }

    public async Task<string> UploadAsync(string container, string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var c = await GetContainerAsync(container, ct);
        var blob = c.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        _logger.LogInformation("Uploaded blob {Container}/{Blob}", container, blobName);
        return blob.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string container, string blobName, CancellationToken ct = default)
    {
        var c = _client.GetBlobContainerClient(container);
        var blob = c.GetBlobClient(blobName);
        var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return download.Value.Content;
    }

    public async Task<bool> DeleteAsync(string container, string blobName, CancellationToken ct = default)
    {
        var c = _client.GetBlobContainerClient(container);
        var blob = c.GetBlobClient(blobName);
        var result = await blob.DeleteIfExistsAsync(cancellationToken: ct);
        return result.Value;
    }

    public Task<string> GetSignedUrlAsync(string container, string blobName, TimeSpan validFor, CancellationToken ct = default)
    {
        var c = _client.GetBlobContainerClient(container);
        var blob = c.GetBlobClient(blobName);

        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException("Blob client cannot generate SAS — use a connection string with account key.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        return Task.FromResult(blob.GenerateSasUri(sasBuilder).ToString());
    }
}
