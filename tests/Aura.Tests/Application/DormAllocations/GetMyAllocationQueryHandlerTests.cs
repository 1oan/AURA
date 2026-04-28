using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Queries.GetMyAllocation;
using Aura.Domain.Entities;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class GetMyAllocationQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();

    [Fact]
    public async Task Handle_HasActiveAllocation_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var campus = Campus.Create("Codrescu");
        var dorm = Dormitory.Create("C1", campus.Id);
        dorm.SetPrivateProperty("Campus", campus);

        var allocation = DormAllocation.Create(userId, dorm.Id, periodId, 1);
        allocation.SetPrivateProperty("Dormitory", dorm);

        _currentUser.GetCurrentUserId().Returns(userId);
        _allocations.FindLatestByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        var handler = new GetMyAllocationQueryHandler(_currentUser, _allocations);
        var result = await handler.Handle(new GetMyAllocationQuery(periodId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.DormitoryName.Should().Be("C1");
        result.CampusName.Should().Be("Codrescu");
    }

    [Fact]
    public async Task Handle_NoActiveAllocation_ReturnsNull()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _allocations.FindLatestByUserAndPeriodAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        var handler = new GetMyAllocationQueryHandler(_currentUser, _allocations);
        var result = await handler.Handle(new GetMyAllocationQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HasExpiredAllocation_ReturnsDtoWithExpiredStatus()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var campus = Campus.Create("Codrescu");
        var dorm = Dormitory.Create("C12", campus.Id);
        dorm.SetPrivateProperty("Campus", campus);

        var allocation = DormAllocation.Create(userId, dorm.Id, periodId, 1);
        allocation.SetPrivateProperty("Dormitory", dorm);
        allocation.Expire();

        _currentUser.GetCurrentUserId().Returns(userId);
        _allocations.FindLatestByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        var handler = new GetMyAllocationQueryHandler(_currentUser, _allocations);
        var result = await handler.Handle(new GetMyAllocationQuery(periodId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Expired");
        result.DormitoryName.Should().Be("C12");
    }
}
