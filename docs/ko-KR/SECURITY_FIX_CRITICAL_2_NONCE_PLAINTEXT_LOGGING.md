# Security Fix: Nonce Values Logged in Plaintext (Critical #2)

**Fixed in:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## What Was Wrong

Two locations logged the actual CSP nonce value verbatim into the application log stream:

**`Services/NonceMiddleware.cs` (line 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (line 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Why This Is Critical

A CSP nonce is the *only* mechanism preventing inline-script injection once CSP is enforced.  Its
security depends entirely on it being **secret for the lifetime of a single response**.

Application logs in a cloud/enterprise environment are typically readable by:
* Operations teams
* Log aggregation services (e.g. Azure Monitor, Splunk, ELK)
* Any account with reader access to the log sink

Anyone who can read a log line containing `Nonce: <value>` can inject an inline `<script>` tag
with that nonce value and have the browser execute it, completely bypassing CSP.  Even if the
nonce rotates per request, an attacker with live log access can act within the same request's
window.

---

## What Was Fixed

Both log statements were replaced with messages that confirm the *status* of nonce generation
without revealing the value:

**`NonceMiddleware.cs`:**
```csharp
// BEFORE (vulnerable):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// AFTER (safe):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// BEFORE (vulnerable):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// AFTER (safe):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## How to Keep This Fixed

1. **Never log the nonce value.**  Log messages may confirm that a nonce was generated or
   retrieved (success/failure status), but the nonce string itself must never appear in any
   log parameter, structured-logging field, or string interpolation.

2. **Review any new log statement in nonce-related code** (`NonceMiddleware`,
   `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) to ensure the
   nonce value is not included.

3. **Do not expose the nonce in telemetry, metrics, or distributed traces** for the same reasons.
   Trace attributes and span tags are often forwarded to log aggregation backends.

4. **The nonce must be treated as a per-request secret.**  It may be stored in `HttpContext.Items`
   for use within a single request's rendering pipeline, but it must not leave the process via
   any observable channel except the HTTP response header and the `nonce="..."` attribute in the
   HTML it secures.

### Tests That Enforce This Fix

| Test | What It Catches |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Fails if the nonce string is reintroduced into any log message in `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Fails if the nonce string is reintroduced into any log message in `NonceMiddleware` |
