using BusinessManagement.Domain.Entities;
using BusinessManagement.Domain.Common;
using Xunit;

namespace BusinessManagement.UnitTests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldNormalizeEmail()
    {
        // Arrange
        var email = "  Vinh.Nguyen@Example.COM  ";

        // Act
        var user = new User(email, "hashed-password", UserRole.Staff);

        // Assert
        Assert.Equal("vinh.nguyen@example.com", user.Email);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenEmailDoesNotContainAt()
    {
        // Arrange
        var email = "vinh.example.com";

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            new User(email, "hashed-password", UserRole.Staff));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPasswordHashIsEmpty()
    {
        // Arrange
        var email = "vinh@example.com";
        var passwordHash = "";

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            new User(email, passwordHash, UserRole.Staff));
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenCalledTwice()
    {
        // Arrange
        var user = new User(
            "vinh@example.com",
            "hashed-password",
            UserRole.Staff);

        // Act
        user.Deactivate();

        // Assert
        Assert.Throws<DomainException>(() => user.Deactivate());
    }
}