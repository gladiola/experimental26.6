# Security Fix: Hardcoded Fallback Nonces (Critical #3)

**Fixed in:** `Services/OptimizedNonceMiddleware.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## What Was Wrong

`OptimizedNonceMiddleware` contained three hardcoded string literals that were used as fallback
nonce values when normal nonce generation failed or had not yet been run:

| Location | Hardcoded Value |
|----------|-----------------|
| `InvokeAsync` — first request, catalog empty | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — generation returned empty string | `"fallback-nonce"` |
| `InvokeAsync` — exception path | `"error-fallback-nonce"` |

### Why This Is Critical

**A nonce is only secure if an attacker cannot predict it.**  Hardcoded literals are committed to
source control and therefore known to anyone with repository access (including any attacker who
has obtained source access or decompiled the binary).

The specific danger is that these fallback paths are activated by **error conditions** — exactly
the situations an attacker is most likely to engineer (e.g., making Key Vault temporarily
unavailable via rate-limiting or network disruption).  When the application degrades gracefully
to a predictable nonce, the CSP header becomes decorative: the attacker simply injects
`<script nonce="fallback-nonce">` and the browser executes it.

### Root-Cause Code (before fix)

```csharp
// First request before any nonce generated
existingNonce = "bootstrap-nonce-placeholder";

// Nonce generation returned empty
nonce = "fallback-nonce";

// Exception path
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## What Was Fixed

All three fallback paths now call `Nonce.GenerateSecureNonce()` to produce a fresh, unpredictable
16-byte random nonce at runtime:

```csharp
// BEFORE (vulnerable):
existingNonce = "bootstrap-nonce-placeholder";
// AFTER (safe):
existingNonce = Nonce.GenerateSecureNonce();

// BEFORE (vulnerable):
nonce = "fallback-nonce";
// AFTER (safe):
nonce = Nonce.GenerateSecureNonce();

// BEFORE (vulnerable):
context.Items["Nonce"] = "error-fallback-nonce";
// AFTER (safe):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` uses `RandomNumberGenerator.Fill` (a CSPRNG) to generate 16
cryptographically random bytes encoded as Base64.  Because it is a static method with no
Key Vault dependency it is safe to call even when Key Vault is unavailable — the very error
condition that previously exposed the hardcoded fallback.

---

## How to Keep This Fixed

1. **Never introduce a hardcoded nonce literal** anywhere in the codebase, regardless of the
   context (fallback, test, placeholder, comment example that gets copy-pasted, etc.).

2. **Every code path that sets `context.Items["Nonce"]` must use a cryptographically random
   value.**  Call `Nonce.GenerateSecureNonce()` or `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **Do not cache a single nonce across requests.**  Each request must receive its own fresh nonce.

4. **Error paths are the most dangerous.**  If nonce generation fails for any reason, the response
   should still receive a random nonce, never a predictable fallback.

5. **Review any future changes to `OptimizedNonceMiddleware`** — particularly the three branches
   where the nonce can be set: the ignore-path branch, the empty-generation branch, and the
   exception-handler branch.

### Tests That Enforce This Fix

| Test | What It Catches |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Fails if `"bootstrap-nonce-placeholder"` is reintroduced in the first-request branch |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Fails if `"fallback-nonce"` is reintroduced in the empty-generation branch |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Fails if `"error-fallback-nonce"` is reintroduced in the exception handler |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Fails if any fallback produces the same nonce twice in 50 consecutive calls (which any hardcoded string would) |
