// Services/PdfService.cs
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace HireIQ.API.Services;

public class PdfService
{
    public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent)
    {
        // Browser download karo (first time only, ~100MB)
        await new BrowserFetcher().DownloadAsync();

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" } // Render/Docker ke liye
        });

        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(htmlContent, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });

        var pdf = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true, // CSS background colors/images include karo
            MarginOptions = new MarginOptions
            {
                Top = "20px",
                Bottom = "20px",
                Left = "20px",
                Right = "20px"
            }
        });

        return pdf;
    }
}