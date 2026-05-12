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

    Task SendGroupInvitationAsync(
        string toEmail, string firstName, string inviterFirstName,
        string dormitoryName, int roomSizePreference,
        CancellationToken cancellationToken);

    Task SendGroupLockedAsync(
        string toEmail, string firstName, string dormitoryName,
        string[] memberFirstNames, CancellationToken cancellationToken);

    Task SendGroupExpiredAsync(
        string toEmail, string firstName, string dormitoryName,
        CancellationToken cancellationToken);

    Task SendPlacementConfirmationAsync(
        string toEmail, string firstName,
        string dormitoryName, string roomNumber,
        string[] roommateFirstNames,
        CancellationToken cancellationToken);

    Task SendForfeitedNotificationAsync(
        string toEmail, string firstName, string dormitoryName,
        CancellationToken cancellationToken);

    Task SendPreCloseWarningAsync(
        string toEmail, string firstName, string dormitoryName,
        DateTime periodClosesAtUtc,
        CancellationToken cancellationToken);
}
