using System.Text;
using System.Xml.Linq;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HireIQ.Infrastructure.JobBoards;

/// <summary>
/// Indeed organic listings work via a public XML feed that Indeed crawls daily.
/// Spec: https://employers.indeed.com/p/xml-feed
/// We don't push anything — we just serve a /feeds/indeed.xml endpoint with all published jobs.
/// </summary>
public sealed class IndeedFeedConnector : IJobBoardConnector
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public IndeedFeedConnector(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public JobBoard Board => JobBoard.Indeed;
    public bool SupportsPush => false;

    public Task<JobBoardSyncResult> PublishAsync(JobPosting posting, CancellationToken ct = default) =>
        Task.FromResult(new JobBoardSyncResult(true, posting.Id.ToString(), BuildShareUrl(posting), null));

    public Task<bool> UnpublishAsync(JobPosting posting, CancellationToken ct = default) => Task.FromResult(true);

    public string BuildShareUrl(JobPosting posting)
    {
        var publicBase = _cfg["PublicBaseUrl"] ?? "https://hireiq-backend-humv.onrender.com";
        return $"{publicBase}/feeds/indeed.xml#job-{posting.Id}";
    }

    /// <summary>Generates the complete Indeed XML feed for all currently published postings.</summary>
    public async Task<string> BuildFullFeedXmlAsync(CancellationToken ct = default)
    {
        var publicBase = _cfg["PublicBaseUrl"] ?? "https://hireiq-backend-humv.onrender.com";
        var publishedAt = DateTime.UtcNow;
        var jobs = await _db.JobPostings
            .Where(j => j.Status == JobPostingStatus.Published)
            .Include(j => j.Hirer)
            .ToListAsync(ct);

        var source = new XElement("source",
            new XElement("publisher", "HireIQ"),
            new XElement("publisherurl", publicBase),
            new XElement("lastBuildDate", publishedAt.ToString("R"))
        );

        foreach (var j in jobs)
        {
            source.Add(new XElement("job",
                new XElement("title", new XCData(j.Title)),
                new XElement("date", (j.PublishedAt ?? j.CreatedAt).ToString("R")),
                new XElement("referencenumber", j.Id.ToString()),
                new XElement("url", new XCData($"{publicBase}/jobs/{j.Id}")),
                new XElement("company", new XCData(j.Company)),
                new XElement("city", new XCData(j.Location)),
                new XElement("country", "IN"),
                new XElement("description", new XCData(j.Description)),
                new XElement("salary", $"{j.SalaryMin}-{j.SalaryMax} {j.Currency} {j.SalaryPeriod}"),
                new XElement("jobtype", j.EmploymentType.ToString().ToLowerInvariant()),
                new XElement("category", "Engineering")
            ));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), source);
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        doc.Save(sw);
        return sb.ToString();
    }
}
