# Skill: port-security-headers

Port the security-headers middleware stack from WebAppExperimental26 / eurocsv into a new
ASP.NET Core project. Established in `gladiola/eurocsv` PR #9.

## What this skill adds

- **`SecurityHeadersMiddleware`** — sets defence-in-depth HTTP response headers on every reply
- **`NonceMiddleware`** — generates a per-request 128-bit cryptographic CSP nonce and emits
  a `Content-Security-Policy` header that gates inline scripts to that nonce
- Both middleware registered in `Program.cs` after `UseHttpsRedirection` and before
  `UseStaticFiles`/`UseRouting`

## Files to create

### `<Project>/Middleware/SecurityHeadersMiddleware.cs`

Implement as a conventional middleware class (constructor takes `RequestDelegate` + `ILogger`).
Use `context.Response.OnStarting(...)` to append headers just before the response is flushed.

Headers to set:
| Header | Value |
|--------|-------|
| `X-Frame-Options` | `DENY` |
| `X-XSS-Protection` | `0` (disables legacy IE filter) |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (skip if already set) |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-site` |
| `Permissions-Policy` | `geolocation=(), camera=(), microphone=(), interest-cohort=()` |
| `Server` | `webserver` (remove the real value first) |
| Remove | `X-Powered-By`, `X-AspNetMvc-Version` |

### `<Project>/Middleware/NonceMiddleware.cs`

- Generate nonce: `RandomNumberGenerator.Fill(byte[16])` → `Convert.ToBase64String`
- Store in `context.Items["Nonce"]`
- Do **not** log the nonce value (would allow log-readers to inject scripts)
- Emit `Content-Security-Policy` via `OnStarting` only if not already set
- CSP directive template:
  ```
  default-src 'self';
  script-src 'self' 'nonce-{nonce}';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data:;
  font-src 'self';
  connect-src 'self';
  frame-ancestors 'none';
  form-action 'self';
  ```

## `Program.cs` changes

```csharp
using <Project>.Middleware;

// After UseHttpsRedirection, before UseStaticFiles:
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<NonceMiddleware>();
```

## View changes

Every `<script>` tag in Razor views must carry the nonce:
```html
<script nonce="@Context.Items["Nonce"]" src="..."></script>
```

## Verification

After deployment, check response headers with browser DevTools → Network → any request →
Response Headers. Confirm `Content-Security-Policy`, `X-Frame-Options: DENY`,
`X-Content-Type-Options: nosniff` are all present and `Server` does not reveal the real stack.

## Source

`gladiola/eurocsv` PR #9 (`copilot/add-security-logging-nonce-features`).
