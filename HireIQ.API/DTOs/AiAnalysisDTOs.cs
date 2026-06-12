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

// ── Structured resume data (AI redesign + keyword generation) ───────────────
// Shape mirrors frontend resumeTemplates.js — don't rename fields casually.
public class ResumeData
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Linkedin { get; set; } = string.Empty;
    public string Github { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public List<ExperienceItem> Experience { get; set; } = new();
    public List<EducationItem> Education { get; set; } = new();
    public List<ProjectItem> Projects { get; set; } = new();
    public string Extra { get; set; } = string.Empty;
}

public class ExperienceItem
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EducationItem
{
    public string Degree { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
}

public class ProjectItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AiGenerateResumeDTO
{
    public string Keywords { get; set; } = string.Empty;   // e.g. "senior react developer, 3 years, fintech"
    public string? TargetRole { get; set; }
    public string? Name { get; set; }
}

// ── AI Coach Room: chat + structured resume updates ─────────────────────────
public class CoachRequestDTO
{
    public string Message { get; set; } = string.Empty;          // user's chat message
    public ResumeData? Resume { get; set; }                       // current resume state
    public List<CoachHistoryItem> History { get; set; } = new();  // recent chat turns
}

public class CoachHistoryItem
{
    public string Role { get; set; } = "user";   // user | assistant
    public string Content { get; set; } = string.Empty;
}

public class CoachResponse
{
    public string Reply { get; set; } = string.Empty;            // conversational answer
    public List<CoachUpdate> Updates { get; set; } = new();      // suggested resume changes
}

public class CoachUpdate
{
    public string Section { get; set; } = string.Empty;  // summary | role | skills | experience | projects | education | extra
    public string Title { get; set; } = string.Empty;    // short label, e.g. "Stronger summary"
    public string Content { get; set; } = string.Empty;  // the new text (skills: comma-separated)
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
