namespace HireIQ.Domain.Entities;

/// <summary>
/// Per-hirer credentials for a third-party job board.
/// Each hirer enters their own API keys/OAuth tokens — we never share credentials across tenants.
/// </summary>
public class HirerIntegration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HirerId { get; set; }
    public JobBoard Board { get; set; }

    public bool Enabled { get; set; }

    // Generic credential storage — interpretation depends on Board.
    //   LinkedIn:  ApiKey = client_id, ApiSecret = client_secret, AccessToken = OAuth bearer
    //   Naukri:    ApiKey = REST token
    //   Glassdoor: ApiKey = partner key, ApiSecret = signature key
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }
    public string? RefreshToken { get; set; }

    public string? ConfigJson { get; set; }   // board-specific extras

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? Hirer { get; set; }
}
