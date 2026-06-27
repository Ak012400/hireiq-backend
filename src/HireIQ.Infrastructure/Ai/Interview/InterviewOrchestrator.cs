using System.Text.Json;
using Hangfire;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Ai.Interview;

/// <summary>
/// Coordinates the 3-agent swarm during a live AI interview.
/// Fast agent runs in the request hot path (sub-second).
/// Visual + Deep agents are dispatched as background Hangfire jobs.
/// Aggregation computes the final score when the session ends.
/// </summary>
public sealed class InterviewOrchestrator : IInterviewOrchestrator
{
    private readonly AppDbContext _db;
    private readonly IFastQuestionAgent _fast;
    private readonly IVisualBehaviorAgent _visual;
    private readonly IDeepAnswerAgent _deep;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<InterviewOrchestrator> _logger;

    public InterviewOrchestrator(
        AppDbContext db,
        IFastQuestionAgent fast,
        IVisualBehaviorAgent visual,
        IDeepAnswerAgent deep,
        IBackgroundJobClient jobs,
        ILogger<InterviewOrchestrator> logger)
    {
        _db = db;
        _fast = fast;
        _visual = visual;
        _deep = deep;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<InterviewSession> StartSessionAsync(Guid roomId, Guid candidateJourneyId, CancellationToken ct = default)
    {
        var session = new InterviewSession
        {
            InterviewRoomId = roomId,
            CandidateJourneyId = candidateJourneyId,
            StartedAt = DateTime.UtcNow,
            Status = InterviewSessionStatus.InProgress
        };
        _db.InterviewSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<string> GetNextQuestionAsync(Guid sessionId, IReadOnlyList<InterviewTurn> history, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions
            .Include(s => s.InterviewRoom).ThenInclude(r => r!.Job)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found");

        var title = session.InterviewRoom?.Job?.Title ?? "the role";
        var jd = session.InterviewRoom?.Job?.Content ?? string.Empty;

        var question = await _fast.NextQuestionAsync(title, jd, history, ct);

        var saved = new InterviewQuestion
        {
            SessionId = sessionId,
            QuestionOrder = history.Count + 1,
            QuestionText = question,
            Source = "AI_GENERATED"
        };
        _db.InterviewQuestions.Add(saved);
        await _db.SaveChangesAsync(ct);

        return question;
    }

    public async Task RecordAnswerAsync(Guid sessionId, Guid questionId, string answerText, long startMs, long endMs, CancellationToken ct = default)
    {
        var question = await _db.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new InvalidOperationException("Question not found");

        // Persist transcript segment
        var segIndex = await _db.InterviewTranscripts.CountAsync(t => t.SessionId == sessionId, ct);
        var segment = new InterviewTranscript
        {
            SessionId = sessionId,
            SegmentIndex = segIndex,
            Speaker = "CANDIDATE",
            Text = answerText,
            StartMs = startMs,
            EndMs = endMs
        };
        _db.InterviewTranscripts.Add(segment);

        var answer = new InterviewAnswer
        {
            SessionId = sessionId,
            QuestionId = questionId,
            AnswerText = answerText,
            DurationSeconds = (int)((endMs - startMs) / 1000),
            TranscriptSegmentIdsJson = JsonSerializer.Serialize(new[] { segment.Id })
        };
        _db.InterviewAnswers.Add(answer);
        await _db.SaveChangesAsync(ct);

        // Background: deep agent analyses this answer (5-8s — don't block the candidate)
        _jobs.Enqueue<InterviewOrchestrator>(o => o.RunDeepAnalysisAsync(sessionId, questionId, answerText));
    }

    public async Task RecordVisualFrameAsync(Guid sessionId, byte[] frameJpeg, long frameAtMs, CancellationToken ct = default)
    {
        // Visual analysis is also background — Gemini ~1-2s
        var jpeg = frameJpeg; // capture for closure
        _jobs.Enqueue<InterviewOrchestrator>(o => o.RunVisualAnalysisAsync(sessionId, jpeg, frameAtMs));
        await Task.CompletedTask;
    }

    public async Task<InterviewFinalScore> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.InterviewSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found");

        session.EndedAt = DateTime.UtcNow;
        session.Status = InterviewSessionStatus.Completed;
        if (session.StartedAt.HasValue)
            session.TotalDurationSeconds = (int)(session.EndedAt.Value - session.StartedAt.Value).TotalSeconds;

        // Aggregate all observations
        var obs = await _db.AiObservations.Where(o => o.SessionId == sessionId).ToListAsync(ct);

        float Avg(Func<AiObservation, float?> sel) =>
            obs.Select(sel).Where(x => x.HasValue).DefaultIfEmpty(0).Average(x => x ?? 0);

        var tech = Avg(o => o.ScoreTechnical);
        var comm = Avg(o => o.ScoreCommunication);
        var conf = Avg(o => o.ScoreConfidence);
        var att = Avg(o => o.ScoreAttention);
        var emo = Avg(o => o.ScoreEmotion);
        var behavioral = (att + emo + conf) / 3f;
        var overall = (tech * 0.35f) + (comm * 0.25f) + (behavioral * 0.4f);
        var rec = overall >= 70 ? "PROCEED" : overall >= 50 ? "MAYBE" : "REJECT";

        var final = new InterviewFinalScore
        {
            SessionId = sessionId,
            TechnicalScore = tech,
            CommunicationScore = comm,
            ConfidenceScore = conf,
            AttentionScore = att,
            BehavioralScore = behavioral,
            OverallScore = overall,
            Recommendation = rec,
            AggregatedReasoning = $"Aggregated across {obs.Count} agent observations. Tech={tech:F0}, Comm={comm:F0}, Behavioral={behavioral:F0}."
        };
        _db.InterviewFinalScores.Add(final);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Interview {Sid} finalised: {Rec} ({Score})", sessionId, rec, overall);
        return final;
    }

    // ── Background job entry points (called by Hangfire) ──

    public async Task RunDeepAnalysisAsync(Guid sessionId, Guid questionId, string answerText)
    {
        var question = await _db.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == questionId);
        var session = await _db.InterviewSessions
            .Include(s => s.InterviewRoom).ThenInclude(r => r!.Job)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (question == null || session == null) return;

        var jobContext = $"{session.InterviewRoom?.Job?.Title} — {session.InterviewRoom?.Job?.Content}";
        var result = await _deep.AnalyzeAnswerAsync(question.QuestionText, answerText, jobContext);

        _db.AiObservations.Add(ObservationFrom(sessionId, question.QuestionOrder, questionId, result));
        await _db.SaveChangesAsync();
    }

    public async Task RunVisualAnalysisAsync(Guid sessionId, byte[] frameJpeg, long frameAtMs)
    {
        var result = await _visual.AnalyzeFrameAsync(frameJpeg);
        var turnIndex = (int)(frameAtMs / 5000); // bucket every 5s
        _db.AiObservations.Add(ObservationFrom(sessionId, turnIndex, null, result));
        await _db.SaveChangesAsync();
    }

    private static AiObservation ObservationFrom(Guid sessionId, int turnIndex, Guid? questionId, AgentObservationResult r) =>
        new()
        {
            SessionId = sessionId,
            Agent = r.Agent,
            TurnIndex = turnIndex,
            RelatedQuestionId = questionId,
            ObservationJson = r.ObservationJson,
            ScoreTechnical = r.ScoreTechnical,
            ScoreCommunication = r.ScoreCommunication,
            ScoreConfidence = r.ScoreConfidence,
            ScoreAttention = r.ScoreAttention,
            ScoreEmotion = r.ScoreEmotion,
            RawResponse = r.RawResponse,
            LatencyMs = r.LatencyMs
        };
}
