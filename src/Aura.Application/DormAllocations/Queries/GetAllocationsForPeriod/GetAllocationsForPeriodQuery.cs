using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Common;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormAllocations.Queries.GetAllocationsForPeriod;

public record GetAllocationsForPeriodQuery(Guid AllocationPeriodId) : IRequest<List<DormAllocationDto>>;

public class GetAllocationsForPeriodQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IDormAllocationRepository dormAllocationRepository) : IRequestHandler<GetAllocationsForPeriodQuery, List<DormAllocationDto>>
{
    public async Task<List<DormAllocationDto>> Handle(GetAllocationsForPeriodQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var allocations = user.Role switch
        {
            UserRole.SuperAdmin => await dormAllocationRepository.GetByPeriodAsync(
                request.AllocationPeriodId, cancellationToken),
            UserRole.FacultyAdmin when user.FacultyId is not null => await dormAllocationRepository.GetByPeriodAndFacultyAsync(
                request.AllocationPeriodId, user.FacultyId.Value, cancellationToken),
            UserRole.FacultyAdmin => throw new DomainException("Faculty admin must be assigned to a faculty."),
            _ => throw new DomainException("Only administrators can list allocations for a period."),
        };

        return allocations.Select(a => new DormAllocationDto(
            a.Id,
            a.UserId,
            a.User?.FirstName,
            a.User?.LastName,
            null,
            a.DormitoryId,
            a.Dormitory!.Name,
            a.Dormitory.Campus!.Name,
            a.AllocationPeriodId,
            a.Round,
            a.Status.ToString(),
            a.AllocatedAt,
            a.RespondedAt)).ToList();
    }
}
