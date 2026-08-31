# PIPDC — Production Hardening Implementation Guide

Learning-led companion to **Phase 12** of `PIPDC_DEVELOPMENT_ROADMAP.md`.

This phase is not about "installing a package and forgetting it". It is about you
understanding *why* each control exists, *how* it works, and being able to **prove**
it works — live, during your defense.

> **How to work through this guide**
> - We implement in order **A → H**, step by step. I narrate and review; you run every
>   command and make every change yourself.
> - Every section: **WHY** (concept) → **STEP** (what to change + exact code) → **VERIFY**
>   (how to prove it works).
> - Tick items off in the **Appendix checklist** as you go.
> - Context (2026): personal / school-defense project running on **localhost**.
>   The infrastructure parts are written as a **deployment runbook** for when you
>   eventually host it publicly; their in-code parts are built now.

---

## SECTION 0 — CONCEPTS YOU'LL USE EVERYWHERE

Read once, cheap, saves hours of confusion later.

### 0.1 Defense in depth
Bots are not stopped by any single control — they are *deterred* by a stack. Each layer
catches what the layer above missed:

```
            INTERNET
                 │
                 ▼
      Cloudflare (proxy/WAF/DDoS)   ← deployment runbook (12.E)
                 │
                 ▼
      ASP.NET Core
        ┌──────────────────┐
        │ Rate limiting    │  ← 12.B
        │ Authentication+  │
        │ Authorization    │
        │ Turnstile check  │  ← 12.C
        │ Validation       │
        │ Business rules   │  ← 12.F
        └────────┬─────────┘
                 ▼
           PostgreSQL       ← backups/PITR (12.E), indexes (12.H), xmin (12.G)
```

### 0.2 Rate limiting: fixed window vs sliding window vs token bucket vs concurrency
- **Fixed window**: N per minute, resets every minute. Simple; bursts at a window boundary.
- **Sliding window**: smoother; weights a rolling window so the boundary burst disappears.
- **Token bucket**: a bucket of N tokens refilled at R/second. Allows bursts, smooths spikes. Best "feel" for user-facing APIs.
- **Concurrency**: caps *in-flight* requests (not rate). Use for expensive/long operations like uploads.

Rule of thumb (what the pros do):
> Start with **fixed window** (easiest to explain/defend), use **concurrency** for uploads, and
> use **token bucket** only once you've measured client behavior.

### 0.3 Partitioning — THE most important idea
"If one user can eat the whole budget for everyone, it's not rate limiting, it's a shared bucket."
Partitioning means **every caller gets their own bucket**, keyed by something that identifies
them: **user id (authenticated)** or **IP address (anonymous)**.

**Critical trap**: behind a reverse proxy/CDN, `RemoteIpAddress` is the *proxy's* IP, so every
anonymous caller lands in one bucket. You MUST configure forwarded headers (0.4) first.

### 0.4 Real client IPs behind a proxy
`app.UseForwardedHeaders()` once, early. It honors `X-Forwarded-For`/`X-Forwarded-Proto`
**only from proxies you trust** (`KnownProxies`/`KnownNetworks`). Local dev = loopback; at
deployment you add Cloudflare's published IP ranges. Without this, IP-based rate limiting,
audit logs and geo-rules silently break.

### 0.5 Turnstile tokens (CAPTCHA)
- The widget runs in the browser and produces a **token**.
- Tokens are **single-use** and expire after **5 minutes** (must be validated on submit).
- **Never trust the token on its own** — the server must call Cloudflare's `siteverify` API
  and confirm `success=true`.
- A "managed" widget is invisible to most humans; bots get a challenge.

### 0.6 Liveness vs readiness (health checks)
- **Liveness** = "is the process alive; should something restart it?" Must NOT check the
  database — otherwise a DB blip restart-loops the whole app.
- **Readiness** = "can this instance serve traffic right now?" THIS is where DB checks live.
  A DB failure takes the instance out of rotation but does not kill it.

### 0.7 Backups: RPO, RTO, WAL, PITR (the "never done this before" part)
- **RPO** = how much data you can afford to lose.
- **RTO** = how fast you must be back online.
- PostgreSQL writes every change to the **Write-Ahead Log (WAL)** *before* touching data
  files. If we **archive** the WAL continuously, we can restore a base backup and **replay
  the WAL forward** to *any instant* — this is **point-in-time recovery (PITR)**.
- **A backup that has never been restored is a hypothesis.** Restore drills turn trust into
  proof.

### 0.8 Idempotency
Some operations must not double-apply even if the client retries. The client sends an
`Idempotency-Key`; the server remembers (key, result); a repeated key returns the stored
result instead of doing the work again. Webhookish POST /payments / enquiry-creation / sends.

### 0.9 Optimistic concurrency (xmin)
PostgreSQL keeps a hidden system column **`xmin`** — the transaction that last wrote the row.
If two clients read a row and both try to update, the second one to commit finds its `xmin`
is stale → conflict detected → you decide how to react. Free, no schema change, industry
standard for Postgres.

---

## WORKSTREAM A — SECURITY HARDENING

Small changes, mostly config. Do these in this order so you can verify each easily.

### A.1 Identity lockout (login brute-force)

**WHY:** right now `LoginAsync` never touches `AccessFailedCount`, so a script can try
thousands of passwords against one account. ASP.NET Identity ships the machinery; we turn it on.

**STEP 1 — enable lockout options** in `AddIdentity` (`src/Infrastructure/DependencyInjection.cs:27-32`):

```csharp
services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
```

**STEP 2 — drive it from `LoginAsync`** (`src/Infrastructure/Auth/AuthService.cs`). Your current
logic checks password and returns `INVALID_CREDENTIALS`. Insert:

```csharp
var user = await _userManager.FindByEmailAsync(request.Email);
if (user is null) { /* return generic INVALID_CREDENTIALS (keep anti-enumeration) */ }

if (await _userManager.IsLockedOutAsync(user))
{
    // Return a locked-out error (e.g. Error.Unauthorized("ACCOUNT_LOCKED", ...))
}

if (!await _userManager.CheckPasswordAsync(user, request.Password))
{
    await _userManager.AccessFailedAsync(user);
    // return generic INVALID_CREDENTIALS as before
}

await _userManager.ResetAccessFailedCountAsync(user);
```

**VERIFY A.1:** log in 6 times with a wrong password → on the 6th attempt you get the lockout
error even with the correct password; wait 15 minutes (or unlock via SQL
`UPDATE "AspNetUsers" SET "LockoutEnd" = NULL WHERE "Email"=...`) and it works again.

### A.2 Fail-closed CORS

**WHY:** currently `DependencyInjection.cs:88-95` **allows every origin when the allow-list is
empty** (`SetIsOriginAllowed(_ => true)`) — intended for "dev", but a misconfigured prod would
let any site call the API with your cookies/credentials. CORS must **refuse by default**.

**STEP:** replace the fallback with an explicit error when the list is empty:

```csharp
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { config["Cors:AllowedOrigin"]! }.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();

if (allowedOrigins.Length == 0)
{
    // Never allow-all. Log a hard error at startup instead.
    throw new InvalidOperationException("Cors:AllowedOrigins is not configured.");
}

services.AddCors(options => options.AddPolicy("AllowFrontend",
    policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
```

**VERIFY A.2:** with `Cors:AllowedOrigins` empty the app fails to start (expected — that's
fail-closed). Set `["http://localhost:5173"]` and it starts; requests from a different origin
get the CORS error in the browser console.

### A.3 Security headers (HTTP response headers + CSP)

**WHY:** these are cheap floor-raisers against MIME-sniffing, clickjacking, referer leaks and
XSS payloads. CSP is the big one. Two delivery points:
- **Frontend SPA** → CSP via a `<meta>` tag in `index.html` (works on `localhost:5173` now).
- **Backend API responses** → a small middleware adding the header-style headers.

**STEP 1 (frontend)** — add to `PIPDC-Frontend/index.html` `<head>`:

```html
<meta http-equiv="Content-Security-Policy" content="
  default-src 'self';
  script-src 'self' https://challenges.cloudflare.com;
  style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
  font-src https://fonts.gstatic.com;
  img-src 'self' data: https://res.cloudinary.com;
  connect-src 'self' https://challenges.cloudflare.com wss: ws: http://localhost:7123;
  frame-src https://challenges.cloudflare.com;
  base-uri 'self';
  form-action 'self'">
```

> **Note for your defense:** dev writes connect-src `http://localhost:7123` + `ws:` — that is a
> *dev allowance*. At deployment you narrow it to your real domain and `wss:`.

**STEP 2 (backend)** — add a headers middleware in `Program.cs`, before `UseAuthentication`:

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseHttpsRedirection();
```

**VERIFY A.3:** load the frontend on `:5173` — the React app still renders (CSP didn't block
your fonts/images/API). Open DevTools → the CSP violates tab is empty. `curl -I
https://localhost:7123/api/properties` shows the three headers.

### A.4 AllowedHosts + Kestrel body limits

**WHY:** `AllowedHosts: "*"` accepts any Host header (host-header injection, cache poisoning).
And Kestrel's default 30 MB JSON body limit is huge for `{ message, propertyId }`.

**STEP 1** — `appsettings.json`:

```json
{
  "AllowedHosts": "localhost;127.0.0.1;pipdc.plateaustate.gov.ng"
}
```

**STEP 2** — global body cap in `Program.cs`, and a tighter cap on chat/SMTP-style endpoints:

```csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB: matches image cap
});
```

```csharp
[HttpPost]
[RequestSizeLimit(100_000)] // 100 KB is plenty for a message/enquiry body
```

**VERIFY A.4:** POST a >100KB body to an endpoint and get `413 Payload Too Large`; send a
request with a bogus `Host` header and get rejected.

### A.5 Refresh tokens hashed at rest

**WHY:** today the DB stores the raw refresh token (a 64-byte secret). If the DB leaks, valid
tokens leak. Hashing makes the stored copy useless to an attacker (SHA-256 is one-way and
collision-safe enough here).

**STEP:** add a helper and hash before persisting + before looking up:

```csharp
private static string HashToken(string raw) =>
    Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
```

- In `TokenService` you generate the raw token for the client.
- In `AuthService`, persist `HashToken(raw)` into `RefreshTokens.Token`, and look up by
  `HasToken(token)` in `RefreshAsync`/`RevokeAsync`.

> The `Token` column has a unique index; a hash of a random input keeps that property.

**VERIFY A.5:** refresh your session (works), then inspect the `RefreshTokens` table — the
value you see is not the token your client holds.

### A.6 JWT strictness

**WHY:** tokens should be typed and expired-checked. `ValidTypes` hardens against
algorithm-confusion-style misuse; `RequireExpirationTime` rejects tokens with no expiry.

**STEP** — in `DependencyInjection.cs` `TokenValidationParameters`:

```csharp
ValidTypes = new[] { "JWT" },
RequireExpirationTime = true,
ClockSkew = TimeSpan.Zero, // already set
```

**VERIFY A.6:** existing logins still work; a tampered token returns 401.

### A.7 Migrate on startup: dev only

**WHY:** auto-`MigrateAsync` in production is an availability risk (a failed migration can
brick a live deploy) and a concurrency hazard with multiple instances.

**STEP** — `Program.cs:49-57`:

```csharp
if (!app.Environment.IsProduction())
{
    await dbContext.Database.MigrateAsync();
    await RoleSeeder.SeedAsync(dbContext);
}
```

(Controlled migrations: `dotnet ef database update` in Phase 13.)

**VERIFY A.7:** `ASPNETCORE_ENVIRONMENT=Production` + `dotnet run` starts without migrating;
Development still migrates.

---

## WORKSTREAM B — RATE LIMITING

**WHY:** the single highest-leverage protection. Built into ASP.NET Core — no packages.

**Placement rule (from Section 0.3/0.4):**
1. `UseForwardedHeaders()` FIRST (used by every limiter for real IPs).
2. `services.AddRateLimiter(...)` with a **GlobalLimiter** + named policies.
3. `app.UseRateLimiter()` **AFTER** `app.UseAuthentication()` (so the global limiter can
   partition by authenticated user) and BEFORE `app.UseAuthorization()`.

### B.1 Forwarded headers

**STEP** — `Program.cs`, very early (before `UseHttpsRedirection`):

```csharp
using Microsoft.AspNetCore.HttpOverrides;
...
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { new IPNetwork(IPAddress.Loopback, 8) }
});
```

> Deploy runbook: add Cloudflare's published proxy ranges to `KnownNetworks`. (Documented in
> 12.E runbook appendix.)

### B.2 Global partitioned limiter + OnRejected

**STEP** — in `Program.cs`:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var response = context.HttpContext.Response;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("0", CultureInfo.InvariantCulture);

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/problem+json";
        await response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too many requests",
            status = 429,
            detail = "Slow down and try again shortly."
        }, ct);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var isAuthed = context.User.Identity?.IsAuthenticated == true;
        var key = isAuthed
            ? $"u:{context.User.FindFirstValue(ClaimTypes.NameIdentifier)}"
            : $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = isAuthed ? 300 : 60,   // per user vs per IP per minute
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,                      // fail fast, don't queue
            AutoReplenishment = true
        });
    });

    options.AddPolicy("auth-strict", context =>
    {
        var key = $"ip:{context.Connection.RemoteIpAddress}"; // limit BEFORE identity exists
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0, AutoReplenishment = true
        });
    });

    options.AddPolicy("writes", context => /* PermitLimit = 30, partition by user-or-ip as global */);
    options.AddPolicy("admin",  context => /* PermitLimit = 60, partition by user */);

    options.AddConcurrencyLimiter("uploads", opts =>
    {
        opts.PermitLimit = 4;   // max 4 concurrent image uploads per instance
        opts.QueueLimit = 0;
    });
});
```

Then attach policies with attributes on controllers:

```csharp
[EnableRateLimiting("auth-strict")]
public class AuthController : ControllerBase { ... }

[EnableRateLimiting("writes")]
public class EnquiriesController : ControllerBase { ... }
// MessagesController, DevelopmentTrackingController, SavedPropertiesController too
```

**VERIFY B.2 (great demo):** hit `POST /api/auth/login` 6 times in a PowerShell loop:

```powershell
1..6 | % { Invoke-WebRequest -Method Post -Uri "https://localhost:7123/api/auth/login" `
  -Body '{"email":"x@y.z","password":"wrong"}' -ContentType 'application/json' -SkipCertificateCheck `
  -ErrorAction SilentlyContinue }
```

Requests 1–5 return `400`; number 6 returns **`429` with a `Retry-After` header** and the
problem+json body. That is your proof.

> **Production shout-out:** in-memory counters reset per-process. Single instance = fine.
> The moment you run 2+ instances you need a Redis-backed counter (stack to upgrade later).

---

## WORKSTREAM C — ANTI-BOT: CLOUDFLARE TURNSTILE

**Prerequisite (you do this):** a free Cloudflare account → **Turnstile** → **Add widget**
(homepage for now `localhost`). Copy the **site key** (public) and **secret key** (private).

### C.1 Backend: the verifier (server-side validation is MANDATORY)

**WHY:** a widget alone proves nothing (tokens are forgeable, single-use, expire in 5 min).
The server asks Cloudflare.

**STEP 1** — `TurnstileSettings` bound from config; **STEP 2** — a `TurnstileVerifier` service;
**STEP 3** — an `[VerifyHuman]` action filter:

```csharp
public sealed record TurnstileSettings
{
    public string SecretKey { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
```

```csharp
public sealed class TurnstileVerifier(HttpClient http, IOptions<TurnstileSettings> options, ILogger<TurnstileVerifier> logger)
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> IsHumanAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        if (!options.Value.Enabled) return true;          // kill-switch for local/offline demos
        if (string.IsNullOrWhiteSpace(token)) return false;

        var form = new Dictionary<string, string>
        {
            ["secret"] = options.Value.SecretKey,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;

        using var response = await http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode) { logger.LogWarning("Turnstile unavailable: {Status}", response.StatusCode); return false; } // fail closed

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.TryGetProperty("success", out var success) && success.GetBoolean();
    }
}
```

**STEP 4** — action filter (MVC version, since you use controllers):

```csharp
public sealed class VerifyHumanAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var verifier = context.HttpContext.RequestServices.GetRequiredService<TurnstileVerifier>();
        var token = context.HttpContext.Request.Headers["X-Turnstile-Token"].ToString()
                    ?? context.HttpContext.Request.Form["cf-turnstile-response"].ToString();
        var ip = context.HttpContext.Request.Headers["CF-Connecting-IP"].ToString()
                 ?? context.HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!await verifier.IsHumanAsync(token, ip, context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(new { code = "HUMAN_VERIFICATION_FAILED",
                message = "Please complete the verification and try again.", type = "Validation" })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
        await next();
    }
}
```

**STEP 5** — register in DI (`DependencyInjection.cs` / `Program.cs`):

```csharp
builder.Services.Configure<TurnstileSettings>(builder.Configuration.GetSection("Turnstile"));
builder.Services.AddHttpClient<TurnstileVerifier>(c => c.BaseAddress = new Uri("https://challenges.cloudflare.com"));
```

**STEP 6** — apply to the endpoints bots aim at:

```csharp
[HttpPost]
[VerifyHuman]
public async Task<IActionResult> Register(...) { ... }

[HttpPost]
[VerifyHuman]
public async Task<IActionResult> ForgotPassword(...) { ... }
```

Also on `POST /enquiries` if you want the usage layer covered. *Do NOT put it on GET reads — garbage UX.*

**VERIFY C.1 (dummy-keys demo):** set
`Turnstile:Enabled=true`, `Turnstile:SecretKey=1x0000000000000000000000000000000AA`
(Cloudflare's always-passes test secret). Request without a token → `403
HUMAN_VERIFICATION_FAILED`. With the matching dummy site key → passes. Switch
`Enabled=false` → everything passes (local/offline mode).

### C.2 Frontend: the widget

**STEP 1** — set the public key: add `VITE_TURNSTILE_SITE_KEY=1x00000000000000000000AA` (dummy)
to `PIPDC-Frontend/.env`, `.env.example`, and declare it in `src/vite-env.d.ts`.

**STEP 2** — `src/lib/turnstileLoader.ts`: load the script once
(`https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit`) and expose a tiny
wrapper: `render(el, sitekey)` and `getToken(widgetId)`.

**STEP 3** — `src/components/TurnstileWidget.tsx`: renders the container div and hosts the
token in state; gives you a `reset()` on failure.

**STEP 4** — in `RegisterPage` / `ForgotPasswordPage`, render the widget above the submit
button and send its token as the `X-Turnstile-Token` header on that POST
(`authService.register(..., { headers: { 'X-Turnstile-Token': token } })`).
Same pattern in `enquiryService.create`.

**VERIFY C.2:** with the dummy site key the widget appears (managed mode, usually invisible);
submit register → succeeds. Remove the header → `403`.

> **Defense talking point:** the token is verified server-side at Cloudflare (`siteverify`).
> A bot calling the API directly never produced a token, so it's rejected.

---

## WORKSTREAM D — OBSERVABILITY

**WHY:** you can't fix what you can't see. Three moves: health endpoints, structured logs,
and an uptime monitor. (The alerts list lives in the runbook.)

### D.1 Health checks (liveness + readiness)

**STEP** — `Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(2));   // never let a probe hang

var app = builder.Build();
...
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false,                 // liveness: NO dependency checks
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), durationMs = e.Value.Duration.TotalMilliseconds })
        });
    }
});
```

`AddDbContextCheck` ships with EF Core (no package). An optional upgrade is
`AspNetCore.HealthChecks.NpgSql` for a raw Npgsql probe.

**VERIFY D.1 (great demo):** `GET /healthz/live` → 200. `GET /healthz/ready` → 200 + JSON with
`database: Healthy`. Now **stop PostgreSQL** → `/healthz/ready` returns **503** (JSON shows
`Unhealthy`) while `/healthz/live` still returns 200. That split is exactly why a DB blip
pulls you out of rotation instead of crash-looping you.

### D.2 Structured logging (Serilog)

**WHY:** JSON logs are greppable, queryable and feed monitors. Flat text isn't.

**STEP:**
```bash
dotnet add package Serilog.AspNetCore
```

`Program.cs`:

```csharp
using Serilog;

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("logs/pipdc-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,        // two weeks
        formatter: new Serilog.Formatting.Compact.CompactJsonFormatter()));

...

app.UseSerilogRequestLogging();  // after forwarding/auth, logs each request with status+duration
```

**VERIFY D.2:** hit any endpoint, open `logs/pipdc-<date>.log` — you see compact JSON lines
with timestamps, method, path, status, elapsed ms.

> **Correlation IDs (next step when you need it):** read an incoming `X-Correlation-ID`
> (or generate one) in a small middleware, stash it in a `AsyncLocal`/log context, and include
> it in Serilog enriches + `TraceIdentifier`. This makes multi-service debugging possible.

### D.3 Uptime monitor

**Deploy-time (runbook 12.E).** For now, note: point a free monitor (UptimeRobot/CronAlert) at
`https://yourapp/healthz/ready`, assert the literal text `Healthy`, plus SSL-expiry and
response-time checks, interval < original pool idle timeout.

---

## WORKSTREAM E — AVAILABILITY

The part you've never done. We build what runs locally; the rest is a runbook you apply at deployment.

### E.1 Best-effort email (in code now)

**WHY:** today a Gmail API hiccup makes registrations/enquiries *fail* — an availability bug.
Email is a notification, not the transaction. The business operation must never fail because
email did.

**STEP** — wrap the Gmail send in `GmailApiEmailService.SendAsync` so failures are logged and
swallowed, not thrown:

```csharp
try
{
    // existing send logic
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    logger.LogError(ex, "Email to {To} failed for subject {Subject}", message.To, message.Subject);
    // best-effort: do NOT rethrow so the caller's business operation succeeds
}
```

> Long-term (12.J): this is exactly the workload that eventually earns a background queue —
> fire-and-forget, retries, backoff. Today, best-effort is the right size.

**VERIFY E.1:** intentionally break `GmailApiSettings` (wrong refresh token), then register an
account — the registration **succeeds** (user created, toast shown) and the email failure is
in the log. That's availability in action.

### E.2 Deployment runbook (apply when you host — not now)

**WHY:** "if something's wrong, the site still works" at the infra level = restart-on-crash,
auto-TLS, edge filtering, off-box backups you've proven you can restore, and deploys that can
roll back. Translated for your future single VPS:

1. **systemd unit** (`/etc/systemd/system/pipdc.service`):
   `Restart=always`, `RestartSec=10`, `KillSignal=SIGINT` (graceful drain), `User=<least-privilege>`.
2. **Caddy reverse proxy**: one Caddyfile line `yourdomain.com { reverse_proxy localhost:5000 }`;
   auto Let's Encrypt TLS, `header_up X-Forwarded-{For,Proto,Host}` (it already does), plus
   the real-IP trust from B.1.
3. **Cloudflare** free plan in front: proxied DNS, WAF/DDOS/bot. SSL: **Full (strict)**.
   (Your existing cost: $0.)
4. **Zero-downtime deploys**: publish to `/opt/pipdc/releases/<sha>` → symlink `current` →
   `systemctl restart` → poll `/healthz/ready` == `Healthy` → else roll back symlink + restart.
   GitHub Actions drives this (Phase 13).
5. **Static frontend** built and served (or on Cloudflare Pages) so even if the API is down,
   the site shell and error messaging still load.

### E.3 PostgreSQL durability (WAL + PITR + restore drills)

**WHY / HOW — you'll LOVE this one for defense:** Postgres already has it; we just turn it on.

**STEP 1 — WAL archiving** in `postgresql.conf`:
```conf
wal_level = replica
archive_mode = on
archive_command = 'pgbackrest --stanza=pipdc archive-push %p'
archive_timeout = 60s
```
Set `archive_timeout` so even a quiet DB keeps producing an archive stream (an RPO gap is
silent until you need it).

**STEP 2 — pgBackRest** (apt `pgbackrest`): a stanza file, a repository. **Offsite** =
S3/R2-compatible (a backup on the same disk is not a backup; it's a recycle bin). Retention
≈ 5 fulls. Schedule weekly full + daily incremental via cron/systemd timer.

**STEP 3 — the non-negotiables:**
- `pgbackrest --stanza=pipdc check` → proves archiving works end-to-end.
- `pgbackrest info` → your backup/restore health signal (alert on age).
- **Restore drill, monthly/quarterly**: to a scratch server → `pgbackrest restore --set=<set>`
  → start Postgres → row-count checks on critical tables → announce success. Log
  `pg_stat_archiver` failures and drill results.
- Keep a plain `pg_dump` too (portable, table-level rescue, future version upgrades).

**Targets you commit to:** **RTO ≤ 60 min, RPO ≤ 15 min** — then the drill *proves* it.

**VERIFY E.3 (local learning drill):** exercise the full restore on a scratch Postgres
container today. If you restore to the minute before you "deleted" a table, you've just done
real PITR — a superb demo.

### E.4 Scale path (Phase 13+, document only)

Your app is already stateless (JWT + DB refresh tokens) → ready for: 2+ instances + LB;
Postgres primary/standby (Patroni auto-failover); Redis SignalR backplane (your hub needs it
at scale) + distributed rate-limit counters. Not now.

---

## WORKSTREAM F — BUSINESS PROTECTION (idempotency + anti-spam)

**WHY:** two server-side rules stop the *business-level* abuse the pasted plan called "the most
underrated protection".

### F.1 Server-side duplicate rule for enquiries

**WHY:** the frontend de-dupes with "GET `/enquiries/mine` then POST" — that's a race, and a
bot ignores your UI entirely.

**STEP** — in the enquiry-create service/controller: if an **open** enquiry exists for the same
(user, property), return it instead of creating:

```csharp
var existing = await _db.Enquiries.FirstOrDefaultAsync(
    e => e.ClientId == userId
      && e.PropertyId == request.PropertyId
      && /* open status: not Resolved/Closed */);
if (existing is not null) return existing;   // reuse, don't duplicate
```

**VERIFY F.1:** POST the same (user, property, active) twice → you get the same enquiry id back.

### F.2 Idempotency keys

**WHY:** network retries and double-clicks otherwise create duplicate enquiries/messages even
with F.1 in place. The client sends a key; the server remembers the result.

**STEP 1** — new entity `IdempotencyRecord(UserId, Key, Status, CreatedAt, ExpiresAt)` +
table + unique index on `(UserId, Key)`.

**STEP 2** — an action filter on `POST /enquiries` (and message sends):

```csharp
public sealed class IdempotentAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers["Idempotency-Key"].ToString();
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(userId)) { await next(); return; }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var existing = await db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Key == key);
        if (existing is not null)  // replay: return the recorded outcome
        { context.Result = existing.ResultStatus == 201
            ? new ObjectResult(existing.ResultBody) { StatusCode = existing.ResultStatus }
            : new StatusCodeResult(existing.ResultStatus);
          return; }

        var result = (IActionResult)await next();
        db.IdempotencyRecords.Add(new IdempotencyRecord { UserId = userId, Key = key,
            Status = ..., ExpiresAt = DateTime.UtcNow.AddHours(24) });
        await db.SaveChangesAsync();
    }
}
```
(TTL cleanup: a background timer or delete-older-than-24h on write — 12.J territory.)

**VERIFY F.2:** POST an enquiry with header `Idempotency-Key: demo-1` twice → one row created,
second call returns the first result + no duplicate.

---

## WORKSTREAM G — CONCURRENCY CONTROL (xmin)

**WHY:** today, two admins editing the same property/enquiry = last write wins silently.
Postgres tracks row versions via the hidden `xmin` column; we expose it as a concurrency token.

**STEP 1** — in configuration (e.g. `PropertyConfiguration`, `EnquiryConfiguration`,
`AgentConfiguration`, `DevelopmentProjectConfiguration`):

```csharp
builder.Entity<Property>().UseXminAsConcurrencyToken();
```

**STEP 2** — handle the conflict where you update:

```csharp
try { await _db.SaveChangesAsync(); }
catch (DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
    { var current = await entry.GetDatabaseValuesAsync(); /* current is the fresh row */ }
    return Conflict("Another user changed this record. Reload and retry.");
}
```

> `.UseXminAsConcurrencyToken()` needs no schema change/migration — it maps the existing
> `xmin` system column. This is the industry-standard Postgres approach (versus SQL Server's
> `rowversion` blob).

**VERIFY G:** open the same property in two browser tabs (or hammer two requests) and update
both — the second gets a conflict error instead of silently overwriting.

---

## WORKSTREAM H — DATABASE OPTIMIZATION (indexes)

**WHY:** indexes make hot lookups fast and amplification attacks expensive. Cheap wins first.

**STEP 1** — audits (did you keep these from earlier phases?):
- `RefreshTokens.Token` → unique ✓ (verify it exists in the migration).
- `VerificationCodes` → index on `(Code)` and `(UserId)`.
- `Enquiries` → indexes on `(PropertyId, Status)` and `(AgentId)`.
- Properties listing filters/search → index on the columns your filters use.

**STEP 2** — with EF Core configuration, e.g.:

```csharp
builder.HasIndex(e => e.Code);
builder.HasIndex(e => new { e.PropertyId, e.Status });
```

then a migration:

```bash
dotnet ef migrations add AddHotPathIndexes
dotnet ef database update
```

**VERIFY H:** `EXPLAIN ANALYZE SELECT ... WHERE "PropertyId"=...` before/after — you should see
`Index Scan` instead of `Seq Scan` on the row-hungry tables.

---

## APPENDIX 1 — MASTER VERIFICATION CHECKLIST

| # | Control | Prove it | Done |
|---|---|---|---|
| A.1 | Lockout | 6th wrong login → locked-out error | ☐ |
| A.2 | CORS fail-closed | empty list → startup error; localhost:5173 works | ☐ |
| A.3 | Headers/CSP | `curl -I` shows 3 headers; DevTools CSP clean | ☐ |
| A.4 | Body limits | >100KB POST → 413 | ☐ |
| A.5 | RT hashing | DB stores hash, refresh still works | ☐ |
| A.6 | JWT strict | tampered token → 401 | ☐ |
| A.7 | Migrate dev-only | Prod env starts without migrating | ☐ |
| B | Rate limits | 6th login → `429` + `Retry-After` | ☐ |
| C | Turnstile | no token → `403`; dummy token → passes | ☐ |
| D.1 | Health split | stop Postgres: ready 503, live 200 | ☐ |
| D.2 | Serilog | JSON lines in `logs/` | ☐ |
| E.1 | Best-effort email | broken Gmail settings → registration still succeeds | ☐ |
| E.3 | Backup/restore | `pgbackrest check` green + completed restore drill | ☐ |
| F.1 | Duplicate rule | 2nd identical enquiry returns same id | ☐ |
| F.2 | Idempotency | same key twice → one row | ☐ |
| G | xmin | conflicting update → concurrency conflict | ☐ |
| H | Indexes | `EXPLAIN ANALYZE` shows Index Scan | ☐ |

## APPENDIX 2 — SECRETS & CONFIG YOU OWN

| Key | Where | Who sets it |
|---|---|---|
| `Turnstile:SecretKey` | user-secrets | you (Cloudflare account) |
| `VITE_TURNSTILE_SITE_KEY` | frontend `.env` | you (public, not secret) |
| Seed/DB/JWT/Gmail/Cloudinary keys | user-secrets | already in place |
| `Cors:AllowedOrigins` | appsettings | `["http://localhost:5173"]` dev; your domain later |

Local **dummy** Turnstile keys (from Cloudflare docs) for offline demos:
- Always pass: site `1x00000000000000000000AA`, secret `1x0000000000000000000000000000000AA`
- Always block: site `2x00000000000000000000AB`, secret `2x0000000000000000000000000000000AA`

## APPENDIX 3 — FILES YOU'LL TOUCH

Backend: `Program.cs`, `src/Infrastructure/DependencyInjection.cs`,
`src/Infrastructure/Auth/AuthService.cs`, `src/Infrastructure/Auth/TokenService.cs`,
`src/Infrastructure/Email/GmailApiEmailService.cs`, auth/email DTOs,
`src/Infrastructure/Data/Configurations/*`, new `TurnstileVerifier` +
`TurnstileSettings`, new `IdempotencyRecord` + filter, controllers (rate-limit + `[VerifyHuman]`
attributes), `appsettings.json`. Frontend: `index.html` (CSP), `.env`/`.env.example`/
`vite-env.d.ts` (site key), new `TurnstileWidget` component, `LoginPage/RegisterPage/
ForgotPasswordPage`, `enquiryService`.

## APPENDIX 4 — DEFENSE PRESENTATION CUES

- Open the pasted "internet → Cloudflare → ASP.NET → CAPTCHA → API → PostgreSQL" diagram and
  walk each layer with a *live proof* from the checklist.
- Rate limiting: show the 429 + Retry-After in PowerShell.
- Health split: stop Postgres live, show ready=503 / live=200.
- Turnstile: show a header-less request curl → 403, then the widget flow succeeding.
- PITR: "I deleted this row at T; I restored the DB to T−1min and here it is."
- Best-effort email: "Gmail tokens broken, registration still succeeds."

**Final thought:** every control in this guide is a *story* you can now tell — what it is, why
it exists, and how you proved it. That is the defense.