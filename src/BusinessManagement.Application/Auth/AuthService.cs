using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Common;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagement.Application.Auth;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
        => (_db, _hasher, _tokens) = (db, hasher, tokens);

    public async Task<UserDto> RegisterAsync(string email, string password, CancellationToken ct)
    {
        var normalized = User.NormalizeEmail(email);
        if (await _db.Users.AnyAsync(u => u.Email == normalized, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User(normalized, _hasher.Hash(password), UserRole.Staff);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Email, user.Role.ToString());
    }

    public async Task<TokenResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized, ct);

        var passwordOk = _hasher.Verify(user?.PasswordHash ?? _hasher.DummyHash, password);   // luôn chạy một lần băm
        if (user is null || !passwordOk || !user.IsActive)
            throw new AuthenticationFailedException("Invalid email or password.");          // cùng một thông báo cho mọi lý do

        return _tokens.Create(user);
    }
}