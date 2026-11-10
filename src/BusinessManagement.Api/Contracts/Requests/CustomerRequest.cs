
using System.ComponentModel.DataAnnotations;
using BusinessManagement.Application.Customers;

namespace BusinessManagement.Api.Contracts.Requests;

public record CustomerRequest
(
    [Required, StringLength(200)] string FullName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [StringLength(30)] string? Phone,
    [StringLength(500)] string? Address);


public sealed class CustomerListRequest
{
    [Range(1, 100_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [StringLength(100)] public string? Search { get; init; }
    public bool? IsActive { get; init; }
    [RegularExpression("^-?(?i:name|email|createdAt)$", ErrorMessage = "Sort must be one of: name, email, createdAt (prefix '-' for descending).")]
    public string? Sort { get; init; }

    public CustomerListQuery ToQuery() => new(Search, IsActive, Sort, Page, PageSize);
}