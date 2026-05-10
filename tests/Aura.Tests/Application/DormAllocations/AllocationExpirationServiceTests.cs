using Aura.Application.DormAllocations.Commands.RunAllocationMaintenance;
using Aura.Infrastructure.Scheduling;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class AllocationExpirationServiceTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _sender);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Tick_DispatchesRunAllocationMaintenanceCommand()
    {
        var time = new FakeTimeProvider(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationExpirationService(BuildProvider(), NullLogger<AllocationExpirationService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _sender.Received(1).Send(Arg.Any<RunAllocationMaintenanceCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_SenderThrows_ExceptionSwallowed()
    {
        _sender.Send(Arg.Any<RunAllocationMaintenanceCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("db error"));

        var time = new FakeTimeProvider(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationExpirationService(BuildProvider(), NullLogger<AllocationExpirationService>.Instance, time);

        var act = async () => await svc.RunOneTickAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
