namespace PIPDC.API.Hubs;

// Single server-side group-name convention for conversation delivery.
// Group names are never accepted from the client; they are always generated
// here from the persisted conversation id, e.g. conversation:42.
internal static class ConversationGroup
{
    public static string For(int conversationId) => $"conversation:{conversationId}";
}
