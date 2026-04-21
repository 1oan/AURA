using Aura.Application.Common.Interfaces;
using Aura.Application.StudentRecords.Queries.GetStudentRecords;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.StudentRecords;

public class GetStudentRecordsQueryHandlerTests
{
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();

    [Fact]
    public async Task Handle_ReturnsMappedDtos()
    {
        var facultyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var faculty = Faculty.Create("Informatica", "INF");
        var record = StudentRecord.Create("MAT001", "Ion", "Popescu", 8, Gender.Male, facultyId, periodId);
        record.SetPrivateProperty("Faculty", faculty);

        _records.GetByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns([record]);

        var handler = new GetStudentRecordsQueryHandler(_records);
        var result = await handler.Handle(new GetStudentRecordsQuery(periodId, facultyId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].MatriculationCode.Should().Be("MAT001");
        result[0].FirstName.Should().Be("Ion");
        result[0].LastName.Should().Be("Popescu");
        result[0].Points.Should().Be(8);
        result[0].Gender.Should().Be("Male");
        result[0].FacultyName.Should().Be("Informatica");
        result[0].FacultyAbbreviation.Should().Be("INF");
    }

    [Fact]
    public async Task Handle_WithNoRecords_ReturnsEmpty()
    {
        _records.GetByPeriodAndFacultyAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetStudentRecordsQueryHandler(_records);
        var result = await handler.Handle(
            new GetStudentRecordsQuery(Guid.NewGuid(), null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNullFacultyId_PassesThroughToRepository()
    {
        var periodId = Guid.NewGuid();
        _records.GetByPeriodAndFacultyAsync(periodId, null, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetStudentRecordsQueryHandler(_records);
        await handler.Handle(new GetStudentRecordsQuery(periodId, null), CancellationToken.None);

        await _records.Received(1).GetByPeriodAndFacultyAsync(
            periodId, null, Arg.Any<CancellationToken>());
    }
}
