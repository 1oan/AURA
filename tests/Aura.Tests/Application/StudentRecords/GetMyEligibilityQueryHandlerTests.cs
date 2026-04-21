using Aura.Application.Common.Interfaces;
using Aura.Application.StudentRecords.Queries.GetMyEligibility;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.StudentRecords;

public class GetMyEligibilityQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();

    private GetMyEligibilityQueryHandler CreateHandler() => new(_currentUser, _users, _records);

    private User CreateUserWithCode(string code)
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetMatriculationCode(code);
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    private StudentRecord CreateRecord(Guid? linkedUserId = null)
    {
        var record = StudentRecord.Create("CS2024001", "Ioan", "Virlescu", 547, Gender.Male, _facultyId, _periodId);
        var faculty = Faculty.Create("Informatica", "INF");
        record.SetPrivateProperty("Faculty", faculty);
        if (linkedUserId is not null)
            record.LinkToUser(linkedUserId.Value);
        return record;
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new GetMyEligibilityQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoMatriculationCode_ReturnsNotEligibleWithoutCode()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(
            new GetMyEligibilityQuery(_periodId), CancellationToken.None);

        result.IsEligible.Should().BeFalse();
        result.HasParticipated.Should().BeFalse();
        result.MatriculationCode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNoStudentRecordForCode_ReturnsNotEligibleWithCode()
    {
        var user = CreateUserWithCode("CS2024001");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns((StudentRecord?)null);

        var result = await CreateHandler().Handle(
            new GetMyEligibilityQuery(_periodId), CancellationToken.None);

        result.IsEligible.Should().BeFalse();
        result.HasParticipated.Should().BeFalse();
        result.MatriculationCode.Should().Be("CS2024001");
    }

    [Fact]
    public async Task Handle_WhenRecordLinkedToCurrentUser_ReturnsEligibleAndParticipated()
    {
        var user = CreateUserWithCode("CS2024001");
        var record = CreateRecord(linkedUserId: _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var result = await CreateHandler().Handle(
            new GetMyEligibilityQuery(_periodId), CancellationToken.None);

        result.IsEligible.Should().BeTrue();
        result.HasParticipated.Should().BeTrue();
        result.FacultyName.Should().Be("Informatica");
        result.FacultyAbbreviation.Should().Be("INF");
        result.Points.Should().Be(547);
        result.MatriculationCode.Should().Be("CS2024001");
    }

    [Fact]
    public async Task Handle_WhenRecordLinkedToAnotherUser_ReturnsNotEligible()
    {
        var user = CreateUserWithCode("CS2024001");
        var record = CreateRecord(linkedUserId: Guid.NewGuid()); // different user

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var result = await CreateHandler().Handle(
            new GetMyEligibilityQuery(_periodId), CancellationToken.None);

        result.IsEligible.Should().BeFalse();
        result.HasParticipated.Should().BeFalse();
        result.MatriculationCode.Should().Be("CS2024001");
    }

    [Fact]
    public async Task Handle_WhenRecordUnlinked_ReturnsEligibleButNotParticipated()
    {
        var user = CreateUserWithCode("CS2024001");
        var record = CreateRecord(linkedUserId: null);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var result = await CreateHandler().Handle(
            new GetMyEligibilityQuery(_periodId), CancellationToken.None);

        result.IsEligible.Should().BeTrue();
        result.HasParticipated.Should().BeFalse();
        result.FacultyName.Should().Be("Informatica");
        result.Points.Should().Be(547);
    }
}
