using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class EmailConfirmationCodeRepository(AuraDbContext context) : IEmailConfirmationCodeRepository
{
    public async Task<EmailConfirmationCode?> FindValidCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        return await context.EmailConfirmationCodes
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Code == code &&
                !c.IsUsed &&
                c.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<EmailConfirmationCode?> GetLatestCodeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.EmailConfirmationCodes
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EmailConfirmationCode confirmationCode, CancellationToken cancellationToken = default)
    {
        await context.EmailConfirmationCodes.AddAsync(confirmationCode, cancellationToken);
    }

    public async Task InvalidateExistingCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await context.EmailConfirmationCodes
            .Where(c => c.UserId == userId && !c.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsUsed, true), cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}