# Security Review — WebAppExperimental266

**Date:** 2026-05-05  
**Scope:** Full codebase static analysis  

---

## Summary Table

| # | Area | Severity |
|---|------|----------|
| 1 | AES-GCM IV reuse in nonce generation | 🔴 Critical ✅ |
| 2 | Nonce logged in plaintext | 🔴 Critical ✅ |
| 3 | Hardcoded fallback nonce strings | 🔴 Critical ✅ |
| 4 | Non-thread-safe global nonce dictionary | 🟠 High |
| 5 | mTLS issuer validation commented out | 🟠 High |
| 6 | mTLS revocation checking off by default | 🟠 High |
| 7 | OCSP always returns valid (stub) | 🟠 High |
| 8 | Auth/authz off by default in config | 🟠 High |
| 9 | Security headers applied too late in pipeline | 🟠 High |
| 10 | Session cookie missing Secure + SameSite | 🟡 Medium |
| 11 | Malformed global Set-Cookie header | 🟡 Medium |
| 12 | Content-Type forced to text/html everywhere | 🟡 Medium |
| 13 | AllowedHosts is wildcard | 🟡 Medium |
| 14 | Nonce not applied to `<script>` tags in layout | 🟡 Medium |
| 15 | Referrer-Policy header missing | 🟡 Medium |
| 16 | PII logged in plaintext | 🔵 Low |
| 17 | Partial connection string in logs | 🔵 Low |
| 18 | Key Vault ops are stubs | 🔵 Low |
| 19 | Deprecated X-XSS-Protection header | 🔵 Low |

---

## 🔴 Critical

### 1. AES-GCM IV Reuse — Nonce Generation is Cryptographically Broken ✅ Fixed in commit 45ae31b

**Files:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

The AES-GCM encryption that generates CSP nonces uses a **fixed IV retrieved from Key Vault on every call**. AES-GCM is broken when the IV is reused with the same key: an attacker who observes two ciphertexts can XOR them to recover the XOR of the plaintexts, and authentication tags can be forged.

The fix is straightforward — CSP nonces do not need encryption at all. A CSP nonce only needs to be **unpredictable and unique per request**; a call to `RandomNumberGenerator.GetBytes(16)` converted to Base64 is sufficient and correct.

---

### 2. Nonce Values Logged in Plaintext ✅ Fixed in commit bb6f27a

**Files:** `Services/NonceMiddleware.cs` (line 31), `Services/NonceRefresherService.cs` (line 82)

The generated CSP nonce is logged verbatim in the application logs:

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

Anyone with access to the logs gains a valid nonce and can trivially bypass the CSP to inject inline scripts.

---

### 3. Hardcoded Fallback Nonces ✅ Fixed in commit 11cc9f7

**File:** `Services/OptimizedNonceMiddleware.cs` (lines 53, 78, 92)

If nonce generation fails or the nonce catalog is empty, the middleware falls back to the string literals `"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, and `"error-fallback-nonce"`. These strings are committed to source code and known to attackers. An error condition (e.g., Key Vault unavailable) would put a predictable, exploitable nonce into the CSP header.

---

## 🟠 High

### 4. NonceCatalogService Uses a Non-Thread-Safe Static Dictionary ✅ Fixed in commit ae2b6c9

**File:** `Services/NonceCatalogService.cs` (line 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` is not thread-safe for concurrent reads and writes. Under load, two requests racing to update the same nonce key can cause data corruption or thrown exceptions. The nonce catalog is also a singleton (effectively a global), meaning one request's nonce can be overwritten by another request mid-flight — a nonce collision between requests. Use `ConcurrentDictionary` and store nonces per-request in `HttpContext.Items` rather than in a shared global.

---

### 5. mTLS Certificate Issuer Validation is Stubbed Out ✅ Fixed in commit fd3d4fb

**File:** `Extensions/ServiceCollectionExtensions.cs` (lines 305–313)

The `ValidateClientCertificateIssuer` setting exists and is `true` by default, but the actual validation code is commented out:

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

With mTLS enabled, any client certificate from any issuer (that chains to a trusted root) can authenticate — no tenant/issuer restriction applies.

---

### 6. mTLS Certificate Revocation Checking Disabled by Default ✅ Fixed in commit fd3d7b3

**Files:** `Models/Settings/MtlsSettings.cs` (line 26), `appsettings.template.json`

`CheckCertificateRevocation` defaults to `false` in both the model and the template. Revoked client certificates can be used to authenticate indefinitely. For production mTLS, revocation checking should default to enabled.

---

### 7. OCSP Validation is a Stub That Always Returns Valid ✅ Fixed in commit b4c3807

**File:** `Services/OcspValidationService.cs` (lines 149–163)

The `PerformOcspValidationAsync` method is explicitly a "template implementation" that always returns `IsValid = true` after a `Task.Delay(100)`. If OCSP validation is ever enabled in configuration, it will silently pass all certificates — including revoked ones — as valid, while logging a warning that is easy to miss.

---

### 8. Authentication and Authorization Disabled by Default ✅ Fixed in commit b392c47

**File:** `appsettings.json` (lines 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

The default configuration ships with no authentication or authorization. A developer who copies `appsettings.template.json` (which also has these off) without carefully reading the docs will deploy an open application. The template defaults should require a deliberate opt-out, not opt-in.

---

### 9. Security Headers Applied After Routing/Auth ✅ Fixed in commit 016e57c

**File:** `Program.cs` (lines 130–152)

`UseNonceAndSecurityHeadersAsync` and `UseStandardSecurityHeaders` are called after `UseRouting`, `UseAuthentication`, and `UseAuthorization`. Responses that short-circuit the pipeline before reaching these middleware (e.g., 401 redirects, 403 denials) may not receive security headers. Security headers should be as early in the pipeline as possible.

---

## 🟡 Medium

### 10. Session Cookie Missing `Secure` and `SameSite` Attributes ✅ Fixed in commit 8f2223c

**File:** `Extensions/ServiceCollectionExtensions.cs` (lines 41–46)

The session cookie sets `HttpOnly = true` and `IsEssential = true`, but omits `Cookie.SecurePolicy = CookieSecurePolicy.Always` and `Cookie.SameSite = SameSiteMode.Strict`. The cookie could be transmitted over plain HTTP (if the redirect hasn't fired yet) or sent cross-site.

---

### 11. Malformed Global `Set-Cookie` Header ✅ Fixed in commit 8f2223c

**File:** `Extensions/ApplicationBuilderExtensions.cs` (line 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

This appends a nameless, valueless `Set-Cookie` header to every response. This is invalid and will be ignored (or rejected) by browsers, but it produces surprising artifacts in all responses including static files, JSON API responses, and health checks. Cookie security should be set in the cookie options of the specific cookie being configured, not injected as a raw header globally.

---

### 12. `Content-Type` Forcibly Set to `text/html` for All Responses ✅ Fixed in commit 8f2223c

**File:** `Extensions/ApplicationBuilderExtensions.cs` (line 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

This overwrites Content-Type for every response — API endpoints, JSON, binary downloads, and static files will all claim to be `text/html`. This conflicts with `X-Content-Type-Options: nosniff`, which prevents browsers from overriding the declared content type.

---

### 13. `AllowedHosts` Set to Wildcard ✅ Fixed in commit 8f2223c

**Files:** `appsettings.json` (line 11), `appsettings.template.json` (line 36)

```json
"AllowedHosts": "*"
```

This disables ASP.NET Core's built-in host header validation. Host header injection attacks allow cache poisoning, password-reset link poisoning, and open redirects. This should be set to the specific domain(s) the app is served from.

---

### 14. Layout Does Not Apply Nonce to `<script>` Tags ✅ Fixed in commit 8f2223c

**File:** `Views/Shared/_Layout.cshtml`

The layout loads several JavaScript files (`jquery.min.js`, `bootstrap.bundle.min.js`, `site.js`) but none of the `<script>` tags include `nonce="@Context.Items["Nonce"]"`. If CSP with nonces is enabled, these scripts would be blocked by the browser. The nonce implementation is wired up in middleware but not consumed in views, rendering the CSP nonce system ineffective.

---

### 15. Referrer-Policy Header Missing ✅ Fixed in commit 8f2223c

**File:** `Extensions/ApplicationBuilderExtensions.cs`

The standard security headers do not include `Referrer-Policy`. Without this, the browser sends the full URL in the `Referer` header to third-party resources (e.g., the ArcGIS CDN included in the CSP), which could leak authenticated session paths.

---

## 🔵 Low / Informational

### 16. PII Logged in Plaintext ✅ Fixed in commit 93bb4e9

**File:** `Services/LoggingHelper.cs` (lines 85, 105)

User OID, email, name, session ID, and roles are logged verbatim on every authenticated request:

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

Depending on applicable privacy regulations (GDPR, CCPA, HIPAA), this could be a compliance issue. Consider masking or hashing identifiers in log output and directing PII-containing logs to an appropriately controlled sink. The goal of forensic session reconstruction can be preserved by logging consistent HMAC-SHA256 hashes of the identifiers rather than their plaintext values.

---

### 17. Partial Connection String in Logs ✅ Fixed in commit 93bb4e9

**File:** `Extensions/ServiceCollectionExtensions.cs` (line 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

Even a partial secret in logs is not best practice. The log statement should instead confirm that a connection string is present (non-empty) rather than logging any portion of it.

---

### 18. Key Vault Operations Are Stubs ✅ Fixed in commit 93bb4e9

**File:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

Both `GetCertificateFromKeyVault` and `GetSecretFromKeyVault` are template stubs that return `null`/dummy values. With Key Vault enabled, `GetCertificateFromKeyVault` returns `null`, which causes an `InvalidOperationException` at startup — a good fail-fast, but it also means there is no actual Key Vault integration to audit for secret handling.

---

### 19. `X-XSS-Protection: 1; mode=block` is Deprecated ✅ Fixed in commit 93bb4e9

**File:** `Extensions/ApplicationBuilderExtensions.cs` (line 70)

Modern browsers have removed support for `X-XSS-Protection`. The header is not harmful, but it provides a false sense of security. The recommended approach is to rely on a strong CSP instead. The `0` value (disabling the XSS auditor) is sometimes considered safer than `1; mode=block` for older browsers because the auditor itself had exploitable behaviors.
