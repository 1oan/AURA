using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.RunAllocationRound;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Scheduling;

public class AllocationSchedulerService(
    IServiceProvider serviceProvider,
    ILogger<AllocationSchedulerService> logger,
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
            var periodRepo = scope.ServiceProvider.GetRequiredService<IAllocationPeriodRepository>();
            var allocationRepo = scope.ServiceProvider.GetRequiredService<IDormAllocationRepository>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var periods = await periodRepo.GetAllocatingDueAtAsync(now, cancellationToken);

            foreach (var period in periods)
            {
                var elapsed = now - period.Round1Date;
                if (elapsed.TotalDays < 0) continue;
                var expectedRound = (int)Math.Floor(elapsed.TotalDays / period.ResponseWindowDays) + 1;

                var lastRound = await allocationRepo.GetMaxRoundAsync(period.Id, cancellationToken);
                for (var round = lastRound + 1; round <= expectedRound; round++)
                {
                    await sender.Send(new RunAllocationRoundCommand(period.Id, round), cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Allocation scheduler tick failed");
        }
    }
}
