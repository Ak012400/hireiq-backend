// DTOs/TemplateEditorDTOs.cs
namespace HireIQ.API.DTOs
{
    public class SaveResumeDTO
    {
        public string TemplateId { get; set; } = "";
        public List<object> Sections { get; set; } = new();
    }

    public class AiFillDTO
    {
        public string ResumeText { get; set; } = "";
    }
}