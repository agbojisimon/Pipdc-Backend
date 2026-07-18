using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class SaleRecordConfiguration : IEntityTypeConfiguration<SaleRecord>
{
    public void Configure(EntityTypeBuilder<SaleRecord> builder)
    {
        builder.Property(s => s.SalePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.BuyerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.BuyerContact)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Notes)
            .HasMaxLength(4000);

        builder.HasOne(s => s.Property)
            .WithOne(p => p.SaleRecord)
            .HasForeignKey<SaleRecord>(s => s.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
