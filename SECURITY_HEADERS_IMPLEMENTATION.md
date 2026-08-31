# Security Headers Implementation

This doc explains how HTTP security headers (plus HSTS and CSP) were applied to the
PIPDC platform, and the pattern the codebase now uses, so future hardening follows
the same approach.

## Why

Browsers apply default policies that are too permissive. Without explicit header
instructions, a page can be embedded in a third-party iframe (clickjacking), the
browser may sniff the content type of a response (MIME confusion / drive-by download),
referrers leak the full URL to external origins, and the SPA can load scripts from
any source. These headers are cheap, defense-in-depth controls that harden both the
browser-facing surfaces of the platform (the React SPA and the ASP.NET Core API).

## Two surfaces, two mechanisms

PIPDC ships two browser-facing surfaces, and each gets its headers differently:

| Surface | How headers are set |
|---|---|
| ASP.NET Core API (`PIPDC/src/API`) | Middleware in `Program.cs` — runs on every response |
| React SPA (`PIPDC-Frontend`) | `<meta http-equiv="Content-Security-Policy">` in `index.html` |

The API cannot set a CSP for the SPA: the SPA is served by Vite (`localhost:5173`
in dev, a static/CDN host in prod), not by the API. A `<meta>` CSP is the only way
a static HTML page can carry a Content-Security-Policy, so the SPA uses one.

## What was applied

### 1. API middleware (`src/API/Program.cs`)

Registered HSTS:

```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);   // 1 year, the HSTS preload requirement
    options.IncludeSubDomains = true;
});
```

Header pipeline (runs for every request, before routing, so even 404/error responses
carry the headers):

```csharp
app.UseHsts();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    await next();
});
```

### 2. SPA CSP (`PIPDC-Frontend/index.html`)

```html
<meta
  http-equiv="Content-Security-Policy"
  content="
    default-src 'self';
    script-src 'self' 'unsafe-inline' https://challenges.cloudflare.com;
    style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
    font-src 'self' https://fonts.gstatic.com;
    img-src 'self' data: https://res.cloudinary.com;
    connect-src 'self' ws://localhost:5173 http://localhost:7123 https://challenges.cloudflare.com;
    frame-src https://challenges.cloudflare.com;
    frame-ancestors 'none';
    base-uri 'self';
    form-action 'self'
  "
/>
```

## Header-by-header rationale

| Header / directive | Purpose | Pattern adopted |
|---|---|---|
| `Strict-Transport-Security` (HSTS) | Tells the browser to only ever talk HTTPS to this host for 1 year | `UseHsts` on the API; header only emitted on HTTPS requests (safe on `localhost`) |
| `X-Content-Type-Options: nosniff` | Stops browsers from MIME-sniffing; JSON served as `application/json` stays that way | Set in API middleware |
| `X-Frame-Options: DENY` | Blocks clickjacking; the app is never meant to be framed | Set in API middleware (browsers ignore `X-Frame-Options` in a `<meta>`, hence server-side) |
| `Referrer-Policy` | Only send origin + path for same-origin, origin only on cross-origin HTTPS | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | Denies geolocation/microphone/camera to this app (it never needs them) | Explicit empty allow-lists |
| `Content-Security-Policy` | Restricts what the SPA may load/connect to | `<meta>` in `index.html` (see sources below) |

CSP sources and why they exist:

- `'self'`, `'unsafe-inline'` — the app's own scripts/styles; Vite injects inline
  styles/scripts in dev and needs a small inline bootstrap.
- `https://challenges.cloudflare.com` — Cloudflare Turnstile (anti-bot) script,
  frame, and its `connect-src`.
- `https://fonts.googleapis.com` / `fonts.gstatic.com` — the Google Fonts used by the UI.
- `https://res.cloudinary.com` — Cloudinary image CDN used for property images.
- `ws://localhost:5173`, `http://localhost:7123` — Vite HMR websocket and the dev
  API. **Replace with the production domains when deploying to a real environment.**
- `frame-ancestors 'none'` — CSP-level clickjacking protection (modern replacement
  for `X-Frame-Options` on the SPA, which has no meta equivalent).

## Pattern adopted (rules for future work)

1. **Server-rendered / API responses** get headers from `Program.cs` middleware —
   never from controllers. Header concerns stay out of handlers.
2. **Static SPA pages** use the `<meta>` CSP in `index.html` — nothing else works
   for static hosting.
3. `UseHsts` must stay **before** `UseHttpsRedirection` and after
   `UseExceptionHandler` so the developer-exception page and redirects never skip it.
4. When the app is deployed: update `connect-src` with the real API origin (and
   remove the Vite `ws://localhost`) in `index.html`, and confirm the TLS/terminating
   proxy (Caddy/Cloudflare) preserves the headers.
5. **Test surface:** `X-Frame-Options`/CSP is only meaningfully verifiable in a
   browser (DevTools → Network → Response Headers, and a `frame-ancestors` check).

## Verifying

API (from `C:\Users\OYALE\Desktop\Project\PIPDC`):

```
dotnet run
curl -k -I https://localhost:7123/api/properties
```

Expected headers on the response: `strict-transport-security`,
`x-content-type-options: nosniff`, `x-frame-options: DENY`,
`referrer-policy: strict-origin-when-cross-origin`, `permissions-policy`.

SPA (from `PIPDC-Frontend`): run `npm run dev`, open DevTools → Network,
select the document request, and read the `Content-Security-Policy`
(under **Response Headers**; it is injected by the meta tag).

## Related

- HSTS preload (only once in production behind a stable domain): https://hstspreload.org
- CSP spec: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
- MDN security headers overview: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers