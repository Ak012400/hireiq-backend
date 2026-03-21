// Controllers/PdfController.cs
using HireIQ.API.Controllers;
using HireIQ.API.Data;
using HireIQ.API.Models;
using HireIQ.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pdf")]
[Authorize]
public class PdfController : BaseController
{
    private readonly PdfExtractorService _extractor;
    private readonly MLService _mlService;
    private readonly GroqService _groqService;
    private readonly AppDbContext _db;

    public PdfController(
        PdfExtractorService extractor,
        MLService mlService,
        GroqService groqService,
        AppDbContext db)
    {
        _extractor = extractor;
        _mlService = mlService;
        _groqService = groqService;
        _db = db;
    }

    // Flow A — PDF upload + JD → MiniLM Screening
    [HttpPost("screen")]
    public async Task<IActionResult> ScreenResume(
        IFormFile resumeFile,
        [FromForm] Guid jdId)
    {
        if (resumeFile == null || resumeFile.Length == 0)
            return BadRequest(new { error = "PDF file required!" });

        var userId = GetCurrentUserId();

        // PDF → text
        using var ms = new MemoryStream();
        await resumeFile.CopyToAsync(ms);
        var text = _extractor.ExtractText(ms.ToArray());

        // JD fetch karo
        var job = await _db.JobDescriptions.FindAsync(jdId);
        if (job == null)
            return NotFound(new { error = "Job not found!" });

        // MiniLM score
        var score = await _mlService.QuickScore(text, job.Content);

        // DB mein resume save karo
        var resume = new Resume
        {
            UserId = userId,
            CandidateName = resumeFile.FileName.Replace(".pdf", ""),
            Content = text
        };
        _db.Resumes.Add(resume);

        // Screening result save karo
        var screening = new ScreeningResult
        {
            ResumeId = resume.Id,
            JdId = jdId,
            MinilmScore = (decimal)score,
            Shortlisted = score >= 0.7
        };
        _db.ScreeningResults.Add(screening);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            score,
            matchLevel = score >= 0.7 ? "HIGH" : score >= 0.5 ? "MEDIUM" : "LOW",
            shortlisted = score >= 0.7,
            resumeId = resume.Id
        });
    }

    // Flow B — PDF upload → Groq AI Review
    [HttpPost("review")]
    public async Task<IActionResult> ReviewResume(IFormFile resumeFile, [FromForm] Guid? jdId = null)
    {
        using var ms = new MemoryStream();
        await resumeFile.CopyToAsync(ms);
        var text = _extractor.ExtractText(ms.ToArray());

        string jdContext = "";
        if (jdId.HasValue)
        {
            var job = await _db.JobDescriptions.FindAsync(jdId.Value);
            if (job != null)
                jdContext = $"\n\nJob Description to match against:\n{job.Content}";
        }

        var prompt = jdContext == ""
            ? $"Review this resume for overall quality, ATS score, strengths and improvements:\n{text}"
            : $"Review this resume against the job description. Give match score, skill gaps, strengths:\n\nResume:\n{text}{jdContext}";

        var review = await _groqService.GenerateAsync(prompt);

        return Ok(new
        {
            review,
            extractedText = text  // ✅ Studio ke liye
        });
    }
}