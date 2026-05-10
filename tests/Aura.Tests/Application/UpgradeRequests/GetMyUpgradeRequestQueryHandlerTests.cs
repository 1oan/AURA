using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Queries.GetMyUpgradeRequest;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.UpgradeRequests;

public class GetMyUpgradeRequestQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUpgradeRequestRepository _upgradeRequests = Substitute.For<IUpgradeRequestRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IFacultyRoomAllocationRepository _facultyRoomAllocations = Substitute.For<IFacultyRoomAllocationRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormA = Guid.NewGuid();
    private readonly Guid _dormB = Guid.NewGuid();
    private readonly Guid _campusId = Guid.NewGuid();

    private GetMyUpgradeRequestQueryHandler CreateHandler() =>
        new(_currentUser, _upgradeRequests, _users, _facultyRoomAllocations);

    private User CreateParticipatedUser()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.UpdateProfile("Ioan", "Virlescu");
        user.AssignToFaculty(_facultyId);
        user.SetGender(Gender.Male);
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    private FacultyRoomAllocation CreateAllocation(Guid dormitoryId, string dormName, string campusName)
    {
        var campus = Campus.Create(campusName);
        campus.SetPrivateProperty("Id", _campusId);

        var dormitory = Dormitory.Create(dormName, _campusId);
        dormitory.SetPrivateProperty("Id", dormitoryId);
        dormitory.SetPrivateProperty("Campus", campus);

        var room = Room.Create("101", dormitoryId, 1, 3, Gender.Male);
        room.SetPrivateProperty("Dormitory", dormitory);

        var allocation = FacultyRoomAllocation.Create(_facultyId, Guid.NewGuid(), _periodId);
        allocation.SetPrivateProperty("Room", room);
        return allocation;
    }

    [Fact]
    public async Task Handle_NoActiveRequest_ReturnsNull()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _upgradeRequests.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((UpgradeRequest?)null);

        var result = await CreateHandler().Handle(
            new GetMyUpgradeRequestQuery(_periodId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActiveRequest_ReturnsDtoWithTargetsOrderedByRank()
    {
        var request = UpgradeRequest.Create(_userId, _periodId, new[] { _dormB, _dormA });

        _currentUser.GetCurrentUserId().Returns(_userId);
        _upgradeRequests.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(request);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _facultyRoomAllocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(new List<FacultyRoomAllocation>
            {
                CreateAllocation(_dormA, "Dorm-A", "X-Campus"),
                CreateAllocation(_dormB, "Dorm-B", "X-Campus"),
            });

        var result = await CreateHandler().Handle(
            new GetMyUpgradeRequestQuery(_periodId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Targets.Should().HaveCount(2);
        result.Targets[0].Rank.Should().Be(1);
        result.Targets[0].DormitoryId.Should().Be(_dormB);
        result.Targets[0].DormitoryName.Should().Be("Dorm-B");
        result.Targets[0].CampusName.Should().Be("X-Campus");
        result.Targets[1].Rank.Should().Be(2);
        result.Targets[1].DormitoryId.Should().Be(_dormA);
        result.Targets[1].DormitoryName.Should().Be("Dorm-A");
    }

    [Fact]
    public async Task Handle_ActiveRequest_DtoHasIdAndCreatedAt()
    {
        var request = UpgradeRequest.Create(_userId, _periodId, new[] { _dormA });

        _currentUser.GetCurrentUserId().Returns(_userId);
        _upgradeRequests.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(request);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _facultyRoomAllocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(new List<FacultyRoomAllocation>
            {
                CreateAllocation(_dormA, "Dorm-A", "X-Campus"),
            });

        var result = await CreateHandler().Handle(
            new GetMyUpgradeRequestQuery(_periodId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(request.Id);
        result.AllocationPeriodId.Should().Be(_periodId);
        result.CreatedAt.Should().Be(request.CreatedAt);
    }
}
