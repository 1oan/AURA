using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Queries.GetMyPreferences;
using Aura.Application.DormPreferences.Queries.GetPreferenceStats;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.DormPreferences;

public class GetMyPreferencesQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDormPreferenceRepository _preferences = Substitute.For<IDormPreferenceRepository>();

    [Fact]
    public async Task Handle_ReturnsPreferencesInOrder()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var campus = Campus.Create("Codrescu");
        var dorm = Dormitory.Create("C1", campus.Id);
        dorm.SetPrivateProperty("Campus", campus);

        var pref = DormPreference.Create(userId, periodId, dorm.Id, 1);
        pref.SetPrivateProperty("Dormitory", dorm);

        _currentUser.GetCurrentUserId().Returns(userId);
        _preferences.GetByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns([pref]);

        var handler = new GetMyPreferencesQueryHandler(_currentUser, _preferences);
        var result = await handler.Handle(new GetMyPreferencesQuery(periodId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DormitoryName.Should().Be("C1");
        result[0].CampusName.Should().Be("Codrescu");
        result[0].Rank.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoPreferences_ReturnsEmpty()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _preferences.GetByUserAndPeriodAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetMyPreferencesQueryHandler(_currentUser, _preferences);
        var result = await handler.Handle(new GetMyPreferencesQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetPreferenceStatsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();
    private readonly IDormPreferenceRepository _preferences = Substitute.For<IDormPreferenceRepository>();

    [Fact]
    public async Task Handle_ReturnsParticipationStats()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var user = User.Create("admin@uaic.ro", "hash");
        user.UpdateProfile("Admin", "Admin");
        user.SetRole(Aura.Domain.Enums.UserRole.FacultyAdmin);
        user.AssignToFaculty(facultyId);

        _currentUser.GetCurrentUserId().Returns(userId);
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _records.CountParticipantsByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns(12);
        _preferences.CountByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns(8);

        var handler = new GetPreferenceStatsQueryHandler(_currentUser, _users, _records, _preferences);
        var result = await handler.Handle(new GetPreferenceStatsQuery(periodId), CancellationToken.None);

        result.TotalParticipants.Should().Be(12);
        result.StudentsWithPreferences.Should().Be(8);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _users.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new GetPreferenceStatsQueryHandler(_currentUser, _users, _records, _preferences);
        var act = async () => await handler.Handle(
            new GetPreferenceStatsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoFaculty_Throws()
    {
        var user = User.Create("admin@uaic.ro", "hash");
        _currentUser.GetCurrentUserId().Returns(user.Id);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new GetPreferenceStatsQueryHandler(_currentUser, _users, _records, _preferences);
        var act = async () => await handler.Handle(
            new GetPreferenceStatsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
