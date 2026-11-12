using System.Text;
using BusinessManagement.Application.Auth;
using BusinessManagement.Application.Common;
using BusinessManagement.Domain.Common;
using BusinessManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

[Collection("postgres")]
public class AuthServiceTests(PostgresFixture fixture)
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string Password = "CorrectHorse1!";
    private static readonly JwtOptions Options = new()
    { Issuer = "i", Audience = "a", Key = new string('k', 40), AccessTokenMinutes = 30 };

    private static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    private static AuthService CreateSut(AppDbContext db, IPasswordHasher? hasher = null)
        => new(db, hasher ?? new IdentityPasswordHasher(), new JwtTokenService(Options));

    [Fact]
    public async Task RegisterThenLogin_ReturnsValidTokenWithRoleAndSubject()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        var user = await CreateSut(db).RegisterAsync(email, Password, Ct);

        await using var db2 = fixture.CreateContext();
        var token = await CreateSut(db2).LoginAsync(email, Password, Ct);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.AccessToken,
            new TokenValidationParameters
            {
                ValidIssuer = Options.Issuer,
                ValidAudience = Options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key))
            });

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal("Staff", result.Claims["role"]);
        Assert.Equal(user.Id.ToString(), result.Claims["sub"]);
        Assert.InRange(token.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public async Task Register_SameEmailDifferentCase_ThrowsConflict()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        await CreateSut(db).RegisterAsync(email.ToUpperInvariant(), Password, Ct);

        await using var db2 = fixture.CreateContext();
        await Assert.ThrowsAsync<ConflictException>(() => CreateSut(db2).RegisterAsync(email, Password, Ct));
    }

    [Fact]
    public async Task Register_InvalidEmail_ThrowsDomainException()
    {
        await using var db = fixture.CreateContext();

        await Assert.ThrowsAsync<DomainException>(() => CreateSut(db).RegisterAsync("not-an-email", Password, Ct));
    }

    [Fact]
    public async Task Register_StoresHashNotPlainPassword()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        await CreateSut(db).RegisterAsync(email, Password, Ct);

        await using var db2 = fixture.CreateContext();
        var stored = await db2.Users.AsNoTracking().SingleAsync(u => u.Email == email);

        Assert.DoesNotContain(Password, stored.PasswordHash);
        Assert.True(new IdentityPasswordHasher().Verify(stored.PasswordHash, Password));
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ThrowTheSameMessage()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        await CreateSut(db).RegisterAsync(email, Password, Ct);

        await using var db2 = fixture.CreateContext();
        var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateSut(db2).LoginAsync(email, "WrongPassword1!", Ct));
        await using var db3 = fixture.CreateContext();
        var unknownEmail = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateSut(db3).LoginAsync(NewEmail(), Password, Ct));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);   // không lộ email nào tồn tại
    }

    [Fact]
    public async Task Login_EmailWithSpacesAndUpperCase_Succeeds()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        await CreateSut(db).RegisterAsync(email, Password, Ct);

        await using var db2 = fixture.CreateContext();
        var token = await CreateSut(db2).LoginAsync($"  {email.ToUpperInvariant()} ", Password, Ct);

        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
    }

    [Fact]
    public async Task Login_DeactivatedUser_ThrowsAuthenticationFailed()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        var user = await CreateSut(db).RegisterAsync(email, Password, Ct);

        await using (var db2 = fixture.CreateContext())
        {
            var entity = await db2.Users.SingleAsync(u => u.Id == user.Id);
            entity.Deactivate();
            await db2.SaveChangesAsync();
        }

        await using var db3 = fixture.CreateContext();
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateSut(db3).LoginAsync(email, Password, Ct));
    }

    [Fact]
    public async Task Login_UnknownEmail_StillRunsOnePasswordVerification()
    {
        var counting = new CountingHasher(new IdentityPasswordHasher());
        await using var db = fixture.CreateContext();

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateSut(db, counting).LoginAsync(NewEmail(), Password, Ct));

        Assert.Equal(1, counting.VerifyCalls);
    }

    private sealed class CountingHasher(IPasswordHasher inner) : IPasswordHasher
    {
        public int VerifyCalls { get; private set; }
        public string DummyHash => inner.DummyHash;
        public string Hash(string password) => inner.Hash(password);
        public bool Verify(string hash, string password) { VerifyCalls++; return inner.Verify(hash, password); }
    }
}