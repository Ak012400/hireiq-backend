using HireIQ.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace HireIQ.Infrastructure.Email;

// SMTP email via env config. If not configured, methods no-op gracefully —
// features (room creation etc.) must never fail because email isn't set up.
//
// Render env vars:
//   EmailSettings__Host       e.g. smtp-relay.brevo.com
//   EmailSettings__Port       e.g. 587
//   EmailSettings__Username   SMTP login
//   EmailSettings__Password   SMTP key
//   EmailSettings__FromEmail  e.g. hireiq@yourdomain.com (verified sender in Brevo)
//   EmailSettings__FromName   e.g. HireIQ Recruiting
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// IEmailService implementation — minimal signature, used by Application layer.
    /// </summary>
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        await SendAsync(to, to, subject, htmlBody, htmlBody);
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_config["EmailSettings:Host"]) &&
        !string.IsNullOrWhiteSpace(_config["EmailSettings:FromEmail"]);

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody, string textBody)
    {
        if (!IsConfigured)
        {
            _logger.LogInformation("EmailService not configured — skipping send to {Email}", toEmail);
            return false;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(
                _config["EmailSettings:FromName"] ?? "HireIQ",
                _config["EmailSettings:FromEmail"]));
            msg.To.Add(new MailboxAddress(toName, toEmail));
            msg.Subject = subject;

            // ✅ Multipart (HTML + plain text) — plain-text part improves spam score
            msg.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["EmailSettings:Host"],
                int.TryParse(_config["EmailSettings:Port"], out var p) ? p : 587,
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed to {Email}", toEmail);
            return false;
        }
    }

    // ── Interview invitation — clean, spam-safe design ───────────────────────
    // No images, no tracking, no spammy words, simple table layout, plain-text alternative.
    public Task<bool> SendInterviewInviteAsync(
        string toEmail, string candidateName, string jobTitle,
        DateTime? scheduledAtUtc, string roomCode, string roomPassword, string joinUrl)
    {
        var name = string.IsNullOrWhiteSpace(candidateName) ? "Candidate" : candidateName;
        var role = string.IsNullOrWhiteSpace(jobTitle) ? "the open position" : jobTitle;
        var when = scheduledAtUtc.HasValue
            ? scheduledAtUtc.Value.ToString("dddd, dd MMMM yyyy 'at' HH:mm 'UTC'")
            : "To be confirmed — the recruiter will share the time shortly";

        var subject = $"Interview Invitation – {role}";

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<body style=""margin:0;padding:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f5f7;padding:24px 0;"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:8px;border:1px solid #e2e5ea;"">
        <tr>
          <td style=""padding:28px 36px;border-bottom:3px solid #4f46e5;"">
            <span style=""font-size:20px;font-weight:bold;color:#1f2330;"">HireIQ</span>
            <span style=""font-size:12px;color:#8a90a0;""> · Interview Invitation</span>
          </td>
        </tr>
        <tr>
          <td style=""padding:32px 36px;"">
            <p style=""margin:0 0 16px;font-size:15px;color:#1f2330;"">Dear {name},</p>
            <p style=""margin:0 0 16px;font-size:14px;color:#3c4257;line-height:1.7;"">
              Thank you for your interest in the <strong>{role}</strong> position.
              We are pleased to invite you to an online interview. Please find the details below.
            </p>
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
                   style=""background:#f8f9fc;border:1px solid #e2e5ea;border-radius:6px;margin:20px 0;"">
              <tr><td style=""padding:8px 20px 4px;font-size:13px;color:#3c4257;""><strong>Position:</strong> {role}</td></tr>
              <tr><td style=""padding:4px 20px;font-size:13px;color:#3c4257;""><strong>Schedule:</strong> {when}</td></tr>
              <tr><td style=""padding:4px 20px;font-size:13px;color:#3c4257;""><strong>Room code:</strong> {roomCode}</td></tr>
              <tr><td style=""padding:4px 20px 8px;font-size:13px;color:#3c4257;""><strong>Access PIN:</strong> {roomPassword}</td></tr>
            </table>
            <p style=""margin:0 0 24px;font-size:14px;color:#3c4257;line-height:1.7;"">
              At the scheduled time, please join using the link below and enter the room code and access PIN.
            </p>
            <p style=""margin:0 0 28px;"">
              <a href=""{joinUrl}""
                 style=""background:#4f46e5;color:#ffffff;text-decoration:none;padding:12px 28px;border-radius:6px;font-size:14px;display:inline-block;"">
                Join Interview Room
              </a>
            </p>
            <p style=""margin:0 0 6px;font-size:13px;color:#3c4257;line-height:1.7;"">
              If the button does not work, copy this link into your browser:<br>
              <span style=""color:#4f46e5;"">{joinUrl}</span>
            </p>
            <p style=""margin:24px 0 0;font-size:14px;color:#3c4257;line-height:1.7;"">
              We look forward to speaking with you.<br><br>
              Best regards,<br>
              <strong>The HireIQ Recruiting Team</strong>
            </p>
          </td>
        </tr>
        <tr>
          <td style=""padding:16px 36px;border-top:1px solid #e2e5ea;"">
            <p style=""margin:0;font-size:11px;color:#8a90a0;line-height:1.6;"">
              You received this email because a recruiter scheduled an interview with you via HireIQ.
              If you believe this was sent in error, you can safely ignore it.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

        var text = $@"Dear {name},

Thank you for your interest in the {role} position.
We are pleased to invite you to an online interview.

Position:  {role}
Schedule:  {when}
Room code: {roomCode}
Access PIN: {roomPassword}

Join here: {joinUrl}

We look forward to speaking with you.

Best regards,
The HireIQ Recruiting Team

—
You received this email because a recruiter scheduled an interview with you via HireIQ.
If you believe this was sent in error, you can safely ignore it.";

        return SendAsync(toEmail, name, subject, html, text);
    }
}
