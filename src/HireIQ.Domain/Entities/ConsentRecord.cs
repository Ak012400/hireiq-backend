namespace HireIQ.Domain.Entities;

public enum ConsentKind
{
    AiInterviewRecording,   // video + audio + transcript + AI scoring
    DataProcessing,         // resume / personal data processing
    EmailCommunications
}

/// <summary>
/// Audit-grade record of candidate consent — required for GDPR / India IT Act compliance.
/// Never delete rows; consent withdrawal creates a new record with Withdrawn=true.
/// </summary>
public class ConsentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ConsentKind Kind { get; set; }
    public Guid? RelatedEntityId { get; set; }      // e.g. CandidateJourney for interview consent
    public string PolicyVersion { get; set; } = "1.0";
    public bool Granted { get; set; }
    public bool Withdrawn { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
