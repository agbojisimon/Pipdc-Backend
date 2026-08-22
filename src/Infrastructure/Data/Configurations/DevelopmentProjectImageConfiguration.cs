using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class DevelopmentProjectImageConfiguration : IEntityTypeConfiguration<DevelopmentProjectImage>
{
    public void Configure(EntityTypeBuilder<DevelopmentProjectImage> builder)
    {
        builder.Property(i => i.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.PublicId)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(i => new { i.DevelopmentProjectId, i.DisplayOrder });

        builder.HasOne(i => i.Project)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.DevelopmentProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
