namespace HireIQ.Domain.Entities;

public enum JobBoard { Indeed, LinkedIn, Naukri, Glassdoor, Custom }
public enum JobBoardSyncStatus { Pending, InProgress, Success, Failed }

public class JobBoardSync
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobPostingId { get; set; }
    public JobBoard Board { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalUrl { get; set; }
    public JobBoardSyncStatus Status { get; set; } = JobBoardSyncStatus.Pending;
    public DateTime? SyncedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public JobPosting? JobPosting { get; set; }
}
