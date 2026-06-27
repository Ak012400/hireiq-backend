namespace HireIQ.Domain.Entities;

/// <summary>
/// Pipeline stages a candidate moves through from application to hire.
/// </summary>
public enum PipelineStage
{
    Applied,
    ScreeningQueued,
    ScreeningDone,
    Shortlisted,
    RejectedByAi,
    AiInterviewInvited,
    AiInterviewScheduled,
    AiInterviewCompleted,
    AiPassed,
    RejectedAfterAi,
    HrInterviewInvited,
    HrInterviewScheduled,
    HrInterviewCompleted,
    OfferExtended,
    Hired,
    RejectedByHr,
    Withdrawn
}

/// <summary>
/// Tracks each candidate's full journey for a given job application.
/// Single row per application; stage transitions are appended to StageHistoryJson.
/// </summary>
public class CandidateJourney
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobApplicationId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public Guid JobPostingId { get; set; }

    public PipelineStage CurrentStage { get; set; } = PipelineStage.Applied;

    /// <summary>
    /// JSON array of { stage, enteredAt, by (userId|"system"|"ai"), reason }.
    /// </summary>
    public string StageHistoryJson { get; set; } = "[]";

    public DateTime LastTransitionAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Nav
    public JobApplication? JobApplication { get; set; }
    public User? Applicant { get; set; }
    public JobPosting? JobPosting { get; set; }
}
