using BusinessManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagement.Application.Reports;

public record TopCustomerDto(Guid CustomerId, string FullName, int OrderCount, decimal Revenue);

public class ReportService
{
    private readonly IAppDbContext _db;
    public ReportService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TopCustomerDto>> TopCustomersAsync(int top, CancellationToken ct)
    {
        top = Math.Clamp(top, 1, 100);

        var totals = await _db.Orders.AsNoTracking()
            .SelectMany(o => o.Items, (o, i) => new { o.CustomerId, OrderId = o.Id, Amount = i.Quantity * i.UnitPrice })
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                Revenue = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Revenue).ThenBy(x => x.CustomerId)
            .Take(top)
            .ToListAsync(ct);                                              // truy vấn 1: gom nhóm trong DB

        var ids = totals.Select(t => t.CustomerId).ToList();
        var names = await _db.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName, ct);            // truy vấn 2: lấy tên một lần

        return totals.Select(t => new TopCustomerDto(t.CustomerId, names[t.CustomerId], t.OrderCount, t.Revenue)).ToList();
    }
}