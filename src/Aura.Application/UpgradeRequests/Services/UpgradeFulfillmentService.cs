using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using MediatR;

namespace Aura.Application.UpgradeRequests.Services;

public class UpgradeFulfillmentService(
    IDormAllocationRepository dormAllocationRepository,
    IUpgradeRequestRepository upgradeRequestRepository,
    IUserRepository userRepository,
    IDormPreferenceRepository dormPreferenceRepository,
    IStudentRecordRepository studentRecordRepository,
    IPublisher publisher) : IUpgradeFulfillmentService
{
    public async Task<bool> TryFulfillForDormAsync(
        Guid freedDormId, Guid periodId, CancellationToken cancellationToken)
    {
        var requests = await upgradeRequestRepository.GetActiveTargetingDormAsync(freedDormId, periodId, cancellationToken);
        if (requests.Count == 0) return false;

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        var records = await studentRecordRepository.GetByPeriodAndUsersAsync(periodId, userIds, cancellationToken);
        var recordsByUser = records.Where(r => r.UserId.HasValue).ToDictionary(r => r.UserId!.Value);

        var preferences = await dormPreferenceRepository.GetByPeriodAndUsersAsync(periodId, userIds, cancellationToken);
        var firstPrefByUser = preferences
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Rank).First());

        var groups = requests
            .Where(r => usersById.ContainsKey(r.UserId))
            .Where(r => usersById[r.UserId].FacultyId.HasValue && usersById[r.UserId].Gender.HasValue)
            .GroupBy(r =>
            {
                var u = usersById[r.UserId];
                return (FacultyId: u.FacultyId!.Value, Gender: u.Gender!.Value);
            });

        foreach (var group in groups)
        {
            var available = await dormAllocationRepository.GetAvailableCapacityAsync(
                freedDormId, group.Key.FacultyId, group.Key.Gender, periodId, cancellationToken);
            if (available < 1) continue;

            var candidates = new List<(UpgradeRequest Request, User User, StudentRecord Record, DormPreference Pref, DormAllocation Active)>();
            foreach (var req in group)
            {
                var user = usersById[req.UserId];
                if (!recordsByUser.TryGetValue(req.UserId, out var record)) continue;
                if (!firstPrefByUser.TryGetValue(req.UserId, out var firstPref)) continue;

                var active = await dormAllocationRepository.FindActiveByUserAndPeriodAsync(req.UserId, periodId, cancellationToken);
                if (active is null) continue;

                candidates.Add((req, user, record, firstPref, active));
            }

            if (candidates.Count == 0) continue;

            candidates.Sort((a, b) =>
            {
                var pointsCmp = b.Record.Points.CompareTo(a.Record.Points);
                if (pointsCmp != 0) return pointsCmp;
                var submissionCmp = a.Pref.CreatedAt.CompareTo(b.Pref.CreatedAt);
                if (submissionCmp != 0) return submissionCmp;
                return string.Compare(a.Record.MatriculationCode, b.Record.MatriculationCode, StringComparison.Ordinal);
            });

            var winner = candidates[0];
            var oldDormId = winner.Active.DormitoryId;

            winner.Active.Replace();

            var newAllocation = DormAllocation.Create(
                winner.User.Id, freedDormId, periodId, winner.Active.Round);
            newAllocation.Accept();
            await dormAllocationRepository.AddAsync(newAllocation, cancellationToken);

            winner.Request.Fulfill();

            await dormAllocationRepository.SaveChangesAsync(cancellationToken);
            await upgradeRequestRepository.SaveChangesAsync(cancellationToken);

            await publisher.Publish(
                new AllocationReplacedEvent(winner.User.Id, oldDormId, freedDormId, periodId),
                cancellationToken);

            // Only fulfill ONE per call — the cascade fires AllocationReplacedEvent for the old dorm.
            return true;
        }
        return false;
    }

    public async Task<int> SweepActiveTargetsAsync(
        Guid periodId, CancellationToken cancellationToken)
    {
        var requests = await upgradeRequestRepository.GetActiveForPeriodAsync(periodId, cancellationToken);
        if (requests.Count == 0) return 0;

        var targetDormIds = requests
            .SelectMany(r => r.Targets.Select(t => t.DormitoryId))
            .Distinct()
            .ToList();

        var fulfilledCount = 0;
        foreach (var dormId in targetDormIds)
        {
            var fulfilled = await TryFulfillForDormAsync(dormId, periodId, cancellationToken);
            if (fulfilled) fulfilledCount++;
        }
        return fulfilledCount;
    }
}
