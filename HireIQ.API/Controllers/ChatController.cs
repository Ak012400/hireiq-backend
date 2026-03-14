using Microsoft.AspNetCore.Mvc;
using HireIQ.API.DTOs;
using HireIQ.API.Services;
using System.Security.Claims;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly MLService _mlService;

    public ChatController(MLService mlService)
    {
        _mlService = mlService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(ChatRequestDTO dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "anonymous";

        var response = await _mlService.Chat(dto.Message, userId);

        return Ok(new ChatResponseDTO
        {
            Response = response,
            HistoryLength = 0
        });
    }
}