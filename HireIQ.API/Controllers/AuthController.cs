using Microsoft.AspNetCore.Mvc;
using HireIQ.API.DTOs;
using HireIQ.API.Services;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO dto)
    {
        var result = await _authService.Register(dto);
        if (result == null)
            return BadRequest(new { error = "Email already exists!" });
        return Ok(result);
    }
    //Have to add the [Authorize] attribute to this endpoint to ensure that only authenticated users can access it.
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        var result = await _authService.Login(dto);
        if (result == null)
            return Unauthorized(new { error = "Invalid credentials!" });
        return Ok(result);
    }
}