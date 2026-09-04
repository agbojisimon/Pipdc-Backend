namespace PIPDC.Infrastructure.Captcha;

public sealed class TurnstileSettings
{
    public string SecretKey { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string? ExpectedHostname { get; set; }
}