namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Single source of truth for rate-limit policy names. Controllers reference these
/// constants (not magic strings) via [EnableRateLimiting(...)].
/// </summary>
public static class RateLimitPolicies
{
    public const string Global = "global";
    public const string AuthStrict = "auth-strict";
    public const string Writes = "writes";
    public const string Uploads = "uploads";
}
