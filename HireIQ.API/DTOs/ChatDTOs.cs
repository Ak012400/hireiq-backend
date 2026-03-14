namespace HireIQ.API.DTOs
{

    public class ChatRequestDTO
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseDTO
    {
        public string Response { get; set; } = string.Empty;
        public int HistoryLength { get; set; }
    }
}
// ```

// ---

// **Confirm karo:**
// ```
// ✅ 5 DTO files bane