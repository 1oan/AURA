using Aura.Application.Auth.Commands.ResendConfirmation;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Auth;

public class ResendConfirmationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationCodeRepository _codes = Substitute.For<IEmailConfirmationCodeRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();

    private readonly Guid _userId = Guid.NewGuid();

    private ResendConfirmationCommandHandler CreateHandler() =>
        new(_currentUser, _users, _codes, _email);

    private User CreateUnconfirmedUser()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    [Fact]
    public async Task Handle_WithNoPriorCode_CreatesCodeAndSendsEmail()
    {
        var user = CreateUnconfirmedUser();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _codes.GetLatestCodeAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((EmailConfirmationCode?)null);

        await CreateHandler().Handle(new ResendConfirmationCommand(), CancellationToken.None);

        await _codes.Received(1).InvalidateExistingCodesAsync(_userId, Arg.Any<CancellationToken>());
        await _codes.Received(1).AddAsync(
            Arg.Is<EmailConfirmationCode>(c => c.UserId == _userId),
            Arg.Any<CancellationToken>());
        await _codes.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _email.Received(1).SendEmailAsync(
            user.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOldPriorCode_SendsNewCode()
    {
        var user = CreateUnconfirmedUser();
        var oldCode = EmailConfirmationCode.Create(_userId, "111111");
        // backdate created_at to 2 minutes ago so it's past the cooldown
        oldCode.SetPrivateProperty("CreatedAt", DateTime.UtcNow.AddMinutes(-2));

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _codes.GetLatestCodeAsync(_userId, Arg.Any<CancellationToken>()).Returns(oldCode);

        await CreateHandler().Handle(new ResendConfirmationCommand(), CancellationToken.None);

        await _codes.Received(1).AddAsync(
            Arg.Any<EmailConfirmationCode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithinCooldown_ThrowsDomainException()
    {
        var user = CreateUnconfirmedUser();
        var recentCode = EmailConfirmationCode.Create(_userId, "111111");
        // CreatedAt = now (inside the 60s cooldown)

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _codes.GetLatestCodeAsync(_userId, Arg.Any<CancellationToken>()).Returns(recentCode);

        var act = async () => await CreateHandler().Handle(
            new ResendConfirmationCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Please wait before requesting a new code.");
        await _email.DidNotReceive().SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new ResendConfirmationCommand(), CancellationToken.None);

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
            new ResendConfirmationCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Email is already confirmed.");
    }
}
