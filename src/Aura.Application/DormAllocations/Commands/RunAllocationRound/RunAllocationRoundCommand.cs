using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormAllocations.Commands.RunAllocationRound;

public record RunAllocationRoundCommand(Guid AllocationPeriodId, int Round) : IRequest;

public class RunAllocationRoundCommandHandler(
    IAllocationPeriodRepository allocationPeriodRepository,
    IDormAllocationRepository dormAllocationRepository,
    IDormPreferenceRepository dormPreferenceRepository,
    IStudentRecordRepository studentRecordRepository,
    IUserRepository userRepository,
    IPublisher publisher) : IRequestHandler<RunAllocationRoundCommand>
{
    public async Task Handle(RunAllocationRoundCommand request, CancellationToken cancellationToken)
    {
        var period = await allocationPeriodRepository.FindByIdAsync(request.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Allocating)
            throw new DomainException("Allocation can only run while the period is in Allocating state.");

        var records = await studentRecordRepository.GetByPeriodAsync(request.AllocationPeriodId, cancellationToken);
        if (records.Count == 0)
            return;

        var userIds = records
            .Where(r => r.UserId.HasValue)
            .Select(r => r.UserId!.Value)
            .Distinct()
            .ToList();
        if (userIds.Count == 0)
            return;

        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        var preferences = await dormPreferenceRepository.GetByPeriodAndUsersAsync(
            request.AllocationPeriodId, userIds, cancellationToken);
        var preferencesByUser = preferences
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Rank).ToList());

        var pool = new List<(StudentRecord Record, User User, List<DormPreference> Prefs)>();

        foreach (var record in records)
        {
            if (record.UserId is null) continue;
            if (!usersById.TryGetValue(record.UserId.Value, out var user)) continue;
            if (!preferencesByUser.TryGetValue(record.UserId.Value, out var prefs) || prefs.Count == 0) continue;
            if (user.FacultyId is null || user.Gender is null) continue;

            var existing = await dormAllocationRepository.FindActiveByUserAndPeriodAsync(
                record.UserId.Value, request.AllocationPeriodId, cancellationToken);
            if (existing is not null) continue;

            var anyTerminal = await dormAllocationRepository.HasTerminalForUserAndPeriodAsync(
                record.UserId.Value, request.AllocationPeriodId, cancellationToken);
            if (anyTerminal) continue;

            pool.Add((record, user, prefs));
        }

        pool.Sort((a, b) =>
        {
            var pointsCmp = b.Record.Points.CompareTo(a.Record.Points);
            if (pointsCmp != 0) return pointsCmp;
            var submissionCmp = a.Prefs[0].CreatedAt.CompareTo(b.Prefs[0].CreatedAt);
            if (submissionCmp != 0) return submissionCmp;
            return string.Compare(a.Record.MatriculationCode, b.Record.MatriculationCode, StringComparison.Ordinal);
        });

        foreach (var (record, user, prefs) in pool)
        {
            foreach (var pref in prefs)
            {
                var available = await dormAllocationRepository.GetAvailableCapacityAsync(
                    pref.DormitoryId, user.FacultyId!.Value, user.Gender!.Value,
                    request.AllocationPeriodId, cancellationToken);
                if (available < 1) continue;

                var allocation = DormAllocation.Create(
                    user.Id, pref.DormitoryId, request.AllocationPeriodId, request.Round);
                await dormAllocationRepository.AddAsync(allocation, cancellationToken);
                await dormAllocationRepository.SaveChangesAsync(cancellationToken);

                await publisher.Publish(new AllocationCreatedEvent(
                    allocation.Id, user.Id, pref.DormitoryId, request.AllocationPeriodId, request.Round),
                    cancellationToken);
                break;
            }
        }
    }
}
