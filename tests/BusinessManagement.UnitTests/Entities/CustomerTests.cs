using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;

namespace BusinessManagement.UnitTests.Entities;

public class CustomerTests
{
    [Fact]
    public void Constructor_WhenEmailHasSpaceUpperAndIsActiveTrue_NomarlizeEmail()
    {
        // Given
        Customer customer = new Customer("Vinh", "   VINH@gmail.com", "099999223", "333 KDV");

        Assert.Equal("vinh@gmail.com", customer.Email);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Constructor_WhenEmailDoesNotHaveCharacter_ThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new Customer("Vinh", "INHgmail.com", "099999223", "333 KDV"));
    }

    [Fact]
    public void Deactivate_WhenDeactivateTwice_ThrowDomainException()
    {
        Customer customer = new Customer("Vinh", "INH@gmail.com", "099999223", "333 KDV");
        customer.Deactivate();

        Assert.Throws<DomainException>(() => customer.Deactivate());
    }

    [Fact]
    public void UpdateContact_WhenSuccess_UpdateEmail()
    {
        Customer customer = new Customer("Vinh", "INH@gmail.com", "099999223", "333 KDV");
        customer.UpdateContact(customer.FullName, "Vinh@gmail.com", "", "");

        Assert.Equal("vinh@gmail.com", customer.Email);
    }

}
