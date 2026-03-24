using Aura.Application.Common.Interfaces;
using Aura.Application.Rooms.Common;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Rooms.Commands.CreateRoom;

public record CreateRoomCommand(string Number, Guid DormitoryId, int Floor, int Capacity, string Gender) : IRequest<RoomDto>;

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, RoomDto>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IDormitoryRepository _dormitoryRepository;

    public CreateRoomCommandHandler(IRoomRepository roomRepository, IDormitoryRepository dormitoryRepository)
    {
        _roomRepository = roomRepository;
        _dormitoryRepository = dormitoryRepository;
    }

    public async Task<RoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var dormitory = await _dormitoryRepository.FindByIdAsync(request.DormitoryId, cancellationToken)
            ?? throw new NotFoundException($"Dormitory with id '{request.DormitoryId}' was not found.");

        if (!Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var gender))
            throw new DomainException($"Invalid gender value '{request.Gender}'. Valid values are: Male, Female.");

        if (await _roomRepository.ExistsByNumberInDormitoryAsync(request.DormitoryId, request.Number, cancellationToken))
            throw new DomainException($"A room with number '{request.Number}' already exists in this dormitory.");

        var room = Room.Create(request.Number, request.DormitoryId, request.Floor, request.Capacity, gender);

        await _roomRepository.AddAsync(room, cancellationToken);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return new RoomDto(room.Id, room.Number, room.DormitoryId, room.Floor, room.Capacity, room.Gender.ToString());
    }
}
