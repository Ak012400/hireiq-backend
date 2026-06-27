using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HireIQ.Infrastructure.Ai.Interview;

/// <summary>
/// Deep post-answer analysis — runs in background after each candidate response.
/// Groq llama-3.3-70b-versatile for higher reasoning quality (5-8s typical).
/// Returns structured JSON with technical/communication/confidence scores.
/// </summary>
public sealed class GroqDeepAnswerAgent : IDeepAnswerAgent
{
    public AiAgentKind Kind => AiAgentKind.DeepAnswer;

    private readonly HttpClient _http;
    private readonly string _groqKey;
    private const string MODEL = "llama-3.3-70b-versatile";
    private const string GROQ_URL = "https://api.groq.com/openai/v1/chat/completions";

    public GroqDeepAnswerAgent(IConfiguration cfg)
    {
        _groqKey = cfg["GroqSettings:ApiKey"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<AgentObservationResult> AnalyzeAnswerAsync(string question, string answer, string jobContext, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var sys = """
You are an expert interviewer evaluator. Given a job context, an interview question, and the candidate's answer,
return STRICT JSON with this exact schema (no markdown, no commentary):
{
  "score_technical": 0-100,
  "score_communication": 0-100,
  "score_confidence": 0-100,
  "strengths": ["..."],
  "weaknesses": ["..."],
  "follow_up_suggestion": "...",
  "summary": "one paragraph"
}
""";

        var user = $"Job: {jobContext}\n\nQuestion: {question}\n\nAnswer: {answer}";
        var payload = new
        {
            model = MODEL,
            messages = new object[] {
                new { role = "system", content = sys },
                new { role = "user", content = user }
            },
            response_format = new { type = "json_object" },
            temperature = 0.3,
            max_tokens = 800
        };

        var req = new HttpRequestMessage(HttpMethod.Post, GROQ_URL)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Authorization", $"Bearer {_groqKey}");

        var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        sw.Stop();

        if (!resp.IsSuccessStatusCode)
        {
            return new AgentObservationResult(Kind, "{}", RawResponse: body, LatencyMs: (int)sw.ElapsedMilliseconds);
        }

        var raw = JsonSerializer.Deserialize<JsonElement>(body)
            .GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(raw);
            return new AgentObservationResult(
                Kind,
                ObservationJson: raw,
                ScoreTechnical: ReadFloat(parsed, "score_technical"),
                ScoreCommunication: ReadFloat(parsed, "score_communication"),
                ScoreConfidence: ReadFloat(parsed, "score_confidence"),
                RawResponse: raw,
                LatencyMs: (int)sw.ElapsedMilliseconds);
        }
        catch
        {
            return new AgentObservationResult(Kind, "{}", RawResponse: raw, LatencyMs: (int)sw.ElapsedMilliseconds);
        }
    }

    private static float? ReadFloat(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : null;
}
