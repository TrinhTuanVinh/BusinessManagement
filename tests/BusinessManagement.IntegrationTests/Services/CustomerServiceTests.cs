
using BusinessManagement.Application.Common;
using BusinessManagement.Application.Customers;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;

[Collection("postgres")]
public class CustomerServiceTests(PostgresFixture fixture)
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static string NewEmail() => $"{Guid.NewGuid():N}@test.com";
    [Fact]
    public async Task Create_DuplicateEmail_ThrowsConflictException()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        var created = await new CustomerService(db).CreateAsync("Vinh", email, null, null, Ct);

        await using var db2 = fixture.CreateContext();
        await Assert.ThrowsAsync<ConflictException>(async () => await new CustomerService(db2).CreateAsync("Vinh Test", email, null, null, Ct));
    }

    [Fact]
    public async Task Update_DuplicateEmail_ThrowsConflictException()
    {
        var email1 = NewEmail();
        var email2 = NewEmail();
        await using var db = fixture.CreateContext();
        var created = await new CustomerService(db).CreateAsync("Vinh", email1, null, null, Ct);

        await using var db2 = fixture.CreateContext();
        var created2 = await new CustomerService(db2).CreateAsync("Vinh Test", email2, null, null, Ct);

        await using var db3 = fixture.CreateContext();
        await Assert.ThrowsAsync<ConflictException>(async () => await new CustomerService(db3).UpdateAsync(created2.Id, created2.FullName, email1, null, null, Ct));

        await using var db4 = fixture.CreateContext();
        var reloaded = await new CustomerService(db4).GetAsync(created2.Id, Ct);
        Assert.Equal(email2, reloaded.Email);
    }

    [Fact]
    public async Task Deactive_TwoTimes_ThrowsDomainExceptionAtTheSecond()
    {
        await using var db = fixture.CreateContext();
        var created = await new CustomerService(db).CreateAsync("Vinh", NewEmail(), null, null, Ct);

        await using var db2 = fixture.CreateContext();
        await new CustomerService(db2).DeactivateAsync(created.Id, Ct);

        await using var db3 = fixture.CreateContext();
        await Assert.ThrowsAsync<DomainException>(async () => await new CustomerService(db3).DeactivateAsync(created.Id, Ct));
    }

    [Fact]
    public async Task List_FilterByIsActive_ReturnInActiveCustomers()
    {
        var prefix = $"T{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext();
        var service = new CustomerService(db);
        var first = await service.CreateAsync($"{prefix} A", NewEmail(), null, null, Ct);
        await service.CreateAsync($"{prefix} B", NewEmail(), null, null, Ct);
        await service.CreateAsync($"{prefix} C", NewEmail(), null, null, Ct);
        await service.DeactivateAsync(first.Id, Ct);

        var inactive = await service.ListAsync(new CustomerListQuery(prefix, false, null), Ct);
        var active = await service.ListAsync(new CustomerListQuery(prefix, true, null), Ct);
        var all = await service.ListAsync(new CustomerListQuery(prefix, null, null), Ct);

        Assert.Equal(1, inactive.TotalCount);
        Assert.Equal(2, active.TotalCount);
        Assert.Equal(3, all.TotalCount);
    }
}