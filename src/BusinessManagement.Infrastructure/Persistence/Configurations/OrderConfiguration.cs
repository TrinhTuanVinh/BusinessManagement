using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessManagement.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).ValueGeneratedNever();
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(o => o.VoucherCode).HasMaxLength(30);
        b.Ignore(o => o.Total);

        b.HasOne(o => o.Customer).WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(o => new { o.CustomerId, o.CreatedAt });
    }
}
