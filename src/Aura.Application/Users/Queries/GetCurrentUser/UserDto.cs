namespace Aura.Application.Users.Queries.GetCurrentUser;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime CreatedAt,
    string? MatriculationCode);
