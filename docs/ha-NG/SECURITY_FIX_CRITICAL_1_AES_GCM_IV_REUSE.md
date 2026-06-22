# Gyaran Tsaro: Sake amfani da AES-GCM IV a Nonce Generation (Critical #1)

**An gyara a:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`

---

## Abin da ya faru ba daidai ba

`Nonce` class tana amfani da AES-GCM tare da **fixed IV** daga Key Vault a kowane kira. Sake amfani da IV tare da key iri ɗaya a AES-GCM babban kuskure ne na cryptography:

- Mai hari na iya kwatanta ciphertexts guda biyu
- Ana iya karya tabbacin integrity (tag forgery)

Baya ga haka, encrypting CSP nonce bai ƙara tsaro ga wannan amfani ba. CSP nonce yana buƙatar:
- **Ba a iya hasashe (unpredictable)**
- **Na daban ga kowace request (unique)**

Waɗannan ana samu kai tsaye ta CSPRNG (`RandomNumberGenerator`).

---

## Abin da aka gyara

`Nonce.GenerateSecureNonce()` yanzu tana amfani da:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

- Babu buƙatar Key Vault IV/key don nonce generation
- Babu AES-GCM ko wani cipher
- Constructor na `Nonce` ba ya karɓar `KeyVaultSecret` yanzu

An kuma gyara race condition a `NonceCatalogService.GetANonce` ta amfani da `TryGetValue` mai `out` kai tsaye (atomic lookup) maimakon check-then-indexer biyu.

---

## Yadda za a ci gaba da gyaran

1. Kada a sake kawo Key Vault IV/key don nonce generation
2. Kada a mayar da nonce generation zuwa AES-CBC/CTR/GCM da reuse
3. A bar nonce aƙalla 16 bytes
4. Kada a maye gurbin CSPRNG da `new Random()`
5. A ci gaba da amfani da `TryGetValue(out ...)` a `GetANonce`

---

## Gwaje-gwajen da ke kare gyaran

- `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters`
- `Nonce_GetNonceAsString_ReturnsNonEmptyBase64`
- `GenerateSecureNonce_Returns16ByteBase64`
- `Nonce_SuccessiveGenerations_AreUnique`
- `Nonce_HasSufficientEntropy`
- `NonceCatalogService_BackingStore_IsConcurrentDictionary`
- `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions`
