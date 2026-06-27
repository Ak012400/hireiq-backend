using HireIQ.Infrastructure.JobBoards;
using Microsoft.AspNetCore.Mvc;

namespace HireIQ.API.Controllers;

/// <summary>
/// Public (no auth) endpoints for job-board crawlers.
/// </summary>
[ApiController]
[Route("feeds")]
public class PublicFeedsController : ControllerBase
{
    private readonly IndeedFeedConnector _indeed;
    public PublicFeedsController(IndeedFeedConnector indeed) => _indeed = indeed;

    [HttpGet("indeed.xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> Indeed(CancellationToken ct)
    {
        var xml = await _indeed.BuildFullFeedXmlAsync(ct);
        return Content(xml, "application/xml");
    }
}
