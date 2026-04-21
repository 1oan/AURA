using Aura.Application.Common.Interfaces;
using Aura.Application.Dormitories.Commands.CreateDormitory;
using Aura.Application.Dormitories.Commands.DeleteDormitory;
using Aura.Application.Dormitories.Commands.UpdateDormitory;
using Aura.Application.Dormitories.Queries.GetDormitories;
using Aura.Application.Dormitories.Queries.GetDormitoryById;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Dormitories;

public class CreateDormitoryCommandHandlerTests
{
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly ICampusRepository _campuses = Substitute.For<ICampusRepository>();

    [Fact]
    public async Task Handle_CreatesDormitoryInExistingCampus()
    {
        var campus = Campus.Create("Codrescu");
        _campuses.FindByIdAsync(campus.Id, Arg.Any<CancellationToken>()).Returns(campus);

        var handler = new CreateDormitoryCommandHandler(_dorms, _campuses);
        var result = await handler.Handle(
            new CreateDormitoryCommand("C1", campus.Id), CancellationToken.None);

        result.Name.Should().Be("C1");
        result.CampusId.Should().Be(campus.Id);
        await _dorms.Received(1).AddAsync(Arg.Any<Dormitory>(), Arg.Any<CancellationToken>());
        await _dorms.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCampusNotFound_Throws()
    {
        _campuses.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Campus?)null);

        var handler = new CreateDormitoryCommandHandler(_dorms, _campuses);
        var act = async () => await handler.Handle(
            new CreateDormitoryCommand("C1", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class UpdateDormitoryCommandHandlerTests
{
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();

    [Fact]
    public async Task Handle_UpdatesExistingDormitory()
    {
        var dorm = Dormitory.Create("Old", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);

        var handler = new UpdateDormitoryCommandHandler(_dorms);
        await handler.Handle(new UpdateDormitoryCommand(dorm.Id, "New"), CancellationToken.None);

        dorm.Name.Should().Be("New");
        await _dorms.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _dorms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        var handler = new UpdateDormitoryCommandHandler(_dorms);
        var act = async () => await handler.Handle(
            new UpdateDormitoryCommand(Guid.NewGuid(), "X"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DeleteDormitoryCommandHandlerTests
{
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();

    [Fact]
    public async Task Handle_DeletesEmptyDormitory()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _dorms.HasRoomsAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteDormitoryCommandHandler(_dorms);
        await handler.Handle(new DeleteDormitoryCommand(dorm.Id), CancellationToken.None);

        _dorms.Received(1).Remove(dorm);
        await _dorms.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHasRooms_ThrowsConflict()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _dorms.HasRoomsAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteDormitoryCommandHandler(_dorms);
        var act = async () => await handler.Handle(
            new DeleteDormitoryCommand(dorm.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _dorms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        var handler = new DeleteDormitoryCommandHandler(_dorms);
        var act = async () => await handler.Handle(
            new DeleteDormitoryCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class GetDormitoriesQueryHandlerTests
{
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();

    [Fact]
    public async Task Handle_ReturnsDormitoriesInCampus()
    {
        var campusId = Guid.NewGuid();
        _dorms.GetByCampusIdAsync(campusId, Arg.Any<CancellationToken>()).Returns(new List<Dormitory>
        {
            Dormitory.Create("C1", campusId),
            Dormitory.Create("C2", campusId),
        });

        var handler = new GetDormitoriesQueryHandler(_dorms);
        var result = await handler.Handle(new GetDormitoriesQuery(campusId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(d => d.Name).Should().Contain(["C1", "C2"]);
    }
}

public class GetDormitoryByIdQueryHandlerTests
{
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();

    [Fact]
    public async Task Handle_ReturnsDormitoryWithRooms()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdWithRoomsAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);

        var handler = new GetDormitoryByIdQueryHandler(_dorms);
        var result = await handler.Handle(new GetDormitoryByIdQuery(dorm.Id), CancellationToken.None);

        result.Id.Should().Be(dorm.Id);
        result.Rooms.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _dorms.FindByIdWithRoomsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        var handler = new GetDormitoryByIdQueryHandler(_dorms);
        var act = async () => await handler.Handle(
            new GetDormitoryByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
