namespace HireIQ.Application.Interfaces;

/// <summary>
/// Blob storage abstraction — implemented by Azure Blob in Infrastructure.
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadAsync(string container, string blobName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string container, string blobName, CancellationToken ct = default);
    Task<bool> DeleteAsync(string container, string blobName, CancellationToken ct = default);
    Task<string> GetSignedUrlAsync(string container, string blobName, TimeSpan validFor, CancellationToken ct = default);
}
