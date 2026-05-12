using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Infrastructure.Allocation;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Services;

public class RoomAssignmentServiceGroupTests
{
    [Fact]
    public async Task PlaceGroupAsync_AnchorSet_PlacesUnassignedMembers()
    {
        var dormId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var leader = Guid.NewGuid();
        var member2 = Guid.NewGuid();

        var anchorRoom = Room.Create("101", dormId, floor: 1, capacity: 2, gender: Gender.Female);
        var leaderAssignment = RoomAssignment.Create(leader, anchorRoom.Id, periodId);

        var group = RoommateGroup.Create(periodId, dormId, leader, RoomSizePreference.TwoBed);
        group.SetAnchor(anchorRoom.Id);
        group.AddMember(member2);
        group.Lock();

        var (service, deps) = BuildService();
        deps.GroupRepo.FindByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        deps.RoomRepo.FindByIdAsync(anchorRoom.Id, Arg.Any<CancellationToken>()).Returns(anchorRoom);
        deps.AssignmentRepo.ListByRoomAndPeriodAsync(anchorRoom.Id, periodId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment> { leaderAssignment });
        deps.AssignmentRepo.FindByUserAndPeriodAsync(member2, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);

        var result = await service.PlaceGroupAsync(group.Id, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(member2);
        result[0].RoomId.Should().Be(anchorRoom.Id);
    }

    [Fact]
    public async Task PlaceGroupAsync_AnchorSet_StrangerInRoom_Throws()
    {
        var dormId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var leader = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        var stranger = Guid.NewGuid();

        var anchorRoom = Room.Create("101", dormId, floor: 1, capacity: 3, gender: Gender.Female);
        var leaderAssignment = RoomAssignment.Create(leader, anchorRoom.Id, periodId);
        var strangerAssignment = RoomAssignment.Create(stranger, anchorRoom.Id, periodId);

        var group = RoommateGroup.Create(periodId, dormId, leader, RoomSizePreference.ThreeBed);
        group.SetAnchor(anchorRoom.Id);
        group.AddMember(member2);
        group.AddMember(Guid.NewGuid());
        group.Lock();

        var (service, deps) = BuildService();
        deps.GroupRepo.FindByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        deps.RoomRepo.FindByIdAsync(anchorRoom.Id, Arg.Any<CancellationToken>()).Returns(anchorRoom);
        deps.AssignmentRepo.ListByRoomAndPeriodAsync(anchorRoom.Id, periodId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment> { leaderAssignment, strangerAssignment });

        var act = async () => await service.PlaceGroupAsync(group.Id, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*non-group occupants*");
    }

    [Fact]
    public async Task PlaceGroupAsync_AnchorNull_FindsEmptyRoom()
    {
        var dormId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var leader = Guid.NewGuid();
        var member2 = Guid.NewGuid();

        var emptyRoom = Room.Create("205", dormId, floor: 2, capacity: 2, gender: Gender.Female);
        var partialRoom = Room.Create("101", dormId, floor: 1, capacity: 2, gender: Gender.Female);

        var group = RoommateGroup.Create(periodId, dormId, leader, RoomSizePreference.TwoBed);
        group.AddMember(member2);
        group.Lock();

        var leaderUser = User.Create("leader@uaic.ro", "h");
        var (service, deps) = BuildService();
        deps.GroupRepo.FindByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        deps.UserRepo.FindByIdAsync(leader, Arg.Any<CancellationToken>()).Returns(leaderUser);
        deps.RoomRepo.ListByDormitoryAndGenderAsync(dormId, Arg.Any<Gender>(), Arg.Any<CancellationToken>())
            .Returns(new List<Room> { partialRoom, emptyRoom });
        deps.AssignmentRepo.GetOccupancyForDormitoryAsync(dormId, periodId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [partialRoom.Id] = 1, [emptyRoom.Id] = 0 });
        deps.AssignmentRepo.FindByUserAndPeriodAsync(leader, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);
        deps.AssignmentRepo.FindByUserAndPeriodAsync(member2, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);

        var result = await service.PlaceGroupAsync(group.Id, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.RoomId.Should().Be(emptyRoom.Id));
    }

    [Fact]
    public async Task PlaceGroupAsync_AnchorNull_NoEmptyRoomOfSize_Throws()
    {
        var dormId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var leader = Guid.NewGuid();

        var group = RoommateGroup.Create(periodId, dormId, leader, RoomSizePreference.ThreeBed);
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        group.Lock();

        var room = Room.Create("101", dormId, floor: 1, capacity: 2, gender: Gender.Female);
        var leaderUser = User.Create("leader@uaic.ro", "h");

        var (service, deps) = BuildService();
        deps.GroupRepo.FindByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        deps.UserRepo.FindByIdAsync(leader, Arg.Any<CancellationToken>()).Returns(leaderUser);
        deps.RoomRepo.ListByDormitoryAndGenderAsync(dormId, Arg.Any<Gender>(), Arg.Any<CancellationToken>())
            .Returns(new List<Room> { room });
        deps.AssignmentRepo.GetOccupancyForDormitoryAsync(dormId, periodId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        var act = async () => await service.PlaceGroupAsync(group.Id, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No empty room*");
    }

    private record TestDeps(
        IRoomRepository RoomRepo,
        IDormAllocationRepository AllocRepo,
        IRoomAssignmentRepository AssignmentRepo,
        IUserRepository UserRepo,
        IRoommateGroupRepository GroupRepo);

    private static (RoomAssignmentService service, TestDeps deps) BuildService()
    {
        var deps = new TestDeps(
            Substitute.For<IRoomRepository>(),
            Substitute.For<IDormAllocationRepository>(),
            Substitute.For<IRoomAssignmentRepository>(),
            Substitute.For<IUserRepository>(),
            Substitute.For<IRoommateGroupRepository>());
        var service = new RoomAssignmentService(
            deps.RoomRepo, deps.AllocRepo, deps.AssignmentRepo, deps.UserRepo, deps.GroupRepo);
        return (service, deps);
    }
}
