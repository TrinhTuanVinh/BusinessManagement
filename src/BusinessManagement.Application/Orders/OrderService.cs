using System.Text.Json;
using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Checkout.Pricing;
using BusinessManagement.Application.Common;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessManagement.Application.Orders;

public record OrderLine(Guid ProductId, int Quantity);
public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice, decimal LineTotal);
public record OrderDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal Subtotal, decimal Discount,
                        string? VoucherCode, decimal Total, DateTime CreatedAt, DateTime? UpdatedAt,
                        IReadOnlyList<OrderItemDto> Items);
public record OrderSummaryDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal Total, int ItemCount, DateTime CreatedAt);
public record OrderListQuery(Guid? CustomerId, OrderStatus? Status, DateTime? From, DateTime? To,
                             string? Sort, int Page = 1, int PageSize = 20);

public class OrderService
{
    private readonly IAppDbContext _db;
    private readonly PricingService _pricing;
    private ILogger<OrderService> _logger;
    public OrderService(IAppDbContext db, PricingService pricing, ILogger<OrderService> logger)
    {
        _db = db;
        _pricing = pricing;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(Guid customerId, IReadOnlyList<OrderLine> lines, string? voucherCode, CancellationToken ct)
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

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var quote = _pricing.Quote(order.Subtotal, "Standard", voucherCode);   // mã sai: DomainException 422, chưa chạm kho
            order.ApplyVoucher(voucherCode);
            order.ApplyDiscount(quote.Discount);
        }
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
            {
                _logger.LogWarning("Insufficient stock for product {ProductId}, requested {Quantity}", line.ProductId, quantity);
                throw new ConflictException($"Insufficient stock for product '{line.ProductId}'.");
            }
        }

        _db.Orders.Add(order);
        var eventId = Guid.CreateVersion7();
        var created = new OrderCreated(eventId, order.Id, customerId, order.Total, DateTime.UtcNow);
        _db.OutboxMessages.Add(new OutboxMessage(eventId, RoutingKeys.OrderCreated, nameof(OrderCreated),
            JsonSerializer.Serialize(created)));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        _logger.LogInformation(
            "Order {OrderId} created for customer {CustomerId}: {ItemCount} items, subtotal {Subtotal}, discount {Discount}",
            order.Id, customerId, merged.Count, order.Subtotal, order.DiscountAmount);
        return await GetAsync(order.Id, ct);
    }

    public async Task<OrderDto> GetAsync(Guid id, CancellationToken ct)
    {
        var row = await _db.Orders.AsNoTracking().Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.Status,
                o.DiscountAmount,
                o.VoucherCode,
                o.CreatedAt,
                o.UpdatedAt,
                Items = o.Items.OrderBy(i => i.Id)
                    .Select(i => new OrderItemDto(i.ProductId, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
                    .ToList()
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Order '{id}' was not found.");
        var subTotal = row.Items.Sum(i => i.LineTotal);
        return new OrderDto(row.Id, row.CustomerId, row.Status, subTotal, row.DiscountAmount,
                        row.VoucherCode, subTotal - row.DiscountAmount, row.CreatedAt, row.UpdatedAt, row.Items);
    }

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
    public async Task<PageResult<OrderSummaryDto>> ListAsync(OrderListQuery q, CancellationToken ct)
    {
        var page = Math.Clamp(q.Page, 1, 100_000);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        if (q.From is { } f && q.To is { } t && f > t)
            throw new DomainException("'from' must not be after 'to'.");

        IQueryable<Order> query = _db.Orders.AsNoTracking();
        if (q.CustomerId is { } customerId)
            query = query.Where(o => o.CustomerId == customerId);
        if (q.Status is { } status)
            query = query.Where(o => o.Status == status);
        if (q.From is { } from)
            query = query.Where(o => o.CreatedAt >= from);
        if (q.To is { } to)
            query = query.Where(o => o.CreatedAt < to);

        var total = await query.CountAsync(ct);

        var ordered = string.Equals(q.Sort, "createdAt", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id)
            : query.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id);

        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new OrderSummaryDto(o.Id, o.CustomerId, o.Status,
                o.Items.Sum(i => i.Quantity * i.UnitPrice) - o.DiscountAmount,
                o.Items.Count(), o.CreatedAt))
            .ToListAsync(ct);

        return new PageResult<OrderSummaryDto>(items, page, pageSize, total);
    }

}