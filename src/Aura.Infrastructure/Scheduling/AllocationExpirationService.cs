using Aura.Application.DormAllocations.Commands.RunAllocationMaintenance;
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
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new RunAllocationMaintenanceCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Allocation maintenance tick failed");
        }
    }
}
