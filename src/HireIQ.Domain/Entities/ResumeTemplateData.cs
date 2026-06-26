using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HireIQ.Domain.Entities
{
    public class ResumeTemplateData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        public Guid UserId { get; set; } // PostgreSQL user ID se link karne ke liye

        public string SelectedTemplate { get; set; } = "modern-dark";

        // User ki custom styling (Colors, Fonts)
        public Dictionary<string, string> ThemeConfig { get; set; } = new();

        // AI generated content aur fields
        public List<ResumeSection> Sections { get; set; } = new();
    }

    public class ResumeSection
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsAiGenerated { get; set; }
    }
}
