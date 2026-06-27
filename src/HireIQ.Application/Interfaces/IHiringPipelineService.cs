using HireIQ.Domain.Entities;

namespace HireIQ.Application.Interfaces;

/// <summary>
/// The state machine for a candidate's journey through the hiring funnel.
/// Every stage change goes through here so the audit log + email triggers stay consistent.
/// </summary>
public interface IHiringPipelineService
{
    Task<CandidateJourney> StartJourneyAsync(Guid applicationId, Guid applicantUserId, Guid jobPostingId, CancellationToken ct = default);

    Task<CandidateJourney> TransitionAsync(
        Guid journeyId,
        PipelineStage toStage,
        string by,                 // user-id string, "system", or "ai"
        string? reason = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<CandidateJourney>> GetJourneysForJobAsync(Guid jobPostingId, CancellationToken ct = default);
    Task<CandidateJourney?> GetJourneyAsync(Guid journeyId, CancellationToken ct = default);
}
