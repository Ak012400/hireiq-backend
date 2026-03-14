namespace HireIQ.API.Models;

public class ScreeningResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResumeId { get; set; }
    public Guid JdId { get; set; }
    public decimal MinilmScore { get; set; }
    public string? HireiqAnalysis { get; set; }
    public bool Shortlisted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Resume? Resume { get; set; }
    public JobDescription? JobDescription { get; set; }
}