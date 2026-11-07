
using System.ComponentModel.DataAnnotations;

public record QuoteRequest(
    [Range(0.01, 1_000_000_000)] decimal OrderTotal,
    [Required, StringLength(30)] string CustomerType,
    [StringLength(30)] string? VoucherCode
);