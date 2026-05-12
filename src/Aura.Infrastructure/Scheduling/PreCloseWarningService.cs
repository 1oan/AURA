using Aura.Application.RoomAssignments.Commands.SendPreCloseWarnings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Scheduling;

public class PreCloseWarningService(
    IServiceProvider serviceProvider,
    ILogger<PreCloseWarningService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

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
            await sender.Send(new SendPreCloseWarningsCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pre-close warning tick failed");
        }
    }
}
