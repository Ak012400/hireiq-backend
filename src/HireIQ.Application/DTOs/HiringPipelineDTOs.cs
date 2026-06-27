using HireIQ.Domain.Entities;

namespace HireIQ.Application.DTOs;

public class CreateJobPostingDTO
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public WorkMode WorkMode { get; set; } = WorkMode.Onsite;
    public int? ExperienceMinYears { get; set; }
    public int? ExperienceMaxYears { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "INR";
    public string SalaryPeriod { get; set; } = "yearly";
    public string Description { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
    public List<string> SkillsRequired { get; set; } = new();
    public List<string> SkillsNiceToHave { get; set; } = new();
    public DateTime? ClosesAt { get; set; }
}

public class JobPostingResponseDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string WorkMode { get; set; } = string.Empty;
    public int? ExperienceMinYears { get; set; }
    public int? ExperienceMaxYears { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "INR";
    public string Description { get; set; } = string.Empty;
    public List<string> SkillsRequired { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LinkedInShareUrl { get; set; }
    public string? IndeedFeedUrl { get; set; }
    public int ApplicationCount { get; set; }
}

public class CandidateJourneyResponseDTO
{
    public Guid Id { get; set; }
    public Guid JobPostingId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public DateTime LastTransitionAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransitionRequestDTO
{
    public string ToStage { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class HrInterviewCreateDTO
{
    public Guid CandidateJourneyId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? MeetLink { get; set; }
}

public class HiringDecisionDTO
{
    public Guid CandidateJourneyId { get; set; }
    public string Decision { get; set; } = string.Empty;     // Hire | Reject | Hold
    public decimal? OfferedSalary { get; set; }
    public string? OfferedCurrency { get; set; }
    public DateTime? JoiningDate { get; set; }
}
