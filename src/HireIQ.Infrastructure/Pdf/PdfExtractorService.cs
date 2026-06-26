// Services/PdfExtractorService.cs
using HireIQ.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace HireIQ.Infrastructure.Pdf;

public class PdfExtractorService : IPdfExtractorService
{
    public async Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, ct);
        return ExtractText(ms.ToArray());
    }

    public string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var sb = new System.Text.StringBuilder();

        foreach (var page in document.GetPages())
        {
            // Words with positions — multi-column resumes bhi handle hoga
            var words = page.GetWords();
            sb.AppendJoin(" ", words.Select(w => w.Text));
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }
}