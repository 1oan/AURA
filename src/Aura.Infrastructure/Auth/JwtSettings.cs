namespace Aura.Infrastructure.Auth;

public class JwtSettings
{
    public string Secret { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public int ExpiryInHours { get; init; } = 24;
}
