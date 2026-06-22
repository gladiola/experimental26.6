# Whakatika Haumaru: Te whakamahi anō i te IV AES-GCM i te hanga nonce (Tino Nui #1)

**I whakatikaina ki:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Ngā whakamātautau:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## He aha te raruraru

I whakamahia e te karaehe `Nonce` te whakamunatanga **AES-GCM me tētahi IV pūmau** i tikina i Azure Key Vault
ia karanga. Ko te whakamahi anō i taua IV me taua kī he hē whakamunatanga tino kino.

Hei tua atu, kāore te whakamunatanga i tuku painga haumaru mō tēnei horopaki CSP nonce.
Ko ngā āhuatanga matua e rua: **kore-matapae** me te **motuhake mō ia tono**. Ka riro ēnei mai i
`RandomNumberGenerator` me te kore AES-GCM.

### Waehere pūtake (i mua i te whakatika)

```csharp
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## He aha i whakatikaina

`Nonce.GenerateSecureNonce()` ināianei ka whakamahi tika i `RandomNumberGenerator.Fill(byte[])` hei
waihanga 16 paita matapōkere, kātahi ka huri ki Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

- Kāore he karanga Key Vault mō IV/kī mō te nonce.
- Kāore he AES-GCM e whakamahia ana.
- Kua tangohia ngā tawhā `KeyVaultSecret` i te kaihanga `Nonce`.

I whakatikahia hoki tētahi hapa TOCTOU i `NonceCatalogService.GetANonce`: kua huri te tauira
`TryGetValue` + indexer ki tētahi karanga `TryGetValue(..., out ...)` kotahi, haumaru ake mō te
`ConcurrentDictionary`.

---

## Me pēhea te pupuri i te whakatika

1. **Kaua rawa e whakahoki i te IV/kī Key Vault mō te hanga nonce.**
2. **Kaua e whakakapi i `GenerateSecureNonce` ki AES-GCM/CBC/CTR me te IV/counter whakamahi anō.**
3. **Puritia te nonce kia iti rawa 16 paita (128-bit).**
4. **Kaua e whakamahi `new Random()` mō tēnei. Me CSPRNG tonu.**
5. **Puritia `NonceCatalogService.GetANonce` ki te tauira `TryGetValue` me te `out`.**

### Ngā whakamātautau e here ana i tēnei whakatika

| Whakamātautau | He aha ka mau |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Ka hinga mēnā ka whakahokia mai ngā tawhā Key Vault ki te constructor |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Ka hinga mēnā ka pakaru te hanga nonce, kāore rānei i te Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | Ka hinga mēnā ka whakahekea te rahi paita i raro i te 16 |
| `Nonce_SuccessiveGenerations_AreUnique` | Ka hinga mēnā ka puta tonu ngā nonce ōrite |
| `Nonce_HasSufficientEntropy` | Ka hinga mēnā kāore te pūtake entropy i te matapōkere tika |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Ka hinga mēnā ka whakahokia ki `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Ka hinga mēnā ka whakahokia te tōraro TOCTOU |
