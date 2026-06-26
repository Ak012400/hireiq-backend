// Controllers/BaseController.cs
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HireIQ.API.Controllers;

[ApiController]
public class BaseController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new UnauthorizedAccessException("User not authenticated");
        return Guid.Parse(claim);
    }

    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}