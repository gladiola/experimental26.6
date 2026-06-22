# Security Fix: AES-GCM IV Reuse in Nonce Generation (Critical #1)

**Fixed in:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## What Was Wrong

The `Nonce` class used **AES-GCM encryption with a fixed IV** fetched from Azure Key Vault on
every call.  Reusing the same IV with the same AES-GCM key is a catastrophic cryptographic error:

* An attacker who observes two ciphertexts encrypted with the same IV and key can XOR them to
  recover the XOR of the two plaintexts.
* More critically for nonce-based authentication tags, IV reuse allows authentication tag forgery,
  breaking the integrity guarantee of AES-GCM entirely.

Beyond the cryptographic failure, the encryption added **no security benefit** for this use case.
A CSP nonce only needs two properties: it must be **unpredictable** and **unique per request**.
These properties are already provided directly by a cryptographically secure random number
generator (`RandomNumberGenerator`).  Encrypting the value added complexity without adding security.

### Root-Cause Code (before fix)

```csharp
// Nonce.cs — same IV fetched from Key Vault on every call
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — fetched once and reused across all requests
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## What Was Fixed

`Nonce.GenerateSecureNonce()` now calls `RandomNumberGenerator.Fill(byte[])` directly to produce
16 bytes of cryptographically random data, then Base64-encodes the result:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* No Key Vault calls for IV or encryption key are needed or made.
* No AES-GCM or any other cipher is involved.
* The `Nonce` constructor no longer accepts `KeyVaultSecret` parameters.

A secondary bug was also fixed in `NonceCatalogService.GetANonce`: the method previously used a
two-step check-then-lookup (`TryGetValue` followed by indexer `[]`), which is not atomic and could
throw a `KeyNotFoundException` when another thread removed the key between the two calls.  The fix
uses `TryGetValue` with the `out` parameter to retrieve the value in a single atomic operation.

---

## How to Keep This Fixed

1. **Never introduce a Key Vault IV or key for nonce generation.**  If Key Vault is used for other
   secrets, that is fine — but nonce generation must never depend on a fixed IV.

2. **Never replace `GenerateSecureNonce` with an AES-GCM or CBC/CTR scheme** that reuses an IV
   or counter across requests.

3. **Keep the nonce at least 16 bytes (128 bits).**  Reducing the byte length increases collision
   probability and reduces the entropy available to CSP.

4. **Do not change `RandomNumberGenerator.Fill` to `new Random()`** or any other non-CSPRNG.

5. **Keep `NonceCatalogService.GetANonce` using `TryGetValue` with the `out` parameter.**  The
   two-step check-then-lookup pattern (`TryGetValue` + indexer) is not thread-safe even with
   `ConcurrentDictionary`.

### Tests That Enforce This Fix

| Test | What It Catches |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Fails to compile if the constructor is reverted to accept `KeyVaultSecret` IV + key |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Fails if nonce generation is broken or returns a non-Base64 value |
| `GenerateSecureNonce_Returns16ByteBase64` | Fails if byte length is reduced below 16 |
| `Nonce_SuccessiveGenerations_AreUnique` | Fails if IV reuse causes the same nonce to be produced repeatedly |
| `Nonce_HasSufficientEntropy` | Fails if the entropy source is non-random |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Fails if `ConcurrentDictionary` is reverted to `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Fails if the TOCTOU race in `GetANonce` is reintroduced |
