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

    [HttpPost("run")]
    public async Task<IActionResult> RunScreening(ScreeningRequestDTO dto)
    {
        var resume = await _db.Resumes.FindAsync(dto.ResumeId);
        var job = await _db.JobDescriptions.FindAsync(dto.JdId);

        if (resume == null)
            return NotFound(new { error = "Resume not found!" });
        if (job == null)
            return NotFound(new { error = "Job not found!" });

        // MiniLM quick score
        var score = await _mlService.QuickScore(
            resume.Content, job.Content
        );

        string? analysis = null;

        // Deep analyze if requested
        if (dto.DeepAnalyze)
        {
            var result = await _mlService.QuickScore(
                resume.Content, job.Content
            );
            analysis = result.ToString();
        }

        // Save to DB
        var screening = new ScreeningResult
        {
            ResumeId = dto.ResumeId,
            JdId = dto.JdId,
            MinilmScore = (decimal)score,
            HireiqAnalysis = analysis,
            Shortlisted = score >= 0.7
        };

        _db.ScreeningResults.Add(screening);
        await _db.SaveChangesAsync();

        return Ok(new ScreeningResponseDTO
        {
            Id = screening.Id,
            MinilmScore = screening.MinilmScore,
            MatchLevel = score >= 0.7 ? "HIGH" :
                         score >= 0.5 ? "MEDIUM" : "LOW",
            Analysis = analysis,
            Shortlisted = screening.Shortlisted
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetResult(Guid id)
    {
        var result = await _db.ScreeningResults.FindAsync(id);
        if (result == null)
            return NotFound(new { error = "Result not found!" });

        return Ok(new ScreeningResponseDTO
        {
            Id = result.Id,
            MinilmScore = result.MinilmScore,
            MatchLevel = result.MinilmScore >= 0.7m ? "HIGH" :
                         result.MinilmScore >= 0.5m ? "MEDIUM" : "LOW",
            Analysis = result.HireiqAnalysis,
            Shortlisted = result.Shortlisted
        });
    }

    // GetAll me userId filter add karo
    // RunScreening me bhi userId save karo agar future me chahiye

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();

        // Resume ke through current user ke screenings lo
        var results = await _db.ScreeningResults
            .Include(s => s.Resume) // ✅ Resume navigate karo
            .Where(s => s.Resume!.UserId == userId) // ✅ filter by user
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ScreeningResponseDTO
            {
                Id = s.Id,
                MinilmScore = s.MinilmScore,
                MatchLevel = s.MinilmScore >= 0.7m ? "HIGH" :
                             s.MinilmScore >= 0.5m ? "MEDIUM" : "LOW",
                Analysis = s.HireiqAnalysis,
                Shortlisted = s.Shortlisted
            })
            .ToListAsync();

        return Ok(results);
    }
    [HttpPost("bulk")]
    public async Task<IActionResult> RunBulkScreening([FromBody] BatchScreeningDTO dto)
    {
        // 1. Fetch the Job Description
        var job = await _db.JobDescriptions.FindAsync(dto.JdId);
        if (job == null)
            return NotFound(new { error = "Job not found!" });

        // 2. Fetch all requested Resumes
        var resumes = await _db.Resumes
            .Where(r => dto.ResumeIds.Contains(r.Id))
            .ToListAsync();

        if (!resumes.Any())
            return BadRequest(new { error = "No valid resumes found in the database." });

        var results = new List<ScreeningResponseDTO>();
        decimal threshold = 0.70m; // 70% threshold for Stage 1

        // 3. Process the Multi-Stage Pipeline
        foreach (var resume in resumes)
        {
            // 🔹 STAGE 1: Fast MiniLM Filtering
            var score = await _mlService.QuickScore(resume.Content, job.Content);
            decimal minilmScore = (decimal)score;
            bool isShortlisted = minilmScore >= threshold;
            string? aiAnalysis = null;

            // 🔹 STAGE 2: Deep Groq Review (ONLY if they passed Stage 1)
                        if (isShortlisted)
                        {
                            var prompt = $@"
            You are HireIQ, an expert AI HR assistant. Analyze this candidate for the role of '{job.Title}'.
            Job Description: {job.Content}
            Resume: {resume.Content}

            Provide:
            1. A Score out of 100 based on exact skill matches.
            2. Key Strengths.
            3. Skill Gaps.
            4. Final Interview Recommendation.
            Make it professional and concise.";

                aiAnalysis = await _groqService.GenerateAsync(prompt);
                        }

            // Save to Database
            var screening = new ScreeningResult
            {
                Id = Guid.NewGuid(), // Manually assign so we can map it to DTO before SaveChanges
                ResumeId = resume.Id,
                JdId = job.Id,
                MinilmScore = minilmScore,
                HireiqAnalysis = aiAnalysis,
                Shortlisted = isShortlisted,
                CreatedAt = DateTime.UtcNow
            };

            _db.ScreeningResults.Add(screening);

            // Add to response list
            results.Add(new ScreeningResponseDTO
            {
                Id = screening.Id,
                MinilmScore = screening.MinilmScore,
                MatchLevel = minilmScore >= 0.8m ? "EXCELLENT" :
                             minilmScore >= 0.7m ? "HIGH" :
                             minilmScore >= 0.5m ? "MEDIUM" : "LOW",
                Analysis = screening.HireiqAnalysis,
                Shortlisted = screening.Shortlisted
            });
        }

        await _db.SaveChangesAsync();

        // ✅ Rank the candidates before returning (Highest score first)
        var rankedResults = results.OrderByDescending(r => r.MinilmScore).ToList();

        return Ok(rankedResults);
    }
}