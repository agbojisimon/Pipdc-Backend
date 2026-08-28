# Email Verification & Password Reset — Step-by-Step Implementation Guide

> This document is a **walking-through-the-implementation** reference. It explains, step by step,
> how the PIPDC auth system was wired up so that the **register**, **forgot password**, and
> **change password** flows send a verification code to the user's email — in a way that is
> close to industry standard for a production system.
>
> Each step lists: the file to touch or create, the exact code, WHY it is done this way,
> and any security considerations. You can re-run this guide from scratch on a fresh
> checkout to reproduce the implementation.

---

## 0. The big picture (read this first)

### What we are building

We are adding a **code-based email verification** layer around four auth interactions:

| Interaction | What happens now | What changes |
| --- | --- | --- |
| `Register` | Creates the user, they can sign in immediately | Creating the user **and sending a 6-digit code** to their email. Sign-in is **blocked until verified**. |
| `Login` | Any user signs in | Unverified users receive `EMAIL_NOT_CONFIRMED` so the frontend sends them to verify. |
| `Forgot password` | Stub — claims success, sends nothing | Generates a 6-digit password-reset code and emails it. |
| `Reset password` | Does not exist | New endpoint: email + code + new password → password reset. |
| `Change password` (logged in) | Works already | After success, emails a "your password was changed" **notification** (not a code step). |

### Industry-standard concepts we are applying

1. **Codes, not magic links.** A short-lived 6-digit code typed into a form is a common pattern
   (Google, GitHub, banking apps all use it). It avoids hosting one-time URLs and works well
   on mobile.
2. **Codes are short-lived** (15 minutes here), **single-use**, and **limited to a number of
   attempts** (5). This prevents brute-force guessing and replay.
3. **Codes are never stored in plain text.** We store a **salted, iterated hash**
   (PBKDF2-HMAC-SHA256, 100,000 iterations) — the same family of hashing used for passwords.
4. **User enumeration on forgot-password is a product decision.** The common industry default is
   anti-enumeration — respond identically whether or not the email exists ("if an account exists,
   we sent a code"). PIPDC chose the opposite for better UX: forgot-password returns
   `404 USER_NOT_FOUND` for an unknown email, so a mistyped address is caught immediately instead
   of silently doing nothing. Trade-off acknowledged: the endpoint reveals whether an email is
   registered (a minor risk for a public listings platform). `ResendVerificationEmailAsync`
   keeps the anti-enumeration silent-success behavior.
5. **Rate limiting on code issuance.** A 60-second minimum gap between "send me another code"
   requests to stop abuse / email bombing. (Full per-IP rate limiting is typically done at a
   gateway or with Redis — deliberately out of scope here — see Notes.)
6. **Anti-account-lockout for seeded users.** Development/admin/agent seed users are created
   with `EmailConfirmed = true` so the new login guard does not lock your own team out.
7. **Best-effort emails.** Email delivery is wrapped in try/catch so a broken email provider
   never breaks the core auth transaction. When the Gmail API is not configured (local dev),
   the code is logged to the server console so the flow can still be tested.
8. **Reuse existing plumbing.** PIPDC already had a `GmailApiEmailService` and `IEmailService`.
   We do NOT create a new email system — we add templates and call the existing sender.
9. **Transactional vs. bulk email is a deliberate, industry-standard classification.** Gmail
   sorts "transactional/relationship" mail (actions the user or a counterparty took: auth codes,
   replies, enquiry updates) into the Primary inbox and exempts it from some anti-spam junk
   factors, while marketing/bulk mail is expected to carry a working `List-Unsubscribe` and an
   unsubscribe link, and lands in Promotions/Spam. Warning: Gmail/Yahoo began (2024) enforcing
   a one-click unsubscribe + spam-rate thresholds on senders of **bulk** mail only — another
   reason to keep genuinely transactional mail free of those markers. Every PIPDC mail today is
   transactional, and a template-setter must pick the correct class deliberately (`false` =
   transactional: no unsubscribe; `true` = bulk/marketing: unsubscribe required). See Step 8.

### Architecture / file map

**Backend (Clean Architecture, single project `src/`)**

```
Domain (no dependencies)
  src/Domain/Enums/VerificationPurpose.cs        → enum { EmailConfirmation, PasswordReset }
  src/Domain/Auth/VerificationCode.cs             → entity (hashed code, expiry, attempts)

Application
  src/Application/Email/EmailTemplates.cs         → 3 new templates (builds EmailMessage)
  src/Application/Auth/Dtos.cs                    → 3 new request records
  src/Application/Auth/IAuthService.cs            → 3 new method signatures

Infrastructure
  src/Infrastructure/Auth/AuthService.cs          → all logic; hashes; email delivery
  src/Infrastructure/Data/AppDbContext.cs          → DbSet<VerificationCode>

API
  src/API/Controllers/AuthController.cs           → 3 new POST endpoints
```

**Frontend (React 18 + Vite + TS SPA)**

```
src/services/api.ts             → extractApiErrorCode(...) helper
src/services/authService.ts     → verifyEmail, resendVerification, resetPassword
src/hooks/mutations.ts          → useVerifyEmail, useResendVerification, useResetPassword
src/pages/auth/VerifyEmailPage.tsx   (NEW)
src/pages/auth/ResetPasswordPage.tsx (NEW)
src/App.tsx                     → /verify-email and /reset-password routes
src/pages/auth/RegisterPage.tsx       → routes to verify-email after register
src/pages/auth/ForgotPasswordPage.tsx → routes to reset-password after sending code
src/pages/auth/LoginPage.tsx          → handles EMAIL_NOT_CONFIRMED
```

---

## Step 1 — Add the `VerificationPurpose` enum

**File:** `src/Domain/Enums/VerificationPurpose.cs` (new)

```csharp
namespace PIPDC.Domain.Enums;

public enum VerificationPurpose
{
    EmailConfirmation = 0,
    PasswordReset = 1
}
```

**Why:** one code table serves both purposes. Storing the purpose lets us invalidate only the
relevant codes and reuse the same issue/verify helpers.

---

## Step 2 — Add the `VerificationCode` entity

**File:** `src/Domain/Auth/VerificationCode.cs` (new)

```csharp
using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Auth;

public class VerificationCode : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public VerificationPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int Attempts { get; set; }

    public bool IsActive(DateTime utcNow) =>
        !IsUsed && RevokedAt is null && ExpiresAt > utcNow;
}
```

Notes on each field (all are what an industry-standard implementation tracks):
- `UserId` — which user the code belongs to (matches `IdentityUser.Id`).
- `CodeHash` — NOT the code. A `salt:hash` string (see Step 4). An attacker who dumps the DB
  cannot recover codes.
- `ExpiresAt` — absolute expiry time. Codes die after 15 minutes.
- `IsUsed` — single-use: a consumed code cannot be replayed.
- `RevokedAt` — set when a newer code replaces this one, or attempts are exhausted.
- `Attempts` — failed-verification counter to enforce the brute-force cap.
- `IsActive(utcNow)` — convenient state check.

`BaseEntity` already supplies `int Id` and `DateTime CreatedAt`.

**File:** `src/Infrastructure/Data/AppDbContext.cs` — add a DbSet (next to the others, e.g. after `RefreshTokens`):

```csharp
public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
```

(Add `using PIPDC.Domain.Auth;` — already present for `RefreshToken`.)

**Why a table and not Identity's built-in tokens?** ASP.NET Identity ships `GenerateEmailConfirmationTokenAsync`
/ `GeneratePasswordResetTokenAsync`, but those produce long opaque strings, and expiry is a bare
column you manage yourself. A dedicated table gives us: readable session-aware 6-digit codes,
attempt counters, purpose separation, and revocation — all of which map directly to the UI we build.

---

## Step 3 — EF Core migration

We add a migration for the new table. The app auto-applies migrations on startup
(`Program.cs` → `dbContext.Database.MigrateAsync()`), so once the migration exists and the app
starts, the table is created.

Commands (run in the repo root):

```bash
dotnet ef migrations add AddVerificationCodes
```

*Prerequisite: the running backend server must be stopped, because it locks `PIPDC.exe` and
`dotnet build` then fails with MSB3027/MSB3021. This is a build-time issue, not a code error.*

The migration folder will contain an "up" operation creating `VerificationCodes` and a safe
"down" operation dropping it.

**Why a migration?** Never create tables by hand in production. Migrations are versioned,
reviewable history; they are how `MigrateAsync` upgrades a deployed database deterministically.

---

## Step 4 — Backend: request DTOs

**File:** `src/Application/Auth/Dtos.cs` — append:

```csharp
public record VerifyEmailRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(6, MinimumLength = 6)] string Code);

public record ResendVerificationRequest(
    [Required, EmailAddress, MaxLength(256)] string Email);

public record ResetPasswordRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(6, MinimumLength = 6)] string Code,
    [Required, MinLength(8)] string NewPassword);
```

**Why DataAnnotations here?** These DTOs are bound by the API layer, and `Program.cs` already
maps automatic model-state failures into the standard `{ code, message, type }` error shape. So
a malformed request (wrong email format, code not six digits) is rejected before it ever reaches
the service. Input validation is layered: DTO (shape) → service (business rules).

---

## Step 5 — Backend: `IAuthService` additions

**File:** `src/Application/Auth/IAuthService.cs` — append three signatures:

```csharp
Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct);
Task<Result> ResendVerificationEmailAsync(string email, CancellationToken ct);
Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
```

---

## Step 6 — Backend: `AuthService` — the heart of the change

**File:** `src/Infrastructure/Auth/AuthService.cs`

### 6a. New dependencies (constructor)

```csharp
public class AuthService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ITokenService tokenService,
    AppDbContext dbContext,
    IOptions<JwtSettings> jwtOptions,
    IEmailService emailService,
    IOptions<GmailApiSettings> gmailOptions,
    IHostEnvironment hostEnvironment,
    ILogger<AuthService> logger) : IAuthService
```

`IEmailService`/`GmailApiEmailService` is already DI-registered (`DependencyInjection.cs`), and
`GmailApiSettings` is bound from configuration, so nothing else needs registering. `IHostEnvironment`
guards the dev-only code logging.

### 6b. Constants + crypto helpers

```csharp
private const int CodeLength = 6;
private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
private const int MaxAttempts = 5;
private static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);

private static string GenerateCode()
{
    return RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
}

private static string HashCode(string code)
{
    var salt = RandomNumberGenerator.GetBytes(16);
    var hash = Rfc2898DeriveBytes.Pbkdf2(
        code, salt, 100_000, HashAlgorithmName.SHA256, 32);
    return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
}

private static bool CodeMatches(string stored, string candidate)
{
    var parts = stored.Split(':', 2);
    if (parts.Length != 2) return false;
    var expected = Convert.FromHexString(parts[1]);
    var actual = Rfc2898DeriveBytes.Pbkdf2(
        candidate, Convert.FromHexString(parts[0]),
        100_000, HashAlgorithmName.SHA256, 32);
    return CryptographicOperations.FixedTimeEquals(expected, actual);
}
```

Security rationale:
- `RandomNumberGenerator.GetInt32` — cryptographically secure, NOT `Random` (which is guessable).
- PBKDF2 with a random 16-byte salt and 100k iterations — same protection family as passwords.
  Even if the DB leaks, codes cannot be reversed or rainbow-tabled.
- `FixedTimeEquals` — constant-time comparison, prevents timing side-channels that could leak
  which digits match.
- `"D6"` zero-pads, so codes like `004271` are valid.

### 6c. Issue + deliver a code

```csharp
private async Task<Result> IssueCodeAndEmailAsync(
    AppUser user, VerificationPurpose purpose, CancellationToken ct)
{
    var now = DateTime.UtcNow;

    var newerThanInterval = await dbContext.VerificationCodes
        .AnyAsync(v => v.UserId == user.Id
                    && v.Purpose == purpose
                    && v.RevokedAt == null
                    && !v.IsUsed
                    && v.CreatedAt > now - ResendInterval, ct);
    if (newerThanInterval)
        return Result.Failure(Error.Validation("RATE_LIMITED",
            "A code was already sent recently. Please wait a minute before requesting another."));

    var code = GenerateCode();
    dbContext.VerificationCodes.Add(new VerificationCode
    {
        UserId = user.Id,
        CodeHash = HashCode(code),
        Purpose = purpose,
        ExpiresAt = now.Add(CodeLifetime),
        CreatedAt = now
    });

    await InvalidateActiveCodesAsync(user.Id, purpose, ct);
    await dbContext.SaveChangesAsync(ct);

    var baseUrl = gmailOptions.Value.FrontendBaseUrl;
    var message = purpose == VerificationPurpose.EmailConfirmation
        ? EmailTemplates.EmailVerification(user.Email!, user.FullName, code,
            (int)CodeLifetime.TotalMinutes, baseUrl)
        : EmailTemplates.PasswordReset(user.Email!, user.FullName, code,
            (int)CodeLifetime.TotalMinutes, baseUrl);

    // Development convenience: print the code even when delivery SUCCEEDS so local
    // testing never depends on the inbox. Guarded by IHostEnvironment — never in prod.
    if (hostEnvironment.IsDevelopment())
        logger.LogInformation(
            "DEV verification code for {Email} ({Purpose}): {Code}",
            user.Email, purpose, code);

    try
    {
        await emailService.SendAsync(message, ct);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        // Local dev fallback: if Gmail is not configured (empty GmailApiSettings),
        // surface the code in the server console so the flow can still be tested.
        // The code is only logged when delivery FAILS, never on success.
        logger.LogWarning(ex,
            "Email delivery failed for {Email} ({Purpose}); verification code is {Code}.",
            user.Email, purpose, code);
    }

    return Result.Success();
}
```

Points worth understanding:
- **Rate limit:** if an active code exists that was created in the last 60 seconds, refuse.
  This throttles both the forgot-password endpoint and the resend button.
- **Invalidate before/at issue:** issuing a new code revokes all previous active codes for the
  same (user, purpose) — only the newest count. (Convenience + prevents replay of old emails.)
- **Best-effort email:** even if `SendAsync` throws (e.g., no credentials, or a transient network
  failure resolving `oauth2.googleapis.com`), the transaction has already been saved and we return
  success — the user record is created / the reset code is stored. The caller must NOT roll back
  because of a mailing failure. A transient delivery failure does not break the flow in dev: the
  code is visible in the server console.

```csharp
private async Task InvalidateActiveCodesAsync(string userId, VerificationPurpose purpose, CancellationToken ct)
{
    var active = await dbContext.VerificationCodes
        .Where(v => v.UserId == userId
                 && v.Purpose == purpose
                 && v.RevokedAt == null
                 && !v.IsUsed)
        .ToListAsync(ct);

    foreach (var code in active)
        code.RevokedAt = DateTime.UtcNow;

    if (active.Count > 0)
        await dbContext.SaveChangesAsync(ct);
}
```

### 6d. Verify (consume) a code

```csharp
private async Task<Result> ConsumeCodeAsync(string userId, VerificationPurpose purpose, string code, CancellationToken ct)
{
    var now = DateTime.UtcNow;

    var anyPending = await dbContext.VerificationCodes.AnyAsync(
        v => v.UserId == userId && v.Purpose == purpose && v.RevokedAt == null && !v.IsUsed, ct);

    if (!anyPending)
        return Result.Failure(Error.Validation("CODE_INVALID",
            "No active code was found. Please request a new one."));

    var active = await dbContext.VerificationCodes
        .Where(v => v.UserId == userId
                 && v.Purpose == purpose
                 && v.RevokedAt == null
                 && !v.IsUsed
                 && v.ExpiresAt > now)
        .OrderByDescending(v => v.CreatedAt)
        .ToListAsync(ct);

    foreach (var stored in active)
    {
        if (CodeMatches(stored.CodeHash, code))
        {
            stored.IsUsed = true;
            await dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
    }

    if (active.Count == 0)
        return Result.Failure(Error.Validation("CODE_EXPIRED",
            "This code has expired. Please request a new one."));

    var newest = active[0];
    newest.Attempts++;
    if (newest.Attempts >= MaxAttempts)
    {
        newest.RevokedAt = now;
        await dbContext.SaveChangesAsync(ct);
        return Result.Failure(Error.Validation("CODE_EXPIRED",
            "Too many failed attempts. Please request a new code."));
    }
    await dbContext.SaveChangesAsync(ct);

    return Result.Failure(Error.Validation("CODE_INVALID", "The code you entered is incorrect."));
}
```

Security logic:
- "Any pending" check distinguishes *no code at all* from *code expired*, so the UI can say the
  right thing.
- Wrong code increments `Attempts`; 5 misses revoke the code and force a fresh request.
- Correct code is marked `IsUsed = true` — the same code can never be replayed.
- `OrderByDescending(CreatedAt)` — always match against the newest active code.

### 6e. `RegisterAsync` — issue a confirmation code

Append just before `return Result.Success();`:

```csharp
await IssueCodeAndEmailAsync(user, VerificationPurpose.EmailConfirmation, ct);
return Result.Success();
```

Registration still returns 200. The *user cannot sign in yet* — that's enforced in `LoginAsync`.

### 6f. `LoginAsync` — block unverified users

Insert after the password check succeeds, before `BuildAuthResponseAsync`:

```csharp
if (!user.EmailConfirmed)
    return Result<AuthResponse>.Failure(Error.Validation("EMAIL_NOT_CONFIRMED",
        "Please verify your email before signing in. We have sent you a verification code."));
```

Why an explicit check instead of `IdentityOptions.SignIn.RequireConfirmedEmail`? We do NOT use
`SignInManager` (passwords are checked directly via `UserManager.CheckPasswordAsync`), so the
option flag would be dead code. An explicit guard gives us a precise error code the frontend can
branch on.

### 6g. `VerifyEmailAsync` (new)

```csharp
public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
        return Result.Failure(Error.Validation("CODE_INVALID", "The verification code is invalid."));

    if (user.EmailConfirmed)
        return Result.Failure(Error.Conflict("ALREADY_VERIFIED", "Your email is already verified."));

    var consume = await ConsumeCodeAsync(user.Id, VerificationPurpose.EmailConfirmation, request.Code, ct);
    if (consume.IsFailure)
        return consume;

    user.EmailConfirmed = true;
    await userManager.UpdateAsync(user);
    await InvalidateActiveCodesAsync(user.Id, VerificationPurpose.EmailConfirmation, ct);

    return Result.Success();
}
```

`ALREADY_VERIFIED` uses `Conflict` → HTTP 409, so the UI can treat it as a success-like state.

### 6h. `ResendVerificationEmailAsync` (new)

```csharp
public async Task<Result> ResendVerificationEmailAsync(string email, CancellationToken ct)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
        return Result.Success();  // anti-enumeration

    if (user.EmailConfirmed)
        return Result.Failure(Error.Conflict("ALREADY_VERIFIED", "Your email is already verified."));

    return await IssueCodeAndEmailAsync(user, VerificationPurpose.EmailConfirmation, ct);
}
```

### 6i. `ForgotPasswordAsync` — replace the stub

```csharp
public async Task<Result> ForgotPasswordAsync(string email, CancellationToken ct)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
        return Result.Failure(Error.NotFound("USER_NOT_FOUND",
            "No account is registered with that email."));

    return await IssueCodeAndEmailAsync(user, VerificationPurpose.PasswordReset, ct);
}
```

> Product decision (overrides the anti-enumeration default): unknown emails get a clear
> `404` "No account is registered with that email." so a mistyped address is caught immediately.
> If you prefer the stealthy industry default, change the null branch back to `Result.Success()`.

### 6j. `ResetPasswordAsync` (new)

```csharp
public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
        return Result.Failure(Error.Validation("CODE_INVALID", "The reset code is invalid."));

    var consume = await ConsumeCodeAsync(user.Id, VerificationPurpose.PasswordReset, request.Code, ct);
    if (consume.IsFailure)
        return consume;

    if (await userManager.HasPasswordAsync(user))
    {
        var remove = await userManager.RemovePasswordAsync(user);
        if (!remove.Succeeded)
            return Result.Failure(Error.Validation("PASSWORD_RESET_FAILED",
                string.Join("; ", remove.Errors.Select(e => e.Description))));
    }

    var add = await userManager.AddPasswordAsync(user, request.NewPassword);
    if (!add.Succeeded)
        return Result.Failure(Error.Validation("PASSWORD_RESET_FAILED",
            string.Join("; ", add.Errors.Select(e => e.Description))));

    await InvalidateActiveCodesAsync(user.Id, VerificationPurpose.PasswordReset, ct);
    return Result.Success();
}
```

Notes:
- Code verified FIRST. Only after the proof that you own the mailbox do we change anything.
- `RemovePasswordAsync` + `AddPasswordAsync` is used because we verify with our own short code
  rather than an Identity reset token. The `HasPasswordAsync` guard keeps it robust for
  password-less accounts.
- All active reset codes are invalidated afterwards.

> Alternative industry pattern: Identity's own
> `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`. That's the right tool when flowing a
> long opaque token through a reset LINK instead of a typed 6-digit code.

### 6k. `ChangePasswordAsync` — notify by email

Append, after the password-change succeeds:

```csharp
try
{
    await emailService.SendAsync(
        EmailTemplates.PasswordChangedNotification(user.Email!, user.FullName,
            gmailOptions.Value.FrontendBaseUrl), ct);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    logger.LogWarning(ex, "Failed to send password-changed notification to {Email}.", user.Email);
}
```

Best-effort: the change itself already succeeded, so a notification failure must never surface
an error to the user.

---

## Step 7 — Backend: `AuthController` endpoints

**File:** `src/API/Controllers/AuthController.cs` — add three public POSTs (place after `forgot-password`):

```csharp
[HttpPost("verify-email")]
public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
{
    var result = await authService.VerifyEmailAsync(request, ct);
    return result.ToActionResult();
}

[HttpPost("resend-verification")]
public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
{
    var result = await authService.ResendVerificationEmailAsync(request.Email, ct);
    return result.ToActionResult();
}

[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
{
    var result = await authService.ResetPasswordAsync(request, ct);
    return result.ToActionResult();
}
```

All three are **public** (no `[Authorize]`) because they are precisely the paths users take
BEFORE they have a session. `ToActionResult()` maps `ErrorType` → HTTP status
(Validation→400, Conflict→409, Unauthorized→401, NotFound→404) — so `RATE_LIMITED`, `CODE_INVALID`,
`CODE_EXPIRED`, `ALREADY_VERIFIED` all reach the frontend as a structured error body.

---

## Step 8 — Backend: email templates

**File:** `src/Application/Email/EmailTemplates.cs` — append three templates using the same
`Esc()` HTML-escaping + brand styling already in the file. Passwords/codes must never appear in
`TextBody` ASCII boxes incorrectly — keep the code inside a clearly formatted block.

```csharp
public static EmailMessage EmailVerification(
    string recipientEmail, string recipientName, string code, int expiryMinutes, string baseUrl)
{
    var ctaUrl = $"{baseUrl}/verify-email";
    var subject = "Verify your PIPDC account";
    // HTML: greeting, "your code is" callout with the code, a CTA button to ctaUrl,
    // footer: "This code expires in {expiryMinutes} minutes."
    ...
}

public static EmailMessage PasswordReset(
    string recipientEmail, string recipientName, string code, int expiryMinutes, string baseUrl)
{
    var ctaUrl = $"{baseUrl}/reset-password";
    var subject = "Reset your PIPDC password";
    ...
}

public static EmailMessage PasswordChangedNotification(
    string recipientEmail, string recipientName, string baseUrl)
{
    var ctaUrl = $"{baseUrl}/forgot-password";
    var subject = "Your PIPDC password was changed";
    // body: if you did not make this change, reset your password immediately (link to ctaUrl).
    ...
}
```

Why the CTA link AND the code? The email gives the user the 6-digit code; the CTA button takes
them straight to the page that consumes it (email address pre-filled via the query string on the
frontend). Both paths work.

**Deliverability — make it transactional, not bulk.** Every PIPDC email (auth **and** the
enquiry/conversation workflow notifications) is transactional:
- none include the "Unsubscribe" footer, and
- all are returned with `IncludeUnsubscribe = false` (`EmailMessage` gained that flag).

This suppresses the `List-Unsubscribe` headers in `GmailApiEmailService.SendAsync`, so Gmail
classifies all of them as transactional/relationship mail (Primary inbox) instead of
marketing/bulk (spam/promotions). That classification is correct for this product: every mail —
new enquiry, chat replies, viewing scheduled, resolved, admin flagging an enquiry to its agent,
auth codes — concerns an enquiry the recipient is directly involved in. If PIPDC ever sends a
genuine marketing broadcast, THAT mail should carry unsubscribe markers; the
`IncludeUnsubscribe` flag is left on by default for that case.

Spam placement is also strongly influenced by **sender reputation**:
- A freshly created Gmail sender address sending automation will land in spam until it builds
  history — "warm up" the account with real, opened messages first.
- For production-grade deliverability, send from a custom domain configured with SPF, DKIM and
  DMARC (a transactional provider like Amazon SES, SendGrid or Mailgun makes this turnkey).
- Verify `GmailApiSettings.FrontendBaseUrl` points at the real public domain
  (e.g. `https://pipdc.plateaustate.gov.ng`), never `localhost`.

### The repeatable pattern — applying `IncludeUnsubscribe` to ANY email

The classification is not hard-coded per template string; it is a flag on the message that the
sender honors. This is the reusable, industry-standard mechanism — add a future email and choose
its class in one place. The law/UX behind it (canonical references):
- Google's official bulk-sender rules (2024): only **bulk** senders must provide a
  one-click unsubscribe and keep spam rates below 0.3%; transactional mail is not bulk.
- CAN-SPAM (US) / CASL (CA) / GDPR ePrivacy (EU): the unsubscribe requirement and consent rules
  target **commercial/promotional** messages; "facilitating a transaction or relationship that
  the recipient has with you" is the classic exemption — exactly what enquiry replies are.

**1) The flag — `src/Application/Email/EmailMessage.cs` (default is `true` = bulk):**

```csharp
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
```

**2) The sender honors it — `src/Infrastructure/Email/GmailApiEmailService.SendAsync`:**

```csharp
if (message.IncludeUnsubscribe)
{
    mimeMessage.Headers.Add("List-Unsubscribe", $"<mailto:{settings.SenderEmail}?subject=Unsubscribe>");
    mimeMessage.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
}
```

**3) Each template then declares its class** by (a) returning the message with
`{ IncludeUnsubscribe = false }` and (b) omitting the "Unsubscribe" footer link from both HTML
and text bodies:

```csharp
return new EmailMessage(agentEmail, subject, html, agentName, text) { IncludeUnsubscribe = false };
```

Decision rule for any future template:
- **Transactional / relationship** (someone the recipient is already dealing with, about an
  enquiry they are part of: auth codes, new enquiry, chat reply, viewing scheduled, resolved,
  admin flag → agent) → `IncludeUnsubscribe = false`, **no** footer link.
- **Genuine marketing broadcast** (newsletter, promotions) → leave `IncludeUnsubscribe = true`
  (default), keep a visible "Unsubscribe" footer. NEVER force `false` on a marketing blast —
  that is a deliverability violation.

All seven enquiry/conversation workflow templates were converted this way
(`NewEnquiryToAgent`, `ClientReplyToAgent`, `AgentReplyToClient`, `ViewingScheduledToClient`,
`ViewingScheduledToAgent`, `EnquiryResolvedToClient`, `AdminNotifyToAgent`), and the now-unused
`EmailTemplates.UnsubscribeEmail` static + its sender-wiring were deleted. After the conversion
the enquiry/workflow emails use the **same already-working Gmail pipeline** as the auth
emails — there is no separate stub: `EnquiryService.NotifyAgentAsync` and
`MessageService.SendReplyEmailAsync` already called `IEmailService.SendAsync`.

> Lesson from the field: the admin-enquiries UI used to toast "Notification payload ready ….
> Email delivery is not enabled yet." — a **stale frontend message** that had never been
> updated after real email sending landed. Keep UI success text truthful with the shipped
> behavior; retitle it ("Agent notified — reminder email sent") rather than copy old
> placeholder wording.

---

## Step 9 — Backend: build + migrate

1. Ensure no instance of the API is running (`Get-Process -Name PIPDC` → empty).
2. `dotnet build` → must succeed.
3. `dotnet ef migrations add AddVerificationCodes`
4. Restart the app → `MigrateAsync()` creates the `VerificationCodes` table.

The EF CLI needs `dotnet-ef` installed (`dotnet tool install --global dotnet-ef` if missing).

---

## Step 10 — Frontend: API error-code helper

**File:** `src/services/api.ts`

```ts
export function extractApiErrorCode(error: unknown): string | undefined {
  if (axios.isAxiosError(error)) {
    const body = error.response?.data as ApiErrorBody | undefined;
    return body?.code;
  }
  return undefined;
}
```

We already read `body.message` for display; now we also need the machine-readable `code`
(`EMAIL_NOT_CONFIRMED`, `CODE_INVALID`, …) to branch behavior.

---

## Step 11 — Frontend: authService

**File:** `src/services/authService.ts`

```ts
async verifyEmail(payload: { email: string; code: string }): Promise<void> {
  await api.post('/auth/verify-email', payload);
},
async resendVerification(email: string): Promise<void> {
  await api.post('/auth/resend-verification', { email });
},
async resetPassword(payload: { email: string; code: string; newPassword: string }): Promise<void> {
  await api.post('/auth/reset-password', payload);
},
```

---

## Step 12 — Frontend: mutations

**File:** `src/hooks/mutations.ts` (same shape as `useChangePassword`)

```ts
export function useVerifyEmail() {
  return useMutation({
    mutationFn: (payload: { email: string; code: string }) => authService.verifyEmail(payload),
  });
}

export function useResendVerification() {
  return useMutation({
    mutationFn: (email: string) => authService.resendVerification(email),
  });
}

export function useResetPassword() {
  return useMutation({
    mutationFn: (payload: { email: string; code: string; newPassword: string }) =>
      authService.resetPassword(payload),
  });
}
```

---

## Step 13 — Frontend: routes + pages

**File:** `src/App.tsx` — inside the `RedirectIfAuthenticated` + `AuthLayout` children:

```tsx
{ path: '/verify-email', element: <VerifyEmailPage /> },
{ path: '/reset-password', element: <ResetPasswordPage /> },
```

Import the two new pages. They live under the AuthLayout group because the user has NO session
while doing these.

### `VerifyEmailPage.tsx`

- Read the email from `useSearchParams().get('email')`.
- 6-digit input (numeric), a "Verify email" submit button, and a "Resend code" button with a
  60s cooldown.
- On success → toast + `navigate('/login')`.
- On `RATE_LIMITED` → show "wait a minute"; on `CODE_INVALID`/`CODE_EXPIRED` → show message and
  prompt to resend.

### `ResetPasswordPage.tsx`

- Read `email` from the URL.
- Form: 6-digit code + new password + confirm (zod: `min(8)` and match).
- On success → toast + `navigate('/login')`.

---

## Step 14 — Frontend: update the three existing pages

### `RegisterPage.tsx`
After `authService.register(...)` succeeds:

```tsx
notify({ type: 'success', title: 'Account created',
  description: 'We sent a 6-digit verification code to your email.' });
navigate(`/verify-email?email=${encodeURIComponent(data.email)}`);
```

Also raise the frontend password minimum to **8** (`z.string().min(8, ...)`) so client and
server validation agree.

### `ForgotPasswordPage.tsx`
Replace the local `sent` screen with navigation:

```tsx
navigate(`/reset-password?email=${encodeURIComponent(data.email)}`);
```

Copy: "We'll send a 6-digit code."

### `LoginPage.tsx`
In the catch:

```tsx
const code = extractApiErrorCode(err);
if (code === 'EMAIL_NOT_CONFIRMED') {
  // render an inline panel: "Please verify your email first."
  // with a link → /verify-email?email=
}
```

---

## Step 15 — Verify everything

**Backend**
```bash
dotnet build            # 0 errors
```

**Frontend**
```bash
npx tsc --noEmit        # 0 errors
```

**Manual E2E smoke test**
1. Register a new email → you are sent to `/verify-email`.
   - Code arrives by email (Gmail configured) **or** is printed in the backend console (fallback).
2. Try to log in BEFORE verifying → `EMAIL_NOT_CONFIRMED` message shown. Good.
3. Enter the code → success → redirected to login → sign in works.
4. Forgot password → sent to `/reset-password` → enter code + new password → sign in with it.
5. Change password in Settings → "password changed" email arrives.
6. Seeded admin/agent (e.g. `agbojisimon107@gmail.com`) still sign in (they are `EmailConfirmed`).
7. Enquiry workflow emails (transactional, no unsubscribe): admin → enquiry → "Notify agent":
   agent receives the reminder email; client sends a chat reply → agent gets `ClientReplyToAgent`;
   agent replies → client gets `AgentReplyToClient`. All arrive WITHOUT a `List-Unsubscribe`
   header and without an "Unsubscribe" footer.
8. The admin UI toast now reads **"Agent notified — reminder email sent"** (the old
   "payload ready … email delivery not enabled" text is gone; the send is real, not a stub).

---

## Appendix — Security checklist & notes

- [x] Codes generated with CSPRNG (`RandomNumberGenerator`).
- [x] Codes stored only as PBKDF2-HMAC-SHA256 hashes with per-code salt.
- [x] Constant-time comparison (`FixedTimeEquals`).
- [x] 15-minute expiry, single-use, 5-attempt cap with auto-revoke.
- [x] New code invalidates older codes for the same purpose.
- [x] 60s resend rate limit.
- [x] Anti-enumeration kept on resend (always 200); forgot-password rejects unknown emails (product decision).
- [x] Login blocked for unconfirmed emails; seeded users pre-confirmed.
- [x] Email failures never break core transactions (best-effort + console fallback in dev).
- [x] Dev-only code console logging guarded by `IHostEnvironment` (never in production).
- [x] No codes plain-text in HTTP responses.
- [x] Transactional auth emails are exempt from List-Unsubscribe / unsubscribe footers.
- [x] Enquiry/conversation workflow notifications are transactional too (no unsubscribe markers).

**Deliberately out of scope (documented decisions):**
- Full per-IP brute-force / rate-limiting is expected in production at the API gateway or via
  Redis (ASP.NET Core RateLimiting, Azure Front Door, Cloudflare, etc.).
- No background workers / queues (per project constraint): emails are sent inline. On a busy
  system you would push the send onto a queue (e.g., Hangfire, Azure Service Bus) so a slow SMTP
  never holds up `register`.
- No magic reset links — this product intentionally uses typed codes. (Switching to links is a
  small change: use Identity's `GeneratePasswordResetTokenAsync` and put the token in the URL.)
- ASPS: always HTTPS in production (`UseHttpsRedirection` already in `Program.cs`).