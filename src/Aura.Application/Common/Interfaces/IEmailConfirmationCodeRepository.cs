using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IEmailConfirmationCodeRepository
{
    Task<EmailConfirmationCode?> FindValidCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<EmailConfirmationCode?> GetLatestCodeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(EmailConfirmationCode confirmationCode, CancellationToken cancellationToken = default);
    Task InvalidateExistingCodesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}