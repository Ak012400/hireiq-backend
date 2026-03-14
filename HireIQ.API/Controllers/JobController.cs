using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;
using System.Security.Claims;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobController(AppDbContext db)
    {
        _db = db;
    }

  [HttpPost]
public async Task<IActionResult> Create(CreateJobDTO dto)
{
    // Pehla user lo DB se (temporary fix)
    var user = await _db.Users.FirstOrDefaultAsync();
    if (user == null)
        return BadRequest(new { error = "No user found!" });

    var job = new JobDescription
    {
        Title = dto.Title,
        Content = dto.Content,
        UserId = user.Id  // ← Pehla user use karo
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
        var jobs = await _db.JobDescriptions
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
        var job = await _db.JobDescriptions.FindAsync(id);
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
        var job = await _db.JobDescriptions.FindAsync(id);
        if (job == null)
            return NotFound(new { error = "Job not found!" });

        _db.JobDescriptions.Remove(job);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Job deleted!" });
    }
}