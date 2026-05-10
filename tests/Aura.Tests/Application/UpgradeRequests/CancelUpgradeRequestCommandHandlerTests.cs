using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Commands.CancelUpgradeRequest;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.UpgradeRequests;

public class CancelUpgradeRequestCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUpgradeRequestRepository _upgradeRequests = Substitute.For<IUpgradeRequestRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormA = Guid.NewGuid();

    private CancelUpgradeRequestCommandHandler CreateHandler() =>
        new(_currentUser, _upgradeRequests, _publisher);

    private UpgradeRequest CreateOwnedActiveRequest()
    {
        return UpgradeRequest.Create(_userId, _periodId, new[] { _dormA });
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _currentUser.GetCurrentUserId().Returns(_userId);
        _upgradeRequests.FindByIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns((UpgradeRequest?)null);

        var act = async () => await CreateHandler().Handle(
            new CancelUpgradeRequestCommand(requestId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*not found*");
        await _upgradeRequests.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<UpgradeRequestCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsDomainException()
    {
        var someoneElseId = Guid.NewGuid();
        _currentUser.GetCurrentUserId().Returns(someoneElseId);
        var request = CreateOwnedActiveRequest();
        _upgradeRequests.FindByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);

        var act = async () => await CreateHandler().Handle(
            new CancelUpgradeRequestCommand(request.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*own upgrade request*");
        await _upgradeRequests.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<UpgradeRequestCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ThrowsDomainException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var request = CreateOwnedActiveRequest();
        request.Cancel();
        _upgradeRequests.FindByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);

        var act = async () => await CreateHandler().Handle(
            new CancelUpgradeRequestCommand(request.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no longer active*");
        await _upgradeRequests.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<UpgradeRequestCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_DeactivatesAndPublishesCancelledEvent()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var request = CreateOwnedActiveRequest();
        _upgradeRequests.FindByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);

        await CreateHandler().Handle(
            new CancelUpgradeRequestCommand(request.Id), CancellationToken.None);

        request.IsActive.Should().BeFalse();
        await _publisher.Received(1).Publish(
            Arg.Is<UpgradeRequestCancelledEvent>(e =>
                e.UpgradeRequestId == request.Id
                && e.UserId == _userId
                && e.AllocationPeriodId == _periodId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_RunsSaveChangesBeforePublish()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var request = CreateOwnedActiveRequest();
        _upgradeRequests.FindByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);

        var callOrder = new List<string>();
        _upgradeRequests.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("save"));
        _publisher.Publish(Arg.Any<UpgradeRequestCancelledEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("publish"));

        await CreateHandler().Handle(
            new CancelUpgradeRequestCommand(request.Id), CancellationToken.None);

        callOrder.Should().Equal("save", "publish");
    }
}
