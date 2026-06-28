using System.Text.Json;
using HireIQ.Application.Interfaces;
using HireIQ.Infrastructure.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireIQ.API.Controllers;

public class GenerateFieldRequest
{
    public string Field { get; set; } = string.Empty;   // title|description|requirements|benefits|skillsRequired|...
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? ExtraContext { get; set; }
}

public class GenerateAllRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Company { get; set; }
}

public class JobPostingDraft
{
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string? WorkMode { get; set; }
    public int? ExperienceMinYears { get; set; }
    public int? ExperienceMaxYears { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public List<string>? Requirements { get; set; }
    public List<string>? Benefits { get; set; }
    public List<string>? SkillsRequired { get; set; }
    public List<string>? SkillsNiceToHave { get; set; }
}

[ApiController]
[Route("api/job-postings/ai")]
[Authorize]
public class JobPostingAiController : BaseController
{
    private readonly GroqService _groq;
    private readonly IDocumentParserService _parser;

    public JobPostingAiController(GroqService groq, IDocumentParserService parser)
    {
        _groq = groq;
        _parser = parser;
    }

    /// <summary>
    /// Hirer uploads any JD doc (PDF/DOCX/XLSX/TXT) → returns structured JobPostingDraft.
    /// </summary>
    [HttpPost("parse-document")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> ParseDocument(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File missing" });

        using var stream = file.OpenReadStream();
        var text = await _parser.ExtractTextAsync(stream, file.FileName, file.ContentType, ct);
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { error = "Could not extract text from this file." });

        // Truncate enormous docs — Groq is fast but tokens cost time
        var clipped = text.Length > 12000 ? text[..12000] : text;

        var prompt = $@"You will receive a raw job description document text. Extract it into the following STRICT JSON schema. Use null for fields you cannot infer. Skills must be a JSON array of strings.

Schema:
{{
  ""title"": ""..."",
  ""company"": ""..."",
  ""location"": ""..."",
  ""employmentType"": ""FullTime|PartTime|Contract|Internship|Freelance"",
  ""workMode"": ""Onsite|Remote|Hybrid"",
  ""experienceMinYears"": 0,
  ""experienceMaxYears"": 0,
  ""salaryMin"": 0,
  ""salaryMax"": 0,
  ""currency"": ""INR|USD|EUR..."",
  ""description"": ""one-paragraph summary of the role"",
  ""requirements"": [""..."", ""...""],
  ""benefits"": [""..."", ""...""],
  ""skillsRequired"": [""..."", ""...""],
  ""skillsNiceToHave"": [""..."", ""...""]
}}

Document text:
---
{clipped}
---";

        var draft = await _groq.GenerateJsonAsync<JobPostingDraft>(prompt);
        if (draft == null)
            return BadRequest(new { error = "AI could not structure this document." });

        return Ok(draft);
    }

    /// <summary>
    /// Generate / rewrite a single field. AI uses other filled fields as context
    /// so the suggestion stays consistent with the rest of the form.
    /// </summary>
    [HttpPost("generate-field")]
    public async Task<IActionResult> GenerateField([FromBody] GenerateFieldRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Field))
            return BadRequest(new { error = "Field name required" });

        var ctx = $"Title: {req.Title ?? "(unspecified)"}\nCompany: {req.Company ?? "(unspecified)"}\nLocation: {req.Location ?? "(unspecified)"}\nDescription: {req.Description ?? "(unspecified)"}\nExtra: {req.ExtraContext ?? ""}";

        var instruction = req.Field.ToLowerInvariant() switch
        {
            "title"              => "Generate ONE concise job title (max 8 words). Return only the title, no quotes.",
            "description"        => "Write a 3-5 sentence professional job description summarizing role, mission, and impact. No bullets.",
            "requirements"       => "List 5-8 must-have requirements as a JSON array of strings. Return ONLY the JSON array.",
            "benefits"           => "List 5-8 typical benefits for this role as a JSON array of strings. Return ONLY the JSON array.",
            "skillsrequired"     => "List 5-10 core required hard skills as a JSON array of short strings. Return ONLY the JSON array.",
            "skillsnicetohave"   => "List 5-8 nice-to-have skills as a JSON array of short strings. Return ONLY the JSON array.",
            _ => $"Generate content for field '{req.Field}' based on the context above. Be concise."
        };

        var prompt = $"{ctx}\n\nTASK: {instruction}";
        var result = await _groq.GenerateFieldAsync(prompt);
        return Ok(new { field = req.Field, value = result?.Trim() ?? "" });
    }

    /// <summary>
    /// Magic button: from just a title (and optional description), generate
    /// the entire JobPostingDraft. Hirer types "Senior Backend Engineer" → form fills.
    /// </summary>
    [HttpPost("generate-all")]
    public async Task<IActionResult> GenerateAll([FromBody] GenerateAllRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Title is required to generate the rest" });

        var prompt = $@"You are an expert recruiter writing a job posting. Generate a complete posting for the role '{req.Title}'{(string.IsNullOrWhiteSpace(req.Company) ? "" : $" at {req.Company}")}.
{(string.IsNullOrWhiteSpace(req.Description) ? "" : $"Context provided by the hirer: {req.Description}")}

Return STRICT JSON matching this schema (no markdown):
{{
  ""title"": ""..."",
  ""employmentType"": ""FullTime|PartTime|Contract|Internship|Freelance"",
  ""workMode"": ""Onsite|Remote|Hybrid"",
  ""experienceMinYears"": <int>,
  ""experienceMaxYears"": <int>,
  ""salaryMin"": <int in INR yearly>,
  ""salaryMax"": <int in INR yearly>,
  ""currency"": ""INR"",
  ""description"": ""3-5 sentence engaging description"",
  ""requirements"": [""5-8 must-haves""],
  ""benefits"": [""5-8 typical benefits""],
  ""skillsRequired"": [""5-10 hard skills""],
  ""skillsNiceToHave"": [""5-8 nice-to-haves""]
}}";

        var draft = await _groq.GenerateJsonAsync<JobPostingDraft>(prompt);
        if (draft == null)
            return BadRequest(new { error = "AI failed to generate posting" });
        return Ok(draft);
    }
}
