using System.Security.Claims;
using System.Text;
using BusinessManagement.Application.Auth;
using BusinessManagement.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256);
    }

    public TokenResult Create(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString())
            }),
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials
        };
        return new TokenResult(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}