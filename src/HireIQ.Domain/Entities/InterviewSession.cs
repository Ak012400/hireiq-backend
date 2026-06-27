namespace HireIQ.Domain.Entities;

public enum InterviewSessionStatus
{
    Scheduled, InProgress, Completed, Abandoned, Failed
}

/// <summary>
/// A live AI-conducted interview session. Extends InterviewRoom with telemetry / recording / AI swarm data.
/// One InterviewSession per scheduled interview (room can be reused for retakes if needed).
/// </summary>
public class InterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InterviewRoomId { get; set; }
    public Guid CandidateJourneyId { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int TotalDurationSeconds { get; set; }

    // Recordings (Azure Blob URLs)
    public string? VideoRecordingUrl { get; set; }
    public string? AudioRecordingUrl { get; set; }
    public string? TranscriptUrl { get; set; }

    public InterviewSessionStatus Status { get; set; } = InterviewSessionStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav
    public InterviewRoom? InterviewRoom { get; set; }
    public CandidateJourney? CandidateJourney { get; set; }
}

public class InterviewTranscript
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public int SegmentIndex { get; set; }
    public string Speaker { get; set; } = "CANDIDATE";     // CANDIDATE | AI | SYSTEM
    public string Text { get; set; } = string.Empty;
    public long StartMs { get; set; }
    public long EndMs { get; set; }
    public float? Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public InterviewSession? Session { get; set; }
}

public class InterviewQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public int QuestionOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Source { get; set; } = "AI_GENERATED";   // PRESET | AI_GENERATED | FOLLOWUP
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;

    public InterviewSession? Session { get; set; }
}

public class InterviewAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public string TranscriptSegmentIdsJson { get; set; } = "[]";  // Guid[]
    public string AnswerText { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    public InterviewSession? Session { get; set; }
    public InterviewQuestion? Question { get; set; }
}
