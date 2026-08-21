namespace PIPDC.Application.Email;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
