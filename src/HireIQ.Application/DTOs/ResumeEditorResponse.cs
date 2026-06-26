using HireIQ.Domain.Entities;

namespace HireIQ.Application.DTOs
{
    public class ResumeEditorResponse
    {
        public Template TemplateMetadata { get; set; } // From PostgreSQL
        public ResumeTemplateData UserData { get; set; } // From MongoDB
    }
}
