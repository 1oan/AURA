using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Rooms.Commands.DeleteRoom;

public record DeleteRoomCommand(Guid Id) : IRequest<Unit>;

public class DeleteRoomCommandHandler : IRequestHandler<DeleteRoomCommand, Unit>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IFacultyRoomAllocationRepository _allocationRepository;

    public DeleteRoomCommandHandler(
        IRoomRepository roomRepository,
        IFacultyRoomAllocationRepository allocationRepository)
    {
        _roomRepository = roomRepository;
        _allocationRepository = allocationRepository;
    }

    public async Task<Unit> Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Room with id '{request.Id}' was not found.");

        if (await _allocationRepository.AnyByRoomIdAsync(request.Id, cancellationToken))
            throw new DomainException("Cannot delete room that has faculty allocations.");

        _roomRepository.Remove(room);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
