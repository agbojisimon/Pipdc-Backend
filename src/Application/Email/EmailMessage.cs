namespace PIPDC.Application.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? ToName = null,
    string? TextBody = null)
{
    /// <summary>
    /// Adds List-Unsubscribe headers. Keep <c>true</c> for bulk-style notifications;
    /// set <c>false</c> for transactional security mail so filters treat it as
    /// transactional rather than marketing.
    /// </summary>
    public bool IncludeUnsubscribe { get; init; } = true;
}