using HireIQ.API.Data;
using HireIQ.API.DTOs;
using HireIQ.API.Models;
using HireIQ.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireIQ.API.Controllers
{
    // ResumeBuilderController.cs (naya controller)
    [ApiController]
    [Route("api/resume-builder")]
    [Authorize]
    public class ResumeBuilderController : BaseController
    {
        private readonly PdfService _pdfService;
        private readonly AppDbContext _db;

        public ResumeBuilderController(PdfService pdfService, AppDbContext db)
        {
            _pdfService = pdfService;
            _db = db;
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
