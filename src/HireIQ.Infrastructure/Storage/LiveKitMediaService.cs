using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HireIQ.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HireIQ.Infrastructure.Storage;

/// <summary>
/// LiveKit (livekit.io) media-server integration. Generates JWT access tokens for browser
/// clients so they can join WebRTC rooms. LiveKit handles the actual SFU + recording.
/// Free tier: 1000 minutes/month — enough for ~30 interviews.
///
/// Env vars:
///   LiveKit__ApiKey
///   LiveKit__ApiSecret
///   LiveKit__Url            wss://your-project.livekit.cloud
/// </summary>
public sealed class LiveKitMediaService : IMediaServerService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _url;

    public LiveKitMediaService(IConfiguration cfg)
    {
        _apiKey = cfg["LiveKit:ApiKey"] ?? "";
        _apiSecret = cfg["LiveKit:ApiSecret"] ?? "";
        _url = cfg["LiveKit:Url"] ?? "";
    }

    public Task<MediaRoomToken> CreateRoomTokenAsync(
        string roomName, string participantIdentity,
        bool canPublish, bool canSubscribe, TimeSpan validFor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret))
            throw new InvalidOperationException("LiveKit credentials not configured.");

        var expiresAt = DateTime.UtcNow.Add(validFor);
        var video = new
        {
            room = roomName,
            roomJoin = true,
            canPublish,
            canSubscribe,
            canPublishData = true
        };

        // LiveKit's spec — claims under "video" + standard JWT fields.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, participantIdentity),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("video", JsonSerializer.Serialize(video))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_apiSecret));
        var token = new JwtSecurityToken(
            issuer: _apiKey,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(new MediaRoomToken(jwt, _url, expiresAt));
    }

    public Task<bool> EndRoomAsync(string roomName, CancellationToken ct = default)
    {
        // Server API call would go here — leaving as no-op for MVP.
        // POST {host}/twirp/livekit.RoomService/DeleteRoom with JWT auth.
        return Task.FromResult(true);
    }

    public Task<string?> GetRecordingUrlAsync(string roomName, CancellationToken ct = default)
    {
        // Egress recording URL retrieval — TODO when recording is enabled.
        return Task.FromResult<string?>(null);
    }
}
