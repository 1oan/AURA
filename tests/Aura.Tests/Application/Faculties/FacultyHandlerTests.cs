using Aura.Application.Common.Interfaces;
using Aura.Application.Faculties.Commands.CreateFaculty;
using Aura.Application.Faculties.Commands.DeleteFaculty;
using Aura.Application.Faculties.Commands.UpdateFaculty;
using Aura.Application.Faculties.Queries.GetFaculties;
using Aura.Application.Faculties.Queries.GetFacultyById;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Faculties;

public class CreateFacultyCommandHandlerTests
{
    private readonly IFacultyRepository _repo = Substitute.For<IFacultyRepository>();

    [Fact]
    public async Task Handle_CreatesAndReturnsDto()
    {
        var handler = new CreateFacultyCommandHandler(_repo);
        var result = await handler.Handle(
            new CreateFacultyCommand("Informatica", "INF"), CancellationToken.None);

        result.Name.Should().Be("Informatica");
        result.Abbreviation.Should().Be("INF");
        await _repo.Received(1).AddAsync(Arg.Any<Faculty>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UpdateFacultyCommandHandlerTests
{
    private readonly IFacultyRepository _repo = Substitute.For<IFacultyRepository>();

    [Fact]
    public async Task Handle_UpdatesExistingFaculty()
    {
        var faculty = Faculty.Create("Old", "OLD");
        _repo.FindByIdAsync(faculty.Id, Arg.Any<CancellationToken>()).Returns(faculty);

        var handler = new UpdateFacultyCommandHandler(_repo);
        await handler.Handle(new UpdateFacultyCommand(faculty.Id, "New", "NEW"), CancellationToken.None);

        faculty.Name.Should().Be("New");
        faculty.Abbreviation.Should().Be("NEW");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Faculty?)null);

        var handler = new UpdateFacultyCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new UpdateFacultyCommand(Guid.NewGuid(), "X", "X"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DeleteFacultyCommandHandlerTests
{
    private readonly IFacultyRepository _repo = Substitute.For<IFacultyRepository>();

    [Fact]
    public async Task Handle_DeletesFacultyWithoutAllocations()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        _repo.FindByIdAsync(faculty.Id, Arg.Any<CancellationToken>()).Returns(faculty);
        _repo.HasAllocationsAsync(faculty.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteFacultyCommandHandler(_repo);
        await handler.Handle(new DeleteFacultyCommand(faculty.Id), CancellationToken.None);

        _repo.Received(1).Remove(faculty);
    }

    [Fact]
    public async Task Handle_WhenHasAllocations_ThrowsConflict()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        _repo.FindByIdAsync(faculty.Id, Arg.Any<CancellationToken>()).Returns(faculty);
        _repo.HasAllocationsAsync(faculty.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteFacultyCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new DeleteFacultyCommand(faculty.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Faculty?)null);

        var handler = new DeleteFacultyCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new DeleteFacultyCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class GetFacultiesQueryHandlerTests
{
    private readonly IFacultyRepository _repo = Substitute.For<IFacultyRepository>();

    [Fact]
    public async Task Handle_ReturnsAllFaculties()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Faculty>
        {
            Faculty.Create("Informatica", "INF"),
            Faculty.Create("Matematica", "MAT"),
        });

        var handler = new GetFacultiesQueryHandler(_repo);
        var result = await handler.Handle(new GetFacultiesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}

public class GetFacultyByIdQueryHandlerTests
{
    private readonly IFacultyRepository _repo = Substitute.For<IFacultyRepository>();

    [Fact]
    public async Task Handle_ReturnsFaculty()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        _repo.FindByIdAsync(faculty.Id, Arg.Any<CancellationToken>()).Returns(faculty);

        var handler = new GetFacultyByIdQueryHandler(_repo);
        var result = await handler.Handle(new GetFacultyByIdQuery(faculty.Id), CancellationToken.None);

        result.Id.Should().Be(faculty.Id);
        result.Name.Should().Be("Informatica");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Faculty?)null);

        var handler = new GetFacultyByIdQueryHandler(_repo);
        var act = async () => await handler.Handle(
            new GetFacultyByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
