using System.ComponentModel.DataAnnotations;
using BusinessManagement.Application.Orders;
using BusinessManagement.Domain.Common;

public record OrderLineRequest([Range(1, 10_000)] int Quantity, Guid ProductId);

public record CreateOrderRequest(
    Guid CustomerId,
    [Required, MinLength(1), MaxLength(100)] List<OrderLineRequest> Items,
    [StringLength(30)] string? VoucherCode
);

public sealed class OrderListRequest
{
    [Range(1, 100_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    public Guid? CustomerId { get; init; }
    public OrderStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    [RegularExpression("^-?(?i:createdAt)$", ErrorMessage = "Sort must be 'createdAt' (prefix '-' for descending).")]
    public string? Sort { get; init; }

    public OrderListQuery ToQuery() => new(CustomerId, Status, From?.UtcDateTime, To?.UtcDateTime, Sort, Page, PageSize);
}