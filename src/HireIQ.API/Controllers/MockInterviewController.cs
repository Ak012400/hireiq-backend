using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using HireIQ.Infrastructure.Persistence;
using HireIQ.Application.DTOs;
using HireIQ.Domain.Entities;
using HireIQ.Application.Interfaces;
using HireIQ.Infrastructure.Identity;
using HireIQ.Infrastructure.Email;
using HireIQ.Infrastructure.Ai;
using HireIQ.Infrastructure.Pdf;
using HireIQ.Infrastructure.Persistence;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/mock-interview")]
[Authorize]
[EnableRateLimiting("ai")]
public class MockInterviewController : BaseController
{
    private readonly AppDbContext _db;
    private readonly GroqService _groq;

    private const int MaxQuestions = 6;

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

        // Generate first question via Groq
        var prompt = $@"You are a senior technical interviewer conducting a mock interview for the role of '{dto.JobTitle}'.
{(string.IsNullOrWhiteSpace(dto.JobDescription) ? "" : $"Job Description: {dto.JobDescription}")}

Start the interview with a warm welcome and your first question. The first question should be a behavioral warm-up (e.g. 'Tell me about yourself' or 'Walk me through your background').

Return ONLY the interviewer's spoken text — no labels, no formatting. Keep it natural and conversational.";

        var firstQuestion = await _groq.GenerateAsync(prompt);

        // ✅ Persist session in DB — survives restarts/redeploys (was an in-memory dictionary)
        var session = new MockInterviewSession
        {
            UserId = userId,
            JobTitle = dto.JobTitle,
            JobDescription = dto.JobDescription,
            Questions = new List<string> { firstQuestion },
            Answers = new List<string>(),
            Status = "InProgress",
        };

        _db.MockInterviewSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(new { sessionId = session.Id, question = firstQuestion });
    }

    [HttpPost("answer")]
    public async Task<IActionResult> SubmitAnswer([FromBody] AnswerMockInterviewDTO dto)
    {
        var userId = GetCurrentUserId();

        // ✅ DB lookup + ownership check
        var session = await _db.MockInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId
                                   && s.UserId == userId
                                   && s.Status == "InProgress");

        if (session == null)
            return NotFound(new { error = "Session not found or already completed. Please start a new interview." });

        // ✅ Reassign (not mutate) — EF change tracking on jsonb columns needs a new reference
        session.Answers = new List<string>(session.Answers) { dto.Answer };

        if (dto.IsLast || session.Answers.Count >= MaxQuestions)
        {
            // ✅ Structured evaluation via Groq JSON mode — no regex parsing
            var evaluation = await _groq.GenerateJsonAsync<InterviewEvaluation>(BuildEvalPrompt(session));

            if (evaluation == null)
            {
                // Groq failed/returned invalid JSON — keep session alive so user can retry
                return StatusCode(502, new { error = "AI evaluation failed. Please submit again." });
            }

            session.AiEvaluation = System.Text.Json.JsonSerializer.Serialize(evaluation, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            session.TechnicalScore = evaluation.TechnicalScore;
            session.CommunicationScore = evaluation.CommunicationScore;
            session.ConfidenceScore = evaluation.ConfidenceScore;
            session.OverallScore = evaluation.OverallScore;
            session.DurationSeconds = (int)(DateTime.UtcNow - session.CreatedAt).TotalSeconds;
            session.Status = "Completed";

            await _db.SaveChangesAsync();

            return Ok(new
            {
                finished = true,
                report = new MockInterviewReportDTO
                {
                    TechnicalScore = evaluation.TechnicalScore,
                    CommunicationScore = evaluation.CommunicationScore,
                    ConfidenceScore = evaluation.ConfidenceScore,
                    OverallScore = evaluation.OverallScore,
                    Strengths = evaluation.Strengths,
                    Improvements = evaluation.Improvements,
                    Evaluation = evaluation.Feedback,
                }
            });
        }

        // Generate next question
        var nextQ = await _groq.GenerateAsync(BuildNextQuestionPrompt(session, dto.Answer));
        session.Questions = new List<string>(session.Questions) { nextQ }; // ✅ new reference for EF
        await _db.SaveChangesAsync();

        return Ok(new { finished = false, nextQuestion = nextQ });
    }

    // ✅ Resume an interrupted interview (page refresh / redeploy no longer kills it)
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSession()
    {
        var userId = GetCurrentUserId();

        var session = await _db.MockInterviewSessions
            .Where(s => s.UserId == userId && s.Status == "InProgress")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session == null) return Ok(new { active = false });

        return Ok(new
        {
            active = true,
            sessionId = session.Id,
            jobTitle = session.JobTitle,
            currentQuestion = session.Questions.LastOrDefault(),
            questionNumber = session.Questions.Count,
            totalQuestions = MaxQuestions,
        });
    }

    private string BuildNextQuestionPrompt(MockInterviewSession session, string lastAnswer)
    {
        var history = string.Join("\n", session.Questions.Zip(session.Answers, (q, a) => $"Q: {q}\nA: {a}"));
        return $@"You are interviewing a candidate for '{session.JobTitle}'.
Interview so far:
{history}
Latest answer: {lastAnswer}

Ask the next interview question. Progress naturally: warm-up → technical skills → problem-solving → behavioral → situational → career goals.
Question {session.Questions.Count + 1} of {MaxQuestions}.
Return ONLY the next question, naturally phrased. No labels.";
    }

    private string BuildEvalPrompt(MockInterviewSession session)
    {
        var qa = string.Join("\n\n", session.Questions.Zip(session.Answers, (q, a) => $"Q: {q}\nA: {a}"));
        return $@"You evaluated a mock interview for '{session.JobTitle}'.

Full Interview:
{qa}

Return JSON with exactly these fields:
{{
  ""technicalScore"": <number 0-100>,
  ""communicationScore"": <number 0-100>,
  ""confidenceScore"": <number 0-100>,
  ""overallScore"": <number 0-100>,
  ""strengths"": [""2-3 specific points""],
  ""improvements"": [""2-3 specific points""],
  ""feedback"": ""2-3 paragraphs of specific, actionable feedback""
}}

Be honest, specific, and constructive.";
    }
}
