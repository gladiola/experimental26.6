# सुरक्षा सुधार: Hardcoded fallback nonces (Critical #3)

**सुधार किया गया फ़ाइल:** `Services/OptimizedNonceMiddleware.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## समस्या क्या थी

`OptimizedNonceMiddleware` में fallback paths पर hardcoded nonce strings उपयोग हो रही थीं:
- `bootstrap-nonce-placeholder`
- `fallback-nonce`
- `error-fallback-nonce`

ये मान predictable थे, इसलिए error स्थिति में CSP bypass संभव था।

---

## क्या सुधारा गया

सभी fallback paths अब `Nonce.GenerateSecureNonce()` से fresh random nonce बनाते हैं।

```csharp
existingNonce = Nonce.GenerateSecureNonce();
nonce = Nonce.GenerateSecureNonce();
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

यह method `RandomNumberGenerator.Fill` आधारित है और Key Vault failure स्थिति में भी सुरक्षित fallback देता है।

---

## इसे सुरक्षित कैसे रखें

1. hardcoded nonce literal कभी न जोड़ें
2. `context.Items["Nonce"]` में हमेशा cryptographically random मान रखें
3. nonce को requests के बीच cache/reuse न करें
4. error paths की सुरक्षा पर विशेष ध्यान दें
5. `OptimizedNonceMiddleware` के nonce-set branches की नियमित समीक्षा करें

---

## इसे enforce करने वाले tests

| Test | क्या रोकता है |
|---|---|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | bootstrap hardcoded fallback |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | empty-generation hardcoded fallback |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | exception-path hardcoded fallback |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | deterministic fallback regression |
