using HireIQ.Application.DTOs;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/pipeline")]
[Authorize]
public class CandidatePipelineController : BaseController
{
    private readonly IHiringPipelineService _pipeline;

    public CandidatePipelineController(IHiringPipelineService pipeline) => _pipeline = pipeline;

    [HttpGet("by-job/{jobPostingId:guid}")]
    public async Task<IActionResult> ByJob(Guid jobPostingId, CancellationToken ct)
    {
        var journeys = await _pipeline.GetJourneysForJobAsync(jobPostingId, ct);
        var resp = journeys.Select(j => new CandidateJourneyResponseDTO
        {
            Id = j.Id,
            JobPostingId = j.JobPostingId,
            ApplicantUserId = j.ApplicantUserId,
            ApplicantName = j.Applicant?.Name ?? "",
            ApplicantEmail = j.Applicant?.Email ?? "",
            CurrentStage = j.CurrentStage.ToString(),
            LastTransitionAt = j.LastTransitionAt,
            CreatedAt = j.CreatedAt
        });
        return Ok(resp);
    }

    [HttpGet("{journeyId:guid}")]
    public async Task<IActionResult> Get(Guid journeyId, CancellationToken ct)
    {
        var j = await _pipeline.GetJourneyAsync(journeyId, ct);
        if (j == null) return NotFound();
        return Ok(j);
    }

    [HttpPost("{journeyId:guid}/transition")]
    public async Task<IActionResult> Transition(Guid journeyId, [FromBody] TransitionRequestDTO dto, CancellationToken ct)
    {
        if (!Enum.TryParse<PipelineStage>(dto.ToStage, out var to))
            return BadRequest(new { error = $"Unknown stage '{dto.ToStage}'" });

        try
        {
            var by = GetCurrentUserId().ToString();
            var j = await _pipeline.TransitionAsync(journeyId, to, by, dto.Reason, ct);
            return Ok(new { stage = j.CurrentStage.ToString(), lastTransitionAt = j.LastTransitionAt });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
