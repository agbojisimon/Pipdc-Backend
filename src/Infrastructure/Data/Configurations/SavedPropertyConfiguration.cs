using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class SavedPropertyConfiguration : IEntityTypeConfiguration<SavedProperty>
{
    public void Configure(EntityTypeBuilder<SavedProperty> builder)
    {
        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(s => new { s.UserId, s.PropertyId })
            .IsUnique();

        builder.HasOne(s => s.User)
            .WithMany(u => u.SavedProperties)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Property)
            .WithMany(p => p.SavedByUsers)
            .HasForeignKey(s => s.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
