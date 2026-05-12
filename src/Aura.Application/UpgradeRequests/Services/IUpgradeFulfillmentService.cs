namespace Aura.Application.UpgradeRequests.Services;

public interface IUpgradeFulfillmentService
{
    Task<bool> TryFulfillForDormAsync(
        Guid dormId, Guid periodId, CancellationToken cancellationToken);

    Task<int> SweepActiveTargetsAsync(
        Guid periodId, CancellationToken cancellationToken);
}
