namespace BusinessManagement.Application.Checkout.Discount;

public interface IVoucherFactory
{
    IDiscountStrategy? Create(string? code);   // null nếu mã không tồn tại
}