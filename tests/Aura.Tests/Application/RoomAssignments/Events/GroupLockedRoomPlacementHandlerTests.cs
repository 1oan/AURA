using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Events;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Events;

public class GroupLockedRoomPlacementHandlerTests
{
    private readonly IRoomAssignmentService _assignmentService = Substitute.For<IRoomAssignmentService>();
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<GroupLockedRoomPlacementHandler> _logger =
        Substitute.For<ILogger<GroupLockedRoomPlacementHandler>>();

    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();

    private GroupLockedRoomPlacementHandler Create() =>
        new(_assignmentService, _rooms, _users, _dorms, _email, _logger);

    [Fact]
    public async Task Handle_AssignmentsCreated_SendsConfirmationEmailsToAllMembers()
    {
        var memberAId = Guid.NewGuid();
        var memberBId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var assignmentA = RoomAssignment.Create(memberAId, roomId, _periodId, _groupId);
        var assignmentB = RoomAssignment.Create(memberBId, roomId, _periodId, _groupId);

        var room = Room.Create("101", _dormId, 1, 2, Gender.Female);
        room.SetPrivateProperty("Id", roomId);

        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        dorm.SetPrivateProperty("Id", _dormId);

        var userA = User.Create("a@uaic.ro", "hash");
        userA.SetPrivateProperty("Id", memberAId);
        userA.SetPrivateProperty("FirstName", "Ana");

        var userB = User.Create("b@uaic.ro", "hash");
        userB.SetPrivateProperty("Id", memberBId);
        userB.SetPrivateProperty("FirstName", "Bogdan");

        var memberIds = new[] { memberAId, memberBId };

        _assignmentService.PlaceGroupAsync(_groupId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment> { assignmentA, assignmentB });
        _rooms.FindByIdAsync(roomId, Arg.Any<CancellationToken>()).Returns(room);
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);
        _users.GetByIdsAsync(Arg.Is<List<Guid>>(l => l.Count == 2), Arg.Any<CancellationToken>())
            .Returns(new List<User> { userA, userB });

        await Create().Handle(
            new GroupLockedEvent(_groupId, _periodId, _dormId, _leaderId, 2, memberIds),
            CancellationToken.None);

        await _email.Received(2).SendPlacementConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), "C1", "101",
            Arg.Is<string[]>(arr => arr.Length == 2 && arr.Contains("Ana") && arr.Contains("Bogdan")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAssignmentsCreated_LogsWarningAndReturnsWithoutEmail()
    {
        var memberIds = new[] { Guid.NewGuid() };

        _assignmentService.PlaceGroupAsync(_groupId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment>());

        await Create().Handle(
            new GroupLockedEvent(_groupId, _periodId, _dormId, _leaderId, 2, memberIds),
            CancellationToken.None);

        await _email.DidNotReceive().SendPlacementConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingDorm_LogsWarningAndReturnsWithoutEmail()
    {
        var memberId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var assignment = RoomAssignment.Create(memberId, roomId, _periodId, _groupId);

        var room = Room.Create("201", _dormId, 2, 1, Gender.Male);
        room.SetPrivateProperty("Id", roomId);

        _assignmentService.PlaceGroupAsync(_groupId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment> { assignment });
        _rooms.FindByIdAsync(roomId, Arg.Any<CancellationToken>()).Returns(room);
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns((Dormitory?)null);
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        await Create().Handle(
            new GroupLockedEvent(_groupId, _periodId, _dormId, _leaderId, 1, new[] { memberId }),
            CancellationToken.None);

        await _email.DidNotReceive().SendPlacementConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), Arg.Any<CancellationToken>());
    }
}
