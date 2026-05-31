using HireIQ.API.Models;

namespace HireIQ.API.DTOs
{
    public class ResumeEditorResponse
    {
        public Template TemplateMetadata { get; set; } // From PostgreSQL
        public ResumeTemplateData UserData { get; set; } // From MongoDB
    }
}
