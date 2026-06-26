namespace HireIQ.Application.DTOs
{

    public class CreateResumeDTO
    {
        public string CandidateName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ResumeResponseDTO
    {
        public Guid Id { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}