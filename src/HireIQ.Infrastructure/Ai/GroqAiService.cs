using HireIQ.Application.Interfaces;

namespace HireIQ.Infrastructure.Ai;

/// <summary>
/// IAiService adapter over GroqService — the Application layer talks to this interface only.
/// Embeddings are delegated to MLService (Flask microservice) until we wire a Groq embedding endpoint.
/// </summary>
public sealed class GroqAiService : IAiService
{
    private readonly GroqService _groq;
    private readonly MLService _ml;

    public GroqAiService(GroqService groq, MLService ml)
    {
        _groq = groq;
        _ml = ml;
    }

    public Task<string> ChatCompletionAsync(string prompt, string? system = null, CancellationToken ct = default)
    {
        // GroqService applies its own system prompt; this passthrough keeps Application layer agnostic.
        return _groq.GenerateFieldAsync(prompt);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // MLService exposes embeddings via Flask — keep wired here. Stub returns empty if ML unavailable.
        try
        {
            return await _ml.GetEmbeddingAsync(text);
        }
        catch
        {
            return Array.Empty<float>();
        }
    }
}
