using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Common;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagement.Application.Orders;

public record OrderLine(Guid ProductId, int Quantity);
public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice, decimal LineTotal);
public record OrderDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal Total,
                       DateTime CreatedAt, DateTime? UpdatedAt, IReadOnlyList<OrderItemDto> Items);

public class OrderService
{
    private readonly IAppDbContext _db;
    public OrderService(IAppDbContext db) => _db = db;

    public async Task<OrderDto> CreateAsync(Guid customerId, IReadOnlyList<OrderLine> lines, CancellationToken ct)
    {
        if (lines.Count == 0) throw new DomainException("Order must contain at least one item.");
        if (lines.Any(l => l.ProductId == Guid.Empty)) throw new DomainException("Product id is required.");
        if (lines.Any(l => l.Quantity <= 0)) throw new DomainException("Quantity must be greater than 0.");

        var merged = lines.GroupBy(l => l.ProductId)
            .Select(g => new OrderLine(g.Key, g.Sum(l => l.Quantity)))
            .OrderBy(l => l.ProductId)
            .ToList();

        var customer = await _db.Customers.AsNoTracking().Where(c => c.Id == customerId)
            .Select(c => new { c.IsActive }).FirstOrDefaultAsync(ct)
            ?? throw new DomainException($"Customer '{customerId}' was not found.");
        if (!customer.IsActive) throw new DomainException("Customer is inactive.");

        var ids = merged.Select(l => l.ProductId).ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var missing = ids.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0) throw new DomainException($"Product '{missing[0]}' was not found.");
        var order = new Order(customerId);
        foreach (var line in merged) order.AddItem(products[line.ProductId], line.Quantity);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var line in merged)
        {
            var quantity = line.Quantity;
            var affected = await _db.Products
                .Where(p => p.Id == line.ProductId && p.Stock >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Stock, p => p.Stock - quantity)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
            if (affected == 0)
                throw new ConflictException($"Insufficient stock for product '{line.ProductId}'.");
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(order.Id, ct);
    }

    public async Task<OrderDto> GetAsync(Guid id, CancellationToken ct)
        => await _db.Orders.AsNoTracking().Where(o => o.Id == id)
            .Select(o => new OrderDto(
                o.Id, o.CustomerId, o.Status,
                o.Items.Sum(i => i.Quantity * i.UnitPrice),
                o.CreatedAt, o.UpdatedAt,
                o.Items.OrderBy(i => i.Id)
                    .Select(i => new OrderItemDto(i.ProductId, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
                    .ToList()))
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.");

    public async Task<OrderDto> CancelAsync(Guid id, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var flipped = await _db.Orders
            .Where(o => o.Id == id && o.Status != OrderStatus.Cancelled)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Cancelled)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

        if (flipped == 0)
        {
            if (await _db.Orders.AsNoTracking().AnyAsync(o => o.Id == id, ct))
                throw new DomainException("Order has been cancelled!");
            throw new NotFoundException($"Order '{id}' was not found.");
        }

        var lines = await _db.Orders.AsNoTracking().Where(o => o.Id == id)
            .SelectMany(o => o.Items)
            .Select(i => new { i.ProductId, i.Quantity })
            .ToListAsync(ct);

        foreach (var line in lines.OrderBy(l => l.ProductId))
        {
            var quantity = line.Quantity;
            await _db.Products.Where(p => p.Id == line.ProductId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Stock, p => p.Stock + quantity)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        }

        await tx.CommitAsync(ct);
        return await GetAsync(id, ct);
    }
}