using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;

namespace HireIQ.Infrastructure.JobBoards;

/// <summary>
/// Naukri Job Posting API — requires a Naukri partner agreement + REST API key per recruiter.
/// Currently a stub: returns a deep-link to Naukri search for the role (no real posting).
/// When partner credentials are configured per-hirer (HirerIntegration), upgrade this to call
/// POST https://www.naukri.com/jobpost-api/v1/post with the bearer token.
/// </summary>
public sealed class NaukriConnector : IJobBoardConnector
{
    public JobBoard Board => JobBoard.Naukri;
    public bool SupportsPush => false;

    public Task<JobBoardSyncResult> PublishAsync(JobPosting posting, CancellationToken ct = default) =>
        Task.FromResult(new JobBoardSyncResult(true, null, BuildShareUrl(posting),
            "Naukri requires partner API access — stub returned a public search link instead."));

    public Task<bool> UnpublishAsync(JobPosting posting, CancellationToken ct = default) => Task.FromResult(true);

    public string BuildShareUrl(JobPosting posting)
    {
        // Public Naukri job search URL — clicks land on relevant listings, not the user's own posting.
        var q = Uri.EscapeDataString(posting.Title);
        var loc = Uri.EscapeDataString(posting.Location);
        return $"https://www.naukri.com/{q}-jobs-in-{loc}";
    }
}
