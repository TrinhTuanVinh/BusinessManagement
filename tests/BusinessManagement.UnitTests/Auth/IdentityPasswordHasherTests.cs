using BusinessManagement.Application.Auth;

public class IdentityPasswordHasherTests
{
    private const string Password = "CorrectHorse1!";
    private readonly IPasswordHasher _sut = new IdentityPasswordHasher();

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        => Assert.NotEqual(_sut.Hash(Password), _sut.Hash(Password));

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
        => Assert.True(_sut.Verify(_sut.Hash(Password), Password));

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
        => Assert.False(_sut.Verify(_sut.Hash(Password), "WrongPassword1!"));

    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
        => Assert.DoesNotContain(Password, _sut.Hash(Password));

    [Fact]
    public void Verify_DummyHash_NeverMatches()
        => Assert.False(_sut.Verify(_sut.DummyHash, Password));
}