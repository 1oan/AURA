using Aura.Application.Campuses.Commands.CreateCampus;
using Aura.Application.Campuses.Commands.DeleteCampus;
using Aura.Application.Campuses.Commands.UpdateCampus;
using Aura.Application.Campuses.Queries.GetCampusById;
using Aura.Application.Campuses.Queries.GetCampuses;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Campuses;

public class CreateCampusCommandHandlerTests
{
    private readonly ICampusRepository _repo = Substitute.For<ICampusRepository>();

    [Fact]
    public async Task Handle_AddsAndSavesCampus_ReturnsDto()
    {
        var handler = new CreateCampusCommandHandler(_repo);

        var result = await handler.Handle(
            new CreateCampusCommand("Codrescu", "Str. Codrescu"), CancellationToken.None);

        result.Name.Should().Be("Codrescu");
        result.Address.Should().Be("Str. Codrescu");
        await _repo.Received(1).AddAsync(Arg.Any<Campus>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UpdateCampusCommandHandlerTests
{
    private readonly ICampusRepository _repo = Substitute.For<ICampusRepository>();

    [Fact]
    public async Task Handle_UpdatesExistingCampus()
    {
        var existing = Campus.Create("Old", "Old Addr");
        var handler = new UpdateCampusCommandHandler(_repo);
        _repo.FindByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        await handler.Handle(new UpdateCampusCommand(existing.Id, "New", "New Addr"), CancellationToken.None);

        existing.Name.Should().Be("New");
        existing.Address.Should().Be("New Addr");
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        var handler = new UpdateCampusCommandHandler(_repo);
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Campus?)null);

        var act = async () => await handler.Handle(
            new UpdateCampusCommand(Guid.NewGuid(), "X", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DeleteCampusCommandHandlerTests
{
    private readonly ICampusRepository _repo = Substitute.For<ICampusRepository>();

    [Fact]
    public async Task Handle_DeletesEmptyCampus()
    {
        var existing = Campus.Create("Codrescu");
        var handler = new DeleteCampusCommandHandler(_repo);
        _repo.FindByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _repo.HasDormitoriesAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(false);

        await handler.Handle(new DeleteCampusCommand(existing.Id), CancellationToken.None);

        _repo.Received(1).Remove(existing);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHasDormitories_ThrowsConflict()
    {
        var existing = Campus.Create("Codrescu");
        var handler = new DeleteCampusCommandHandler(_repo);
        _repo.FindByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _repo.HasDormitoriesAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await handler.Handle(
            new DeleteCampusCommand(existing.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        var handler = new DeleteCampusCommandHandler(_repo);
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Campus?)null);

        var act = async () => await handler.Handle(
            new DeleteCampusCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class GetCampusesQueryHandlerTests
{
    private readonly ICampusRepository _repo = Substitute.For<ICampusRepository>();

    [Fact]
    public async Task Handle_ReturnsAllCampusesAsDtos()
    {
        var campuses = new List<Campus>
        {
            Campus.Create("Codrescu"),
            Campus.Create("Targusor", "Str. Targusor"),
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(campuses);

        var handler = new GetCampusesQueryHandler(_repo);
        var result = await handler.Handle(new GetCampusesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Codrescu");
        result[1].Address.Should().Be("Str. Targusor");
    }

    [Fact]
    public async Task Handle_WhenEmpty_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetCampusesQueryHandler(_repo);
        var result = await handler.Handle(new GetCampusesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetCampusByIdQueryHandlerTests
{
    private readonly ICampusRepository _repo = Substitute.For<ICampusRepository>();

    [Fact]
    public async Task Handle_ReturnsCampusWithDormitories()
    {
        var campus = Campus.Create("Codrescu");
        _repo.FindByIdWithDormitoriesAsync(campus.Id, Arg.Any<CancellationToken>()).Returns(campus);

        var handler = new GetCampusByIdQueryHandler(_repo);
        var result = await handler.Handle(new GetCampusByIdQuery(campus.Id), CancellationToken.None);

        result.Id.Should().Be(campus.Id);
        result.Name.Should().Be("Codrescu");
        result.Dormitories.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdWithDormitoriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Campus?)null);

        var handler = new GetCampusByIdQueryHandler(_repo);
        var act = async () => await handler.Handle(
            new GetCampusByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
