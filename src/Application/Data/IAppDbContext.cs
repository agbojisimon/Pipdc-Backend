using Microsoft.EntityFrameworkCore;
using PIPDC.Domain.Auth;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.Data;

public interface IAppDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<PropertyImage> PropertyImages { get; }
    DbSet<Agent> Agents { get; }
    DbSet<Enquiry> Enquiries { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<SaleRecord> SaleRecords { get; }
    DbSet<LeaseRecord> LeaseRecords { get; }
    DbSet<BlogPost> BlogPosts { get; }
    DbSet<SavedProperty> SavedProperties { get; }
    DbSet<AiChatSession> AiChatSessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
