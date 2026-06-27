using System.Text.Json;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Pipeline;

/// <summary>
/// Centralised state machine for a candidate's journey from application to hire.
/// Every transition is validated, audit-logged, and fires the right email category.
/// </summary>
public sealed class HiringPipelineService : IHiringPipelineService
{
    private readonly AppDbContext _db;
    private readonly IEmailQueueService _email;
    private readonly ILogger<HiringPipelineService> _logger;

    public HiringPipelineService(AppDbContext db, IEmailQueueService email, ILogger<HiringPipelineService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    // ── Allowed forward transitions (defensive — keeps callers honest) ──
    private static readonly Dictionary<PipelineStage, HashSet<PipelineStage>> Allowed = new()
    {
        [PipelineStage.Applied]              = new() { PipelineStage.ScreeningQueued, PipelineStage.Withdrawn },
        [PipelineStage.ScreeningQueued]      = new() { PipelineStage.ScreeningDone, PipelineStage.Withdrawn },
        [PipelineStage.ScreeningDone]        = new() { PipelineStage.Shortlisted, PipelineStage.RejectedByAi },
        [PipelineStage.Shortlisted]          = new() { PipelineStage.AiInterviewInvited, PipelineStage.Withdrawn },
        [PipelineStage.AiInterviewInvited]   = new() { PipelineStage.AiInterviewScheduled, PipelineStage.Withdrawn },
        [PipelineStage.AiInterviewScheduled] = new() { PipelineStage.AiInterviewCompleted, PipelineStage.Withdrawn },
        [PipelineStage.AiInterviewCompleted] = new() { PipelineStage.AiPassed, PipelineStage.RejectedAfterAi },
        [PipelineStage.AiPassed]             = new() { PipelineStage.HrInterviewInvited, PipelineStage.Withdrawn },
        [PipelineStage.HrInterviewInvited]   = new() { PipelineStage.HrInterviewScheduled, PipelineStage.Withdrawn },
        [PipelineStage.HrInterviewScheduled] = new() { PipelineStage.HrInterviewCompleted, PipelineStage.Withdrawn },
        [PipelineStage.HrInterviewCompleted] = new() { PipelineStage.OfferExtended, PipelineStage.RejectedByHr },
        [PipelineStage.OfferExtended]        = new() { PipelineStage.Hired, PipelineStage.Withdrawn, PipelineStage.RejectedByHr },
        // Terminal stages — no further transitions
        [PipelineStage.Hired]                = new(),
        [PipelineStage.RejectedByAi]         = new(),
        [PipelineStage.RejectedAfterAi]      = new(),
        [PipelineStage.RejectedByHr]         = new(),
        [PipelineStage.Withdrawn]            = new(),
    };

    public async Task<CandidateJourney> StartJourneyAsync(Guid applicationId, Guid applicantUserId, Guid jobPostingId, CancellationToken ct = default)
    {
        var existing = await _db.CandidateJourneys.FirstOrDefaultAsync(j => j.JobApplicationId == applicationId, ct);
        if (existing != null) return existing;

        var journey = new CandidateJourney
        {
            JobApplicationId = applicationId,
            ApplicantUserId = applicantUserId,
            JobPostingId = jobPostingId,
            CurrentStage = PipelineStage.Applied,
            StageHistoryJson = SerializeHistory(new[] { NewEvent(PipelineStage.Applied, "system", "Application received") })
        };
        _db.CandidateJourneys.Add(journey);
        await _db.SaveChangesAsync(ct);

        // Application-received auto-email — fire and forget
        await FireStageEmailAsync(journey, EmailCategory.ApplicationReceived, ct);
        return journey;
    }

    public async Task<CandidateJourney> TransitionAsync(
        Guid journeyId, PipelineStage toStage, string by, string? reason = null, CancellationToken ct = default)
    {
        var j = await _db.CandidateJourneys.FirstOrDefaultAsync(x => x.Id == journeyId, ct)
                ?? throw new InvalidOperationException($"Journey {journeyId} not found");

        if (j.CurrentStage == toStage) return j; // idempotent

        if (!Allowed.TryGetValue(j.CurrentStage, out var allowed) || !allowed.Contains(toStage))
        {
            throw new InvalidOperationException($"Illegal transition: {j.CurrentStage} → {toStage}");
        }

        var history = DeserializeHistory(j.StageHistoryJson);
        history.Add(NewEvent(toStage, by, reason));
        j.StageHistoryJson = SerializeHistory(history);
        j.CurrentStage = toStage;
        j.LastTransitionAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Journey {Id} → {Stage} by {By}", j.Id, toStage, by);

        // Fire the matching email category if this stage has one
        var category = StageToEmail(toStage);
        if (category.HasValue) await FireStageEmailAsync(j, category.Value, ct);

        return j;
    }

    public async Task<IReadOnlyList<CandidateJourney>> GetJourneysForJobAsync(Guid jobPostingId, CancellationToken ct = default) =>
        await _db.CandidateJourneys
            .Include(j => j.Applicant)
            .Where(j => j.JobPostingId == jobPostingId)
            .OrderByDescending(j => j.LastTransitionAt)
            .ToListAsync(ct);

    public Task<CandidateJourney?> GetJourneyAsync(Guid journeyId, CancellationToken ct = default) =>
        _db.CandidateJourneys.Include(j => j.Applicant).Include(j => j.JobPosting).FirstOrDefaultAsync(j => j.Id == journeyId, ct);

    // ── helpers ──
    private static EmailCategory? StageToEmail(PipelineStage stage) => stage switch
    {
        PipelineStage.Shortlisted          => EmailCategory.Shortlist,
        PipelineStage.RejectedByAi         => EmailCategory.Reject,
        PipelineStage.AiInterviewInvited   => EmailCategory.AiInterviewInvite,
        PipelineStage.RejectedAfterAi      => EmailCategory.Reject,
        PipelineStage.HrInterviewInvited   => EmailCategory.HrInterviewInvite,
        PipelineStage.RejectedByHr         => EmailCategory.Reject,
        PipelineStage.OfferExtended        => EmailCategory.OfferLetter,
        PipelineStage.Hired                => EmailCategory.Congratulations,
        _ => null
    };

    private async Task FireStageEmailAsync(CandidateJourney j, EmailCategory category, CancellationToken ct)
    {
        try
        {
            var applicant = await _db.Users.FirstOrDefaultAsync(u => u.Id == j.ApplicantUserId, ct);
            var posting = await _db.JobPostings.FirstOrDefaultAsync(p => p.Id == j.JobPostingId, ct);
            if (applicant == null) return;

            var tokens = new Dictionary<string, string>
            {
                ["candidateName"] = applicant.Name,
                ["candidateEmail"] = applicant.Email,
                ["jobTitle"] = posting?.Title ?? "the role",
                ["company"] = posting?.Company ?? "our company",
                ["stage"] = j.CurrentStage.ToString(),
            };

            await _email.EnqueueAsync(
                applicant.Email, applicant.Name, category, tokens,
                relatedEntityType: nameof(CandidateJourney), relatedEntityId: j.Id, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to queue {Category} email for journey {Id}", category, j.Id);
        }
    }

    private static Dictionary<string, object?> NewEvent(PipelineStage stage, string by, string? reason) => new()
    {
        ["stage"] = stage.ToString(),
        ["enteredAt"] = DateTime.UtcNow.ToString("o"),
        ["by"] = by,
        ["reason"] = reason
    };

    private static List<Dictionary<string, object?>> DeserializeHistory(string json)
    {
        try { return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json) ?? new(); }
        catch { return new(); }
    }

    private static string SerializeHistory(IEnumerable<Dictionary<string, object?>> events) =>
        JsonSerializer.Serialize(events);
}
