using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessManagement.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products", t =>
        {
            t.HasCheckConstraint("ck_products_price_nonneg", "\"Price\" >= 0");
            t.HasCheckConstraint("ck_products_stock_nonneg", "\"Stock\" >= 0");
        });
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.Category).HasMaxLength(100).IsRequired();
        b.Property(p => p.Price).HasPrecision(18, 2);
        b.HasIndex(p => p.Category);
        b.HasIndex(p => p.Name);
    }
}