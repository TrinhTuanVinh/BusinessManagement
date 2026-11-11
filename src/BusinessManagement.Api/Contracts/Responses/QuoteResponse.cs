namespace BusinessManagement.Api.Contracts.Response;

public record QuoteResponse(decimal OrderTotal, decimal Discount, decimal FinalAmount);