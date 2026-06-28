using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Email;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

public class ScheduleAiInterviewDTO
{
    public Guid CandidateJourneyId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public List<string>? PresetQuestions { get; set; }
}

[ApiController]
[Route("api/ai-interview/schedule")]
[Authorize]
public class AiInterviewSchedulingController : BaseController
{
    private readonly AppDbContext _db;
    private readonly IHiringPipelineService _pipeline;
    private readonly SmtpEmailService _email;
    private readonly IConfiguration _config;

    public AiInterviewSchedulingController(
        AppDbContext db, IHiringPipelineService pipeline,
        SmtpEmailService email, IConfiguration config)
    {
        _db = db; _pipeline = pipeline; _email = email; _config = config;
    }

    /// <summary>
    /// Hirer schedules an AI interview for a shortlisted candidate.
    /// Creates InterviewRoom, transitions journey to AiInterviewInvited (auto-fires template email),
    /// also sends rich interview-invite via SendInterviewInviteAsync.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] ScheduleAiInterviewDTO dto, CancellationToken ct)
    {
        var hirerId = GetCurrentUserId();
        var journey = await _db.CandidateJourneys
            .Include(j => j.JobPosting)
            .Include(j => j.Applicant)
            .FirstOrDefaultAsync(j => j.Id == dto.CandidateJourneyId, ct);
        if (journey == null) return NotFound();
        if (journey.Applicant == null || journey.JobPosting == null) return BadRequest(new { error = "Journey missing applicant or job" });

        // Find legacy job_descriptions row (interview_rooms FK targets it, not job_postings)
        var legacyJd = await _db.JobDescriptions
            .FirstOrDefaultAsync(j => j.UserId == journey.JobPosting.HirerId && j.Title == journey.JobPosting.Title, ct);

        // Generate unique room code + PIN
        string roomCode;
        do { roomCode = GenerateRoomCode(); }
        while (await _db.InterviewRooms.AnyAsync(r => r.RoomCode == roomCode, ct));
        var pin = new Random().Next(100000, 999999).ToString();

        var room = new InterviewRoom
        {
            RoomCode = roomCode,
            RoomPassword = pin,
            HirerId = hirerId,
            JobId = legacyJd?.Id,
            CandidateEmail = journey.Applicant.Email,
            CandidateName = journey.Applicant.Name,
            CandidateUserId = journey.ApplicantUserId,
            Status = "Scheduled",
            ScheduledAt = DateTime.SpecifyKind(dto.ScheduledAtUtc, DateTimeKind.Utc),
            PresetQuestions = dto.PresetQuestions ?? new List<string>(),
        };
        _db.InterviewRooms.Add(room);
        await _db.SaveChangesAsync(ct);

        // Advance journey (fires AI_INVITE template email)
        await _pipeline.TransitionAsync(journey.Id, PipelineStage.AiInterviewInvited, hirerId.ToString(), "AI interview scheduled", ct);

        // Rich invite email with join link
        try
        {
            var frontendUrl = (_config["Cors:AllowedOrigins"] ?? "http://localhost:3000")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .First().TrimEnd('/');
            await _email.SendInterviewInviteAsync(
                room.CandidateEmail, room.CandidateName ?? "", journey.JobPosting.Title,
                room.ScheduledAt, room.RoomCode, room.RoomPassword,
                $"{frontendUrl}/ai-interview/{room.Id}/{journey.Id}");
        }
        catch { /* logged inside SmtpEmailService */ }

        return Ok(new { roomId = room.Id, roomCode = room.RoomCode, pin = room.RoomPassword, scheduledAt = room.ScheduledAt });
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rand = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[rand.Next(chars.Length)]).ToArray());
    }
}
