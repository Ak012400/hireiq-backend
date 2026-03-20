using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HireIQ.API.DTOs;
using HireIQ.API.Services;

namespace HireIQ.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize] // ✅ add karo
public class ChatController : BaseController // ✅ BaseController
{
    private readonly GroqService _groqService;
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
            var userId = GetCurrentUserId().ToString(); // ✅ JWT se lo, dto se nahi

            if (!_conversations.ContainsKey(userId))
                _conversations[userId] = new List<object>();

            var history = _conversations[userId];
            var response = await _groqService.GenerateAsync(dto.Message, history);

            history.Add(new { role = "user", content = dto.Message });
            history.Add(new { role = "assistant", content = response });

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
    public IActionResult Clear()
    {
        var userId = GetCurrentUserId().ToString(); // ✅ query param nahi, JWT se
        if (_conversations.ContainsKey(userId))
            _conversations[userId] = new();
        return Ok(new { message = "History cleared!" });
    }
    // ChatController.cs me add karo
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        var prompt = "Generate 4 short suggestion chips for an HR AI assistant. Topics: resume tips, career guidance, salary, skills. Return ONLY a JSON array of 4 strings, nothing else. Example: [\"Tip 1\", \"Tip 2\", \"Tip 3\", \"Tip 4\"]";

        var response = await _groqService.GenerateAsync(prompt);

        // JSON parse karo
        var match = System.Text.RegularExpressions.Regex.Match(response, @"\[.*?\]",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

        if (match.Success)
        {
            var suggestions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(match.Value);
            return Ok(new { suggestions });
        }

        // Fallback
        return Ok(new
        {
            suggestions = new List<string> {
        "What skills should a Python developer add?",
        "How to transition to Data Science?",
        "What salary for fresher ML Engineer?",
        "How to write a strong resume summary?"
    }
        });
    }
}