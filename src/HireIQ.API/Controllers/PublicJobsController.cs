using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

/// <summary>
/// Public (no-auth) browsing of published jobs — what candidates see before signing up.
/// </summary>
[ApiController]
[Route("api/public/jobs")]
public class PublicJobsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicJobsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Browse([FromQuery] string? q, [FromQuery] string? location, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var query = _db.JobPostings.Where(j => j.Status == JobPostingStatus.Published);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(j => EF.Functions.ILike(j.Title, $"%{q}%") || EF.Functions.ILike(j.Description, $"%{q}%"));
        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(j => EF.Functions.ILike(j.Location, $"%{location}%"));

        var total = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.PublishedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(j => new {
                j.Id, j.Title, j.Company, j.Location, j.WorkMode, j.EmploymentType,
                j.SalaryMin, j.SalaryMax, j.Currency,
                j.ExperienceMinYears, j.ExperienceMaxYears,
                j.PublishedAt
            })
            .ToListAsync();

        return Ok(new { total, page, size, results = jobs });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var j = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id && x.Status == JobPostingStatus.Published);
        if (j == null) return NotFound();
        return Ok(j);
    }
}
