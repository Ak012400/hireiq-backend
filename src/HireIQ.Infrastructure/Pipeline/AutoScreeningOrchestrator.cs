using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Ai;
using HireIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Pipeline;

/// <summary>
/// Runs AI screening on an application (background Hangfire job) and transitions the journey.
/// </summary>
public sealed class AutoScreeningOrchestrator : IAutoScreeningOrchestrator
{
    private readonly AppDbContext _db;
    private readonly IHiringPipelineService _pipeline;
    private readonly MLService _ml;
    private readonly GroqService _groq;
    private readonly ILogger<AutoScreeningOrchestrator> _logger;

    public AutoScreeningOrchestrator(
        AppDbContext db, IHiringPipelineService pipeline,
        MLService ml, GroqService groq, ILogger<AutoScreeningOrchestrator> logger)
    {
        _db = db; _pipeline = pipeline; _ml = ml; _groq = groq; _logger = logger;
    }

    public async Task RunScreeningAsync(Guid applicationId)
    {
        var app = await _db.JobApplications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app == null) { _logger.LogWarning("Application {Id} not found", applicationId); return; }

        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.Id == app.ResumeId);
        var journey = await _db.CandidateJourneys.FirstOrDefaultAsync(j => j.JobApplicationId == applicationId);
        if (resume == null || journey == null || app.Job == null) return;

        try
        {
            await _pipeline.TransitionAsync(journey.Id, PipelineStage.ScreeningQueued, "system", "AI screening started");

            // MiniLM similarity score
            double score = await _ml.QuickScore(resume.Content, app.Job.Content);

            // Groq HR-style analysis
            var prompt = $"Resume:\n{resume.Content}\n\nJob description:\n{app.Job.Content}\n\nEvaluate fit and give Shortlist/Reject decision.";
            string analysis = await _groq.GenerateFieldAsync(prompt);

            bool shortlist = score >= 0.5;

            _db.ScreeningResults.Add(new ScreeningResult
            {
                ResumeId = resume.Id,
                JdId = app.Job.Id,
                MinilmScore = (decimal)score,
                HireiqAnalysis = analysis,
                Shortlisted = shortlist,
                CandidateStatus = "Screened"
            });
            await _db.SaveChangesAsync();

            // Advance journey — ScreeningDone regardless. Hirer reviews and decides Shortlist/Reject.
            await _pipeline.TransitionAsync(journey.Id, PipelineStage.ScreeningDone, "ai",
                $"MiniLM={score:F2}, AI-suggested={(shortlist ? "Shortlist" : "Reject")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-screening failed for application {Id}", applicationId);
        }
    }
}
