using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Data;
using PIPDC.Domain.Auth;
using PIPDC.Domain.Entities;

namespace PIPDC.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<SaleRecord> SaleRecords => Set<SaleRecord>();
    public DbSet<LeaseRecord> LeaseRecords => Set<LeaseRecord>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();
    public DbSet<AiChatSession> AiChatSessions => Set<AiChatSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DevelopmentProject> DevelopmentProjects => Set<DevelopmentProject>();
    public DbSet<DevelopmentUnit> DevelopmentUnits => Set<DevelopmentUnit>();
    public DbSet<DevelopmentUpdate> DevelopmentUpdates => Set<DevelopmentUpdate>();
    public DbSet<DevelopmentProjectImage> DevelopmentProjectImages => Set<DevelopmentProjectImage>();
    public DbSet<DevelopmentTracking> DevelopmentTrackings => Set<DevelopmentTracking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
