using System.Text.RegularExpressions;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.Infrastructure.Email;

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly AppDbContext _db;
    // {{tokenName}} — extracts inner identifier
    private static readonly Regex TokenPattern = new(@"\{\{\s*([a-zA-Z][a-zA-Z0-9_]*)\s*\}\}", RegexOptions.Compiled);

    public EmailTemplateService(AppDbContext db) => _db = db;

    public Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _db.EmailTemplates.FirstOrDefaultAsync(t => t.Name == name && t.IsActive, ct);

    public Task<EmailTemplate?> GetByCategoryAsync(EmailCategory category, CancellationToken ct = default) =>
        _db.EmailTemplates.FirstOrDefaultAsync(t => t.Category == category && t.IsActive, ct);

    public string RenderSubject(EmailTemplate t, IReadOnlyDictionary<string, string> tokens) => Render(t.SubjectTemplate, tokens);
    public string RenderBody(EmailTemplate t, IReadOnlyDictionary<string, string> tokens) => Render(t.BodyTemplate, tokens);

    private static string Render(string template, IReadOnlyDictionary<string, string> tokens) =>
        TokenPattern.Replace(template, m => tokens.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    public async Task SeedDefaultsAsync(CancellationToken ct = default)
    {
        foreach (var t in DefaultTemplates())
        {
            var existing = await _db.EmailTemplates.FirstOrDefaultAsync(x => x.Name == t.Name, ct);
            if (existing != null) continue;
            _db.EmailTemplates.Add(t);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static IEnumerable<EmailTemplate> DefaultTemplates() => new[]
    {
        new EmailTemplate
        {
            Name = "application_received",
            Category = EmailCategory.ApplicationReceived,
            SubjectTemplate = "We received your application — {{jobTitle}}",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "Thanks for applying for <b>{{jobTitle}}</b> at {{company}}. Our team (and our AI screening) will review your profile and get back to you shortly.",
                "Best,<br>The HireIQ Team")
        },
        new EmailTemplate
        {
            Name = "shortlist",
            Category = EmailCategory.Shortlist,
            SubjectTemplate = "Good news — you've been shortlisted for {{jobTitle}}",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "Congratulations! You've been shortlisted for <b>{{jobTitle}}</b> at {{company}}. We'll be inviting you to the next round shortly.",
                "Best,<br>The HireIQ Team")
        },
        new EmailTemplate
        {
            Name = "reject",
            Category = EmailCategory.Reject,
            SubjectTemplate = "Update on your application — {{jobTitle}}",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "Thank you for applying for <b>{{jobTitle}}</b> at {{company}}. After careful review we've decided to move forward with other candidates whose profile is a closer match. We genuinely appreciate the time you invested and wish you the very best in your search.",
                "Warm regards,<br>The HireIQ Team")
        },
        new EmailTemplate
        {
            Name = "ai_interview_invite",
            Category = EmailCategory.AiInterviewInvite,
            SubjectTemplate = "Your AI interview invitation — {{jobTitle}}",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "You're invited to a short AI-led interview for <b>{{jobTitle}}</b> at {{company}}. Please join the room at your scheduled time and ensure your camera + microphone are working.",
                "Best,<br>The HireIQ Team")
        },
        new EmailTemplate
        {
            Name = "hr_interview_invite",
            Category = EmailCategory.HrInterviewInvite,
            SubjectTemplate = "Final HR round — {{jobTitle}}",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "Great work in the AI interview! We'd like to invite you to a final HR conversation for <b>{{jobTitle}}</b> at {{company}}. Calendar invite to follow.",
                "Best,<br>The HireIQ Team")
        },
        new EmailTemplate
        {
            Name = "offer_letter",
            Category = EmailCategory.OfferLetter,
            SubjectTemplate = "Offer of employment — {{jobTitle}} at {{company}}",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "We're delighted to offer you the role of <b>{{jobTitle}}</b> at {{company}}. The detailed offer letter is attached. Please review and let us know your decision.",
                "Warm regards,<br>The HireIQ Team")
        },
        new EmailTemplate
        {
            Name = "congratulations",
            Category = EmailCategory.Congratulations,
            SubjectTemplate = "Welcome to {{company}}!",
            BodyTemplate = Wrap("Hi {{candidateName}},",
                "Welcome aboard! 🎉 We're thrilled to have you join {{company}} as <b>{{jobTitle}}</b>. The HR team will reach out with next steps for onboarding.",
                "Cheers,<br>The HireIQ Team")
        },
    };

    private static string Wrap(string greeting, string body, string sign) => $@"
<div style=""font-family:Arial,Helvetica,sans-serif;color:#1f2330;line-height:1.7;font-size:14px;"">
  <p style=""margin:0 0 16px;"">{greeting}</p>
  <p style=""margin:0 0 16px;"">{body}</p>
  <p style=""margin:24px 0 0;"">{sign}</p>
</div>";
}
