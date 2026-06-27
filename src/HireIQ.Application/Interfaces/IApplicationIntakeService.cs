using HireIQ.Domain.Entities;

namespace HireIQ.Application.Interfaces;

public sealed record ApplyResult(Guid ApplicationId, Guid CandidateJourneyId, Guid ResumeId);

public interface IApplicationIntakeService
{
    /// <summary>
    /// Candidate apply flow.
    /// Creates/finds Resume, JobApplication, starts CandidateJourney, queues auto-screening.
    /// </summary>
    Task<ApplyResult> ApplyAsync(
        Guid applicantUserId,
        Guid jobPostingId,
        string candidateName,
        Stream? resumeStream,
        string? resumeFileName,
        string? existingResumeContent,
        string? coverLetter,
        CancellationToken ct = default);
}

public interface IAutoScreeningOrchestrator
{
    /// <summary>Background Hangfire entry — runs AI screening for an application and advances the journey.</summary>
    Task RunScreeningAsync(Guid applicationId);
}
