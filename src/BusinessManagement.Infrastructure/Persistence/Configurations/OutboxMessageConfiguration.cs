using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessManagement.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.RoutingKey).HasMaxLength(100).IsRequired();
        b.Property(c => c.Type).HasMaxLength(100).IsRequired();
        b.Property(c => c.Payload).IsRequired();
        b.Property(c => c.LastError).HasMaxLength(1000);
        b.HasIndex(m => m.CreatedAt).HasFilter("\"ProcessedAt\" IS NULL");
    }
}
