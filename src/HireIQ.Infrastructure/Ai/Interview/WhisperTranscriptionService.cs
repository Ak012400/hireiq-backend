using System.Net.Http.Headers;
using System.Text.Json;
using HireIQ.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Ai.Interview;

/// <summary>
/// Whisper transcription via Groq (Groq hosts OpenAI Whisper at very high throughput).
/// Alternative: OpenAI Whisper API ($0.006/min) — set OpenAiSettings:ApiKey instead.
/// </summary>
public sealed class WhisperTranscriptionService : ITranscriptionService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _model;
    private readonly ILogger<WhisperTranscriptionService> _logger;

    public WhisperTranscriptionService(IConfiguration cfg, ILogger<WhisperTranscriptionService> logger)
    {
        // Prefer Groq Whisper (faster + cheaper). Fall back to OpenAI if configured.
        var groqKey = cfg["GroqSettings:ApiKey"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
        var openaiKey = cfg["OpenAiSettings:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            _apiKey = groqKey;
            _endpoint = "https://api.groq.com/openai/v1/audio/transcriptions";
            _model = "whisper-large-v3-turbo";
        }
        else
        {
            _apiKey = openaiKey;
            _endpoint = "https://api.openai.com/v1/audio/transcriptions";
            _model = "whisper-1";
        }

        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(Stream audio, string mimeType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("No Whisper API key configured — returning empty transcript");
            return string.Empty;
        }

        using var form = new MultipartFormDataContent();
        var audioContent = new StreamContent(audio);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(audioContent, "file", "audio.webm");
        form.Add(new StringContent(_model), "model");
        form.Add(new StringContent("text"), "response_format");

        var req = new HttpRequestMessage(HttpMethod.Post, _endpoint) { Content = form };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Whisper transcription failed: {Status} {Body}", resp.StatusCode, body);
                return string.Empty;
            }
            return body.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper call failed");
            return string.Empty;
        }
    }
}
