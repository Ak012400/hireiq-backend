using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

public class StartSessionRequest { public Guid RoomId { get; set; } public Guid JourneyId { get; set; } }
public class AnswerRequest
{
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public long StartMs { get; set; }
    public long EndMs { get; set; }
}
public class FrameRequest
{
    public Guid SessionId { get; set; }
    public string FrameBase64 { get; set; } = string.Empty;
    public long FrameAtMs { get; set; }
}

[ApiController]
[Route("api/ai-interview")]
[Authorize]
public class AiInterviewController : BaseController
{
    private readonly IInterviewOrchestrator _orch;
    private readonly AppDbContext _db;

    public AiInterviewController(IInterviewOrchestrator orch, AppDbContext db)
    {
        _orch = orch;
        _db = db;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest req, CancellationToken ct)
    {
        var session = await _orch.StartSessionAsync(req.RoomId, req.JourneyId, ct);
        return Ok(new { sessionId = session.Id, startedAt = session.StartedAt });
    }

    [HttpPost("next-question")]
    public async Task<IActionResult> NextQuestion([FromQuery] Guid sessionId, CancellationToken ct)
    {
        // Build turn history from DB
        var questions = await _db.InterviewQuestions
            .Where(q => q.SessionId == sessionId)
            .OrderBy(q => q.QuestionOrder)
            .ToListAsync(ct);
        var answers = await _db.InterviewAnswers
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(ct);

        var history = questions.Select(q => new InterviewTurn(
            q.QuestionOrder, q.Id, q.QuestionText,
            answers.FirstOrDefault(a => a.QuestionId == q.Id)?.AnswerText,
            new DateTimeOffset(q.AskedAt).ToUnixTimeMilliseconds(),
            null
        )).ToList();

        var question = await _orch.GetNextQuestionAsync(sessionId, history, ct);
        var saved = await _db.InterviewQuestions
            .Where(q => q.SessionId == sessionId)
            .OrderByDescending(q => q.QuestionOrder)
            .FirstAsync(ct);
        return Ok(new { questionId = saved.Id, questionText = question, order = saved.QuestionOrder });
    }

    [HttpPost("answer")]
    public async Task<IActionResult> RecordAnswer([FromBody] AnswerRequest req, CancellationToken ct)
    {
        await _orch.RecordAnswerAsync(req.SessionId, req.QuestionId, req.AnswerText, req.StartMs, req.EndMs, ct);
        return Accepted();
    }

    [HttpPost("frame")]
    public async Task<IActionResult> RecordFrame([FromBody] FrameRequest req, CancellationToken ct)
    {
        var bytes = Convert.FromBase64String(req.FrameBase64);
        await _orch.RecordVisualFrameAsync(req.SessionId, bytes, req.FrameAtMs, ct);
        return Accepted();
    }

    [HttpPost("end")]
    public async Task<IActionResult> End([FromQuery] Guid sessionId, CancellationToken ct)
    {
        var score = await _orch.EndSessionAsync(sessionId, ct);
        return Ok(new
        {
            score.OverallScore,
            score.TechnicalScore,
            score.CommunicationScore,
            score.BehavioralScore,
            score.AttentionScore,
            score.Recommendation,
            score.AggregatedReasoning
        });
    }

    [HttpGet("{sessionId:guid}/report")]
    public async Task<IActionResult> Report(Guid sessionId, CancellationToken ct)
    {
        var final = await _db.InterviewFinalScores.FirstOrDefaultAsync(f => f.SessionId == sessionId, ct);
        if (final == null) return NotFound();

        var observations = await _db.AiObservations.Where(o => o.SessionId == sessionId).ToListAsync(ct);
        var transcript = await _db.InterviewTranscripts.Where(t => t.SessionId == sessionId).OrderBy(t => t.SegmentIndex).ToListAsync(ct);

        return Ok(new { final, observations, transcript });
    }
}
