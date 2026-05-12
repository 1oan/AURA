using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Queries.GetMyGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Queries;

public class GetMyGroupQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IDormitoryRepository _dormitories = Substitute.For<IDormitoryRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private GetMyGroupQueryHandler CreateHandler() =>
        new(_currentUser, _groups, _dormitories, _users, _periods);

    private AllocationPeriod CreateActivePeriod()
    {
        var period = AllocationPeriod.Create("test", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 7);
        period.SetPrivateProperty("Id", _periodId);
        period.Activate();
        period.StartAllocating();
        return period;
    }

    [Fact]
    public async Task Handle_NoActivePeriod_ReturnsNull()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod>());

        var result = await CreateHandler().Handle(new GetMyGroupQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoActiveGroup_ReturnsNull()
    {
        var period = CreateActivePeriod();
        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod> { period });
        _groups.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns((RoommateGroup?)null);

        var result = await CreateHandler().Handle(new GetMyGroupQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActiveGroup_ReturnsMappedDto()
    {
        var period = CreateActivePeriod();
        var group = RoommateGroup.Create(_periodId, _dormId, _userId, RoomSizePreference.TwoBed);
        group.SetPrivateProperty("Id", Guid.NewGuid());

        var campusId = Guid.NewGuid();
        var dorm = Dormitory.Create("Casa Studentului", campusId);
        dorm.SetPrivateProperty("Id", _dormId);

        var leader = User.Create("ana@uaic.ro", "hash");
        leader.SetPrivateProperty("Id", _userId);
        leader.UpdateProfile("Ana", "Ionescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod> { period });
        _groups.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns(group);
        _dormitories.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { leader });

        var result = await CreateHandler().Handle(new GetMyGroupQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.DormitoryName.Should().Be("Casa Studentului");
        result.Status.Should().Be("Forming");
        result.Members.Should().HaveCount(1);
        result.Members[0].IsLeader.Should().BeTrue();
        result.Members[0].FirstName.Should().Be("Ana");
    }
}
