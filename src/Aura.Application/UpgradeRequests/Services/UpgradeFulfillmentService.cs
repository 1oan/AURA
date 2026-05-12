using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
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
        Guid dormId, Guid periodId, CancellationToken cancellationToken)
    {
        var requests = await upgradeRequestRepository.GetActiveTargetingDormAsync(dormId, periodId, cancellationToken);
        if (requests.Count == 0) return false;

        var context = await LoadCandidateContextAsync(requests, periodId, cancellationToken);
        var groups = GroupRequestsByFacultyAndGender(requests, context);

        foreach (var group in groups)
        {
            if (await TryFulfillGroupAsync(group, dormId, periodId, context, cancellationToken))
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
            if (await TryFulfillForDormAsync(dormId, periodId, cancellationToken))
                fulfilledCount++;
        }
        return fulfilledCount;
    }

    private async Task<CandidateContext> LoadCandidateContextAsync(
        List<UpgradeRequest> requests, Guid periodId, CancellationToken cancellationToken)
    {
        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var records = await studentRecordRepository.GetByPeriodAndUsersAsync(periodId, userIds, cancellationToken);
        var preferences = await dormPreferenceRepository.GetByPeriodAndUsersAsync(periodId, userIds, cancellationToken);

        return new CandidateContext(
            users.ToDictionary(u => u.Id),
            records.Where(r => r.UserId.HasValue).ToDictionary(r => r.UserId!.Value),
            preferences.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.OrderBy(p => p.Rank).First()));
    }

    private static IEnumerable<IGrouping<(Guid FacultyId, Gender Gender), UpgradeRequest>> GroupRequestsByFacultyAndGender(
        List<UpgradeRequest> requests, CandidateContext context)
    {
        return requests
            .Where(r => context.UsersById.ContainsKey(r.UserId))
            .Where(r => context.UsersById[r.UserId].FacultyId.HasValue && context.UsersById[r.UserId].Gender.HasValue)
            .GroupBy(r =>
            {
                var u = context.UsersById[r.UserId];
                return (FacultyId: u.FacultyId!.Value, Gender: u.Gender!.Value);
            });
    }

    private async Task<bool> TryFulfillGroupAsync(
        IGrouping<(Guid FacultyId, Gender Gender), UpgradeRequest> group,
        Guid dormId, Guid periodId, CandidateContext context, CancellationToken cancellationToken)
    {
        var available = await dormAllocationRepository.GetAvailableCapacityAsync(
            dormId, group.Key.FacultyId, group.Key.Gender, periodId, cancellationToken);
        if (available < 1) return false;

        // Batch-fetch active allocations for this group's candidates — runs once per group,
        // not once per candidate. Placed after the capacity check so we don't hit the DB
        // when the dorm has no spots for this (faculty, gender) bucket.
        var groupUserIds = group.Select(r => r.UserId).ToList();
        var groupActive = await dormAllocationRepository.GetActiveByUsersAndPeriodAsync(groupUserIds, periodId, cancellationToken);
        var activeByUser = groupActive.ToDictionary(a => a.UserId);

        var candidates = BuildCandidates(group, context, activeByUser);
        if (candidates.Count == 0) return false;

        candidates.Sort(CompareCandidates);
        await FulfillWinnerAsync(candidates[0], dormId, periodId, cancellationToken);
        return true;
    }

    private static List<Candidate> BuildCandidates(
        IGrouping<(Guid FacultyId, Gender Gender), UpgradeRequest> group,
        CandidateContext context,
        Dictionary<Guid, DormAllocation> activeByUser)
    {
        var candidates = new List<Candidate>();
        foreach (var req in group)
        {
            var user = context.UsersById[req.UserId];
            if (!context.RecordsByUser.TryGetValue(req.UserId, out var record)) continue;
            if (!context.FirstPrefByUser.TryGetValue(req.UserId, out var firstPref)) continue;
            if (!activeByUser.TryGetValue(req.UserId, out var active)) continue;

            candidates.Add(new Candidate(req, user, record, firstPref, active));
        }
        return candidates;
    }

    private static int CompareCandidates(Candidate a, Candidate b)
    {
        var pointsCmp = b.Record.Points.CompareTo(a.Record.Points);
        if (pointsCmp != 0) return pointsCmp;
        var submissionCmp = a.Pref.CreatedAt.CompareTo(b.Pref.CreatedAt);
        if (submissionCmp != 0) return submissionCmp;
        return string.Compare(a.Record.MatriculationCode, b.Record.MatriculationCode, StringComparison.Ordinal);
    }

    private async Task FulfillWinnerAsync(
        Candidate winner, Guid dormId, Guid periodId, CancellationToken cancellationToken)
    {
        var oldDormId = winner.Active.DormitoryId;
        winner.Active.Replace();

        var newAllocation = DormAllocation.Create(winner.User.Id, dormId, periodId, winner.Active.Round);
        newAllocation.Accept();
        await dormAllocationRepository.AddAsync(newAllocation, cancellationToken);

        winner.Request.Fulfill();

        await dormAllocationRepository.SaveChangesAsync(cancellationToken);
        await upgradeRequestRepository.SaveChangesAsync(cancellationToken);

        // The cascade fires AllocationReplacedEvent for the old dorm — handlers there may chain
        // another fulfillment pass against that newly-freed slot.
        await publisher.Publish(
            new AllocationReplacedEvent(winner.User.Id, oldDormId, dormId, periodId),
            cancellationToken);
    }

    private sealed record CandidateContext(
        Dictionary<Guid, User> UsersById,
        Dictionary<Guid, StudentRecord> RecordsByUser,
        Dictionary<Guid, DormPreference> FirstPrefByUser);

    private sealed record Candidate(
        UpgradeRequest Request,
        User User,
        StudentRecord Record,
        DormPreference Pref,
        DormAllocation Active);
}
