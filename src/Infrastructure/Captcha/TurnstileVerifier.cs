using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PIPDC.Infrastructure.Captcha;

public sealed class TurnstileVerifier(
    HttpClient http,
    IOptions<TurnstileSettings> options,
    ILogger<TurnstileVerifier> logger)
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> IsHumanAsync(
        string? token, string? remoteIp, string? idempotencyKey = null, CancellationToken ct = default)
    {
        if (!options.Value.Enabled) return true; // kill-switch for local/offline demos
        if (string.IsNullOrWhiteSpace(token)) return false;

        var form = new Dictionary<string, string>
        {
            ["secret"] = options.Value.SecretKey,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;
        if (!string.IsNullOrWhiteSpace(idempotencyKey)) form["idempotency_key"] = idempotencyKey;

        using var response = await http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Turnstile unavailable: {Status}", response.StatusCode);
            return false; // fail closed
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        if (!result.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            var codes = result.TryGetProperty("error-codes", out var codesEl)
                ? string.Join(", ", codesEl.EnumerateArray().Select(c => c.GetString()))
                : "(none)";
            logger.LogWarning("Turnstile verification failed. Error codes: {Codes}", codes);
            return false;
        }

        // Hostname pinning: only enforce when configured, so dev (dummy keys) is unaffected.
        var expected = options.Value.ExpectedHostname;
        if (!string.IsNullOrWhiteSpace(expected))
        {
            var hostname = result.TryGetProperty("hostname", out var h) ? h.GetString() : null;
            if (!string.Equals(hostname, expected, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Turnstile hostname mismatch: expected {Expected}, got {Actual}", expected, hostname);
                return false;
            }
        }

        return true;
    }
}