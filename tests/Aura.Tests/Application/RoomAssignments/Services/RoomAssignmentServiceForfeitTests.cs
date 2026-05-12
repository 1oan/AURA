using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Infrastructure.Allocation;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Services;

public class RoomAssignmentServiceForfeitTests
{
    [Fact]
    public async Task ForfeitNonCommittedAsync_ForfeitsAcceptedWithoutRoom()
    {
        var periodId = Guid.NewGuid();
        var allocation = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), periodId, 1);
        allocation.Accept();

        var (service, deps) = BuildService();
        deps.AllocRepo.ListAcceptedWithoutRoomAsync(periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { allocation });

        var result = await service.ForfeitNonCommittedAsync(periodId, CancellationToken.None);

        allocation.Status.Should().Be(AllocationStatus.Forfeited);
        result.Should().ContainSingle().Which.Should().Be(allocation.UserId);
    }

    [Fact]
    public async Task ForfeitNonCommittedAsync_NoCandidates_ReturnsEmptyList()
    {
        var periodId = Guid.NewGuid();
        var (service, deps) = BuildService();
        deps.AllocRepo.ListAcceptedWithoutRoomAsync(periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());

        var result = await service.ForfeitNonCommittedAsync(periodId, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ForfeitNonCommittedAsync_CallsSaveChanges()
    {
        var periodId = Guid.NewGuid();
        var allocation = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), periodId, 1);
        allocation.Accept();

        var (service, deps) = BuildService();
        deps.AllocRepo.ListAcceptedWithoutRoomAsync(periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { allocation });

        await service.ForfeitNonCommittedAsync(periodId, CancellationToken.None);

        await deps.AllocRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
