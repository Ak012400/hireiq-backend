using HireIQ.Domain.Entities;

namespace HireIQ.Application.Interfaces;

public sealed record InterviewTurn(
    int TurnIndex,
    Guid QuestionId,
    string Question,
    string? CandidateAnswer,
    long QuestionAskedAtMs,
    long? AnswerCompletedAtMs);

/// <summary>
/// Coordinates the 3-agent swarm during a live interview.
/// Fast agent stays in interactive loop; Visual + Deep agents process out-of-band per turn.
/// </summary>
public interface IInterviewOrchestrator
{
    Task<InterviewSession> StartSessionAsync(Guid roomId, Guid candidateJourneyId, CancellationToken ct = default);

    /// <summary>Fast agent generates the next question (sub-second).</summary>
    Task<string> GetNextQuestionAsync(Guid sessionId, IReadOnlyList<InterviewTurn> history, CancellationToken ct = default);

    /// <summary>Persist a transcript segment + trigger deep analysis as a background job.</summary>
    Task RecordAnswerAsync(Guid sessionId, Guid questionId, string answerText, long startMs, long endMs, CancellationToken ct = default);

    /// <summary>Per-frame visual telemetry (called every ~3-5s during interview).</summary>
    Task RecordVisualFrameAsync(Guid sessionId, byte[] frameJpeg, long frameAtMs, CancellationToken ct = default);

    Task<InterviewFinalScore> EndSessionAsync(Guid sessionId, CancellationToken ct = default);
}

public sealed record AgentObservationResult(
    AiAgentKind Agent,
    string ObservationJson,
    float? ScoreTechnical = null,
    float? ScoreCommunication = null,
    float? ScoreConfidence = null,
    float? ScoreAttention = null,
    float? ScoreEmotion = null,
    string? RawResponse = null,
    int LatencyMs = 0);

/// <summary>Common contract for all 3 AI agents in the swarm.</summary>
public interface IInterviewAgent
{
    AiAgentKind Kind { get; }
}

public interface IFastQuestionAgent : IInterviewAgent
{
    Task<string> NextQuestionAsync(string jobTitle, string jobDescription, IReadOnlyList<InterviewTurn> history, CancellationToken ct = default);
}

public interface IVisualBehaviorAgent : IInterviewAgent
{
    Task<AgentObservationResult> AnalyzeFrameAsync(byte[] jpegBytes, CancellationToken ct = default);
}

public interface IDeepAnswerAgent : IInterviewAgent
{
    Task<AgentObservationResult> AnalyzeAnswerAsync(string question, string answer, string jobContext, CancellationToken ct = default);
}

public interface ITranscriptionService
{
    Task<string> TranscribeAsync(Stream audio, string mimeType, CancellationToken ct = default);
}
