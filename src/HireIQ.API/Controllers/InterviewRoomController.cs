using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HireIQ.Infrastructure.Persistence;
using HireIQ.Application.DTOs;
using HireIQ.Domain.Entities;
using HireIQ.Application.Interfaces;
using HireIQ.Infrastructure.Identity;
using HireIQ.Infrastructure.Email;
using HireIQ.Infrastructure.Ai;
using HireIQ.Infrastructure.Pdf;
using HireIQ.Infrastructure.Persistence;
using System.Text.Json;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/interview-rooms")]
[Authorize]
public class InterviewRoomController : BaseController
{
    private readonly AppDbContext _db;
    private readonly SmtpEmailService _email;
    private readonly IConfiguration _config;

    public InterviewRoomController(AppDbContext db, SmtpEmailService email, IConfiguration config)
    {
        _db = db;
        _email = email;
        _config = config;
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rand = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[rand.Next(chars.Length)]).ToArray());
    }

    private static string GeneratePin()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRooms()
    {
        var userId = GetCurrentUserId();
        var rooms = await _db.InterviewRooms
            .Include(r => r.Job)
            .Where(r => r.HirerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new InterviewRoomResponseDTO
            {
                Id = r.Id,
                RoomCode = r.RoomCode,
                RoomPassword = r.RoomPassword,
                CandidateEmail = r.CandidateEmail,
                CandidateName = r.CandidateName,
                JobTitle = r.Job != null ? r.Job.Title : null,
                JobId = r.JobId,
                Status = r.Status,
                ScheduledAt = r.ScheduledAt,
                PresetQuestions = r.PresetQuestions,
                FinalDecision = r.FinalDecision,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateInterviewRoomDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CandidateEmail))
            return BadRequest(new { error = "Candidate email is required." });

        // Ensure unique room code
        string code;
        do { code = GenerateRoomCode(); }
        while (await _db.InterviewRooms.AnyAsync(r => r.RoomCode == code));

        var room = new InterviewRoom
        {
            RoomCode     = code,
            RoomPassword = GeneratePin(),
            HirerId      = GetCurrentUserId(),
            JobId        = dto.JobId,
            CandidateEmail  = dto.CandidateEmail,
            CandidateName   = dto.CandidateName,
            // ✅ Npgsql timestamptz needs UTC — convert whatever the frontend sends
            ScheduledAt     = dto.ScheduledAt.HasValue
                ? DateTime.SpecifyKind(dto.ScheduledAt.Value, DateTimeKind.Utc)
                : null,
            PresetQuestions = dto.PresetQuestions,
        };

        // Check if candidate is already a user
        var candidateUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.CandidateEmail);
        if (candidateUser != null)
            room.CandidateUserId = candidateUser.Id;

        _db.InterviewRooms.Add(room);
        await _db.SaveChangesAsync();

        // Load job title for response
        JobDescription? job = null;
        if (room.JobId.HasValue)
            job = await _db.JobDescriptions.FindAsync(room.JobId.Value);

        // ✅ Send interview invitation email — never fail room creation if email fails
        bool emailSent = false;
        try
        {
            var frontendUrl = (_config["Cors:AllowedOrigins"] ?? "http://localhost:3000")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .First().TrimEnd('/');
            emailSent = await _email.SendInterviewInviteAsync(
                room.CandidateEmail, room.CandidateName ?? "", job?.Title ?? "",
                room.ScheduledAt, room.RoomCode, room.RoomPassword,
                $"{frontendUrl}/interview-rooms?code={room.RoomCode}");
        }
        catch { /* logged inside SmtpEmailService */ }

        return Ok(new InterviewRoomResponseDTO
        {
            Id = room.Id,
            RoomCode = room.RoomCode,
            RoomPassword = room.RoomPassword,
            CandidateEmail = room.CandidateEmail,
            CandidateName = room.CandidateName,
            JobTitle = job?.Title,
            JobId = room.JobId,
            Status = room.Status,
            ScheduledAt = room.ScheduledAt,
            PresetQuestions = room.PresetQuestions,
            FinalDecision = room.FinalDecision,
            CreatedAt = room.CreatedAt,
            EmailSent = emailSent,
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var userId = GetCurrentUserId();
        var room = await _db.InterviewRooms.FirstOrDefaultAsync(r => r.Id == id && r.HirerId == userId);
        if (room == null) return NotFound(new { error = "Room not found." });

        _db.InterviewRooms.Remove(room);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Room deleted." });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCandidateStatusDTO dto)
    {
        var userId = GetCurrentUserId();
        var room = await _db.InterviewRooms.FirstOrDefaultAsync(r => r.Id == id && r.HirerId == userId);
        if (room == null) return NotFound();
        room.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok(new { id, status = dto.Status });
    }
}
