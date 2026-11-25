
using System.Net;
using BusinessManagement.Infrastructure.Persistence;
using BusinessManagement.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;

[Collection("postgres")]
public sealed class MigrationOnStartupTests
{
    private readonly PostgresFixture _postgres;

    public MigrationOnStartupTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

  
    [Fact]
    public async Task Api_OnEmptyDatabase_AppliesMigrationsAtStartup()
    {
        var connectionString = await _postgres.CreateEmptyDatabaseAsync();

        await using var factory = new ApiFactory();
        var client = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", connectionString);
            b.UseSetting("Database:MigrateOnStartup", "true");
        }).CreateClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }
    [Fact]
    public async Task Api_OnEmptyDatabase_WithoutFlag_DoesNotMigrate()
    {
        var connectionString = await _postgres.CreateEmptyDatabaseAsync();

        await using var factory = new ApiFactory();
        using var client = factory.WithWebHostBuilder(b =>
            b.UseSetting("ConnectionStrings:Default", connectionString)).CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
        Assert.NotEmpty(await db.Database.GetPendingMigrationsAsync());  
    }
}
