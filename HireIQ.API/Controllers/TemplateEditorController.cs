// TemplateEditorController.cs — complete
using HireIQ.API.Data;
using HireIQ.API.Services;
using HireIQ.API.Models;
using HireIQ.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace HireIQ.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/template-editor")]
    public class TemplateEditorController : BaseController
    {
        private readonly MongoDbService _mongoService;
        private readonly AppDbContext _context;
        private readonly GroqService _groqService;

        public TemplateEditorController(
            MongoDbService mongoService,
            AppDbContext context,
            GroqService groqService)
        {
            _mongoService = mongoService;
            _context = context;
            _groqService = groqService;
        }

        // GET all templates list
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            var templates = await _context.Templates
                .Select(t => new {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.Category,
                    t.PreviewImageUrl,
                    t.IsPremium
                })
                .ToListAsync();
            return Ok(templates);
        }

        // GET editor state — template config + user data
        [HttpGet("init/{templateId}")]
        public async Task<IActionResult> GetEditorState(string templateId)
        {
            var userId = GetCurrentUserId();

            var template = await _context.Templates.FindAsync(templateId);
            if (template == null) return NotFound("Template not found");

            // ✅ Fixed method name
            var userData = await _mongoService.GetResumeByUserIdAsync(userId);

            return Ok(new
            {
                Config = template,
                ResumeData = userData != null
                    ? MongoDB.Bson.Serialization.BsonSerializer
                        .Deserialize<object>(userData)
                    : null
            });
        }

        // POST save user resume data
        [HttpPost("save")]
        public async Task<IActionResult> SaveResumeData([FromBody] SaveResumeDTO dto)
        {
            var userId = GetCurrentUserId();
            var doc = new BsonDocument {
                { "userId", new BsonBinaryData(userId, MongoDB.Bson.GuidRepresentation.Standard) },
                { "templateId", dto.TemplateId },
                { "sections", BsonDocument.Parse(
                    System.Text.Json.JsonSerializer.Serialize(dto.Sections)) }
            };
            await _mongoService.SaveResumeAsync(doc);
            return Ok(new { message = "Saved!" });
        }

        // POST AI fill — resume text se sections generate karo
        [HttpPost("ai-fill")]
        public async Task<IActionResult> AiFill([FromBody] AiFillDTO dto)
        {
            var prompt = $@"Extract resume data from this text and return ONLY valid JSON.
Resume Text: {dto.ResumeText}

Return this exact structure:
{{
  ""name"": ""Full Name"",
  ""role"": ""Job Title"",
  ""email"": ""email"",
  ""phone"": ""phone"",
  ""summary"": ""Professional summary"",
  ""skills"": [""Skill1"", ""Skill2""],
  ""experience"": [{{""title"": """", ""company"": """", ""description"": """"}}],
  ""education"": [{{""degree"": """", ""school"": """"}}],
  ""projects"": [{{""name"": """", ""description"": """"}}]
}}
NO markdown, NO explanation, ONLY JSON.";

            var result = await _groqService.GenerateFieldAsync(prompt);
            var clean = result.Replace("```json", "").Replace("```", "").Trim();
            return Ok(new { data = clean });
        }
    }
}