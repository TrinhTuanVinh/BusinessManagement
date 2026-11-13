using System.ComponentModel.DataAnnotations;

public record OrderLineRequest([Range(1, 10_000)] int Quantity, Guid ProductId);

public record CreateOrderRequest(
    Guid CustomerId,
    [Required, MinLength(1), MaxLength(100)] List<OrderLineRequest> Items);