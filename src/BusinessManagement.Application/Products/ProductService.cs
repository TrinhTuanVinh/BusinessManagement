using System.Linq.Expressions;
using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Common;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagement.Application.Products;

public record ProductDTO(Guid Id, string Name, string Category, decimal Price, int Stock,
                         DateTime CreatedAt, DateTime? UpdatedAt);

public record ProductListQuery(string? Search, string? Category, decimal? MinPrice, decimal? MaxPrice,
                               bool? InStock, string? Sort, int Page = 1, int PageSize = 20);

public class ProductService
{
    private static readonly Expression<Func<Product, ProductDTO>> ToDtoExpr =
    p => new ProductDTO(p.Id, p.Name, p.Category, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);
    private static readonly Func<Product, ProductDTO> Map = ToDtoExpr.Compile();

    private readonly IAppDbContext _db;
    public ProductService(IAppDbContext db) => _db = db;

    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    public async Task<ProductDTO> CreateAsync(string name, string category, decimal price, int stock, CancellationToken ct)
    {
        var product = new Product(name, category, price, stock);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return Map(product);
    }
    public async Task<ProductDTO> GetAsync(Guid id, CancellationToken ct)
    => await _db.Products.AsNoTracking().Where(p => p.Id == id).Select(ToDtoExpr).SingleOrDefaultAsync(ct)
    ?? throw new NotFoundException($"Product '{id}' was not found.");

    public async Task<PageResult<ProductDTO>> ListAsync(ProductListQuery listQuery, CancellationToken ct)
    {
        var page = Math.Clamp(listQuery.Page, 1, 100_000);
        var pageSize = Math.Clamp(listQuery.PageSize, 1, 100);

        IQueryable<Product> query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(listQuery.Category))
        {
            var c = listQuery.Category.Trim();
            query = query.Where(p => p.Category == c);
        }
        if (!string.IsNullOrWhiteSpace(listQuery.Search))
        {
            var pattern = $"%{EscapeLike(listQuery.Search.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\"));
        }


        if (listQuery.MinPrice is { } min) query = query.Where(p => p.Price >= min);
        if (listQuery.MaxPrice is { } max) query = query.Where(p => p.Price <= max);
        if (listQuery.InStock is true) query = query.Where(p => p.Stock > 0);
        if (listQuery.InStock is false) query = query.Where(p => p.Stock == 0);

        var total = await query.CountAsync(ct);

        var items = await ApplySort(query, listQuery.Sort)
        .Skip((page - 1) * pageSize).Take(pageSize)
        .Select(ToDtoExpr)
        .ToListAsync(ct);

        return new PageResult<ProductDTO>(items, page, pageSize, total);
    }

    public async Task<ProductDTO> UpdateAsync(Guid id, string name, string category, decimal price, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
           ?? throw new NotFoundException($"Product '{id}' was not found.");
        product.UpdateDetails(name, category, price);
        await _db.SaveChangesAsync(ct);
        return Map(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                  ?? throw new NotFoundException($"Product '{id}' was not found.");
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> q, string? sort)
    {
        var desc = sort?.StartsWith("-") == true;
        var key = sort?.TrimStart('-', '+').ToLowerInvariant();
        return (key, desc)
        switch
        {
            ("name", false) => q.OrderBy(p => p.Name).ThenBy(p => p.Id),
            ("name", true) => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            ("price", false) => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            ("price", true) => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            ("stock", false) => q.OrderBy(p => p.Stock).ThenBy(p => p.Id),
            ("stock", true) => q.OrderByDescending(p => p.Stock).ThenBy(p => p.Id),
            ("createdat", false) => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
        };
    }
}