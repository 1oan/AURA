namespace Aura.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    Task SendAllocationPlacedAsync(
        string toEmail, string firstName, string dormitoryName, string campusName,
        DateTime respondByUtc, CancellationToken cancellationToken);

    Task SendAllocationReminderAsync(
        string toEmail, string firstName, string dormitoryName, string campusName,
        DateTime respondByUtc, CancellationToken cancellationToken);

    Task SendAllocationExpiredAsync(
        string toEmail, string firstName, string dormitoryName,
        CancellationToken cancellationToken);

    Task SendAllocationUpgradedAsync(
        string toEmail, string firstName, string oldDormName, string newDormName, string campusName,
        CancellationToken cancellationToken);
}
