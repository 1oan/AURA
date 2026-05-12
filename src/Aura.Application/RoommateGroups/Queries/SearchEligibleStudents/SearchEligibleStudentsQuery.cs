using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Queries.SearchEligibleStudents;

public record SearchEligibleStudentsQuery(string Query, Guid PeriodId) : IRequest<List<EligibleStudentDto>>;

public class SearchEligibleStudentsQueryHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IDormAllocationRepository allocationRepository,
    IRoommateGroupRepository groupRepository) : IRequestHandler<SearchEligibleStudentsQuery, List<EligibleStudentDto>>
{
    public async Task<List<EligibleStudentDto>> Handle(SearchEligibleStudentsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length < 2)
            return [];

        var userId = currentUser.GetCurrentUserId();
        var caller = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Current user not found.");
        var callerAllocation = await allocationRepository.FindActiveByUserAndPeriodAsync(userId, request.PeriodId, cancellationToken)
            ?? throw new DomainException("You don't have an active allocation in this period.");

        var matches = await userRepository.SearchByNameForLobbyAsync(
            request.Query.Trim(),
            request.PeriodId,
            callerAllocation.DormitoryId,
            caller.Gender!.Value,
            excludeUserId: userId,
            limit: 20,
            cancellationToken);

        var result = new List<EligibleStudentDto>();
        foreach (var match in matches)
        {
            var inGroup = await groupRepository.FindActiveByUserAndPeriodAsync(match.Id, request.PeriodId, cancellationToken);
            if (inGroup is not null) continue;
            result.Add(new EligibleStudentDto(match.Id, match.FirstName, match.LastName, match.MatriculationCode ?? ""));
        }
        return result;
    }
}
