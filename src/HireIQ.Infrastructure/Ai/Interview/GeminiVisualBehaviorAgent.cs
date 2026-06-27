using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Ai.Interview;

/// <summary>
/// Per-frame visual analysis — eye contact, attention, posture, engagement.
/// Uses Gemini 1.5 Flash multimodal (free tier 15 req/min, then $0.075/M input tokens).
/// Intentionally avoids any appearance/identity scoring to reduce bias risk.
/// </summary>
public sealed class GeminiVisualBehaviorAgent : IVisualBehaviorAgent
{
    public AiAgentKind Kind => AiAgentKind.VisualBehavior;

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<GeminiVisualBehaviorAgent> _logger;
    private const string MODEL = "gemini-1.5-flash";
    private static readonly string EndpointTemplate =
        "https://generativelanguage.googleapis.com/v1beta/models/" + MODEL + ":generateContent?key={0}";

    public GeminiVisualBehaviorAgent(IConfiguration cfg, ILogger<GeminiVisualBehaviorAgent> logger)
    {
        _apiKey = cfg["GeminiSettings:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _logger = logger;
    }

    public async Task<AgentObservationResult> AnalyzeFrameAsync(byte[] jpegBytes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new AgentObservationResult(Kind, "{\"skipped\":\"no_api_key\"}");
        }

        var sw = Stopwatch.StartNew();
        var b64 = Convert.ToBase64String(jpegBytes);

        var prompt = """
You are scoring an interview candidate's engagement from a single video frame.
Return STRICT JSON only, no markdown:
{
  "score_attention": 0-100,
  "score_confidence": 0-100,
  "score_emotion": 0-100,
  "eye_contact": "high|medium|low|none",
  "posture": "open|neutral|closed",
  "notes": "very short"
}
DO NOT comment on appearance, age, gender, ethnicity, attire, or background.
""";

        var payload = new
        {
            contents = new[] {
                new { parts = new object[] {
                    new { text = prompt },
                    new { inline_data = new { mime_type = "image/jpeg", data = b64 } }
                }}
            },
            generationConfig = new { temperature = 0.2, response_mime_type = "application/json" }
        };

        var json = JsonSerializer.Serialize(payload);
        var req = new HttpRequestMessage(HttpMethod.Post, string.Format(EndpointTemplate, _apiKey))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        try
        {
            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini visual call failed: {Status} {Body}", resp.StatusCode, body);
                return new AgentObservationResult(Kind, "{}", RawResponse: body, LatencyMs: (int)sw.ElapsedMilliseconds);
            }

            var raw = JsonSerializer.Deserialize<JsonElement>(body)
                .GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";

            var parsed = JsonSerializer.Deserialize<JsonElement>(raw);
            return new AgentObservationResult(
                Kind,
                ObservationJson: raw,
                ScoreAttention: ReadFloat(parsed, "score_attention"),
                ScoreConfidence: ReadFloat(parsed, "score_confidence"),
                ScoreEmotion: ReadFloat(parsed, "score_emotion"),
                RawResponse: raw,
                LatencyMs: (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini visual frame analysis failed");
            return new AgentObservationResult(Kind, "{\"error\":\"exception\"}", LatencyMs: (int)sw.ElapsedMilliseconds);
        }
    }

    private static float? ReadFloat(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : null;
}
