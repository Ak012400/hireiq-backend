namespace HireIQ.Domain.Entities;

public enum EmploymentType { FullTime, PartTime, Contract, Internship, Freelance }
public enum WorkMode { Onsite, Remote, Hybrid }
public enum JobPostingStatus { Draft, Published, Closed, Archived }

public class JobPosting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HirerId { get; set; }

    // Core identification
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public WorkMode WorkMode { get; set; } = WorkMode.Onsite;

    // Experience + compensation
    public int? ExperienceMinYears { get; set; }
    public int? ExperienceMaxYears { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "INR";
    public string SalaryPeriod { get; set; } = "yearly";

    // Content
    public string Description { get; set; } = string.Empty;
    public string RequirementsJson { get; set; } = "[]";   // string[] of requirements
    public string BenefitsJson { get; set; } = "[]";
    public string SkillsRequiredJson { get; set; } = "[]";
    public string SkillsNiceToHaveJson { get; set; } = "[]";

    // Lifecycle
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // External job-board IDs (cross-post tracking)
    public string? LinkedInPostUrl { get; set; }
    public string? IndeedExternalId { get; set; }
    public string? NaukriExternalId { get; set; }

    // Nav
    public User? Hirer { get; set; }
}
