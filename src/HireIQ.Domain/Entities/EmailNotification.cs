namespace HireIQ.Domain.Entities;

public enum EmailCategory
{
    ApplicationReceived,
    Shortlist,
    Reject,
    AiInterviewInvite,
    HrInterviewInvite,
    OfferLetter,
    Congratulations,
    Generic
}

public enum EmailStatus { Queued, Sending, Sent, Failed, Bounced }

public class EmailNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public Guid? TemplateId { get; set; }
    public EmailCategory Category { get; set; } = EmailCategory.Generic;

    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    public EmailStatus Status { get; set; } = EmailStatus.Queued;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    // Audit linkage
    public string? RelatedEntityType { get; set; }   // "CandidateJourney", "InterviewSession", etc.
    public Guid? RelatedEntityId { get; set; }
}

public class EmailTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;            // unique key
    public EmailCategory Category { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty; // "{{candidateName}} — interview invitation"
    public string BodyTemplate { get; set; } = string.Empty;    // mustache-style {{placeholder}}
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
