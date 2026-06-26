using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize] // ✅
public class JobController : BaseController // ✅ BaseController
{
    private readonly AppDbContext _db;

    public JobController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJobDTO dto)
    {
        var userId = GetCurrentUserId(); // ✅ JWT se, DB se nahi

        var job = new JobDescription
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = userId
        };

        _db.JobDescriptions.Add(job);
        await _db.SaveChangesAsync();

        return Ok(new JobResponseDTO
        {
            Id = job.Id,
            Title = job.Title,
            Content = job.Content,
            CreatedAt = job.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId(); // ✅ sirf apna data

        var jobs = await _db.JobDescriptions
            .Where(j => j.UserId == userId) // ✅ filter lagao
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobResponseDTO
            {
                Id = j.Id,
                Title = j.Title,
                Content = j.Content,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var job = await _db.JobDescriptions
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId); // ✅ ownership check

        if (job == null)
            return NotFound(new { error = "Job not found!" });

        return Ok(new JobResponseDTO
        {
            Id = job.Id,
            Title = job.Title,
            Content = job.Content,
            CreatedAt = job.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var job = await _db.JobDescriptions
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId); // ✅ ownership check

        if (job == null)
            return NotFound(new { error = "Job not found!" });

        _db.JobDescriptions.Remove(job);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Job deleted!" });
    }
}