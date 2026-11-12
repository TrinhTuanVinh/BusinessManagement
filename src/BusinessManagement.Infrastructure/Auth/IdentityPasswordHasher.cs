using BusinessManagement.Application.Auth;
using Microsoft.AspNetCore.Identity;

public sealed class IdentityPasswordHasher : BusinessManagement.Application.Auth.IPasswordHasher
{
    private static readonly object Holder = new();
    private readonly PasswordHasher<object> _inner = new();

    public string DummyHash { get; }

    public IdentityPasswordHasher() => DummyHash = _inner.HashPassword(Holder, Guid.NewGuid().ToString("N"));

    public string Hash(string password) => _inner.HashPassword(Holder, password);

    public bool Verify(string hash, string password)
        => _inner.VerifyHashedPassword(Holder, hash, password) != PasswordVerificationResult.Failed;
}