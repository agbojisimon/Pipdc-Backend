using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Infrastructure.Data.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(b => b.Slug)
            .IsUnique();

        builder.Property(b => b.Content)
            .IsRequired();

        builder.Property(b => b.Excerpt)
            .HasMaxLength(1000);

        builder.Property(b => b.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(b => b.CoverImagePublicId)
            .HasMaxLength(200);

        builder.Property(b => b.Status)
            .HasConversion<string>();

        builder.Property(b => b.KeyQuote)
            .HasMaxLength(500);

        builder.HasOne(b => b.Category)
            .WithMany(c => c.BlogPosts)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
