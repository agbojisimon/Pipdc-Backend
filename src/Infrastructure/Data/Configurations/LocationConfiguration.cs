using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(l => l.Name)
            .IsUnique();

        builder.Property(l => l.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(l => l.Slug)
            .IsUnique();

        builder.Property(l => l.Type)
            .HasConversion<string>();

        builder.HasIndex(l => l.Type);

        builder.HasOne(l => l.Parent)
            .WithMany(l => l.Children)
            .HasForeignKey(l => l.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.Name, l.ParentId })
            .IsUnique();
    }
}
