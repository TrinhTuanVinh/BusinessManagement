namespace BusinessManagement.Application.Checkout.Discount;

public record DiscountContext(string CustomerType, string? VoucherCode, decimal OrderTotal);
