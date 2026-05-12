using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Infrastructure.Allocation;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Services;

public class RoomAssignmentServiceSoloTests
{
    [Fact]
    public async Task PlaceSoloAsync_PicksMaxFillRoom()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var dormId = Guid.NewGuid();

        var user = User.Create("test@uaic.ro", "hash");
        var allocation = DormAllocation.Create(userId, dormId, periodId, 1);
        allocation.Accept();

        var room1 = Room.Create("101", dormId, floor: 1, capacity: 2, gender: Gender.Female);
        var room2 = Room.Create("102", dormId, floor: 1, capacity: 2, gender: Gender.Female);
        var room3 = Room.Create("103", dormId, floor: 1, capacity: 3, gender: Gender.Female);

        var (service, deps) = BuildService();
        deps.UserRepo.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        deps.AllocRepo.FindActiveByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(allocation);
        deps.RoomRepo.ListByDormitoryAndGenderAsync(dormId, Arg.Any<Gender>(), Arg.Any<CancellationToken>())
            .Returns(new List<Room> { room1, room2, room3 });
        deps.AssignmentRepo.FindByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);
        deps.AssignmentRepo.GetOccupancyForDormitoryAsync(dormId, periodId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>
            {
                [room1.Id] = 1,
                [room2.Id] = 0,
                [room3.Id] = 2,
            });

        var result = await service.PlaceSoloAsync(userId, CancellationToken.None);

        result.RoomId.Should().Be(room3.Id);
        await deps.AssignmentRepo.Received(1).AddAsync(Arg.Is<RoomAssignment>(a => a.RoomId == room3.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceSoloAsync_TiebreaksByLowestRoomNumber()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var dormId = Guid.NewGuid();

        var user = User.Create("test@uaic.ro", "hash");
        var allocation = DormAllocation.Create(userId, dormId, periodId, 1);
        allocation.Accept();

        var room1 = Room.Create("205", dormId, floor: 2, capacity: 2, gender: Gender.Female);
        var room2 = Room.Create("101", dormId, floor: 1, capacity: 2, gender: Gender.Female);

        var (service, deps) = BuildService();
        deps.UserRepo.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        deps.AllocRepo.FindActiveByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(allocation);
        deps.RoomRepo.ListByDormitoryAndGenderAsync(dormId, Arg.Any<Gender>(), Arg.Any<CancellationToken>())
            .Returns(new List<Room> { room1, room2 });
        deps.AssignmentRepo.FindByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);
        deps.AssignmentRepo.GetOccupancyForDormitoryAsync(dormId, periodId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [room1.Id] = 1, [room2.Id] = 1 });

        var result = await service.PlaceSoloAsync(userId, CancellationToken.None);

        result.RoomId.Should().Be(room2.Id);
    }

    [Fact]
    public async Task PlaceSoloAsync_NoNonFullRoom_Throws()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var dormId = Guid.NewGuid();

        var user = User.Create("test@uaic.ro", "hash");
        var allocation = DormAllocation.Create(userId, dormId, periodId, 1);
        allocation.Accept();
        var room = Room.Create("101", dormId, floor: 1, capacity: 2, gender: Gender.Female);

        var (service, deps) = BuildService();
        deps.UserRepo.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        deps.AllocRepo.FindActiveByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(allocation);
        deps.RoomRepo.ListByDormitoryAndGenderAsync(dormId, Arg.Any<Gender>(), Arg.Any<CancellationToken>())
            .Returns(new List<Room> { room });
        deps.AssignmentRepo.FindByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);
        deps.AssignmentRepo.GetOccupancyForDormitoryAsync(dormId, periodId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [room.Id] = 2 });

        var act = async () => await service.PlaceSoloAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No rooms available*");
    }

    [Fact]
    public async Task PlaceSoloAsync_NoAcceptedAllocation_Throws()
    {
        var userId = Guid.NewGuid();
        var (service, deps) = BuildService();
        deps.UserRepo.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(User.Create("t@uaic.ro", "h"));
        deps.AllocRepo.FindActiveByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        var act = async () => await service.PlaceSoloAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No Accepted allocation*");
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
