namespace HireIQ.Application.Interfaces;

public interface IPdfService
{
    Task<byte[]> RenderHtmlToPdfAsync(string html, CancellationToken ct = default);
}

public interface IPdfExtractorService
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default);
}
