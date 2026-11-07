namespace BusinessManagement.Application.Checkout.Events;

public record OrderProcessed(Guid OrderId, decimal FinalAmount, string Email);
