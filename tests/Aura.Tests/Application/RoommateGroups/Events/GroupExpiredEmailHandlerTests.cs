using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Events;
using Aura.Domain.Entities;
using Aura.Domain.Events;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Events;

public class GroupExpiredEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<GroupExpiredEmailHandler> _logger = Substitute.For<ILogger<GroupExpiredEmailHandler>>();

    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private GroupExpiredEmailHandler Create() => new(_users, _dorms, _email, _logger);

    [Fact]
    public async Task Handle_AllDataPresent_SendsEmailToEachMember()
    {
        var memberId = Guid.NewGuid();
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        dorm.SetPrivateProperty("Id", _dormId);
        var user = User.Create("a@uaic.ro", "hash");
        user.SetPrivateProperty("Id", memberId);
        user.SetPrivateProperty("FirstName", "Ana");

        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        await Create().Handle(
            new GroupExpiredEvent(_groupId, _periodId, _dormId, new[] { memberId }),
            CancellationToken.None);

        await _email.Received(1).SendGroupExpiredAsync(
            "a@uaic.ro", "Ana", "C1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsersEmpty_DoesNotSend()
    {
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>())
            .Returns(Dormitory.Create("C1", Guid.NewGuid()));
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        await Create().Handle(
            new GroupExpiredEvent(_groupId, _periodId, _dormId, new[] { Guid.NewGuid() }),
            CancellationToken.None);

        await _email.DidNotReceive().SendGroupExpiredAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
