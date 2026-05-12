using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Events;
using Aura.Domain.Entities;
using Aura.Domain.Events;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Events;

public class GroupLockedEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<GroupLockedEmailHandler> _logger = Substitute.For<ILogger<GroupLockedEmailHandler>>();

    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();

    private GroupLockedEmailHandler Create() => new(_users, _dorms, _email, _logger);

    [Fact]
    public async Task Handle_AllDataPresent_SendsEmailToEachMember()
    {
        var memberAId = Guid.NewGuid();
        var memberBId = Guid.NewGuid();

        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        dorm.SetPrivateProperty("Id", _dormId);

        var userA = User.Create("a@uaic.ro", "hash");
        userA.SetPrivateProperty("Id", memberAId);
        userA.SetPrivateProperty("FirstName", "Ana");

        var userB = User.Create("b@uaic.ro", "hash");
        userB.SetPrivateProperty("Id", memberBId);
        userB.SetPrivateProperty("FirstName", "Bogdan");

        var memberIds = new[] { memberAId, memberBId };

        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);
        _users.GetByIdsAsync(Arg.Is<List<Guid>>(l => l.Count == 2), Arg.Any<CancellationToken>())
            .Returns(new List<User> { userA, userB });

        await Create().Handle(
            new GroupLockedEvent(_groupId, _periodId, _dormId, _leaderId, 2, memberIds),
            CancellationToken.None);

        await _email.Received(2).SendGroupLockedAsync(
            Arg.Any<string>(), Arg.Any<string>(), "C1",
            Arg.Is<string[]>(arr => arr.Length == 2 && arr.Contains("Ana") && arr.Contains("Bogdan")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DormMissing_DoesNotSend()
    {
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns((Dormitory?)null);
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        await Create().Handle(
            new GroupLockedEvent(_groupId, _periodId, _dormId, _leaderId, 2, new[] { Guid.NewGuid() }),
            CancellationToken.None);

        await _email.DidNotReceive().SendGroupLockedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string[]>(), Arg.Any<CancellationToken>());
    }
}
