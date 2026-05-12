using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Events;
using Aura.Domain.Entities;
using Aura.Domain.Events;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Events;

public class PeriodClosedForfeitHandlerTests
{
    private readonly IRoomAssignmentService _assignmentService = Substitute.For<IRoomAssignmentService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormAllocationRepository _dormAllocations = Substitute.For<IDormAllocationRepository>();
    private readonly IDormitoryRepository _dormitories = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<PeriodClosedForfeitHandler> _logger =
        Substitute.For<ILogger<PeriodClosedForfeitHandler>>();

    private readonly Guid _periodId = Guid.NewGuid();

    private PeriodClosedForfeitHandler Create() =>
        new(_assignmentService, _users, _dormAllocations, _dormitories, _email, _logger);

    [Fact]
    public async Task Handle_ForfeitsAllocations_SendsEmailsToAffectedUsers()
    {
        var userId = Guid.NewGuid();
        var dormId = Guid.NewGuid();

        var user = User.Create("student@uaic.ro", "hash");
        user.SetPrivateProperty("Id", userId);
        user.SetPrivateProperty("FirstName", "Ana");

        var dorm = Dormitory.Create("Dorm A", Guid.NewGuid());
        dorm.SetPrivateProperty("Id", dormId);

        var allocation = DormAllocation.Create(userId, dormId, _periodId, 1);
        allocation.SetPrivateProperty("Dormitory", dorm);

        _assignmentService.ForfeitNonCommittedAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId });
        _users.GetByIdsAsync(Arg.Is<List<Guid>>(l => l.Count == 1 && l[0] == userId), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _dormAllocations.FindLatestByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        await Create().Handle(new AllocationPeriodClosedEvent(_periodId), CancellationToken.None);

        await _email.Received(1).SendForfeitedNotificationAsync(
            "student@uaic.ro", "Ana", "Dorm A", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoForfeits_ReturnsEarlyWithoutLoadingUsers()
    {
        _assignmentService.ForfeitNonCommittedAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        await Create().Handle(new AllocationPeriodClosedEvent(_periodId), CancellationToken.None);

        await _users.DidNotReceive().GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendForfeitedNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserHasNoEmail_SkipsEmail()
    {
        var userId = Guid.NewGuid();
        var dormId = Guid.NewGuid();

        var user = User.Create("noemail@uaic.ro", "hash");
        user.SetPrivateProperty("Id", userId);
        user.SetPrivateProperty("Email", string.Empty);

        var dorm = Dormitory.Create("Dorm B", Guid.NewGuid());
        dorm.SetPrivateProperty("Id", dormId);

        var allocation = DormAllocation.Create(userId, dormId, _periodId, 1);
        allocation.SetPrivateProperty("Dormitory", dorm);

        _assignmentService.ForfeitNonCommittedAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _dormAllocations.FindLatestByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        await Create().Handle(new AllocationPeriodClosedEvent(_periodId), CancellationToken.None);

        await _email.DidNotReceive().SendForfeitedNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
