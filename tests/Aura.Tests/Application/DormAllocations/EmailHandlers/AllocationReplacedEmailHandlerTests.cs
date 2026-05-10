using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations.EmailHandlers;

public class AllocationReplacedEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<AllocationReplacedEmailHandler> _logger = Substitute.For<ILogger<AllocationReplacedEmailHandler>>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _oldDormId = Guid.NewGuid();
    private readonly Guid _newDormId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    private AllocationReplacedEmailHandler Create() => new(_users, _dorms, _email, _logger);

    [Fact]
    public async Task Handle_AllDataPresent_CallsSendAllocationUpgradedAsync()
    {
        var user = User.Create("ana@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        user.SetPrivateProperty("FirstName", "Ana");

        var oldDorm = Dormitory.Create("C1", Guid.NewGuid());
        oldDorm.SetPrivateProperty("Id", _oldDormId);

        var campus = Campus.Create("Tudor");
        campus.SetPrivateProperty("Id", Guid.NewGuid());
        var newDorm = Dormitory.Create("C2", campus.Id);
        newDorm.SetPrivateProperty("Id", _newDormId);
        newDorm.SetPrivateProperty("Campus", campus);

        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _dorms.FindByIdAsync(_oldDormId, Arg.Any<CancellationToken>()).Returns(oldDorm);
        _dorms.FindByIdWithCampusAsync(_newDormId, Arg.Any<CancellationToken>()).Returns(newDorm);

        await Create().Handle(
            new AllocationReplacedEvent(_userId, _oldDormId, _newDormId, _periodId),
            CancellationToken.None);

        await _email.Received(1).SendAllocationUpgradedAsync(
            "ana@uaic.ro", "Ana", "C1", "C2", "Tudor", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewDormCampusMissing_DoesNotCallEmail()
    {
        var user = User.Create("ana@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _dorms.FindByIdAsync(_oldDormId, Arg.Any<CancellationToken>()).Returns((Dormitory?)null);
        _dorms.FindByIdWithCampusAsync(_newDormId, Arg.Any<CancellationToken>()).Returns((Dormitory?)null);

        await Create().Handle(
            new AllocationReplacedEvent(_userId, _oldDormId, _newDormId, _periodId),
            CancellationToken.None);

        await _email.DidNotReceive().SendAllocationUpgradedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
