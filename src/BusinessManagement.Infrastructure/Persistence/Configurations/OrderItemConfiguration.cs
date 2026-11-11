using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessManagement.Infrastructure.Persistence.Configurations;


public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems", t =>
        {
            t.HasCheckConstraint("ck_orderitems_quantity_positive", "\"Quantity\" > 0");
            t.HasCheckConstraint("ck_orderitems_unitprice_nonneg", "\"UnitPrice\" >= 0");
        });
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.UnitPrice).HasPrecision(18, 2);

        b.HasOne<Order>().WithMany(o => o.Items)
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Product>().WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex("OrderId", nameof(OrderItem.ProductId)).IsUnique();
    }
}