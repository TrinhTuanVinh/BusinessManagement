using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessManagement.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }          // OrderItem đi qua Order (aggregate root)
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}