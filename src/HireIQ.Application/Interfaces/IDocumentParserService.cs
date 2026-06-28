namespace HireIQ.Application.Interfaces;

/// <summary>
/// Extracts plain text from any uploaded job-description document.
/// PDF, DOCX, XLSX, CSV, or plain text — single entry point.
/// </summary>
public interface IDocumentParserService
{
    /// <summary>Returns extracted plain text. Empty string if format unsupported.</summary>
    Task<string> ExtractTextAsync(Stream stream, string fileName, string? contentType, CancellationToken ct = default);
}
