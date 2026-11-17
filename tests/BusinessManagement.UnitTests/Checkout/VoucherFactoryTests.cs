using BusinessManagement.Application.Checkout.Discount;

namespace BusinessManagement.UnitTests.Checkout;
public class VoucherFactoryTests
{
    private readonly InMemoryVoucherFactory _sut = new();

    [Theory]
    [InlineData("SALE50K")]
    [InlineData("sale50k")]
    [InlineData(" SALE50K ")]
    public void Create_KnownCode_ReturnsStrategy(string code)
        => Assert.NotNull(_sut.Create(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UNKNOWN")]
    public void Create_UnknownOrEmptyCode_ReturnsNull(string? code)
        => Assert.Null(_sut.Create(code));

    [Fact]
    public void Create_Sale50k_AppliesOnlyToItsOwnCodeAndGivesFixedAmount()
    {
        var voucher = _sut.Create("SALE50K")!;

        Assert.True(voucher.IsApplicable(new("VIP", "SALE50K", 100_000m)));
        Assert.False(voucher.IsApplicable(new("VIP", "SALE10", 100_000m)));
        Assert.Equal(50m, voucher.CalculateDiscount(new("VIP", "SALE50K", 100_000m)));
    }
}