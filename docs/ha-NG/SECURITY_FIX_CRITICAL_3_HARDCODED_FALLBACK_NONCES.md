# Gyaran Tsaro: Hardcoded Fallback Nonces (Critical #3)

**An gyara a:** `Services/OptimizedNonceMiddleware.cs`

---

## Abin da ya faru ba daidai ba

Akwai hardcoded fallback nonce strings uku:

- `"bootstrap-nonce-placeholder"`
- `"fallback-nonce"`
- `"error-fallback-nonce"`

Idan generation ya fadi ko catalog babu komai, waɗannan predictable strings ake amfani da su.

### Dalilin da ya sa wannan critical ne

Nonce mai tsaro dole ne ba za a iya hasashe ba. Hardcoded values ana iya sani daga source code kuma mai hari zai iya amfani da su wajen ketare CSP, musamman a lokacin kuskure/outsge.

---

## Abin da aka gyara

Duk fallback paths yanzu suna amfani da:

```csharp
Nonce.GenerateSecureNonce();
```

wacce ke samar da random 16-byte Base64 nonce ta CSPRNG.

---

## Yadda za a ci gaba da gyaran

1. Kada a taba saka hardcoded nonce literal ko a fallback ko a comments da za a iya copy-paste
2. Duk paths da ke saita `context.Items["Nonce"]` su samar da random value
3. Kada a cache nonce ɗaya a requests da yawa
4. Error paths su ma su samar da random nonce
5. A duba canje-canje na gaba a `OptimizedNonceMiddleware` da kyau

---

## Gwaje-gwajen da ke kare gyaran

- `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce`
- `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce`
- `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce`
- `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique`
