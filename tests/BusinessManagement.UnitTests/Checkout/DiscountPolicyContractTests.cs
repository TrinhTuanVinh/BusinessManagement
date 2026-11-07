namespace BusinessManagement.UnitTests.Checkout;

using BusinessManagement.Application.Checkout.Discount;
using Xunit.Abstractions;

public class DiscountPolicyContractTests
{
    private readonly ITestOutputHelper _output;
    public DiscountPolicyContractTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Theory]
    [InlineData("19.02", "0.95")]   // 0.951  → 0.95
    [InlineData("10.10", "0.51")]   // 0.505  → 0.51 (AwayFromZero). Nếu dùng làm tròn mặc định C# (banker's) sẽ ra 0.50
    public void StudentDiscount_RoundsToTwoDecimals(string total, string expected)
    {
        var discount = new StudentDiscountStrategy().CalculateDiscount(new DiscountContext("Student", "", decimal.Parse(total)));
        Assert.Equal(decimal.Parse(expected), discount);
    }

}