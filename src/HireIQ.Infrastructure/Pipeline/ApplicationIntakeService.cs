using Hangfire;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using HireIQ.Infrastructure.Pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Pipeline;

public sealed class ApplicationIntakeService : IApplicationIntakeService
{
    private readonly AppDbContext _db;
    private readonly IHiringPipelineService _pipeline;
    private readonly PdfExtractorService _pdf;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<ApplicationIntakeService> _logger;

    public ApplicationIntakeService(
        AppDbContext db, IHiringPipelineService pipeline, PdfExtractorService pdf,
        IBackgroundJobClient jobs, ILogger<ApplicationIntakeService> logger)
    {
        _db = db; _pipeline = pipeline; _pdf = pdf; _jobs = jobs; _logger = logger;
    }

    public async Task<ApplyResult> ApplyAsync(
        Guid applicantUserId, Guid jobPostingId, string candidateName,
        Stream? resumeStream, string? resumeFileName,
        string? existingResumeContent, string? coverLetter,
        CancellationToken ct = default)
    {
        // 1. Resolve resume — either pdf upload, or existing text content
        string content = existingResumeContent ?? string.Empty;
        if (resumeStream != null)
        {
            using var ms = new MemoryStream();
            await resumeStream.CopyToAsync(ms, ct);
            content = _pdf.ExtractText(ms.ToArray());
        }

        var resume = new Resume
        {
            UserId = applicantUserId,
            CandidateName = candidateName,
            Content = content
        };
        _db.Resumes.Add(resume);

        // 2. JobApplication
        var posting = await _db.JobPostings.FirstOrDefaultAsync(p => p.Id == jobPostingId, ct)
            ?? throw new InvalidOperationException("Job posting not found");

        // We back-fill a JobDescription row (legacy code joins on this) — link by title.
        var legacyJd = await _db.JobDescriptions.FirstOrDefaultAsync(j => j.UserId == posting.HirerId && j.Title == posting.Title, ct);
        if (legacyJd == null)
        {
            legacyJd = new JobDescription
            {
                UserId = posting.HirerId,
                Title = posting.Title,
                Content = posting.Description
            };
            _db.JobDescriptions.Add(legacyJd);
            await _db.SaveChangesAsync(ct);
        }

        var application = new JobApplication
        {
            JobId = legacyJd.Id,
            ApplicantUserId = applicantUserId,
            ResumeId = resume.Id,
            CoverLetter = coverLetter,
            Status = "Applied"
        };
        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        // 3. Start journey
        var journey = await _pipeline.StartJourneyAsync(application.Id, applicantUserId, jobPostingId, ct);

        // 4. Queue auto-screening (Hangfire — happens out of request path)
        _jobs.Enqueue<IAutoScreeningOrchestrator>(o => o.RunScreeningAsync(application.Id));

        _logger.LogInformation("Application {AppId} created for job {JobId}, journey {Jid}", application.Id, jobPostingId, journey.Id);

        return new ApplyResult(application.Id, journey.Id, resume.Id);
    }
}
