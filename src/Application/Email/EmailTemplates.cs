namespace PIPDC.Application.Email;

/// <summary>
/// Factory methods that produce <see cref="EmailMessage"/> instances for
/// each notification event.  The application layer calls these; the
/// infrastructure layer (GmailApiEmailService) sends them.
/// </summary>
public static class EmailTemplates
{
    private const string BrandName = "PIPDC";

    /// <summary>
    /// Set once at startup from <c>GmailApiSettings.SenderEmail</c> so that
    /// unsubscribe mailto: links can reference the sender address.
    /// </summary>
    public static string UnsubscribeEmail { get; set; } = string.Empty;

    // ── 1. New enquiry → Agent ───────────────────────────────────────────

    public static EmailMessage NewEnquiryToAgent(
        string agentEmail,
        string agentName,
        string clientName,
        string clientMessage,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"New enquiry about {propertyTitle}";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">New Property Enquiry</h2>
          <p>Hi <strong>{Esc(agentName)}</strong>,</p>
          <p><strong>{Esc(clientName)}</strong> has submitted a new enquiry regarding <strong>{Esc(propertyTitle)}</strong>.</p>
          <blockquote style="border-left:4px solid #1a5276;padding:12px 16px;margin:16px 0;background:#f8f9fa">
            {Esc(clientMessage)}
          </blockquote>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View &amp; Reply to Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        New Property Enquiry

        Hi {agentName},

        {clientName} has submitted a new enquiry regarding {propertyTitle}.

        Message:
        {clientMessage}

        View and reply: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(agentEmail, subject, html, agentName, text);
    }

    // ── 2. Agent reply → Client ──────────────────────────────────────────

    public static EmailMessage AgentReplyToClient(
        string clientEmail,
        string clientName,
        string agentName,
        string messagePreview,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"{agentName} replied to your enquiry about {propertyTitle}";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">New Reply</h2>
          <p>Hi <strong>{Esc(clientName)}</strong>,</p>
          <p><strong>{Esc(agentName)}</strong> has replied to your enquiry about <strong>{Esc(propertyTitle)}</strong>.</p>
          <blockquote style="border-left:4px solid #1a5276;padding:12px 16px;margin:16px 0;background:#f8f9fa">
            {Esc(messagePreview)}
          </blockquote>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View &amp; Reply to Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        New Reply

        Hi {clientName},

        {agentName} has replied to your enquiry about {propertyTitle}.

        Message preview:
        {messagePreview}

        View and reply: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(clientEmail, subject, html, clientName, text);
    }

    // ── 3. Client reply → Agent ──────────────────────────────────────────

    public static EmailMessage ClientReplyToAgent(
        string agentEmail,
        string agentName,
        string clientName,
        string messagePreview,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"{clientName} replied to their enquiry about {propertyTitle}";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">New Reply</h2>
          <p>Hi <strong>{Esc(agentName)}</strong>,</p>
          <p><strong>{Esc(clientName)}</strong> has replied to the enquiry about <strong>{Esc(propertyTitle)}</strong>.</p>
          <blockquote style="border-left:4px solid #1a5276;padding:12px 16px;margin:16px 0;background:#f8f9fa">
            {Esc(messagePreview)}
          </blockquote>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View &amp; Reply to Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        New Reply

        Hi {agentName},

        {clientName} has replied to the enquiry about {propertyTitle}.

        Message preview:
        {messagePreview}

        View and reply: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(agentEmail, subject, html, agentName, text);
    }

    // ── 4. Viewing scheduled → Client + Agent ────────────────────────────

    public static EmailMessage ViewingScheduledToClient(
        string clientEmail,
        string clientName,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"Viewing scheduled for {propertyTitle}";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">Viewing Scheduled</h2>
          <p>Hi <strong>{Esc(clientName)}</strong>,</p>
          <p>A viewing has been scheduled for <strong>{Esc(propertyTitle)}</strong>.</p>
          <p>Please check your conversation for further details from the agent.</p>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        Viewing Scheduled

        Hi {clientName},

        A viewing has been scheduled for {propertyTitle}.
        Please check your conversation for further details from the agent.

        View enquiry: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(clientEmail, subject, html, clientName, text);
    }

    public static EmailMessage ViewingScheduledToAgent(
        string agentEmail,
        string agentName,
        string clientName,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"Viewing scheduled for {propertyTitle}";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">Viewing Scheduled</h2>
          <p>Hi <strong>{Esc(agentName)}</strong>,</p>
          <p>A viewing has been scheduled with <strong>{Esc(clientName)}</strong> for <strong>{Esc(propertyTitle)}</strong>.</p>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        Viewing Scheduled

        Hi {agentName},

        A viewing has been scheduled with {clientName} for {propertyTitle}.

        View enquiry: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(agentEmail, subject, html, agentName, text);
    }

    // ── 5. Enquiry resolved → Client ─────────────────────────────────────

    public static EmailMessage EnquiryResolvedToClient(
        string clientEmail,
        string clientName,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"Your enquiry about {propertyTitle} has been resolved";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">Enquiry Resolved</h2>
          <p>Hi <strong>{Esc(clientName)}</strong>,</p>
          <p>Your enquiry about <strong>{Esc(propertyTitle)}</strong> has been marked as resolved.</p>
          <p>If you have any further questions, feel free to submit a new enquiry.</p>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        Enquiry Resolved

        Hi {clientName},

        Your enquiry about {propertyTitle} has been marked as resolved.
        If you have any further questions, feel free to submit a new enquiry.

        View enquiry: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(clientEmail, subject, html, clientName, text);
    }

    // ── 6. Admin notifies Agent ──────────────────────────────────────────

    public static EmailMessage AdminNotifyToAgent(
        string agentEmail,
        string agentName,
        string clientName,
        string propertyTitle,
        int enquiryId,
        string baseUrl)
    {
        var ctaUrl = $"{baseUrl}/enquiries/{enquiryId}";
        var subject = $"Enquiry from {clientName} about {propertyTitle} needs your attention";
        var unsubscribe = $"mailto:{UnsubscribeEmail}?subject=Unsubscribe";

        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#333">
          <h2 style="color:#1a5276">Enquiry Reminder</h2>
          <p>Hi <strong>{Esc(agentName)}</strong>,</p>
          <p>An admin has flagged the enquiry from <strong>{Esc(clientName)}</strong> regarding <strong>{Esc(propertyTitle)}</strong> for your attention.</p>
          <a href="{ctaUrl}" style="display:inline-block;padding:12px 24px;background:#1a5276;color:#fff;text-decoration:none;border-radius:4px;margin:16px 0">View &amp; Reply to Enquiry</a>
          <p style="font-size:12px;color:#888;margin-top:24px">This is an automated notification from {BrandName}.<br><a href="{unsubscribe}">Unsubscribe</a></p>
        </div>
        """;

        var text = $"""
        Enquiry Reminder

        Hi {agentName},

        An admin has flagged the enquiry from {clientName} regarding {propertyTitle} for your attention.

        View and reply: {ctaUrl}

        ---
        This is an automated notification from {BrandName}.
        Unsubscribe: {unsubscribe}
        """;

        return new EmailMessage(agentEmail, subject, html, agentName, text);
    }

    // ── Helper ───────────────────────────────────────────────────────────

    private static string Esc(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
