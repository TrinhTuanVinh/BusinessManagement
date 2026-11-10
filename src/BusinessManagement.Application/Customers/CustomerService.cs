using System.Linq.Expressions;
using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Common;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagement.Application.Customers;

public record CustomerDto(Guid Id, string FullName, string Email, string? Phone, string? Address, bool? IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public record CustomerListQuery(string? Search, bool? IsActive, string? Sort, int Page = 1, int PageSize = 20);

public class CustomerService
{
    private static readonly Expression<Func<Customer, CustomerDto>> ToDtoExpr =
  c => new CustomerDto(c.Id, c.FullName, c.Email, c.Phone, c.Address, c.IsActive, c.CreatedAt, c.UpdatedAt);
    private static readonly Func<Customer, CustomerDto> Map = ToDtoExpr.Compile();
    private readonly IAppDbContext _db;
    public CustomerService(IAppDbContext db) => _db = db;

    public async Task<CustomerDto> CreateAsync(string fullName, string email, string? phone, string? address, CancellationToken ct)
    {
        var customer = new Customer(fullName, email, phone, address);
        var checkExist = await _db.Customers.AnyAsync(c => c.Email == customer.Email, ct);
        if (checkExist)
            throw new ConflictException("A customer with this email already exists.");
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return Map(customer);
    }
    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken ct)
   => await _db.Customers.AsNoTracking().Where(c => c.Id == id).Select(ToDtoExpr).SingleOrDefaultAsync(ct)
   ?? throw new NotFoundException($"Customer '{id}' was not found.");

    public async Task<PageResult<CustomerDto>> ListAsync(CustomerListQuery listQuery, CancellationToken ct)
    {
        var page = Math.Clamp(listQuery.Page, 1, 100_000);
        var pageSize = Math.Clamp(listQuery.PageSize, 1, 100);

        IQueryable<Customer> query = _db.Customers.AsNoTracking();

        if (listQuery.IsActive is { } active)
            query = query.Where(c => c.IsActive == active);
        if (!string.IsNullOrWhiteSpace(listQuery.Search))
        {
            var s = listQuery.Search.Trim().ToLowerInvariant();
            query = query.Where(p => p.FullName.ToLower().Contains(s) || p.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await ApplySort(query, listQuery.Sort)
        .Skip((page - 1) * pageSize).Take(pageSize)
        .Select(ToDtoExpr)
        .ToListAsync(ct);

        return new PageResult<CustomerDto>(items, page, pageSize, total);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, string fullName, string email, string? phone, string? address, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(p => p.Id == id, ct)
           ?? throw new NotFoundException($"Customer '{id}' was not found.");
        var checkExist = await _db.Customers.AnyAsync(c => c.Email == email && c.Id != id, ct);
        if (checkExist)
            throw new ConflictException("A customer with this email already exists.");

        customer.UpdateContact(fullName, email, phone, address);
        await _db.SaveChangesAsync(ct);

        return Map(customer);
    }

    public async Task<CustomerDto> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NotFoundException($"Customer '{id}' was not found.");
        customer.Deactivate();
        await _db.SaveChangesAsync(ct);
        return Map(customer);
    }
    public async Task<CustomerDto> ActivateAsync(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
               ?? throw new NotFoundException($"Customer '{id}' was not found.");
        customer.Activate();
        await _db.SaveChangesAsync(ct);
        return Map(customer);
    }
    private static IQueryable<Customer> ApplySort(IQueryable<Customer> q, string? sort)
    {
        var desc = sort?.StartsWith('-') == true;
        var key = sort?.TrimStart('-', '+').ToLowerInvariant();
        return (key, desc) switch
        {
            ("name", false) => q.OrderBy(p => p.FullName).ThenBy(p => p.Id),
            ("name", true) => q.OrderByDescending(p => p.FullName).ThenBy(p => p.Id),
            ("email", false) => q.OrderBy(p => p.Email).ThenBy(p => p.Id),
            ("email", true) => q.OrderByDescending(p => p.Email).ThenBy(p => p.Id),
            ("createdat", false) => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),   // mặc định: mới nhất trước
        };
    }
}