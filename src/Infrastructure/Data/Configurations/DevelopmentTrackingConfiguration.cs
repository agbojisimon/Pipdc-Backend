using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class DevelopmentTrackingConfiguration : IEntityTypeConfiguration<DevelopmentTracking>
{
    public void Configure(EntityTypeBuilder<DevelopmentTracking> builder)
    {
        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(t => t.Status)
            .HasConversion<string>();

        builder.HasIndex(t => new { t.UserId, t.DevelopmentProjectId })
            .IsUnique();

        builder.HasIndex(t => t.UserId);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.TrackedBy)
            .HasForeignKey(t => t.DevelopmentProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Unit)
            .WithMany(u => u.TrackedBy)
            .HasForeignKey(t => t.DevelopmentUnitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
