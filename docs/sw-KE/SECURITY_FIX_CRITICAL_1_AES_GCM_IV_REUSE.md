# Marekebisho ya Usalama: Kutumia Tena IV ya AES-GCM katika Uzalishaji wa Nonce (Critical #1)

**Imerekebishwa katika:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Majaribio:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Nini Kilikuwa Kibaya

Darasa la `Nonce` lilitumia **usimbaji wa AES-GCM wenye IV isiyobadilika** iliyochukuliwa kutoka Azure Key Vault katika
kila mwito. Kutumia tena IV ileile na key ileile ya AES-GCM ni kosa kubwa sana la cryptography:

* Mshambuliaji anayeona ciphertext mbili zilizosimbwa kwa IV na key zilezile anaweza kufanya XOR yao ili
  kupata XOR ya plaintext hizo mbili.
* Muhimu zaidi kwa authentication tags zinazotegemea nonce, kutumia IV tena huruhusu forgery ya authentication tag,
  na kuvunja kabisa dhamana ya integrity ya AES-GCM.

Mbali na kushindwa huku kwa cryptography, usimbaji huu **haukuongeza faida yoyote ya usalama** kwa matumizi haya.
Nonce ya CSP inahitaji sifa mbili pekee: lazima iwe **isiyotabirika** na **ya kipekee kwa kila request**.
Sifa hizi tayari hutolewa moja kwa moja na cryptographically secure random number
generator (`RandomNumberGenerator`). Kusimba thamani kuliongeza ugumu bila kuongeza usalama.

### Msimbo wa Chanzo cha Tatizo (kabla ya marekebisho)

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

## Nini Kilirekebishwa

`Nonce.GenerateSecureNonce()` sasa hupiga moja kwa moja `RandomNumberGenerator.Fill(byte[])` ili kuzalisha
bytes 16 za data ya cryptographic random, kisha huweka Base64 juu ya matokeo:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* Hakuna miito ya Key Vault kwa IV au encryption key inayohitajika wala kufanywa.
* Hakuna AES-GCM au cipher nyingine yoyote inayohusika.
* Constructor ya `Nonce` haikubali tena parameters za `KeyVaultSecret`.

Bug ya pili pia ilirekebishwa katika `NonceCatalogService.GetANonce`: method hiyo awali ilitumia
ukaguzi wa hatua mbili kisha lookup (`TryGetValue` ikifuatiwa na indexer `[]`), jambo ambalo si atomic na lingeweza
kutupa `KeyNotFoundException` wakati thread nyingine iliondoa key kati ya miito hiyo miwili. Marekebisho
hutumia `TryGetValue` pamoja na parameter ya `out` ili kurejesha value katika operesheni moja ya atomic.

---

## Jinsi ya Kuhakikisha Hili Linabaki Limeboreshwa

1. **Usiwahi kuingiza IV au key ya Key Vault kwa uzalishaji wa nonce.** Ikiwa Key Vault inatumika kwa siri nyingine,
   hilo ni sawa — lakini uzalishaji wa nonce haupaswi kamwe kutegemea IV isiyobadilika.

2. **Usibadilishe `GenerateSecureNonce` na mpango wa AES-GCM au CBC/CTR** unaotumia tena IV
   au counter kwenye requests mbalimbali.

3. **Hifadhi nonce kuwa angalau bytes 16 (128 bits).** Kupunguza urefu wa byte huongeza uwezekano wa collision
   na hupunguza entropy inayopatikana kwa CSP.

4. **Usibadilishe `RandomNumberGenerator.Fill` kuwa `new Random()`** au kitu kingine chochote kisicho CSPRNG.

5. **Hifadhi `NonceCatalogService.GetANonce` ikitumia `TryGetValue` pamoja na parameter ya `out`.**
   Muundo wa hatua mbili wa ukaguzi kisha lookup (`TryGetValue` + indexer) si thread-safe hata ukiwa na
   `ConcurrentDictionary`.

### Majaribio Yanayolazimisha Marekebisho Haya

| Jaribio | Kinachogundua |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Hushindwa ku-compile ikiwa constructor itarejeshwa kukubali `KeyVaultSecret` IV + key |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Hushindwa ikiwa uzalishaji wa nonce umeharibika au unarudisha thamani isiyo Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | Hushindwa ikiwa urefu wa byte unapunguzwa chini ya 16 |
| `Nonce_SuccessiveGenerations_AreUnique` | Hushindwa ikiwa kutumia IV tena kunasababisha nonce ileile kuzalishwa mara kwa mara |
| `Nonce_HasSufficientEntropy` | Hushindwa ikiwa chanzo cha entropy si cha nasibu |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Hushindwa ikiwa `ConcurrentDictionary` itarejeshwa kuwa `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Hushindwa ikiwa TOCTOU race katika `GetANonce` itarudishwa |

