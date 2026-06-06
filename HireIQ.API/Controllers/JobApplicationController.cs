using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/job-applications")]
[Authorize]
public class JobApplicationController : BaseController
{
    private readonly AppDbContext _db;

    public JobApplicationController(AppDbContext db)
    {
        _db = db;
    }

    // Candidate applies to a job
    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] ApplyJobDTO dto)
    {
        var userId = GetCurrentUserId();

        var exists = await _db.JobApplications
            .AnyAsync(a => a.JobId == dto.JobId && a.ApplicantUserId == userId);
        if (exists)
            return Conflict(new { error = "You already applied to this job." });

        var job = await _db.JobDescriptions.FindAsync(dto.JobId);
        if (job == null) return NotFound(new { error = "Job not found." });

        var app = new JobApplication
        {
            JobId           = dto.JobId,
            ApplicantUserId = userId,
            ResumeId        = dto.ResumeId,
            CoverLetter     = dto.CoverLetter,
        };

        _db.JobApplications.Add(app);
        await _db.SaveChangesAsync();

        return Ok(new JobApplicationResponseDTO
        {
            Id       = app.Id,
            JobId    = app.JobId,
            JobTitle = job.Title,
            Status   = app.Status,
            AppliedAt = app.AppliedAt,
        });
    }

    // Candidate's own applications
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();
        var apps = await _db.JobApplications
            .Include(a => a.Job)
            .Where(a => a.ApplicantUserId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new JobApplicationResponseDTO
            {
                Id       = a.Id,
                JobId    = a.JobId,
                JobTitle = a.Job != null ? a.Job.Title : "",
                Status   = a.Status,
                AppliedAt = a.AppliedAt,
            })
            .ToListAsync();

        return Ok(apps);
    }

    // Hirer sees who applied to their job
    [HttpGet("for-job/{jobId}")]
    public async Task<IActionResult> GetForJob(Guid jobId)
    {
        var userId = GetCurrentUserId();
        var job = await _db.JobDescriptions.FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);
        if (job == null) return Forbid();

        var apps = await _db.JobApplications
            .Include(a => a.Applicant)
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new
            {
                a.Id, a.Status, a.AppliedAt,
                CandidateName  = a.Applicant != null ? a.Applicant.Name : "",
                CandidateEmail = a.Applicant != null ? a.Applicant.Email : "",
            })
            .ToListAsync();

        return Ok(apps);
    }
}
