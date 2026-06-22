# የደህንነት ጥገና፡ Hardcoded Fallback Nonces (Critical #3)

**የተስተካከለበት:** `Services/OptimizedNonceMiddleware.cs`

---

## ስህተቱ ምን ነበር

ሶስት hardcoded fallback nonce strings ነበሩ:

- `"bootstrap-nonce-placeholder"`
- `"fallback-nonce"`
- `"error-fallback-nonce"`

nonce generation ሲወድቅ ወይም ካታሎግ ባዶ ሲሆን እነዚህ predictable values ይጠቀሙ ነበር።

### ይህ ለምን critical ነው

ደህንነታማ nonce የማይታመን መሆን አለበት። hardcoded values ከsource code በቀላሉ ሊታወቁ ስለሚችሉ CSP bypass ሊያስከትሉ ይችላሉ።

---

## የተደረገው ጥገና

ሁሉም fallback paths አሁን ይህን ይጠቀማሉ:

```csharp
Nonce.GenerateSecureNonce();
```

ይህም በCSPRNG 16-byte Base64 random nonce ያመነጫል።

---

## ይህን ጥገና እንዴት እንደሚጠብቁ

1. hardcoded nonce literal እንዳይጨመር
2. `context.Items["Nonce"]` የሚሞሉ መንገዶች ሁሉ random value እንዲጠቀሙ
3. አንድ nonce በብዙ requests ላይ እንዳይጋራ
4. error paths እንኳ random nonce እንዲያመነጩ
5. `OptimizedNonceMiddleware` ለውጦች በጥንቃቄ እንዲገምገሙ

---

## ጥገናውን የሚጠብቁ ሙከራዎች

- `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce`
- `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce`
- `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce`
- `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique`
