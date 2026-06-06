namespace HireIQ.API.Models;

public class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public Guid? ResumeId { get; set; }
    public string? CoverLetter { get; set; }
    public string Status { get; set; } = "Applied"; // Applied | Screening | Interview | Hired | Rejected
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public JobDescription? Job { get; set; }
    public User? Applicant { get; set; }
}
