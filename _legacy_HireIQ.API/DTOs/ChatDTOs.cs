namespace HireIQ.API.DTOs;

public class ChatRequestDTO
{
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

public class ChatResponseDTO
{
    public string Response { get; set; } = string.Empty;
    public int HistoryLength { get; set; }
}