using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations.EmailHandlers;

public class AllocationExpiredEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<AllocationExpiredEmailHandler> _logger = Substitute.For<ILogger<AllocationExpiredEmailHandler>>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _allocationId = Guid.NewGuid();

    private AllocationExpiredEmailHandler Create() => new(_users, _dorms, _email, _logger);

    [Fact]
    public async Task Handle_AllDataPresent_CallsSendAllocationExpiredAsync()
    {
        var user = User.Create("ana@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        user.SetPrivateProperty("FirstName", "Ana");

        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        dorm.SetPrivateProperty("Id", _dormId);

        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);

        await Create().Handle(
            new AllocationExpiredEvent(_allocationId, _userId, _dormId, _periodId),
            CancellationToken.None);

        await _email.Received(1).SendAllocationExpiredAsync(
            "ana@uaic.ro", "Ana", "C1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DormMissing_DoesNotCallEmail()
    {
        var user = User.Create("ana@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        await Create().Handle(
            new AllocationExpiredEvent(_allocationId, _userId, _dormId, _periodId),
            CancellationToken.None);

        await _email.DidNotReceive().SendAllocationExpiredAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
