using System.Web;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HireIQ.Infrastructure.JobBoards;

/// <summary>
/// LinkedIn Jobs API requires Talent Solutions partnership (expensive + slow approval).
/// Until then we expose a plain "Share to LinkedIn" deep-link the hirer can click.
/// This posts the job as an organic share on their profile — not as an official job listing.
/// </summary>
public sealed class LinkedInShareConnector : IJobBoardConnector
{
    private readonly IConfiguration _cfg;
    public LinkedInShareConnector(IConfiguration cfg) => _cfg = cfg;

    public JobBoard Board => JobBoard.LinkedIn;
    public bool SupportsPush => false;

    public Task<JobBoardSyncResult> PublishAsync(JobPosting posting, CancellationToken ct = default) =>
        Task.FromResult(new JobBoardSyncResult(true, null, BuildShareUrl(posting), null));

    public Task<bool> UnpublishAsync(JobPosting posting, CancellationToken ct = default) => Task.FromResult(true);

    public string BuildShareUrl(JobPosting posting)
    {
        var publicBase = _cfg["PublicBaseUrl"] ?? "https://hireiq-backend-humv.onrender.com";
        var jobUrl = $"{publicBase}/jobs/{posting.Id}";
        return $"https://www.linkedin.com/sharing/share-offsite/?url={HttpUtility.UrlEncode(jobUrl)}";
    }
}
