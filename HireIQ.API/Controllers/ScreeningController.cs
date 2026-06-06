using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;
using HireIQ.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/screening")]
[Authorize]
public class ScreeningController : BaseController
{
    private readonly AppDbContext _db;
    private readonly MLService _mlService;
    private readonly GroqService _groqService;

    public ScreeningController(AppDbContext db, MLService mlService, GroqService groqService)
    {
        _db = db;
        _mlService = mlService;
        _groqService = groqService;
    }

    // ─── Helper ────────────────────────────────────────────────────────────────
    private static string GetMatchLevel(decimal score) =>
        score >= 0.85m ? "EXCELLENT" :
        score >= 0.70m ? "HIGH" :
        score >= 0.50m ? "MEDIUM" : "LOW";

    // ─── Single Screening ───────────────────────────────────────────────────────
    [HttpPost("run")]
    public async Task<IActionResult> RunScreening(ScreeningRequestDTO dto)
    {
        var resume = await _db.Resumes.FindAsync(dto.ResumeId);
        var job = await _db.JobDescriptions.FindAsync(dto.JdId);

        if (resume == null) return NotFound(new { error = "Resume not found!" });
        if (job == null)    return NotFound(new { error = "Job not found!" });

        // Stage 1 — MiniLM similarity score
        var score = await _mlService.QuickScore(resume.Content, job.Content);
        decimal minilmScore = (decimal)score;

        // Stage 2 — Groq deep analysis (only when requested AND score ≥ 70 %)
        string? analysis = null;
        if (dto.DeepAnalyze && minilmScore >= 0.70m)
        {
            var prompt = $@"You are HireIQ, an expert AI HR assistant. Analyze this candidate for the role of '{job.Title}'.

Job Description:
{job.Content}

Resume:
{resume.Content}

Provide a structured analysis with:
1. Overall Match Score (out of 100) based on skill overlap.
2. Key Strengths (3–5 bullet points).
3. Skill Gaps (what is missing).
4. Final Interview Recommendation (Yes / Conditional / No) with a one-line reason.

Be professional, concise, and specific.";

            analysis = await _groqService.GenerateAsync(prompt);
        }

        var screening = new ScreeningResult
        {
            ResumeId    = dto.ResumeId,
            JdId        = dto.JdId,
            MinilmScore = minilmScore,
            HireiqAnalysis = analysis,
            Shortlisted = minilmScore >= 0.70m,
        };

        _db.ScreeningResults.Add(screening);
        await _db.SaveChangesAsync();

        return Ok(new ScreeningResponseDTO
        {
            Id             = screening.Id,
            ResumeId       = screening.ResumeId,
            JdId           = screening.JdId,
            CandidateName  = resume.CandidateName,
            JobTitle       = job.Title,
            MinilmScore    = screening.MinilmScore,
            MatchLevel     = GetMatchLevel(minilmScore),
            Analysis       = analysis,
            Shortlisted    = screening.Shortlisted,
            CandidateStatus = screening.CandidateStatus,
            CreatedAt      = screening.CreatedAt,
        });
    }

    // ─── Get Single Result ──────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetResult(Guid id)
    {
        var result = await _db.ScreeningResults
            .Include(s => s.Resume)
            .Include(s => s.JobDescription)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (result == null) return NotFound(new { error = "Result not found!" });

        return Ok(new ScreeningResponseDTO
        {
            Id             = result.Id,
            ResumeId       = result.ResumeId,
            JdId           = result.JdId,
            CandidateName  = result.Resume?.CandidateName ?? "",
            JobTitle       = result.JobDescription?.Title ?? "",
            MinilmScore    = result.MinilmScore,
            MatchLevel     = GetMatchLevel(result.MinilmScore),
            Analysis       = result.HireiqAnalysis,
            Shortlisted    = result.Shortlisted,
            CandidateStatus = result.CandidateStatus,
            CreatedAt      = result.CreatedAt,
        });
    }

    // ─── Get All (for current user) ─────────────────────────────────────────────
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();

        var results = await _db.ScreeningResults
            .Include(s => s.Resume)
            .Include(s => s.JobDescription)
            .Where(s => s.Resume!.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ScreeningResponseDTO
            {
                Id             = s.Id,
                ResumeId       = s.ResumeId,
                JdId           = s.JdId,
                CandidateName  = s.Resume!.CandidateName,
                JobTitle       = s.JobDescription != null ? s.JobDescription.Title : "",
                MinilmScore    = s.MinilmScore,
                MatchLevel     = s.MinilmScore >= 0.85m ? "EXCELLENT" :
                                 s.MinilmScore >= 0.70m ? "HIGH" :
                                 s.MinilmScore >= 0.50m ? "MEDIUM" : "LOW",
                Analysis       = s.HireiqAnalysis,
                Shortlisted    = s.Shortlisted,
                CandidateStatus = s.CandidateStatus,
                CreatedAt      = s.CreatedAt,
            })
            .ToListAsync();

        return Ok(results);
    }

    // ─── Bulk Screening (parallel Stage 1, sequential Stage 2 for Groq limits) ──
    [HttpPost("bulk")]
    public async Task<IActionResult> RunBulkScreening([FromBody] BatchScreeningDTO dto)
    {
        var job = await _db.JobDescriptions.FindAsync(dto.JdId);
        if (job == null) return NotFound(new { error = "Job not found!" });

        var resumes = await _db.Resumes
            .Where(r => dto.ResumeIds.Contains(r.Id))
            .ToListAsync();

        if (!resumes.Any())
            return BadRequest(new { error = "No valid resumes found in the database." });

        const decimal threshold = 0.70m;

        // Stage 1 — run all MiniLM calls in parallel
        var scoreTasks = resumes.Select(r =>
            _mlService.QuickScore(r.Content, job.Content)
                .ContinueWith(t => (Resume: r, Score: (decimal)t.Result))
        );
        var scored = await Task.WhenAll(scoreTasks);

        // Stage 2 — Groq deep analysis only for shortlisted (sequential to respect rate limits)
        var screenings = new List<ScreeningResult>();
        foreach (var (resume, minilmScore) in scored)
        {
            bool isShortlisted = minilmScore >= threshold;
            string? aiAnalysis = null;

            if (isShortlisted)
            {
                var prompt = $@"You are HireIQ, an expert AI HR assistant. Analyze this candidate for the role of '{job.Title}'.

Job Description:
{job.Content}

Resume:
{resume.Content}

Provide:
1. Overall Match Score (out of 100).
2. Key Strengths (3–5 bullet points).
3. Skill Gaps.
4. Final Interview Recommendation (Yes / Conditional / No) with a one-line reason.

Be professional and concise.";

                aiAnalysis = await _groqService.GenerateAsync(prompt);
            }

            screenings.Add(new ScreeningResult
            {
                Id             = Guid.NewGuid(),
                ResumeId       = resume.Id,
                JdId           = job.Id,
                MinilmScore    = minilmScore,
                HireiqAnalysis = aiAnalysis,
                Shortlisted    = isShortlisted,
                CreatedAt      = DateTime.UtcNow,
            });
        }

        _db.ScreeningResults.AddRange(screenings);
        await _db.SaveChangesAsync();

        // Map + rank
        var rankedResults = screenings
            .Select(s =>
            {
                var resume = resumes.First(r => r.Id == s.ResumeId);
                return new ScreeningResponseDTO
                {
                    Id             = s.Id,
                    ResumeId       = s.ResumeId,
                    JdId           = s.JdId,
                    CandidateName  = resume.CandidateName,
                    JobTitle       = job.Title,
                    MinilmScore    = s.MinilmScore,
                    MatchLevel     = GetMatchLevel(s.MinilmScore),
                    Analysis       = s.HireiqAnalysis,
                    Shortlisted    = s.Shortlisted,
                    CandidateStatus = s.CandidateStatus,
                    CreatedAt      = s.CreatedAt,
                };
            })
            .OrderByDescending(r => r.MinilmScore)
            .ToList();

        return Ok(rankedResults);
    }

    // ─── Update Candidate Status (NEW FEATURE) ──────────────────────────────────
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateCandidateStatus(Guid id, [FromBody] UpdateCandidateStatusDTO dto)
    {
        var validStatuses = new[] { "Screened", "Interview", "Hired", "Rejected" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { error = "Invalid status. Use: Screened, Interview, Hired, or Rejected." });

        var result = await _db.ScreeningResults.FindAsync(id);
        if (result == null) return NotFound(new { error = "Screening result not found." });

        result.CandidateStatus = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(new { id, status = dto.Status });
    }
}