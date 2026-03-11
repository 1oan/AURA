namespace Aura.Application.Auth.Common;

public record AuthResult(
    string Token,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);
