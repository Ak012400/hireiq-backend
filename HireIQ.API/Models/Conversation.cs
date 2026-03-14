namespace HireIQ.API.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}
// ```

// ---

// **Confirm karo:**
// ```
// ✅ Models folder bana
// ✅ 5 model files bane