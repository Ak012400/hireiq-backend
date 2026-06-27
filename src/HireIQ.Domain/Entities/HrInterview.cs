namespace HireIQ.Domain.Entities;

public enum HrInterviewStatus { Scheduled, Completed, Cancelled, NoShow }
public enum HrDecisionOutcome { Pending, Hire, Reject, Hold }

public class HrInterview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateJourneyId { get; set; }
    public Guid HirerId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? MeetLink { get; set; }
    public string? HirerNotes { get; set; }
    public HrInterviewStatus Status { get; set; } = HrInterviewStatus.Scheduled;
    public HrDecisionOutcome Decision { get; set; } = HrDecisionOutcome.Pending;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CandidateJourney? CandidateJourney { get; set; }
    public User? Hirer { get; set; }
}

public enum HiringDecisionType { Hire, Reject, Hold }
public enum CandidateResponseType { Pending, Accepted, Declined, Negotiating }

public class HiringDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateJourneyId { get; set; }
    public HiringDecisionType Decision { get; set; }
    public Guid DecidedBy { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

    // Offer details (only populated when Decision = Hire)
    public decimal? OfferedSalary { get; set; }
    public string? OfferedCurrency { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string? OfferDetailsJson { get; set; }
    public string? OfferLetterUrl { get; set; }

    public CandidateResponseType CandidateResponse { get; set; } = CandidateResponseType.Pending;
    public DateTime? CandidateRespondedAt { get; set; }

    public CandidateJourney? CandidateJourney { get; set; }
    public User? Decider { get; set; }
}
