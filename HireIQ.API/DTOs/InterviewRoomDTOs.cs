namespace HireIQ.API.DTOs;

public class CreateInterviewRoomDTO
{
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidateName { get; set; }
    public Guid? JobId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public List<string> PresetQuestions { get; set; } = new();
}

public class InterviewRoomResponseDTO
{
    public Guid Id { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomPassword { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidateName { get; set; }
    public string? JobTitle { get; set; }
    public Guid? JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public List<string> PresetQuestions { get; set; } = new();
    public string FinalDecision { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StartMockInterviewDTO
{
    public string JobTitle { get; set; } = string.Empty;
    public string? JobDescription { get; set; }
}

public class AnswerMockInterviewDTO
{
    public Guid SessionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public bool IsLast { get; set; } = false;
    public string JobTitle { get; set; } = string.Empty;
}

public class MockInterviewReportDTO
{
    public decimal TechnicalScore { get; set; }
    public decimal CommunicationScore { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal OverallScore { get; set; }
    public List<string> Strengths { get; set; } = new();      // ✅ structured (was buried in free text)
    public List<string> Improvements { get; set; } = new();   // ✅ structured
    public string Evaluation { get; set; } = string.Empty;    // detailed feedback paragraphs
}

public class ApplyJobDTO
{
    public Guid JobId { get; set; }
    public Guid? ResumeId { get; set; }
    public string? CoverLetter { get; set; }
}

public class JobApplicationResponseDTO
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}
