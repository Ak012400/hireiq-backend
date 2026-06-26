using HireIQ.Infrastructure.Persistence;
using HireIQ.Application.DTOs;
using HireIQ.Domain.Entities;
using HireIQ.Application.Interfaces;
using HireIQ.Infrastructure.Identity;
using HireIQ.Infrastructure.Email;
using HireIQ.Infrastructure.Ai;
using HireIQ.Infrastructure.Pdf;
using HireIQ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HireIQ.API.Controllers
{
    [ApiController]
    [Route("api/resume-builder")]
    [Authorize]
    [EnableRateLimiting("ai")]
    public class ResumeBuilderController : BaseController
    {
        private readonly PdfService _pdfService;
        private readonly AppDbContext _db;
        private readonly PdfExtractorService _extractor;
        private readonly GroqService _groq;

        public ResumeBuilderController(PdfService pdfService, AppDbContext db,
            PdfExtractorService extractor, GroqService groq)
        {
            _pdfService = pdfService;
            _db = db;
            _extractor = extractor;
            _groq = groq;
        }

        // ── AI Redesign: existing resume PDF → structured ResumeData ──────────
        // Frontend renders the result in ANY template — instant redesign.
        [HttpPost("ai-redesign")]
        public async Task<IActionResult> AiRedesign(IFormFile resumeFile)
        {
            if (resumeFile == null || resumeFile.Length == 0)
                return BadRequest(new { error = "PDF file required!" });
            if (resumeFile.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "PDF must be under 5 MB." });

            using var ms = new MemoryStream();
            await resumeFile.CopyToAsync(ms);
            var text = _extractor.ExtractText(ms.ToArray());

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "Could not extract text from this PDF (scanned image?)." });

            var prompt = $@"Extract and IMPROVE resume information from this text.
Rewrite weak bullet points with strong action verbs and quantifiable metrics where reasonable (do not invent numbers — only sharpen wording).

Resume Text:
{text}

Return JSON with exactly this structure:
{{
  ""name"": ""Full Name"",
  ""role"": ""Job Title/Role"",
  ""email"": """", ""phone"": """", ""linkedin"": """", ""github"": """",
  ""summary"": ""Sharp 2-3 sentence professional summary"",
  ""skills"": [""skill1"", ""skill2""],
  ""experience"": [{{""title"": """", ""company"": """", ""description"": ""improved bullet-style description""}}],
  ""education"": [{{""degree"": """", ""school"": ""University, Year, CGPA""}}],
  ""projects"": [{{""name"": """", ""description"": """"}}],
  ""extra"": ""certifications, languages, awards""
}}";

            var data = await _groq.GenerateJsonAsync<ResumeData>(prompt);
            if (data == null)
                return StatusCode(502, new { error = "AI parsing failed. Please try again." });

            return Ok(data);
        }

        // ── AI Generate: keywords → complete ResumeData draft ─────────────────
        [HttpPost("ai-generate")]
        public async Task<IActionResult> AiGenerate([FromBody] AiGenerateResumeDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Keywords))
                return BadRequest(new { error = "Keywords required! e.g. 'senior react developer, 3 years, fintech'" });

            var prompt = $@"Create a complete, realistic, ATS-friendly resume draft from these inputs:
Keywords: {dto.Keywords}
{(string.IsNullOrWhiteSpace(dto.TargetRole) ? "" : $"Target Role: {dto.TargetRole}")}
{(string.IsNullOrWhiteSpace(dto.Name) ? "" : $"Candidate Name: {dto.Name}")}

Rules:
- Use placeholder name '{dto.Name ?? "Your Name"}' and generic contact placeholders.
- Experience descriptions: strong action verbs, realistic achievements with metrics, 2-3 lines each.
- 8-14 relevant skills. 2-3 experience entries. 2-3 projects relevant to the keywords.
- Do NOT use clichés like 'team player' or 'hard-working'.

Return JSON with exactly this structure:
{{
  ""name"": """", ""role"": """", ""email"": ""you@email.com"", ""phone"": ""+91-XXXXX-XXXXX"",
  ""linkedin"": """", ""github"": """",
  ""summary"": """",
  ""skills"": [],
  ""experience"": [{{""title"": """", ""company"": """", ""description"": """"}}],
  ""education"": [{{""degree"": """", ""school"": """"}}],
  ""projects"": [{{""name"": """", ""description"": """"}}],
  ""extra"": """"
}}";

            var data = await _groq.GenerateJsonAsync<ResumeData>(prompt);
            if (data == null)
                return StatusCode(502, new { error = "AI generation failed. Please try again." });

            return Ok(data);
        }

        // ── AI Coach: conversational resume improvement with applicable updates ──
        [HttpPost("coach")]
        public async Task<IActionResult> Coach([FromBody] CoachRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { error = "Message required." });

            var resumeJson = dto.Resume == null
                ? "(empty — user hasn't added resume data yet)"
                : System.Text.Json.JsonSerializer.Serialize(dto.Resume, new System.Text.Json.JsonSerializerOptions
                  {
                      PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                  });

            var history = string.Join("\n", dto.History.TakeLast(6).Select(h => $"{h.Role}: {h.Content}"));

            var prompt = $@"You are HireIQ Resume Coach — a friendly expert resume advisor chatting with a candidate while their resume is visible next to the chat.

Current resume (JSON):
{resumeJson}

Recent conversation:
{history}

User's message: {dto.Message}

Respond conversationally AND propose concrete resume updates the user can apply with one click.
Rules:
- reply: 2-4 sentences, helpful and specific. Mention WHY each change helps.
- updates: 0-3 items. Only propose updates when relevant. Each update FULLY REPLACES that section's text.
- section must be one of: summary, role, skills, experience, projects, education, extra.
- For skills: content is a comma-separated list (complete list, not just additions).
- For experience/projects/education: content is the complete improved text for that section (one entry per line).
- Use strong action verbs. Never invent employers, degrees, or numbers the user hasn't mentioned.

Return JSON with exactly this structure:
{{
  ""reply"": ""conversational answer"",
  ""updates"": [
    {{""section"": ""summary"", ""title"": ""short label"", ""content"": ""new text""}}
  ]
}}";

            var result = await _groq.GenerateJsonAsync<CoachResponse>(prompt);
            if (result == null)
                return StatusCode(502, new { error = "Coach is unavailable right now. Try again." });

            // Defensive: drop updates with unknown sections
            var valid = new[] { "summary", "role", "skills", "experience", "projects", "education", "extra" };
            result.Updates = result.Updates
                .Where(u => valid.Contains(u.Section?.ToLowerInvariant()))
                .Take(3)
                .ToList();

            return Ok(result);
        }

        [HttpPost("generate-pdf")]
        public async Task<IActionResult> GeneratePdf([FromBody] GeneratePdfDTO dto)
        {
            var userId = GetCurrentUserId();

            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(dto.HtmlContent);

            // DB mein save karo (generated_resumes table)
            var generated = new GeneratedResume
            {
                UserId = userId,
                HtmlContent = dto.HtmlContent,
                // pdf_url baad mein cloud storage se aayega
            };
            _db.GeneratedResumes.Add(generated);
            await _db.SaveChangesAsync();

            // PDF as file return karo
            return File(pdfBytes, "application/pdf", $"resume_{userId}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }
}
