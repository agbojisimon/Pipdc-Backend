using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class DevelopmentUpdateConfiguration : IEntityTypeConfiguration<DevelopmentUpdate>
{
    public void Configure(EntityTypeBuilder<DevelopmentUpdate> builder)
    {
        builder.Property(u => u.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(u => u.ImageUrls)
            .HasColumnType("text[]");

        builder.Property(u => u.ImagePublicIds)
            .HasColumnType("text[]")
            .IsRequired(false);

        builder.HasIndex(u => new { u.DevelopmentProjectId, u.UpdateDate });

        builder.HasOne(u => u.Project)
            .WithMany(p => p.Updates)
            .HasForeignKey(u => u.DevelopmentProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
