namespace HireIQ.API.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;


    [Table("templates")] // Explicit snake_case mapping [cite: 59]
    public class Template
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = null!;

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("description")]
        public string? Description { get; set; }

        [Column("category")]
        public string? Category { get; set; }

        [Column("preview_image_url")]
        public string? PreviewImageUrl { get; set; }

        [Column("base_structure_json")]
        public string? BaseStructureJson { get; set; } // JSONB mapping

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_premium")]
        public bool IsPremium { get; set; } = false;
    }
}
