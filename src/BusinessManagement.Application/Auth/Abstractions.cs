using BusinessManagement.Domain.Entities;
namespace BusinessManagement.Application.Auth;

public record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
public record UserDto(Guid Id, string Email, string Role);

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
    string DummyHash { get; }
}

public interface ITokenService
{
    TokenResult Create(User user);
}