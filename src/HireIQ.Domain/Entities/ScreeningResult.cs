using HireIQ.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

public class ScreeningResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResumeId { get; set; }
    public Guid JdId { get; set; }
    public decimal MinilmScore { get; set; }
    public string? HireiqAnalysis { get; set; }
    public bool Shortlisted { get; set; } = false;
    public string CandidateStatus { get; set; } = "Screened"; // Screened | Interview | Hired | Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Resume? Resume { get; set; }

    // ← Navigation property ka naam change karo!
    [System.Text.Json.Serialization.JsonIgnore]
    public JobDescription? JobDescription { get; set; }
}