using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
namespace BusinessManagement.IntegrationTests.Helpers;

public static class TestTokens
{
    public const string Issuer = "test-issuer";
    public const string Audience = "test-audience";
    public const string Key = "test-key-test-key-test-key-test-key-123456";

    public static string Create(string role = "Staff", int minutes = 30, string key = Key, string audience = Audience)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("role", role)
            }),
            NotBefore = now.AddMinutes(-60),
            Expires = now.AddMinutes(minutes),                     // minutes < 0: token đã hết hạn
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}