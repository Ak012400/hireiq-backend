using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;
using System.Security.Claims;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/resumes")]
public class ResumeController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResumeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
public async Task<IActionResult> Create(CreateResumeDTO dto)
{
    // Pehla user lo DB se (temporary fix)
    var user = await _db.Users.FirstOrDefaultAsync();
    if (user == null)
        return BadRequest(new { error = "No user found!" });

    var resume = new Resume
    {
        CandidateName = dto.CandidateName,
        Content = dto.Content,
        UserId = user.Id  // ← Pehla user use karo
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
        var resumes = await _db.Resumes
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
        var resume = await _db.Resumes.FindAsync(id);
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
        var resume = await _db.Resumes.FindAsync(id);
        if (resume == null)
            return NotFound(new { error = "Resume not found!" });

        _db.Resumes.Remove(resume);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Resume deleted!" });
    }
}