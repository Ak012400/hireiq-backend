using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

public class IntegrationUpsertDTO
{
    public string Board { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

[ApiController]
[Route("api/integrations")]
[Authorize]
public class HirerIntegrationsController : BaseController
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public HirerIntegrationsController(AppDbContext db, IConfiguration cfg)
    {
        _db = db; _cfg = cfg;
    }

    /// <summary>List all integration cards (one per supported board, even if not configured).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var hirerId = GetCurrentUserId();
        var existing = await _db.HirerIntegrations
            .Where(i => i.HirerId == hirerId)
            .ToDictionaryAsync(i => i.Board, ct);

        var all = Enum.GetValues<JobBoard>().Select(b =>
        {
            existing.TryGetValue(b, out var i);
            return new
            {
                board = b.ToString(),
                enabled = i?.Enabled ?? false,
                configured = i != null && !string.IsNullOrWhiteSpace(i.ApiKey),
                hasOAuth = i != null && !string.IsNullOrWhiteSpace(i.AccessToken),
                supportsPush = b == JobBoard.Indeed,   // only Indeed pushes without partner approval
                requiresPartnership = b is JobBoard.LinkedIn or JobBoard.Naukri or JobBoard.Glassdoor,
                lastUpdated = i?.UpdatedAt,
            };
        });
        return Ok(all);
    }

    [HttpPut("{board}")]
    public async Task<IActionResult> Upsert(string board, [FromBody] IntegrationUpsertDTO dto, CancellationToken ct)
    {
        if (!Enum.TryParse<JobBoard>(board, ignoreCase: true, out var b))
            return BadRequest(new { error = $"Unknown board '{board}'" });
        var hirerId = GetCurrentUserId();
        var existing = await _db.HirerIntegrations.FirstOrDefaultAsync(i => i.HirerId == hirerId && i.Board == b, ct);
        if (existing == null)
        {
            existing = new HirerIntegration { HirerId = hirerId, Board = b };
            _db.HirerIntegrations.Add(existing);
        }
        existing.Enabled = dto.Enabled;
        existing.ApiKey = dto.ApiKey;
        existing.ApiSecret = dto.ApiSecret;
        existing.AccessToken = dto.AccessToken;
        existing.RefreshToken = dto.RefreshToken;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { saved = true });
    }

    /// <summary>LinkedIn OAuth: returns the URL the browser should redirect to.</summary>
    [HttpGet("linkedin/oauth-url")]
    public IActionResult LinkedInOAuthUrl()
    {
        var clientId = _cfg["LinkedIn:ClientId"] ?? "";
        var redirect = _cfg["LinkedIn:RedirectUri"] ?? "https://hireiq-aipowered.vercel.app/integrations/linkedin/callback";
        if (string.IsNullOrWhiteSpace(clientId))
            return Ok(new { url = (string?)null, error = "LinkedIn:ClientId not configured on backend" });

        var state = Guid.NewGuid().ToString("N");
        var scope = Uri.EscapeDataString("openid profile email w_member_social");
        var url = $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirect)}&state={state}&scope={scope}";
        return Ok(new { url, state });
    }
}
