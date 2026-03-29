using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Dormitories.Commands.DeleteDormitory;

public record DeleteDormitoryCommand(Guid Id) : IRequest<Unit>;

public class DeleteDormitoryCommandHandler : IRequestHandler<DeleteDormitoryCommand, Unit>
{
    private readonly IDormitoryRepository _dormitoryRepository;

    public DeleteDormitoryCommandHandler(IDormitoryRepository dormitoryRepository)
    {
        _dormitoryRepository = dormitoryRepository;
    }

    public async Task<Unit> Handle(DeleteDormitoryCommand request, CancellationToken cancellationToken)
    {
        var dormitory = await _dormitoryRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Dormitory with id '{request.Id}' was not found.");

        if (await _dormitoryRepository.HasRoomsAsync(request.Id, cancellationToken))
            throw new ConflictException("Cannot delete dormitory that has rooms.");

        _dormitoryRepository.Remove(dormitory);
        await _dormitoryRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
