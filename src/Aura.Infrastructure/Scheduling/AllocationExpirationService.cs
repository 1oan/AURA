using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Scheduling;

public class AllocationExpirationService(
    IServiceProvider serviceProvider,
    ILogger<AllocationExpirationService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOneTickAsync(stoppingToken);
        }
    }

    internal async Task RunOneTickAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDormAllocationRepository>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var stale = await repo.GetExpirablePendingAsync(now, cancellationToken);
            if (stale.Count == 0) return;

            foreach (var allocation in stale)
            {
                allocation.Expire();
            }
            await repo.SaveChangesAsync(cancellationToken);

            foreach (var allocation in stale)
            {
                await publisher.Publish(
                    new AllocationExpiredEvent(allocation.Id, allocation.UserId, allocation.DormitoryId, allocation.AllocationPeriodId),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Allocation expiration tick failed");
        }
    }
}
