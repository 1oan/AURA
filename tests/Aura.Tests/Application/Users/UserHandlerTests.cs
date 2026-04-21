using Aura.Application.Common.Interfaces;
using Aura.Application.Users.Commands.ChangePassword;
using Aura.Application.Users.Commands.PromoteUser;
using Aura.Application.Users.Commands.SetMatriculationCode;
using Aura.Application.Users.Commands.UpdateProfile;
using Aura.Application.Users.Queries.GetCurrentUser;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Users;

public class GetCurrentUserQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    [Fact]
    public async Task Handle_ReturnsCurrentUserAsDto()
    {
        var user = User.Create("ioan@uaic.ro", "hash");
        user.UpdateProfile("Ioan", "Virlescu");
        user.SetMatriculationCode("CS2024001");
        var userId = user.Id;

        _currentUser.GetCurrentUserId().Returns(userId);
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new GetCurrentUserQueryHandler(_users, _currentUser);
        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Id.Should().Be(userId);
        result.Email.Should().Be("ioan@uaic.ro");
        result.FirstName.Should().Be("Ioan");
        result.MatriculationCode.Should().Be("CS2024001");
        result.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _users.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new GetCurrentUserQueryHandler(_users, _currentUser);
        var act = async () => await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class UpdateProfileCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    [Fact]
    public async Task Handle_UpdatesProfile()
    {
        var user = User.Create("ioan@uaic.ro", "hash");
        _currentUser.GetCurrentUserId().Returns(user.Id);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new UpdateProfileCommandHandler(_users, _currentUser);
        await handler.Handle(new UpdateProfileCommand("New", "Name"), CancellationToken.None);

        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _users.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new UpdateProfileCommandHandler(_users, _currentUser);
        var act = async () => await handler.Handle(
            new UpdateProfileCommand("X", "Y"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class SetMatriculationCodeCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    [Fact]
    public async Task Handle_SetsCode()
    {
        var user = User.Create("ioan@uaic.ro", "hash");
        _currentUser.GetCurrentUserId().Returns(user.Id);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new SetMatriculationCodeCommandHandler(_users, _currentUser);
        await handler.Handle(new SetMatriculationCodeCommand("CS2024001"), CancellationToken.None);

        user.MatriculationCode.Should().Be("CS2024001");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _users.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new SetMatriculationCodeCommandHandler(_users, _currentUser);
        var act = async () => await handler.Handle(
            new SetMatriculationCodeCommand("CS2024001"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class PromoteUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    [Fact]
    public async Task Handle_SetsNewRole()
    {
        var user = User.Create("admin@uaic.ro", "hash");
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new PromoteUserCommandHandler(_users);
        await handler.Handle(new PromoteUserCommand(user.Id, UserRole.FacultyAdmin), CancellationToken.None);

        user.Role.Should().Be(UserRole.FacultyAdmin);
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _users.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new PromoteUserCommandHandler(_users);
        var act = async () => await handler.Handle(
            new PromoteUserCommand(Guid.NewGuid(), UserRole.FacultyAdmin), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task Handle_WithCorrectCurrent_UpdatesHash()
    {
        var user = User.Create("ioan@uaic.ro", "old-hash");
        _currentUser.GetCurrentUserId().Returns(user.Id);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("old-pass", "old-hash").Returns(true);
        _hasher.HashPassword("new-pass").Returns("new-hash");

        var handler = new ChangePasswordCommandHandler(_users, _currentUser, _hasher);
        await handler.Handle(new ChangePasswordCommand("old-pass", "new-pass"), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task Handle_WithWrongCurrent_Throws()
    {
        var user = User.Create("ioan@uaic.ro", "old-hash");
        _currentUser.GetCurrentUserId().Returns(user.Id);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = new ChangePasswordCommandHandler(_users, _currentUser, _hasher);
        var act = async () => await handler.Handle(
            new ChangePasswordCommand("wrong", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _users.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new ChangePasswordCommandHandler(_users, _currentUser, _hasher);
        var act = async () => await handler.Handle(
            new ChangePasswordCommand("x", "y"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
