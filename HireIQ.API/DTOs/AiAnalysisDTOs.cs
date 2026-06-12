namespace HireIQ.API.DTOs;

// ── Structured screening analysis (Groq JSON mode) ──────────────────────────
public class ScreeningAnalysis
{
    public int MatchScore { get; set; }                       // 0–100
    public List<string> Strengths { get; set; } = new();
    public List<string> SkillGaps { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty; // Yes | Conditional | No
    public string Reason { get; set; } = string.Empty;
}

// ── Structured mock-interview evaluation (Groq JSON mode) ───────────────────
public class InterviewEvaluation
{
    public decimal TechnicalScore { get; set; }
    public decimal CommunicationScore { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal OverallScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public string Feedback { get; set; } = string.Empty;
}
