using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessManagement.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        b.Property(c => c.Email).HasMaxLength(320).IsRequired();
        b.Property(c => c.Phone).HasMaxLength(30);
        b.Property(c => c.Address).HasMaxLength(500);
        b.HasIndex(c => c.Email).IsUnique();
    }
}
