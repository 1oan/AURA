using Aura.Application.Auth.Commands.Register;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly IEmailConfirmationCodeRepository _codes = Substitute.For<IEmailConfirmationCodeRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();

    private RegisterUserCommandHandler CreateHandler() =>
        new(_users, _passwordHasher, _jwt, _codes, _email);

    [Fact]
    public async Task Handle_WithNewEmail_CreatesUserAndSendsConfirmationCode()
    {
        _users.ExistsByEmailAsync("new@uaic.ro", Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.HashPassword("password123").Returns("hashed");
        _jwt.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        var result = await CreateHandler().Handle(
            new RegisterUserCommand("new@uaic.ro", "password123"), CancellationToken.None);

        result.Token.Should().Be("jwt-token");
        result.Email.Should().Be("new@uaic.ro");
        result.Role.Should().Be(UserRole.Student.ToString());
        result.IsEmailConfirmed.Should().BeFalse();

        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _codes.Received(1).AddAsync(Arg.Any<EmailConfirmationCode>(), Arg.Any<CancellationToken>());
        await _email.Received(1).SendEmailAsync(
            "new@uaic.ro", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ThrowsDomainException()
    {
        _users.ExistsByEmailAsync("existing@uaic.ro", Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new RegisterUserCommand("existing@uaic.ro", "password123"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("A user with this email already exists.");
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeExistsCheck()
    {
        _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed");
        _jwt.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        await CreateHandler().Handle(
            new RegisterUserCommand("  User@UAIC.RO  ", "password123"), CancellationToken.None);

        await _users.Received(1).ExistsByEmailAsync(
            "user@uaic.ro", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailSendFails_StillCompletesRegistration()
    {
        _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed");
        _jwt.GenerateToken(Arg.Any<User>()).Returns("jwt-token");
        _email.SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP down")));

        var result = await CreateHandler().Handle(
            new RegisterUserCommand("new@uaic.ro", "password123"), CancellationToken.None);

        result.Token.Should().Be("jwt-token");
        // user was still created even though email failed
        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GeneratesTokenFromCreatedUser()
    {
        _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.HashPassword("password123").Returns("bcrypt-hash");
        _jwt.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        await CreateHandler().Handle(
            new RegisterUserCommand("new@uaic.ro", "password123"), CancellationToken.None);

        _jwt.Received(1).GenerateToken(Arg.Is<User>(u =>
            u.Email == "new@uaic.ro" &&
            u.PasswordHash == "bcrypt-hash" &&
            u.Role == UserRole.Student));
    }
}
