using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class LeaseRecordConfiguration : IEntityTypeConfiguration<LeaseRecord>
{
    public void Configure(EntityTypeBuilder<LeaseRecord> builder)
    {
        builder.Property(l => l.TenantName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.TenantContact)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.MonthlyRent)
            .HasColumnType("decimal(18,2)");

        builder.Property(l => l.Notes)
            .HasMaxLength(4000);

        builder.HasOne(l => l.Property)
            .WithOne(p => p.LeaseRecord)
            .HasForeignKey<LeaseRecord>(l => l.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
