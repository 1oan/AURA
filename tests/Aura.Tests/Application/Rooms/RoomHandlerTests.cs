using Aura.Application.Common.Interfaces;
using Aura.Application.Rooms.Commands.BulkCreateRooms;
using Aura.Application.Rooms.Commands.CreateRoom;
using Aura.Application.Rooms.Commands.DeleteRoom;
using Aura.Application.Rooms.Commands.UpdateRoom;
using Aura.Application.Rooms.Queries.GetRoomById;
using Aura.Application.Rooms.Queries.GetRooms;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Rooms;

public class CreateRoomCommandHandlerTests
{
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();

    private CreateRoomCommandHandler Create() => new(_rooms, _dorms);

    [Fact]
    public async Task Handle_CreatesRoom()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _rooms.ExistsByNumberInDormitoryAsync(dorm.Id, "101", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Create().Handle(
            new CreateRoomCommand("101", dorm.Id, 1, 3, "Male"), CancellationToken.None);

        result.Number.Should().Be("101");
        result.Gender.Should().Be("Male");
        await _rooms.Received(1).AddAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDormitoryNotFound_Throws()
    {
        _dorms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        var act = async () => await Create().Handle(
            new CreateRoomCommand("101", Guid.NewGuid(), 1, 3, "Male"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithInvalidGender_Throws()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);

        var act = async () => await Create().Handle(
            new CreateRoomCommand("101", dorm.Id, 1, 3, "Alien"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithExistingNumber_Throws()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _rooms.ExistsByNumberInDormitoryAsync(dorm.Id, "101", Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await Create().Handle(
            new CreateRoomCommand("101", dorm.Id, 1, 3, "Male"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class UpdateRoomCommandHandlerTests
{
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();

    [Fact]
    public async Task Handle_UpdatesRoom()
    {
        var room = Room.Create("101", Guid.NewGuid(), 1, 3, Gender.Male);
        _rooms.FindByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);

        var handler = new UpdateRoomCommandHandler(_rooms);
        await handler.Handle(new UpdateRoomCommand(room.Id, "202", 2, 4, "Female"), CancellationToken.None);

        room.Number.Should().Be("202");
        room.Gender.Should().Be(Gender.Female);
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _rooms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);

        var handler = new UpdateRoomCommandHandler(_rooms);
        var act = async () => await handler.Handle(
            new UpdateRoomCommand(Guid.NewGuid(), "101", 1, 3, "Male"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithInvalidGender_Throws()
    {
        var room = Room.Create("101", Guid.NewGuid(), 1, 3, Gender.Male);
        _rooms.FindByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);

        var handler = new UpdateRoomCommandHandler(_rooms);
        var act = async () => await handler.Handle(
            new UpdateRoomCommand(room.Id, "101", 1, 3, "Invalid"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class DeleteRoomCommandHandlerTests
{
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();
    private readonly IFacultyRoomAllocationRepository _allocations = Substitute.For<IFacultyRoomAllocationRepository>();

    [Fact]
    public async Task Handle_DeletesUnallocatedRoom()
    {
        var room = Room.Create("101", Guid.NewGuid(), 1, 3, Gender.Male);
        _rooms.FindByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _allocations.AnyByRoomIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteRoomCommandHandler(_rooms, _allocations);
        await handler.Handle(new DeleteRoomCommand(room.Id), CancellationToken.None);

        _rooms.Received(1).Remove(room);
    }

    [Fact]
    public async Task Handle_WhenAllocated_ThrowsConflict()
    {
        var room = Room.Create("101", Guid.NewGuid(), 1, 3, Gender.Male);
        _rooms.FindByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _allocations.AnyByRoomIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteRoomCommandHandler(_rooms, _allocations);
        var act = async () => await handler.Handle(new DeleteRoomCommand(room.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _rooms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);

        var handler = new DeleteRoomCommandHandler(_rooms, _allocations);
        var act = async () => await handler.Handle(new DeleteRoomCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class BulkCreateRoomsCommandHandlerTests
{
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();

    [Fact]
    public async Task Handle_GeneratesRoomsPerFloor()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _rooms.GetByDormitoryIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new BulkCreateRoomsCommandHandler(_rooms, _dorms);
        var count = await handler.Handle(new BulkCreateRoomsCommand(
            dorm.Id,
            [
                new FloorConfiguration(0, 3, 2, "Male"),
                new FloorConfiguration(1, 5, 3, "Female"),
            ]), CancellationToken.None);

        count.Should().Be(8);
        await _rooms.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Room>>(r => r.Count() == 8), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDormitoryNotFound_Throws()
    {
        _dorms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        var handler = new BulkCreateRoomsCommandHandler(_rooms, _dorms);
        var act = async () => await handler.Handle(
            new BulkCreateRoomsCommand(Guid.NewGuid(), [new FloorConfiguration(0, 1, 2, "Male")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithInvalidGender_Throws()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _rooms.GetByDormitoryIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new BulkCreateRoomsCommandHandler(_rooms, _dorms);
        var act = async () => await handler.Handle(
            new BulkCreateRoomsCommand(dorm.Id, [new FloorConfiguration(0, 1, 2, "X")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithExistingNumber_Throws()
    {
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        _dorms.FindByIdAsync(dorm.Id, Arg.Any<CancellationToken>()).Returns(dorm);
        _rooms.GetByDormitoryIdAsync(dorm.Id, Arg.Any<CancellationToken>())
            .Returns([Room.Create("1", dorm.Id, 0, 2, Gender.Male)]);

        var handler = new BulkCreateRoomsCommandHandler(_rooms, _dorms);
        var act = async () => await handler.Handle(
            new BulkCreateRoomsCommand(dorm.Id, [new FloorConfiguration(0, 3, 2, "Male")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class GetRoomsQueryHandlerTests
{
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();

    [Fact]
    public async Task Handle_ReturnsRooms()
    {
        var dormId = Guid.NewGuid();
        _rooms.GetByDormitoryIdAsync(dormId, Arg.Any<CancellationToken>()).Returns([
            Room.Create("101", dormId, 1, 3, Gender.Male),
            Room.Create("102", dormId, 1, 3, Gender.Female),
        ]);

        var handler = new GetRoomsQueryHandler(_rooms);
        var result = await handler.Handle(new GetRoomsQuery(dormId), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}

public class GetRoomByIdQueryHandlerTests
{
    private readonly IRoomRepository _rooms = Substitute.For<IRoomRepository>();

    [Fact]
    public async Task Handle_ReturnsRoom()
    {
        var room = Room.Create("101", Guid.NewGuid(), 1, 3, Gender.Male);
        _rooms.FindByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);

        var handler = new GetRoomByIdQueryHandler(_rooms);
        var result = await handler.Handle(new GetRoomByIdQuery(room.Id), CancellationToken.None);

        result.Id.Should().Be(room.Id);
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _rooms.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);

        var handler = new GetRoomByIdQueryHandler(_rooms);
        var act = async () => await handler.Handle(new GetRoomByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
