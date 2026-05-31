using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HireIQ.API.Models
{
    public class UserResumeData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? InternalId { get; set; }

        [BsonElement("userId")]
        public Guid UserId { get; set; } // 

        [BsonElement("templateId")]
        public string TemplateId { get; set; } = "modern-dark";

        // Flexible sections for the "Special" editor
        [BsonElement("sections")]
        public List<ResumeSection> Sections { get; set; } = new();
    }
}
