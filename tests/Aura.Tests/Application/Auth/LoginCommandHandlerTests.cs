using Aura.Application.Auth.Commands.Login;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();

    private LoginCommandHandler CreateHandler() => new(_users, _passwordHasher, _jwt);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenAndRecordsLogin()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "stored-hash");

        _users.FindByEmailAsync("ioan.virlescu@student.uaic.ro", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("password123", "stored-hash").Returns(true);
        _jwt.GenerateToken(user).Returns("jwt-token");

        var result = await CreateHandler().Handle(
            new LoginCommand("ioan.virlescu@student.uaic.ro", "password123"), CancellationToken.None);

        result.Token.Should().Be("jwt-token");
        result.Email.Should().Be("ioan.virlescu@student.uaic.ro");
        user.LastLoginAt.Should().NotBeNull();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsDomainException()
    {
        _users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("missing@uaic.ro", "password123"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsDomainException()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "stored-hash");

        _users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("wrong", "stored-hash").Returns(false);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("ioan.virlescu@student.uaic.ro", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_LowercasesEmailBeforeLookup()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "stored-hash");

        _users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwt.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        await CreateHandler().Handle(
            new LoginCommand("Ioan.Virlescu@Student.UAIC.RO", "password123"), CancellationToken.None);

        await _users.Received(1).FindByEmailAsync(
            "ioan.virlescu@student.uaic.ro", Arg.Any<CancellationToken>());
    }
}
