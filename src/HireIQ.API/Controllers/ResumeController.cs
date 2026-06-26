using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireIQ.Infrastructure.Persistence;
using HireIQ.Application.DTOs;
using HireIQ.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumeController : BaseController
{
    private readonly AppDbContext _db;

    public ResumeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
public async Task<IActionResult> Create(CreateResumeDTO dto)
{
        var user = GetCurrentUserId();
    if (string.IsNullOrEmpty(user.ToString()))
        return BadRequest(new { error = "No user found!" });

    var resume = new Resume
    {
        CandidateName = dto.CandidateName,
        Content = dto.Content,
        UserId = user
    };

    _db.Resumes.Add(resume);
    await _db.SaveChangesAsync();

    return Ok(new ResumeResponseDTO
    {
        Id = resume.Id,
        CandidateName = resume.CandidateName,
        Content = resume.Content,
        CreatedAt = resume.CreatedAt
    });
}

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var resumes = await _db.Resumes.Where(r=> r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ResumeResponseDTO
            {
                Id = r.Id,
                CandidateName = r.CandidateName,
                Content = r.Content,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(resumes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var resume = await _db.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (resume == null)
            return NotFound(new { error = "Resume not found!" });

        return Ok(new ResumeResponseDTO
        {
            Id = resume.Id,
            CandidateName = resume.CandidateName,
            Content = resume.Content,
            CreatedAt = resume.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var resume = await _db.Resumes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (resume == null)
            return NotFound(new { error = "Resume not found!" });

        _db.Resumes.Remove(resume);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Resume deleted!" });
    }
}