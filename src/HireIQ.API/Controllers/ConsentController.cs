using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

public class GrantConsentDTO
{
    public string Kind { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public string PolicyVersion { get; set; } = "1.0";
}

[ApiController]
[Route("api/consent")]
[Authorize]
public class ConsentController : BaseController
{
    private readonly AppDbContext _db;
    public ConsentController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Grant([FromBody] GrantConsentDTO dto, CancellationToken ct)
    {
        if (!Enum.TryParse<ConsentKind>(dto.Kind, ignoreCase: true, out var kind))
            return BadRequest(new { error = $"Unknown consent kind '{dto.Kind}'" });

        var record = new ConsentRecord
        {
            UserId = GetCurrentUserId(),
            Kind = kind,
            RelatedEntityId = dto.RelatedEntityId,
            PolicyVersion = dto.PolicyVersion,
            Granted = true,
            Withdrawn = false,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        _db.ConsentRecords.Add(record);
        await _db.SaveChangesAsync(ct);
        return Ok(new { record.Id, granted = true });
    }

    [HttpGet("check")]
    public async Task<IActionResult> Check([FromQuery] string kind, [FromQuery] Guid? relatedEntityId, CancellationToken ct)
    {
        if (!Enum.TryParse<ConsentKind>(kind, ignoreCase: true, out var k))
            return BadRequest(new { error = "Unknown consent kind" });
        var userId = GetCurrentUserId();

        var latest = await _db.ConsentRecords
            .Where(c => c.UserId == userId && c.Kind == k && c.RelatedEntityId == relatedEntityId)
            .OrderByDescending(c => c.RecordedAt)
            .FirstOrDefaultAsync(ct);

        return Ok(new { granted = latest?.Granted == true && !latest.Withdrawn, latestAt = latest?.RecordedAt });
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] GrantConsentDTO dto, CancellationToken ct)
    {
        if (!Enum.TryParse<ConsentKind>(dto.Kind, ignoreCase: true, out var kind))
            return BadRequest();
        var record = new ConsentRecord
        {
            UserId = GetCurrentUserId(),
            Kind = kind,
            RelatedEntityId = dto.RelatedEntityId,
            PolicyVersion = dto.PolicyVersion,
            Granted = false,
            Withdrawn = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        _db.ConsentRecords.Add(record);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }
}
