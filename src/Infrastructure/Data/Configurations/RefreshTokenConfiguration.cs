using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Auth;

namespace PIPDC.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(t => t.UserId);
    }
}
