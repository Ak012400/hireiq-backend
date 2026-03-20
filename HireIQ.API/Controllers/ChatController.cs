using Microsoft.AspNetCore.Mvc;
using HireIQ.API.DTOs;
using HireIQ.API.Services;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly GroqService _groqService;

    // In-memory conversation store
    private static readonly Dictionary<string, List<object>> _conversations = new();

    public ChatController(GroqService groqService)
    {
        _groqService = groqService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(ChatRequestDTO dto)
    {
        try
        {
            var userId = dto.UserId ?? "anonymous";

            if (!_conversations.ContainsKey(userId))
                _conversations[userId] = new List<object>();

            var history = _conversations[userId];
            var response = await _groqService.GenerateAsync(dto.Message, history);

            // History update karo
            history.Add(new { role = "user", content = dto.Message });
            history.Add(new { role = "assistant", content = response });

            // Last 10 rakho
            if (history.Count > 10)
                _conversations[userId] = history.TakeLast(10).ToList();

            return Ok(new ChatResponseDTO
            {
                Response = response,
                HistoryLength = _conversations[userId].Count
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { error = e.Message });
        }
    }

    [HttpDelete("clear")]
    public IActionResult Clear([FromQuery] string userId = "anonymous")
    {
        if (_conversations.ContainsKey(userId))
            _conversations[userId] = new();
        return Ok(new { message = "History cleared!" });
    }
}