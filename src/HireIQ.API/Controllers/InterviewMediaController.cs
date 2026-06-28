using HireIQ.Application.Interfaces;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

public class LiveKitTokenRequest
{
    public Guid RoomId { get; set; }
    public string Identity { get; set; } = string.Empty;
}

public class TranscribeChunkRequest
{
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public long StartMs { get; set; }
    public long EndMs { get; set; }
}

[ApiController]
[Route("api/interview-media")]
[Authorize]
public class InterviewMediaController : BaseController
{
    private readonly IMediaServerService _media;
    private readonly ITranscriptionService _transcription;
    private readonly IInterviewOrchestrator _orch;
    private readonly AppDbContext _db;

    public InterviewMediaController(
        IMediaServerService media, ITranscriptionService transcription,
        IInterviewOrchestrator orch, AppDbContext db)
    {
        _media = media; _transcription = transcription; _orch = orch; _db = db;
    }

    /// <summary>Browser asks for a LiveKit access token to join the room.</summary>
    [HttpPost("livekit-token")]
    public async Task<IActionResult> LiveKitToken([FromBody] LiveKitTokenRequest req, CancellationToken ct)
    {
        var room = await _db.InterviewRooms.FirstOrDefaultAsync(r => r.Id == req.RoomId, ct);
        if (room == null) return NotFound();

        var identity = string.IsNullOrWhiteSpace(req.Identity)
            ? GetCurrentUserId().ToString()
            : req.Identity;

        try
        {
            var token = await _media.CreateRoomTokenAsync(
                roomName: $"hireiq-{room.RoomCode}",
                participantIdentity: identity,
                canPublish: true, canSubscribe: true,
                validFor: TimeSpan.FromHours(2), ct: ct);
            return Ok(new { token = token.Token, url = token.Url, expiresAt = token.ExpiresAt });
        }
        catch (InvalidOperationException ex)
        {
            // LiveKit not configured — graceful degrade
            return Ok(new { token = (string?)null, url = (string?)null, error = ex.Message });
        }
    }

    /// <summary>
    /// Browser MediaRecorder uploads audio chunk (~30s). Server transcribes via Whisper,
    /// stores transcript segment, and triggers deep AI analysis as a background job.
    /// </summary>
    [HttpPost("transcribe-chunk")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> TranscribeChunk(
        [FromForm] Guid sessionId,
        [FromForm] Guid questionId,
        [FromForm] long startMs,
        [FromForm] long endMs,
        IFormFile audio,
        CancellationToken ct)
    {
        if (audio == null || audio.Length == 0) return BadRequest(new { error = "Audio file missing" });

        using var stream = audio.OpenReadStream();
        var transcript = await _transcription.TranscribeAsync(stream, audio.ContentType ?? "audio/webm", ct);

        if (string.IsNullOrWhiteSpace(transcript))
            return Ok(new { text = "", note = "Whisper returned empty — no transcription" });

        await _orch.RecordAnswerAsync(sessionId, questionId, transcript, startMs, endMs, ct);
        return Ok(new { text = transcript });
    }
}
