using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;

namespace BusinessManagement.UnitTests.Entities;

public class ProductTests
{
    [Fact]
    public void Constructor_WhenNameIsNull_ThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new Product("", "Plastic", 19.02m, 5));
    }

    [Fact]
    public void Constructor_WhenPriceIsNegative_ThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new Product("Test Plastic 1", "Plastic", -19.02m, 5));
    }
    [Fact]
    public void Constructor_WhenStockIsNegative_ThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new Product("Plastic 1", "Plastic", 19.02m, -5));
    }
    [Fact]
    public void DecreaseStock_WhenValid_ReducesStockAndSetsUpdatedAt()
    {
        // Arrange
        var product = new Product("Plastic 1", "Plastic", 19.02m, 5);

        // Act
        product.DecreaseStock(2);

        // Assert
        Assert.Equal(3, product.Stock);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void IncreaseStock_WhenValid_IncreaseStockAndSetsUpdatedAT()
    {
        // Arrange
        var product = new Product("Plastic 1", "Plastic", 19.02m, 5);

        // Act
        product.IncreaseStock(2);

        // Assert

        Assert.Equal(7, product.Stock);
        Assert.NotNull(product.UpdatedAt);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecreaseStock_WhenQuantityNotPositive_ThrowsDomainException(int quantity)
    {
        var product = new Product("Plastic 1", "Plastic", 19.02m, 5);

        Assert.Throws<DomainException>(() => product.DecreaseStock(quantity));
    }
    [Fact]
    public void ChangePrice_WhenPriceIsNegative_ThrowsDomainException()
    {
        var product = new Product("Plastic 1", "Plastic", 19, 5);

        Assert.Throws<DomainException>(() => product.ChangePrice(-5));
    }
    [Fact]
    public void ChangePrice_WhenPriceIsValid_UpdatesPrice()
    {
        var product = new Product("Plastic 1", "Plastic", 19, 5);
        product.ChangePrice(55);

        Assert.Equal(55, product.Price);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void DecreaseStock_WhenQuantityExceedsStock_ThrowsDomainException()
    {
        var product = new Product("Plastic 1", "Plastic", 19.02m, 5);

        Assert.Throws<DomainException>(() => product.DecreaseStock(6));
    }

    [Fact]
    public void UpdateDetails_ValidInput_UpdatesDetailsAndKeepsStock()
    {
        var product = new Product("Old Name", "Old Category", 100m, 10);

        product.UpdateDetails("  New Name  ", "  New Category  ", 200m);

        Assert.Equal("New Name", product.Name);
        Assert.Equal("New Category", product.Category);
        Assert.Equal(200m, product.Price);
        Assert.Equal(10, product.Stock);
        Assert.NotEqual(default, product.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_EmptyName_ThrowsAndDoesNotChangeFields()
    {
        var product = new Product("Old Name", "Old Category", 100m, 10);

        var oldName = product.Name;
        var oldCategory = product.Category;
        var oldPrice = product.Price;
        var oldStock = product.Stock;
        var oldUpdatedAt = product.UpdatedAt;

        Assert.Throws<DomainException>(() =>
            product.UpdateDetails("", "New Category", 200m));

        Assert.Equal(oldName, product.Name);
        Assert.Equal(oldCategory, product.Category);
        Assert.Equal(oldPrice, product.Price);
        Assert.Equal(oldStock, product.Stock);
        Assert.Equal(oldUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_NegativePrice_ThrowsAndDoesNotChangeFields()
    {
        var product = new Product("Old Name", "Old Category", 100m, 10);

        var oldName = product.Name;
        var oldCategory = product.Category;
        var oldPrice = product.Price;
        var oldStock = product.Stock;
        var oldUpdatedAt = product.UpdatedAt;

        Assert.Throws<DomainException>(() =>
            product.UpdateDetails("New Name", "New Category", -100m));

        Assert.Equal(oldName, product.Name);
        Assert.Equal(oldCategory, product.Category);
        Assert.Equal(oldPrice, product.Price);
        Assert.Equal(oldStock, product.Stock);
        Assert.Equal(oldUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_EmptyCategory_ThrowsAndDoesNotChangeFields()
    {
        var product = new Product("Old Name", "Old Category", 100m, 10);

        var oldName = product.Name;
        var oldCategory = product.Category;
        var oldPrice = product.Price;
        var oldStock = product.Stock;
        var oldUpdatedAt = product.UpdatedAt;

        Assert.Throws<DomainException>(() =>
            product.UpdateDetails("New Name", "", 200m));

        Assert.Equal(oldName, product.Name);
        Assert.Equal(oldCategory, product.Category);
        Assert.Equal(oldPrice, product.Price);
        Assert.Equal(oldStock, product.Stock);
        Assert.Equal(oldUpdatedAt, product.UpdatedAt);
    }
}
