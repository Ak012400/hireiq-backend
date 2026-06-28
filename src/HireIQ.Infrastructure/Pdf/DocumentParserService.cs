using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using HireIQ.Application.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace HireIQ.Infrastructure.Pdf;

/// <summary>
/// Unified text extraction for PDF / DOCX / XLSX / CSV / TXT.
/// Picks the right library based on file extension.
/// </summary>
public sealed class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;
    public DocumentParserService(ILogger<DocumentParserService> logger) => _logger = logger;

    public async Task<string> ExtractTextAsync(Stream stream, string fileName, string? contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        // Buffer to memory — most parsers need seekable stream
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        try
        {
            return ext switch
            {
                ".pdf"  => ExtractPdf(ms),
                ".docx" => ExtractDocx(ms),
                ".xlsx" or ".xlsm" => ExtractXlsx(ms),
                ".csv"  or ".txt"  => Encoding.UTF8.GetString(ms.ToArray()),
                _ => Encoding.UTF8.GetString(ms.ToArray()),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document extraction failed for {File}", fileName);
            return string.Empty;
        }
    }

    private static string ExtractPdf(Stream s)
    {
        using var doc = PdfDocument.Open(s);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            sb.AppendJoin(' ', page.GetWords().Select(w => w.Text));
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private static string ExtractDocx(Stream s)
    {
        using var word = WordprocessingDocument.Open(s, false);
        return word.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
    }

    private static string ExtractXlsx(Stream s)
    {
        using var wb = new XLWorkbook(s);
        var sb = new StringBuilder();
        foreach (var ws in wb.Worksheets)
        {
            sb.AppendLine($"# Sheet: {ws.Name}");
            foreach (var row in ws.RowsUsed())
            {
                sb.AppendLine(string.Join(" | ", row.CellsUsed().Select(c => c.GetString())));
            }
        }
        return sb.ToString().Trim();
    }
}
