namespace HireIQ.Domain.Entities;

public class InterviewRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoomCode { get; set; } = string.Empty;
    public string RoomPassword { get; set; } = string.Empty;
    public Guid HirerId { get; set; }
    public Guid? JobId { get; set; }
    public string CandidateEmail { get; set; } = string.Empty;
    public Guid? CandidateUserId { get; set; }
    public string? CandidateName { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled | Active | Completed | Cancelled
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public List<string> PresetQuestions { get; set; } = new();
    public string? AiReport { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? BehavioralScore { get; set; }
    public decimal? AttentionScore { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public decimal? EmotionScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public string FinalDecision { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Hirer { get; set; }
    public JobDescription? Job { get; set; }
}
