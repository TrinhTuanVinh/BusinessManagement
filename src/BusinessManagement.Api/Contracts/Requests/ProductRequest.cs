using System.ComponentModel.DataAnnotations;
using BusinessManagement.Application.Products;
namespace BusinessManagement.Api.Contracts.Requests;

public record CreateProductRequest(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(100)] string Category,
    [Range(0, 1_000_000_000)] decimal Price,
    [Range(0, 1_000_000)] int Stock);

public record UpdateProductRequest(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(100)] string Category,
    [Range(0, 1_000_000_000)] decimal Price);

public sealed class ProductListRequest
{
    [Range(1, 100_000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [StringLength(100)] public string? Search { get; init; }
    [StringLength(100)] public string? Category { get; init; }
    [Range(0, 1_000_000_000)] public decimal? MinPrice { get; init; }
    [Range(0, 1_000_000_000)] public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }
    [RegularExpression("^-?(?i:name|price|stock|createdAt)$", ErrorMessage = "Sort must be one of: name, price, stock, createdAt (prefix '-' for descending).")]
    public string? Sort { get; init; }

    public ProductListQuery ToQuery() => new(Search, Category, MinPrice, MaxPrice, InStock, Sort, Page, PageSize);
}