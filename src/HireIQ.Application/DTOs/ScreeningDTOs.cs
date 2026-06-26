namespace HireIQ.Application.DTOs
{

    public class ScreeningRequestDTO
    {
        public Guid ResumeId { get; set; }
        public Guid JdId { get; set; }
        public bool DeepAnalyze { get; set; } = false;
    }

    public class ScreeningResponseDTO
    {
        public Guid Id { get; set; }
        public Guid ResumeId { get; set; }
        public Guid JdId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public decimal MinilmScore { get; set; }
        public string MatchLevel { get; set; } = string.Empty;
        public string? Analysis { get; set; }
        public bool Shortlisted { get; set; }
        public string CandidateStatus { get; set; } = "Screened"; // Screened | Interview | Hired | Rejected
        public DateTime CreatedAt { get; set; }
    }

    public class BatchScreeningDTO
    {
        public List<Guid> ResumeIds { get; set; } = new();
        public Guid JdId { get; set; }
    }

    public class UpdateCandidateStatusDTO
    {
        public string Status { get; set; } = string.Empty; // Screened | Interview | Hired | Rejected
    }
}