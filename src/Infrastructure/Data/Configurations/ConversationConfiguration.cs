using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.Property(c => c.ClientUserId)
            .IsRequired()
            .HasMaxLength(450);

        // One enquiry has at most one conversation. Enforced at the database level.
        builder.HasIndex(c => c.EnquiryId)
            .IsUnique();

        // Efficient lookup of a user's conversations and an agent's conversations.
        builder.HasIndex(c => c.ClientUserId);

        builder.HasIndex(c => c.AgentId);

        builder.HasOne(c => c.Enquiry)
            .WithOne(e => e.Conversation)
            .HasForeignKey<Conversation>(c => c.EnquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Client)
            .WithMany(u => u.Conversations)
            .HasForeignKey(c => c.ClientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Agent)
            .WithMany(a => a.Conversations)
            .HasForeignKey(c => c.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
