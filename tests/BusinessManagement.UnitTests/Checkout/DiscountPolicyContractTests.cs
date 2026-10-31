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
        var discount = new StudentDiscount().Calculate(decimal.Parse(total));
        Assert.Equal(decimal.Parse(expected), discount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(30_000)]
    [InlineData(19.02)]
    public void Calculate_ForAnyPolicy_ReturnsBetweenZeroAndOrderTotal(decimal total)
    {
        var policies = new IDiscountPolicy[]
        {
            new VipDiscount(), new EmployeeDiscount(), new FixedVoucherDiscount(35m), new StudentDiscount()
        };

        foreach (var p in policies)
        {
            var discount = p.Calculate(total);
            _output.WriteLine(
           $"Policy: {p.GetType().Name} | " +
           $"OrderTotal: {total:N0} | " +
           $"Discount: {discount:N2} | " +
           $"Valid: {discount >= 0 && discount <= total}");
            // TODO: Assert discount >= 0 và discount <= total
            Assert.InRange(discount, 0M, total);
        }
    }

}