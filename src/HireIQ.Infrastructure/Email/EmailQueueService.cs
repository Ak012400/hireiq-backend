using Hangfire;
using HireIQ.Application.Interfaces;
using HireIQ.Domain.Entities;
using HireIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireIQ.Infrastructure.Email;

public sealed class EmailQueueService : IEmailQueueService
{
    private readonly AppDbContext _db;
    private readonly IEmailTemplateService _templates;
    private readonly SmtpEmailService _smtp;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<EmailQueueService> _logger;

    public EmailQueueService(
        AppDbContext db,
        IEmailTemplateService templates,
        SmtpEmailService smtp,
        IBackgroundJobClient jobs,
        ILogger<EmailQueueService> logger)
    {
        _db = db;
        _templates = templates;
        _smtp = smtp;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<Guid> EnqueueAsync(
        string recipientEmail, string? recipientName, EmailCategory category,
        IReadOnlyDictionary<string, string> tokens,
        string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
    {
        var template = await _templates.GetByCategoryAsync(category, ct)
            ?? throw new InvalidOperationException($"No active template for category {category}. Run seed first.");

        var notification = new EmailNotification
        {
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            TemplateId = template.Id,
            Category = category,
            Subject = _templates.RenderSubject(template, tokens),
            BodyHtml = _templates.RenderBody(template, tokens),
            Status = EmailStatus.Queued,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
        };
        _db.EmailNotifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        // Queue background dispatch via Hangfire — survives app restarts
        _jobs.Enqueue<IEmailQueueService>(svc => svc.DispatchAsync(notification.Id));
        return notification.Id;
    }

    public async Task DispatchAsync(Guid notificationId)
    {
        var n = await _db.EmailNotifications.FirstOrDefaultAsync(x => x.Id == notificationId);
        if (n == null || n.Status == EmailStatus.Sent) return;

        n.Status = EmailStatus.Sending;
        await _db.SaveChangesAsync();

        try
        {
            await _smtp.SendAsync(n.RecipientEmail, n.RecipientName ?? n.RecipientEmail, n.Subject, n.BodyHtml, StripHtml(n.BodyHtml));
            n.Status = EmailStatus.Sent;
            n.SentAt = DateTime.UtcNow;
            _logger.LogInformation("Email {Id} sent to {To}", n.Id, n.RecipientEmail);
        }
        catch (Exception ex)
        {
            n.Status = EmailStatus.Failed;
            n.ErrorMessage = ex.Message;
            n.RetryCount++;
            _logger.LogError(ex, "Email {Id} dispatch failed", n.Id);
        }
        await _db.SaveChangesAsync();
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ");
}
