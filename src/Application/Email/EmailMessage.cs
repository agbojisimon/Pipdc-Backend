namespace PIPDC.Application.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? ToName = null,
    string? TextBody = null);
