using HireIQ.Application.Interfaces;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : BaseController
{
    private readonly IApplicationIntakeService _intake;
    private readonly AppDbContext _db;

    public ApplicationsController(IApplicationIntakeService intake, AppDbContext db)
    {
        _intake = intake; _db = db;
    }

    /// <summary>
    /// Candidate applies to a job. Accepts either a PDF resume file or existing resume text.
    /// </summary>
    [HttpPost("apply")]
    [RequestSizeLimit(20_000_000)]   // 20 MB max
    public async Task<IActionResult> Apply(
        [FromForm] Guid jobPostingId,
        [FromForm] string candidateName,
        [FromForm] string? coverLetter,
        [FromForm] string? existingResumeContent,
        IFormFile? resume,
        CancellationToken ct)
    {
        var applicantId = GetCurrentUserId();
        Stream? stream = null;
        string? fileName = null;
        if (resume != null && resume.Length > 0)
        {
            stream = resume.OpenReadStream();
            fileName = resume.FileName;
        }

        try
        {
            var result = await _intake.ApplyAsync(
                applicantId, jobPostingId, candidateName,
                stream, fileName, existingResumeContent, coverLetter, ct);
            return Ok(result);
        }
        finally
        {
            stream?.Dispose();
        }
    }

    /// <summary>Candidate's "My Applications" view.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var mine = await _db.CandidateJourneys
            .Include(j => j.JobPosting)
            .Where(j => j.ApplicantUserId == userId)
            .OrderByDescending(j => j.LastTransitionAt)
            .Select(j => new {
                j.Id,
                j.CurrentStage,
                j.LastTransitionAt,
                j.CreatedAt,
                jobId = j.JobPostingId,
                jobTitle = j.JobPosting!.Title,
                company = j.JobPosting.Company,
                location = j.JobPosting.Location
            })
            .ToListAsync(ct);
        return Ok(mine);
    }
}
