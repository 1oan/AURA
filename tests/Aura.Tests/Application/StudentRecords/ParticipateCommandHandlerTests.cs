using Aura.Application.Common.Interfaces;
using Aura.Application.StudentRecords.Commands.Participate;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.StudentRecords;

public class ParticipateCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();

    private ParticipateCommandHandler CreateHandler() =>
        new(_currentUser, _users, _periods, _records);

    // ─── Test data factories ─────────────────────────────────────────────

    private User CreateUser(string email)
    {
        var user = User.Create(email, "hash");
        user.ConfirmEmail();
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    private AllocationPeriod CreateOpenPeriod()
    {
        var period = AllocationPeriod.Create(
            "2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);
        period.Activate();
        return period;
    }

    private StudentRecord CreateStudentRecord(string first, string last, string code = "CS2024001")
    {
        var record = StudentRecord.Create(code, first, last, 547, Gender.Male, _facultyId, _periodId);
        var faculty = Faculty.Create("Informatica", "INF");
        record.SetPrivateProperty("Faculty", faculty);
        return record;
    }

    // ─── Identity verification — matching names ──────────────────────────

    [Fact]
    public async Task Handle_WithMatchingFirstnameLastnameOrder_Succeeds()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Virlescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var result = await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        result.FacultyName.Should().Be("Informatica");
        result.FacultyAbbreviation.Should().Be("INF");
        result.Points.Should().Be(547);
        record.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task Handle_WithReversedLastnameFirstnameEmailOrder_Succeeds()
    {
        // Email is lastname.firstname, CSV is still FirstName=Ioan, LastName=Virlescu
        var user = CreateUser("virlescu.ioan@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Virlescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var result = await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        result.Should().NotBeNull();
        record.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task Handle_WithDiacriticsInCsv_MatchesNormalizedEmail()
    {
        // CSV has "Vîrlescu" with diacritics, email has "virlescu" without
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Vîrlescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WithMultipleRomanianDiacritics_MatchesNormalized()
    {
        // All 5 Romanian diacritics: ă, â, î, ș, ț
        var user = CreateUser("stefan.tanase@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ștefan", "Țănase");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ─── Identity verification — mismatched names ────────────────────────

    [Fact]
    public async Task Handle_WithMismatchedNames_ThrowsDomainException()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Maria", "Popescu"); // Different person

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Identity mismatch*");
    }

    // ─── Side effects on success ─────────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_SetsUserNameFacultyAndGenderFromRecord()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Virlescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        await CreateHandler().Handle(new ParticipateCommand(_periodId, null), CancellationToken.None);

        user.FirstName.Should().Be("Ioan");
        user.LastName.Should().Be("Virlescu");
        user.FacultyId.Should().Be(_facultyId);
        user.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public async Task Handle_OnSuccess_PersistsChanges()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Virlescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        await CreateHandler().Handle(new ParticipateCommand(_periodId, null), CancellationToken.None);

        await _records.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── Guard clauses ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_WithoutMatriculationCodeAnywhere_ThrowsDomainException()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro"); // no matriculation code set

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Matriculation code is required.");
    }

    [Fact]
    public async Task Handle_WhenPeriodIsNotOpen_ThrowsDomainException()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        // period in Draft status (never activated)
        var draftPeriod = AllocationPeriod.Create(
            "2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(draftPeriod);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Allocation period is not accepting participants.");
    }

    [Fact]
    public async Task Handle_WhenCodeNotInCsv_ThrowsNotFoundException()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns((StudentRecord?)null);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("You are not eligible for this allocation period.");
    }

    [Fact]
    public async Task Handle_WhenAlreadyParticipated_ThrowsDomainException()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        user.SetMatriculationCode("CS2024001");
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Virlescu");
        record.LinkToUser(Guid.NewGuid()); // already linked to someone else

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        var act = async () => await CreateHandler().Handle(
            new ParticipateCommand(_periodId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You have already participated in this allocation period.");
    }

    // ─── Matriculation code handling ─────────────────────────────────────

    [Fact]
    public async Task Handle_WithCommandMatriculationCode_PersistsItToUserProfile()
    {
        var user = CreateUser("ioan.virlescu@student.uaic.ro");
        // user has no matriculation code yet
        var period = CreateOpenPeriod();
        var record = CreateStudentRecord("Ioan", "Virlescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _records.FindByMatriculationCodeAndPeriodAsync("CS2024001", _periodId, Arg.Any<CancellationToken>())
            .Returns(record);

        await CreateHandler().Handle(
            new ParticipateCommand(_periodId, "CS2024001"), CancellationToken.None);

        user.MatriculationCode.Should().Be("CS2024001");
    }
}
