namespace HireIQ.Application.Interfaces;

/// <summary>
/// AI Layer entry point — orchestrates Groq / ML / embeddings calls.
/// Per roadmap, this expands into agents: Resume, ATS, Career Coach, Interview, Portfolio, JobMatch.
/// </summary>
public interface IAiService
{
    Task<string> ChatCompletionAsync(string prompt, string? system = null, CancellationToken ct = default);
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
