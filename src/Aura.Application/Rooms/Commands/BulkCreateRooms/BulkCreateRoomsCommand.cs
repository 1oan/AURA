using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Rooms.Commands.BulkCreateRooms;

public record FloorConfiguration(int FloorNumber, int RoomCount, int Capacity, string Gender);

public record BulkCreateRoomsCommand(Guid DormitoryId, List<FloorConfiguration> Floors) : IRequest<int>;

public class BulkCreateRoomsCommandHandler : IRequestHandler<BulkCreateRoomsCommand, int>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IDormitoryRepository _dormitoryRepository;

    public BulkCreateRoomsCommandHandler(IRoomRepository roomRepository, IDormitoryRepository dormitoryRepository)
    {
        _roomRepository = roomRepository;
        _dormitoryRepository = dormitoryRepository;
    }

    public async Task<int> Handle(BulkCreateRoomsCommand request, CancellationToken cancellationToken)
    {
        _ = await _dormitoryRepository.FindByIdAsync(request.DormitoryId, cancellationToken)
            ?? throw new NotFoundException($"Dormitory with id '{request.DormitoryId}' was not found.");

        var existingRooms = await _roomRepository.GetByDormitoryIdAsync(request.DormitoryId, cancellationToken);
        var existingNumbers = existingRooms.Select(r => r.Number).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newRooms = new List<Room>();
        var generatedNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var floor in request.Floors)
        {
            if (!Enum.TryParse<Gender>(floor.Gender, ignoreCase: true, out var gender))
                throw new DomainException($"Invalid gender value '{floor.Gender}' for floor {floor.FloorNumber}. Valid values are: Male, Female.");

            for (var sequence = 1; sequence <= floor.RoomCount; sequence++)
            {
                // Ground floor rooms use just the sequence number; upper floors prefix with floor * 100
                var number = floor.FloorNumber == 0
                    ? sequence.ToString()
                    : (floor.FloorNumber * 100 + sequence).ToString();

                if (existingNumbers.Contains(number))
                    throw new DomainException($"Room number '{number}' already exists in this dormitory.");

                if (!generatedNumbers.Add(number))
                    throw new DomainException($"Duplicate room number '{number}' generated across floor configurations.");

                newRooms.Add(Room.Create(number, request.DormitoryId, floor.FloorNumber, floor.Capacity, gender));
            }
        }

        await _roomRepository.AddRangeAsync(newRooms, cancellationToken);
        await _roomRepository.SaveChangesAsync(cancellationToken);

        return newRooms.Count;
    }
}
