using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;
using HireIQ.API.Services;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/mock-interview")]
[Authorize]
public class MockInterviewController : BaseController
{
    private readonly AppDbContext _db;
    private readonly GroqService _groq;

    // In-memory session store (fine for demo — replace with Redis/DB for prod)
    private static readonly Dictionary<Guid, MockSessionState> _sessions = new();

    public MockInterviewController(AppDbContext db, GroqService groq)
    {
        _db = db;
        _groq = groq;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartInterview([FromBody] StartMockInterviewDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.JobTitle))
            return BadRequest(new { error = "Job title is required." });

        var userId = GetCurrentUserId();
        var sessionId = Guid.NewGuid();

        // Generate first question via Groq
        var prompt = $@"You are a senior technical interviewer conducting a mock interview for the role of '{dto.JobTitle}'.
{(string.IsNullOrWhiteSpace(dto.JobDescription) ? "" : $"Job Description: {dto.JobDescription}")}

Start the interview with a warm welcome and your first question. The first question should be a behavioral warm-up (e.g. 'Tell me about yourself' or 'Walk me through your background').

Return ONLY the interviewer's spoken text — no labels, no formatting. Keep it natural and conversational.";

        var firstQuestion = await _groq.GenerateAsync(prompt);

        // Create session
        var session = new MockSessionState
        {
            UserId = userId,
            JobTitle = dto.JobTitle,
            JobDescription = dto.JobDescription,
            Questions = new List<string> { firstQuestion },
            Answers = new List<string>(),
            ConversationHistory = new List<(string role, string content)>
            {
                ("system", $"You are a senior technical interviewer for the role of '{dto.JobTitle}'. Conduct a professional mock interview. Ask one question at a time. Be conversational. Evaluate each answer internally. After the final answer, provide detailed scoring."),
                ("assistant", firstQuestion)
            }
        };

        _sessions[sessionId] = session;

        return Ok(new { sessionId, question = firstQuestion });
    }

    [HttpPost("answer")]
    public async Task<IActionResult> SubmitAnswer([FromBody] AnswerMockInterviewDTO dto)
    {
        if (!_sessions.TryGetValue(dto.SessionId, out var session))
            return NotFound(new { error = "Session not found or expired. Please start a new interview." });

        session.Answers.Add(dto.Answer);
        session.ConversationHistory.Add(("user", dto.Answer));

        if (dto.IsLast || session.Answers.Count >= 6)
        {
            // Generate final evaluation
            var evalPrompt = BuildEvalPrompt(session);
            var evalText = await _groq.GenerateAsync(evalPrompt);

            // Parse scores from eval (simple extraction)
            var scores = ExtractScores(evalText);

            // Save to DB
            var dbSession = new MockInterviewSession
            {
                UserId = session.UserId,
                JobTitle = session.JobTitle,
                JobDescription = session.JobDescription,
                Questions = session.Questions,
                Answers = session.Answers,
                AiEvaluation = evalText,
                TechnicalScore = scores.technical,
                CommunicationScore = scores.communication,
                ConfidenceScore = scores.confidence,
                OverallScore = scores.overall,
                DurationSeconds = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds,
            };
            _db.MockInterviewSessions.Add(dbSession);
            await _db.SaveChangesAsync();

            _sessions.Remove(dto.SessionId);

            return Ok(new
            {
                finished = true,
                report = new MockInterviewReportDTO
                {
                    TechnicalScore = scores.technical,
                    CommunicationScore = scores.communication,
                    ConfidenceScore = scores.confidence,
                    OverallScore = scores.overall,
                    Evaluation = evalText,
                }
            });
        }

        // Generate next question
        var nextPrompt = BuildNextQuestionPrompt(session, dto.Answer);
        session.ConversationHistory.Add(("system", nextPrompt));
        var nextQ = await _groq.GenerateAsync(nextPrompt);

        session.Questions.Add(nextQ);
        session.ConversationHistory.Add(("assistant", nextQ));

        return Ok(new { finished = false, nextQuestion = nextQ });
    }

    private string BuildNextQuestionPrompt(MockSessionState session, string lastAnswer)
    {
        var history = string.Join("\n", session.Questions.Zip(session.Answers, (q, a) => $"Q: {q}\nA: {a}"));
        return $@"You are interviewing a candidate for '{session.JobTitle}'. 
Interview so far:
{history}
Latest answer: {lastAnswer}

Ask the next interview question. Progress naturally: warm-up → technical skills → problem-solving → behavioral → situational → career goals.
Question {session.Questions.Count + 1} of 6.
Return ONLY the next question, naturally phrased. No labels.";
    }

    private string BuildEvalPrompt(MockSessionState session)
    {
        var qa = string.Join("\n\n", session.Questions.Zip(session.Answers, (q, a) => $"Q: {q}\nA: {a}"));
        return $@"You evaluated a mock interview for '{session.JobTitle}'.

Full Interview:
{qa}

Provide a comprehensive evaluation with these exact sections:
1. TECHNICAL SCORE: [number 0-100]
2. COMMUNICATION SCORE: [number 0-100]
3. CONFIDENCE SCORE: [number 0-100]
4. OVERALL SCORE: [number 0-100]
5. STRENGTHS: (2-3 specific points)
6. AREAS TO IMPROVE: (2-3 specific points)
7. DETAILED FEEDBACK: (2-3 paragraphs, specific and actionable)

Be honest, specific, and constructive.";
    }

    private (decimal technical, decimal communication, decimal confidence, decimal overall) ExtractScores(string eval)
    {
        decimal Parse(string label)
        {
            var line = eval.Split('\n').FirstOrDefault(l => l.Contains(label, StringComparison.OrdinalIgnoreCase)) ?? "";
            var num = new string(line.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return decimal.TryParse(num, out var v) ? Math.Min(v, 100) : 70;
        }
        return (Parse("TECHNICAL SCORE"), Parse("COMMUNICATION SCORE"), Parse("CONFIDENCE SCORE"), Parse("OVERALL SCORE"));
    }
}

// In-memory session state
public class MockSessionState
{
    public Guid UserId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? JobDescription { get; set; }
    public List<string> Questions { get; set; } = new();
    public List<string> Answers { get; set; } = new();
    public List<(string role, string content)> ConversationHistory { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
