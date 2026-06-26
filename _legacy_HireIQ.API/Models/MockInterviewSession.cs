namespace HireIQ.API.Models;

public class MockInterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? JobTitle { get; set; }
    public string? JobDescription { get; set; }
    public List<string> Questions { get; set; } = new();
    public List<string> Answers { get; set; } = new();
    public string? AiEvaluation { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public decimal? OverallScore { get; set; }
    public int? DurationSeconds { get; set; }
    public string Status { get; set; } = "InProgress"; // ✅ InProgress | Completed — survives restarts
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
