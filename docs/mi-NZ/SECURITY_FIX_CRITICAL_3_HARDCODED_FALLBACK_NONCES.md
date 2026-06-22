# Whakatika Haumaru: Ngā fallback nonce i hardcodehia (Tino Nui #3)

**I whakatikaina ki:** `Services/OptimizedNonceMiddleware.cs`  
**Whakamātautau:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## He aha te raruraru

I roto i `OptimizedNonceMiddleware`, i whakamahia ngā aho pūmau e toru hei fallback nonce:

| Wāhi | Uara hardcoded |
|----------|-----------------|
| `InvokeAsync` — tono tuatahi, kāore he nonce i te catalog | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — i hoki mai he aho wātea i te hanga | `"fallback-nonce"` |
| `InvokeAsync` — ara hapa | `"error-fallback-nonce"` |

Mēnā ka mōhiotia ēnei aho, ka taea te matapae i te nonce i ngā ara hapa, ā, ka ngoikore te CSP.

---

## He aha i whakatikaina

Kua whakakapia ngā fallback katoa ki `Nonce.GenerateSecureNonce()`:

```csharp
// BEFORE:
existingNonce = "bootstrap-nonce-placeholder";
// AFTER:
existingNonce = Nonce.GenerateSecureNonce();

// BEFORE:
nonce = "fallback-nonce";
// AFTER:
nonce = Nonce.GenerateSecureNonce();

// BEFORE:
context.Items["Nonce"] = "error-fallback-nonce";
// AFTER:
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

Ka whakamahi a `Nonce.GenerateSecureNonce()` i `RandomNumberGenerator.Fill` hei waihanga nonce hou,
kore-matapae, 16-paita i ia wā.

---

## Me pēhea te pupuri i te whakatika

1. **Kaua rawa e hardcode i tētahi nonce literal** ahakoa fallback, placeholder rānei.
2. Ko ngā ara katoa e tautuhi ana i `context.Items["Nonce"]` me whakamahi uara matapōkere haumaru.
3. Kaua e cache i tētahi nonce kotahi mā ngā tono maha.
4. I ngā ara hapa, me random tonu te nonce.
5. Arotakehia ngā panoni a muri ake i `OptimizedNonceMiddleware`, otirā ngā peka e toru.

### Ngā whakamātautau e here ana i tēnei whakatika

| Whakamātautau | He aha ka mau |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Ka hinga mēnā ka hoki mai `"bootstrap-nonce-placeholder"` |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Ka hinga mēnā ka hoki mai `"fallback-nonce"` |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Ka hinga mēnā ka hoki mai `"error-fallback-nonce"` |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Ka hinga mēnā he fallback nonce tukurua |
