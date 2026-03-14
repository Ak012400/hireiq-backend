using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;
using HireIQ.API.Services;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/screening")]
public class ScreeningController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MLService _mlService;

    public ScreeningController(AppDbContext db, MLService mlService)
    {
        _db = db;
        _mlService = mlService;
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
            var result = await _mlService.DeepAnalyze(
                resume.Content, job.Content
            );
            analysis = result?.ToString();
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

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var results = await _db.ScreeningResults
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
}