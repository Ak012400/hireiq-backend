using HireIQ.Domain.Entities;

namespace HireIQ.Application.Interfaces;

public interface IEmailTemplateService
{
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EmailTemplate?> GetByCategoryAsync(EmailCategory category, CancellationToken ct = default);
    string RenderSubject(EmailTemplate template, IReadOnlyDictionary<string, string> tokens);
    string RenderBody(EmailTemplate template, IReadOnlyDictionary<string, string> tokens);
    Task SeedDefaultsAsync(CancellationToken ct = default);
}

public interface IEmailQueueService
{
    /// <summary>Queues an email for background delivery (Hangfire job).</summary>
    Task<Guid> EnqueueAsync(
        string recipientEmail,
        string? recipientName,
        EmailCategory category,
        IReadOnlyDictionary<string, string> tokens,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default);

    /// <summary>Internal — called by Hangfire to actually send the queued email.</summary>
    Task DispatchAsync(Guid notificationId);
}
