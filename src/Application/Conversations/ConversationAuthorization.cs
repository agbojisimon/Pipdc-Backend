using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Auth;
using PIPDC.Application.Data;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.Conversations;

// Centralized authorization for conversation/enquiry access.
// Admin can inspect any conversation but is never treated as a sender participant.
internal static class ConversationAuthorization
{
    public static async Task<bool> CanAccessConversationAsync(
        IAppDbContext dbContext, Conversation conversation, string currentUserId, IList<string> currentUserRoles, CancellationToken ct)
    {
        if (currentUserRoles.Contains(Roles.Admin))
            return true;

        return await IsParticipantAsync(dbContext, conversation, currentUserId, ct);
    }

    public static async Task<bool> IsParticipantAsync(
        IAppDbContext dbContext, Conversation conversation, string currentUserId, CancellationToken ct)
    {
        if (conversation.ClientUserId == currentUserId)
            return true;

        // The agent linked to the conversation (the agent who manages the enquiry's property).
        return await dbContext.Agents.AnyAsync(
            a => a.Id == conversation.AgentId && a.UserId == currentUserId, ct);
    }

    public static async Task<bool> IsEnquiryParticipantAsync(
        IAppDbContext dbContext, Enquiry enquiry, string currentUserId, CancellationToken ct)
    {
        if (enquiry.UserId == currentUserId)
            return true;

        // The agent who manages the enquiry's property.
        return await dbContext.Properties.AnyAsync(
            p => p.Id == enquiry.PropertyId && p.Agent != null && p.Agent.UserId == currentUserId, ct);
    }

    public static async Task<bool> CanAccessEnquiryAsync(
        IAppDbContext dbContext, Enquiry enquiry, string currentUserId, IList<string> currentUserRoles, CancellationToken ct)
    {
        if (currentUserRoles.Contains(Roles.Admin))
            return true;

        if (enquiry.UserId == currentUserId)
            return true;

        // The agent who manages the enquiry's property.
        return await dbContext.Properties.AnyAsync(
            p => p.Id == enquiry.PropertyId && p.Agent != null && p.Agent.UserId == currentUserId, ct);
    }
}
