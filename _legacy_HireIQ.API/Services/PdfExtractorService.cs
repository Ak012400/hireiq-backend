// Services/PdfExtractorService.cs
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace HireIQ.API.Services;

public class PdfExtractorService
{
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