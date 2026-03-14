namespace HireIQ.API.DTOs
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
        public decimal MinilmScore { get; set; }
        public string MatchLevel { get; set; } = string.Empty;
        public string? Analysis { get; set; }
        public bool Shortlisted { get; set; }
    }

    public class BatchScreeningDTO
    {
        public List<Guid> ResumeIds { get; set; } = new();
        public Guid JdId { get; set; }
    }
}