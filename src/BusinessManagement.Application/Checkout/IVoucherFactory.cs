public interface IVoucherFactory
{
    IDiscountStrategy? Create(string? code);   // null nếu mã không tồn tại
}

public class InMemoryVoucherFactory : IVoucherFactory
{
    private static readonly Dictionary<string, Func<IDiscountStrategy>> Definitions =
     new(StringComparer.OrdinalIgnoreCase)
     {
         ["SALE50K"] = () => new FixedVoucher(2, "SALE50K", 50_000m),
         ["SALE10"] = () => new PercentDiscount(2, 0.10m, c => IsCode(c, "SALE10")),
     };
    public IDiscountStrategy? Create(string? code)
    => !string.IsNullOrWhiteSpace(code) && Definitions.TryGetValue(code.Trim(), out var make)
            ? make()
            : null;
    private static bool IsCode(DiscountContext c, string code)
        => string.Equals(c.VoucherCode, code, StringComparison.OrdinalIgnoreCase);
}