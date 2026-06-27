using HireIQ.Domain.Entities;

namespace HireIQ.Application.Interfaces;

public sealed record JobBoardSyncResult(bool Success, string? ExternalId, string? ExternalUrl, string? Error);

/// <summary>
/// One implementation per job board (Indeed XML feed, LinkedIn share, Naukri API, etc.)
/// Per-connector capabilities will vary — some are push (API), some are pull (XML feed).
/// </summary>
public interface IJobBoardConnector
{
    JobBoard Board { get; }
    bool SupportsPush { get; }
    Task<JobBoardSyncResult> PublishAsync(JobPosting posting, CancellationToken ct = default);
    Task<bool> UnpublishAsync(JobPosting posting, CancellationToken ct = default);
    string BuildShareUrl(JobPosting posting);
}
