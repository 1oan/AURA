using Aura.Application.Auth.Commands.ConfirmEmail;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Auth;

public class ConfirmEmailCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationCodeRepository _codes = Substitute.For<IEmailConfirmationCodeRepository>();

    private readonly Guid _userId = Guid.NewGuid();

    private ConfirmEmailCommandHandler CreateHandler() => new(_currentUser, _users, _codes);

    private User CreateUnconfirmedUser()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCode_ConfirmsEmailAndMarksCodeUsed()
    {
        var user = CreateUnconfirmedUser();
        var code = EmailConfirmationCode.Create(_userId, "123456");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _codes.FindValidCodeAsync(_userId, "123456", Arg.Any<CancellationToken>()).Returns(code);

        await CreateHandler().Handle(new ConfirmEmailCommand("123456"), CancellationToken.None);

        user.IsEmailConfirmed.Should().BeTrue();
        code.IsUsed.Should().BeTrue();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new ConfirmEmailCommand("123456"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyConfirmed_ThrowsDomainException()
    {
        var user = CreateUnconfirmedUser();
        user.ConfirmEmail();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new ConfirmEmailCommand("123456"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email is already confirmed.");
    }

    [Fact]
    public async Task Handle_WhenCodeInvalidOrExpired_ThrowsDomainException()
    {
        var user = CreateUnconfirmedUser();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _codes.FindValidCodeAsync(_userId, "999999", Arg.Any<CancellationToken>())
            .Returns((EmailConfirmationCode?)null);

        var act = async () => await CreateHandler().Handle(
            new ConfirmEmailCommand("999999"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Invalid or expired confirmation code.");
        user.IsEmailConfirmed.Should().BeFalse();
    }
}
