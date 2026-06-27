namespace HireIQ.Application.Interfaces;

public sealed record MediaRoomToken(string Token, string Url, DateTime ExpiresAt);

/// <summary>
/// Abstraction over WebRTC SFU (LiveKit / Daily.co / Twilio Video).
/// Implementation generates room + access tokens for client.
/// </summary>
public interface IMediaServerService
{
    Task<MediaRoomToken> CreateRoomTokenAsync(string roomName, string participantIdentity, bool canPublish, bool canSubscribe, TimeSpan validFor, CancellationToken ct = default);
    Task<bool> EndRoomAsync(string roomName, CancellationToken ct = default);
    Task<string?> GetRecordingUrlAsync(string roomName, CancellationToken ct = default);
}
