using System.Net;
using Aura.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

#pragma warning disable CA2254

namespace Aura.Infrastructure.Email;

public class SmtpEmailService(
    IOptions<SmtpSettings> settings,
    IOptions<FrontendSettings> frontendSettings,
    IHostEnvironment environment,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpSettings _settings = settings.Value;
    private readonly FrontendSettings _frontend = frontendSettings.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment() && string.IsNullOrEmpty(_settings.Username))
        {
            logger.LogInformation(
                "Email to {To} | Subject: {Subject} | Body: {Body}",
                to, subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public Task SendAllocationPlacedAsync(
        string toEmail, string firstName, string dormitoryName, string campusName,
        DateTime respondByUtc, CancellationToken cancellationToken)
    {
        var subject = $"Your dorm allocation — {dormitoryName}";
        var body = BuildHtml(
            firstName,
            $@"<p>You have been placed in <strong>{WebUtility.HtmlEncode(dormitoryName)}</strong>
                  in <strong>{WebUtility.HtmlEncode(campusName)}</strong>.</p>
               <p>Please respond by <strong>{respondByUtc:yyyy-MM-dd HH:mm} UTC</strong>
                  or your placement will expire and you will be removed from the period.</p>
               <p>Open the dashboard to accept or decline:
                  <a href=""{WebUtility.HtmlEncode(_frontend.BaseUrl)}"">View my allocation</a></p>");

        return SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendAllocationReminderAsync(
        string toEmail, string firstName, string dormitoryName, string campusName,
        DateTime respondByUtc, CancellationToken cancellationToken)
    {
        var subject = "Reminder: respond to your dorm allocation";
        var body = BuildHtml(
            firstName,
            $@"<p>You have less than 24 hours to respond to your placement in
                  <strong>{WebUtility.HtmlEncode(dormitoryName)}</strong>
                  ({WebUtility.HtmlEncode(campusName)}).</p>
               <p>Pending placements expire on <strong>{respondByUtc:yyyy-MM-dd HH:mm} UTC</strong>.</p>
               <p>Open the dashboard to accept or decline:
                  <a href=""{WebUtility.HtmlEncode(_frontend.BaseUrl)}"">View my allocation</a></p>");

        return SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendAllocationExpiredAsync(
        string toEmail, string firstName, string dormitoryName,
        CancellationToken cancellationToken)
    {
        var subject = "Your dorm allocation expired";
        var body = BuildHtml(
            firstName,
            $@"<p>Your placement in <strong>{WebUtility.HtmlEncode(dormitoryName)}</strong>
                  expired because the response window closed.</p>
               <p>You are no longer in this period's pool. Contact your faculty admin
                  if this is unexpected.</p>");

        return SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendAllocationUpgradedAsync(
        string toEmail, string firstName, string oldDormName, string newDormName, string campusName,
        CancellationToken cancellationToken)
    {
        var subject = $"Your upgrade succeeded — {newDormName}";
        var body = BuildHtml(
            firstName,
            $@"<p>Your upgrade request was fulfilled. You are now placed in
                  <strong>{WebUtility.HtmlEncode(newDormName)}</strong>
                  in <strong>{WebUtility.HtmlEncode(campusName)}</strong>
                  instead of <strong>{WebUtility.HtmlEncode(oldDormName)}</strong>.</p>
               <p>Open the dashboard to view your placement:
                  <a href=""{WebUtility.HtmlEncode(_frontend.BaseUrl)}"">View my allocation</a></p>");

        return SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    private static string BuildHtml(string firstName, string innerHtml) => $@"
<html><body style=""font-family: Arial, sans-serif; color: #1a1a1a;"">
  <h2 style=""color: #1a1a1a;"">AURA</h2>
  <p>Hi {WebUtility.HtmlEncode(firstName)},</p>
  {innerHtml}
  <p style=""color: #666; font-size: 12px;"">This is an automated message from AURA.
     Contact your faculty admin if you have questions.</p>
</body></html>";
}
