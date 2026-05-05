using System.Text;
using Aura.Application.Common.Interfaces;
using Aura.Application.StudentRecords.Commands.UploadCsv;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.StudentRecords;

public class UploadCsvCommandHandlerTests
{
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    private UploadCsvCommandHandler CreateHandler() =>
        new(_records, _periods, _users, _currentUser);

    private User CreateFacultyAdmin()
    {
        var user = User.Create("admin@uaic.ro", "hash");
        user.UpdateProfile("Admin", "Admin");
        user.SetRole(UserRole.FacultyAdmin);
        user.AssignToFaculty(_facultyId);
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    private static AllocationPeriod CreateDraftPeriod()
    {
        return AllocationPeriod.Create(
            "2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);
    }

    private static Stream CsvStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    private void StubHappyPath(User user, AllocationPeriod period)
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
    }

    // ─── Happy path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCsv_CreatesRecordsAndReturnsCount()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,547,Male\n" +
            "Maria,Popescu,CS2024002,512,Female\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(2);
        result.Errors.Should().BeEmpty();
        await _records.Received(1).DeleteByFacultyAndPeriodAsync(
            _facultyId, _periodId, Arg.Any<CancellationToken>());
        await _records.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<StudentRecord>>(r => r.Count() == 2),
            Arg.Any<CancellationToken>());
        await _records.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SkipsHeaderAndBlankLines()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "\n" + // blank line
            "Ioan,Virlescu,CS2024001,547,Male\n" +
            "   \n" + // whitespace-only line
            "Maria,Popescu,CS2024002,512,Female\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AcceptsLowercaseGenderValues()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,547,male\n" +
            "Maria,Popescu,CS2024002,512,FEMALE\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    // ─── Guard clauses ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, CsvStream("")), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_WhenUserNotAssignedToFaculty_ThrowsDomainException()
    {
        var user = User.Create("admin@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, CsvStream("")), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You must be assigned to a faculty to upload student records.");
    }

    [Fact]
    public async Task Handle_WhenPeriodNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateFacultyAdmin());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var act = async () => await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, CsvStream("")), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Allocation period not found.");
    }

    [Fact]
    public async Task Handle_WhenPeriodIsNotDraft_ThrowsDomainException()
    {
        var period = CreateDraftPeriod();
        period.Activate(); // Now Open, not Draft
        StubHappyPath(CreateFacultyAdmin(), period);

        var act = async () => await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, CsvStream("")), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*only be uploaded while the allocation period is in Draft status*");
    }

    // ─── Row-level validation errors ─────────────────────────────────────

    [Fact]
    public async Task Handle_WithTooFewColumns_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,547\n"); // 4 cols, no gender

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Errors.Should().ContainSingle(e => e.Row == 2 && e.Message.Contains("Expected 5 columns"));
    }

    [Fact]
    public async Task Handle_WithEmptyFirstName_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            ",Virlescu,CS2024001,547,Male\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Message == "First name is required.");
    }

    [Fact]
    public async Task Handle_WithEmptyLastName_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,,CS2024001,547,Male\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Message == "Last name is required.");
    }

    [Fact]
    public async Task Handle_WithEmptyMatriculationCode_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,,547,Male\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Message == "Matriculation code is required.");
    }

    [Fact]
    public async Task Handle_WithInvalidPointsValue_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,not-a-number,Male\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("Invalid points value"));
    }

    [Fact]
    public async Task Handle_WithNegativePoints_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,-5,Male\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("Points must be greater than or equal to zero"));
    }

    [Fact]
    public async Task Handle_WithInvalidGenderValue_ReportsRowError()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,547,Other\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("Invalid gender value"));
    }

    [Fact]
    public async Task Handle_WithDuplicateMatriculationCode_ReportsRowErrorForSecond()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,547,Male\n" +
            "Maria,Popescu,CS2024001,512,Female\n"); // Same code

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Duplicate matriculation code"));
    }

    [Fact]
    public async Task Handle_DuplicateDetectionIsCaseInsensitive()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,cs2024001,547,Male\n" +
            "Maria,Popescu,CS2024001,512,Female\n"); // Different case, same code

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Duplicate"));
    }

    [Fact]
    public async Task Handle_WithValidAndInvalidRows_CreatesValidAndReportsInvalid()
    {
        StubHappyPath(CreateFacultyAdmin(), CreateDraftPeriod());
        var csv = CsvStream(
            "FirstName,LastName,MatriculationCode,Points,Gender\n" +
            "Ioan,Virlescu,CS2024001,547,Male\n" +
            ",Popescu,CS2024002,512,Female\n" + // missing first name
            "Andrei,Ionescu,CS2024003,438,Male\n");

        var result = await CreateHandler().Handle(
            new UploadCsvCommand(_periodId, csv), CancellationToken.None);

        result.Created.Should().Be(2);
        result.Errors.Should().ContainSingle(e => e.Row == 3);
    }
}
