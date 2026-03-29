using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Rooms.Commands.UpdateRoom;

public record UpdateRoomCommand(Guid Id, string Number, int Floor, int Capacity, string Gender) : IRequest<Unit>;

public class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand, Unit>
{
    private readonly IRoomRepository _roomRepository;

    public UpdateRoomCommandHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<Unit> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Room with id '{request.Id}' was not found.");

        if (!Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var gender))
            throw new DomainException($"Invalid gender value '{request.Gender}'. Valid values are: Male, Female.");

        room.Update(request.Number, request.Floor, request.Capacity, gender);

        await _roomRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
