using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;

namespace HireIQ.Infrastructure.JobBoards;

/// <summary>
/// Glassdoor Job Posting API — partner program only (https://www.glassdoor.com/developer).
/// Stub: returns a search URL for the role until partner key is configured.
/// </summary>
public sealed class GlassdoorConnector : IJobBoardConnector
{
    public JobBoard Board => JobBoard.Glassdoor;
    public bool SupportsPush => false;

    public Task<JobBoardSyncResult> PublishAsync(JobPosting posting, CancellationToken ct = default) =>
        Task.FromResult(new JobBoardSyncResult(true, null, BuildShareUrl(posting),
            "Glassdoor requires partner API access — stub returned a public search link instead."));

    public Task<bool> UnpublishAsync(JobPosting posting, CancellationToken ct = default) => Task.FromResult(true);

    public string BuildShareUrl(JobPosting posting)
    {
        var q = Uri.EscapeDataString(posting.Title);
        return $"https://www.glassdoor.com/Job/jobs.htm?sc.keyword={q}";
    }
}
