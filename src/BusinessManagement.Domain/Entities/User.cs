using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;

public enum UserRole { Staff, Admin }

public class User : BaseEntity
{
    private User() { }                                       // chỉ dành cho EF Core

    public User(string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
    }

    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        email = email.Trim().ToLowerInvariant();
        if (!email.Contains('@')) throw new DomainException("Email must contain '@'.");
        return email;
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("User is already inactive.");
        IsActive = false;
        Touch();
    }
}