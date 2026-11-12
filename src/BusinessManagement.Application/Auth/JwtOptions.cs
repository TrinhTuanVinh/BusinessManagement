using System.Text;
namespace BusinessManagement.Application.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string Key { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Missing configuration 'Jwt:Issuer' or 'Jwt:Audience'.");
        if (Encoding.UTF8.GetByteCount(Key) < 32)
            throw new InvalidOperationException(
                "'Jwt:Key' must be at least 32 bytes. Set it with user-secrets or the environment variable Jwt__Key.");
        if (AccessTokenMinutes is < 1 or > 1440)
            throw new InvalidOperationException("'Jwt:AccessTokenMinutes' must be between 1 and 1440.");
    }
}