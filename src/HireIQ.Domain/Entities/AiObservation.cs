namespace HireIQ.Domain.Entities;

public enum AiAgentKind
{
    /// <summary>Sub-second Q&A (Groq Llama 3.1 8B).</summary>
    FastQuestion,
    /// <summary>Per-frame behavior analysis (Gemini Flash vision).</summary>
    VisualBehavior,
    /// <summary>Deep per-answer analysis (Llama 70B / Claude Sonnet).</summary>
    DeepAnswer,
    /// <summary>Final aggregation across all agents.</summary>
    Aggregator
}

/// <summary>
/// One observation row per AI-agent per turn (or per frame, for visual).
/// </summary>
public class AiObservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public AiAgentKind Agent { get; set; }
    public int TurnIndex { get; set; }
    public Guid? RelatedQuestionId { get; set; }

    public string ObservationJson { get; set; } = "{}";     // freeform agent-specific structured output

    // Score columns (nullable — visual agent fills attention; deep fills technical, etc.)
    public float? ScoreTechnical { get; set; }
    public float? ScoreCommunication { get; set; }
    public float? ScoreConfidence { get; set; }
    public float? ScoreAttention { get; set; }
    public float? ScoreEmotion { get; set; }

    public string? RawResponse { get; set; }                // raw LLM output for audit/debug
    public int LatencyMs { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public InterviewSession? Session { get; set; }
}

/// <summary>
/// Final aggregated scores per session (computed after interview ends).
/// </summary>
public class InterviewFinalScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }

    public float TechnicalScore { get; set; }
    public float BehavioralScore { get; set; }
    public float CommunicationScore { get; set; }
    public float AttentionScore { get; set; }
    public float ConfidenceScore { get; set; }
    public float OverallScore { get; set; }

    /// <summary>PROCEED | REJECT | MAYBE.</summary>
    public string Recommendation { get; set; } = "MAYBE";
    public string AggregatedReasoning { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public InterviewSession? Session { get; set; }
}
