using HireIQ.Application.DTOs;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/hiring-decisions")]
[Authorize]
public class HiringDecisionController : BaseController
{
    private readonly AppDbContext _db;
    private readonly IHiringPipelineService _pipeline;

    public HiringDecisionController(AppDbContext db, IHiringPipelineService pipeline)
    {
        _db = db;
        _pipeline = pipeline;
    }

    /// <summary>
    /// Final hirer consent — Hire / Reject / Hold. Triggers the right email automatically
    /// via the pipeline state machine.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Decide([FromBody] HiringDecisionDTO dto, CancellationToken ct)
    {
        if (!Enum.TryParse<HiringDecisionType>(dto.Decision, ignoreCase: true, out var decision))
            return BadRequest(new { error = $"Unknown decision '{dto.Decision}'" });

        var hirerId = GetCurrentUserId();
        var existing = await _db.HiringDecisions.FirstOrDefaultAsync(x => x.CandidateJourneyId == dto.CandidateJourneyId, ct);
        if (existing != null) return Conflict(new { error = "A decision already exists for this journey." });

        var hd = new HiringDecision
        {
            CandidateJourneyId = dto.CandidateJourneyId,
            Decision = decision,
            DecidedBy = hirerId,
            OfferedSalary = dto.OfferedSalary,
            OfferedCurrency = dto.OfferedCurrency,
            JoiningDate = dto.JoiningDate
        };
        _db.HiringDecisions.Add(hd);
        await _db.SaveChangesAsync(ct);

        // Transition + auto-email
        var nextStage = decision switch
        {
            HiringDecisionType.Hire   => PipelineStage.OfferExtended,
            HiringDecisionType.Reject => PipelineStage.RejectedByHr,
            _                         => PipelineStage.HrInterviewCompleted  // hold → stay
        };
        await _pipeline.TransitionAsync(dto.CandidateJourneyId, nextStage, hirerId.ToString(), $"Hirer decision: {decision}", ct);

        return Ok(hd);
    }

    [HttpPost("{decisionId:guid}/accept")]
    public async Task<IActionResult> CandidateAccepts(Guid decisionId, CancellationToken ct)
    {
        var hd = await _db.HiringDecisions.FirstOrDefaultAsync(x => x.Id == decisionId, ct);
        if (hd == null) return NotFound();
        hd.CandidateResponse = CandidateResponseType.Accepted;
        hd.CandidateRespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _pipeline.TransitionAsync(hd.CandidateJourneyId, PipelineStage.Hired, "candidate", "Offer accepted", ct);
        return Ok(hd);
    }
}
