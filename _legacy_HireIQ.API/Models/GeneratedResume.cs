namespace HireIQ.API.Models;

public class GeneratedResume
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? HtmlContent { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}