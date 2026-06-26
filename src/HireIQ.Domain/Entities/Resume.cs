namespace HireIQ.Domain.Entities;

public class Resume
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}