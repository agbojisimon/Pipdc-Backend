using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class AiChatSessionConfiguration : IEntityTypeConfiguration<AiChatSession>
{
    public void Configure(EntityTypeBuilder<AiChatSession> builder)
    {
        builder.Property(s => s.Title)
            .HasMaxLength(200);

        builder.Property(s => s.MessagesJson)
            .IsRequired();

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasOne(s => s.User)
            .WithMany(u => u.AiChatSessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
