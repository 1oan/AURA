using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Dormitories.Commands.UpdateDormitory;

public record UpdateDormitoryCommand(Guid Id, string Name) : IRequest<Unit>;

public class UpdateDormitoryCommandHandler : IRequestHandler<UpdateDormitoryCommand, Unit>
{
    private readonly IDormitoryRepository _dormitoryRepository;

    public UpdateDormitoryCommandHandler(IDormitoryRepository dormitoryRepository)
    {
        _dormitoryRepository = dormitoryRepository;
    }

    public async Task<Unit> Handle(UpdateDormitoryCommand request, CancellationToken cancellationToken)
    {
        var dormitory = await _dormitoryRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Dormitory with id '{request.Id}' was not found.");

        dormitory.Update(request.Name);

        await _dormitoryRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
