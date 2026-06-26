using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HireIQ.Application.DTOs;
using HireIQ.Application.Interfaces;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")] // brute-force protection
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO dto, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(dto, ct);
        if (result == null)
            return BadRequest(new { error = "Email already exists!" });
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        if (result == null)
            return Unauthorized(new { error = "Invalid credentials!" });
        return Ok(result);
    }
}
