using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations.EmailHandlers;

public class AllocationReminderEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<AllocationReminderEmailHandler> _logger = Substitute.For<ILogger<AllocationReminderEmailHandler>>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _allocationId = Guid.NewGuid();

    private AllocationReminderEmailHandler Create() => new(_users, _dorms, _allocations, _periods, _email, _logger);

    [Fact]
    public async Task Handle_AllDataPresent_CallsSendAllocationReminderAsync()
    {
        var user = User.Create("ana@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        user.SetPrivateProperty("FirstName", "Ana");

        var campus = Campus.Create("Tudor");
        campus.SetPrivateProperty("Id", Guid.NewGuid());
        var dorm = Dormitory.Create("C1", campus.Id);
        dorm.SetPrivateProperty("Id", _dormId);
        dorm.SetPrivateProperty("Campus", campus);

        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.SetPrivateProperty("Id", _allocationId);

        var period = AllocationPeriod.Create("Period", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 7);

        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns(allocation);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _dorms.FindByIdWithCampusAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);

        await Create().Handle(
            new AllocationReminderDueEvent(_allocationId, _userId, _dormId, _periodId),
            CancellationToken.None);

        await _email.Received(1).SendAllocationReminderAsync(
            "ana@uaic.ro", "Ana", "C1", "Tudor",
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllocationMissing_DoesNotCallEmail()
    {
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns((DormAllocation?)null);

        await Create().Handle(
            new AllocationReminderDueEvent(_allocationId, _userId, _dormId, _periodId),
            CancellationToken.None);

        await _email.DidNotReceive().SendAllocationReminderAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
