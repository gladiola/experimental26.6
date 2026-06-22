# सुरक्षा सुधार: Nonce Generation में AES-GCM IV Reuse (Critical #1)

**सुधार किए गए फ़ाइलें:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`, `WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## समस्या क्या थी

`Nonce` क्लास AES-GCM encryption के साथ fixed IV उपयोग कर रही थी। AES-GCM में same key + same IV reuse एक गंभीर क्रिप्टोग्राफिक त्रुटि है:

- ciphertext संबंधों से plaintext संबंध निकाले जा सकते हैं
- authentication tag forgery संभव हो सकती है
- integrity guarantee टूट जाती है

इस use case में encryption की आवश्यकता ही नहीं थी। CSP nonce के लिए केवल यह ज़रूरी है:
- **unpredictable** हो
- **per-request unique** हो

ये गुण `RandomNumberGenerator` से सीधे मिल जाते हैं।

---

## क्या सुधारा गया

`Nonce.GenerateSecureNonce()` अब सीधे CSPRNG का उपयोग करता है:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

- Key Vault IV/key पर निर्भरता हटाई गई
- AES-GCM आधारित nonce generation हटाया गया
- `Nonce` constructor से `KeyVaultSecret` इनपुट हटे

साथ ही `NonceCatalogService.GetANonce` में check-then-indexer race को `TryGetValue(out ...)` आधारित atomic retrieval से ठीक किया गया।

---

## इसे सुरक्षित कैसे रखें

1. Nonce generation में कभी fixed IV/key का उपयोग न करें
2. `GenerateSecureNonce` को कमजोर scheme से replace न करें
3. nonce आकार कम से कम 16 bytes (128 bits) रखें
4. `RandomNumberGenerator.Fill` को `new Random()` से कभी न बदलें
5. `GetANonce` में single-step `TryGetValue(out ...)` pattern रखें

---

## इसे enforce करने वाले tests

| Test | क्या रोकता है |
|---|---|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | constructor regression |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | nonce format regression |
| `GenerateSecureNonce_Returns16ByteBase64` | nonce size regression |
| `Nonce_SuccessiveGenerations_AreUnique` | repeat nonce regression |
| `Nonce_HasSufficientEntropy` | weak randomness regression |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | backing store regression |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | concurrency race regression |
