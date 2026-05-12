using Aura.Application.RoommateGroups.Commands.ExpireOverdueGroups;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Scheduling;

public class GroupExpirationService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<GroupExpirationService> logger) : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Cadence, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ExpireOverdueGroupsCommand(), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Group expiration tick failed.");
            }
        }
    }
}
