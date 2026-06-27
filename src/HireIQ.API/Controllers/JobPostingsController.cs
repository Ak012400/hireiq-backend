using System.Text.Json;
using HireIQ.Application.DTOs;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.JobBoards;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/job-postings")]
[Authorize]
public class JobPostingsController : BaseController
{
    private readonly AppDbContext _db;
    private readonly IndeedFeedConnector _indeed;
    private readonly LinkedInShareConnector _linkedin;

    public JobPostingsController(AppDbContext db, IndeedFeedConnector indeed, LinkedInShareConnector linkedin)
    {
        _db = db;
        _indeed = indeed;
        _linkedin = linkedin;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var hirerId = GetCurrentUserId();
        var jobs = await _db.JobPostings
            .Where(j => j.HirerId == hirerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

        var result = jobs.Select(j => Map(j)).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var j = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j == null) return NotFound();
        return Ok(Map(j));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobPostingDTO dto, CancellationToken ct)
    {
        var posting = new JobPosting
        {
            HirerId = GetCurrentUserId(),
            Title = dto.Title,
            Company = dto.Company,
            Location = dto.Location,
            EmploymentType = dto.EmploymentType,
            WorkMode = dto.WorkMode,
            ExperienceMinYears = dto.ExperienceMinYears,
            ExperienceMaxYears = dto.ExperienceMaxYears,
            SalaryMin = dto.SalaryMin,
            SalaryMax = dto.SalaryMax,
            Currency = dto.Currency,
            SalaryPeriod = dto.SalaryPeriod,
            Description = dto.Description,
            RequirementsJson = JsonSerializer.Serialize(dto.Requirements),
            BenefitsJson = JsonSerializer.Serialize(dto.Benefits),
            SkillsRequiredJson = JsonSerializer.Serialize(dto.SkillsRequired),
            SkillsNiceToHaveJson = JsonSerializer.Serialize(dto.SkillsNiceToHave),
            ClosesAt = dto.ClosesAt,
            Status = JobPostingStatus.Draft
        };
        _db.JobPostings.Add(posting);
        await _db.SaveChangesAsync(ct);
        return Ok(Map(posting));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var p = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null) return NotFound();
        p.Status = JobPostingStatus.Published;
        p.PublishedAt = DateTime.UtcNow;
        p.UpdatedAt = DateTime.UtcNow;
        p.LinkedInPostUrl = _linkedin.BuildShareUrl(p);
        await _db.SaveChangesAsync(ct);

        // Auto-create board syncs (status records)
        _db.JobBoardSyncs.Add(new JobBoardSync
        {
            JobPostingId = p.Id,
            Board = JobBoard.Indeed,
            Status = JobBoardSyncStatus.Success,
            ExternalUrl = _indeed.BuildShareUrl(p),
            SyncedAt = DateTime.UtcNow
        });
        _db.JobBoardSyncs.Add(new JobBoardSync
        {
            JobPostingId = p.Id,
            Board = JobBoard.LinkedIn,
            Status = JobBoardSyncStatus.Success,
            ExternalUrl = p.LinkedInPostUrl,
            SyncedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Ok(Map(p));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var p = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null) return NotFound();
        p.Status = JobPostingStatus.Closed;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private JobPostingResponseDTO Map(JobPosting j) => new()
    {
        Id = j.Id,
        Title = j.Title,
        Company = j.Company,
        Location = j.Location,
        EmploymentType = j.EmploymentType.ToString(),
        WorkMode = j.WorkMode.ToString(),
        ExperienceMinYears = j.ExperienceMinYears,
        ExperienceMaxYears = j.ExperienceMaxYears,
        SalaryMin = j.SalaryMin,
        SalaryMax = j.SalaryMax,
        Currency = j.Currency,
        Description = j.Description,
        SkillsRequired = Parse(j.SkillsRequiredJson),
        Status = j.Status.ToString(),
        PublishedAt = j.PublishedAt,
        ClosesAt = j.ClosesAt,
        CreatedAt = j.CreatedAt,
        LinkedInShareUrl = j.LinkedInPostUrl ?? _linkedin.BuildShareUrl(j),
        IndeedFeedUrl = _indeed.BuildShareUrl(j),
    };

    private static List<string> Parse(string json) {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); } catch { return new(); }
    }
}
