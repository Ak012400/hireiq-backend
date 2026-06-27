using HireIQ.Application.DTOs;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/hr-interviews")]
[Authorize]
public class HrInterviewController : BaseController
{
    private readonly AppDbContext _db;
    private readonly IHiringPipelineService _pipeline;

    public HrInterviewController(AppDbContext db, IHiringPipelineService pipeline)
    {
        _db = db;
        _pipeline = pipeline;
    }

    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] HrInterviewCreateDTO dto, CancellationToken ct)
    {
        var hirerId = GetCurrentUserId();
        var hr = new HrInterview
        {
            CandidateJourneyId = dto.CandidateJourneyId,
            HirerId = hirerId,
            ScheduledAt = DateTime.SpecifyKind(dto.ScheduledAt, DateTimeKind.Utc),
            MeetLink = dto.MeetLink,
            Status = HrInterviewStatus.Scheduled
        };
        _db.HrInterviews.Add(hr);
        await _db.SaveChangesAsync(ct);

        // Auto-transition the journey
        await _pipeline.TransitionAsync(dto.CandidateJourneyId, PipelineStage.HrInterviewScheduled, hirerId.ToString(), "HR interview scheduled", ct);

        return Ok(hr);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] HrInterview update, CancellationToken ct)
    {
        var hr = await _db.HrInterviews.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (hr == null) return NotFound();
        hr.Status = HrInterviewStatus.Completed;
        hr.HirerNotes = update.HirerNotes;
        hr.Decision = update.Decision;
        hr.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _pipeline.TransitionAsync(hr.CandidateJourneyId, PipelineStage.HrInterviewCompleted, GetCurrentUserId().ToString(), "HR interview completed", ct);
        return Ok(hr);
    }
}
