using System.Text;
using System.Text.Json;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HireIQ.Infrastructure.Ai.Interview;

/// <summary>
/// Sub-second next-question generator.
/// Uses Groq llama-3.1-8b-instant for ~250ms TTFT — fast enough to feel conversational.
/// </summary>
public sealed class GroqFastQuestionAgent : IFastQuestionAgent
{
    public AiAgentKind Kind => AiAgentKind.FastQuestion;

    private readonly HttpClient _http;
    private readonly string _groqKey;
    private const string MODEL = "llama-3.1-8b-instant";
    private const string GROQ_URL = "https://api.groq.com/openai/v1/chat/completions";

    public GroqFastQuestionAgent(IConfiguration cfg)
    {
        _groqKey = cfg["GroqSettings:ApiKey"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    public async Task<string> NextQuestionAsync(string jobTitle, string jobDescription, IReadOnlyList<InterviewTurn> history, CancellationToken ct = default)
    {
        var sys = $"""
You are an interviewer for the role of {jobTitle}. Job description: {jobDescription}.
Ask ONE crisp, conversational question to assess the candidate. Build on their last answer if there is one.
Rules:
- One question only. No preamble, no commentary.
- Mix behavioural, technical, and situational over the course of the interview.
- Keep it under 30 words.
- Never repeat a previous question.
""";

        var messages = new List<object> { new { role = "system", content = sys } };
        foreach (var turn in history.TakeLast(6))
        {
            messages.Add(new { role = "assistant", content = turn.Question });
            if (!string.IsNullOrWhiteSpace(turn.CandidateAnswer))
                messages.Add(new { role = "user", content = turn.CandidateAnswer });
        }
        messages.Add(new { role = "user", content = "Ask the next question." });

        var payload = new { model = MODEL, messages, max_tokens = 120, temperature = 0.7 };
        var req = new HttpRequestMessage(HttpMethod.Post, GROQ_URL)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Authorization", $"Bearer {_groqKey}");

        var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) return "Tell me about a recent challenge you solved.";

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim()
               ?? "Tell me about a recent challenge you solved.";
    }
}
