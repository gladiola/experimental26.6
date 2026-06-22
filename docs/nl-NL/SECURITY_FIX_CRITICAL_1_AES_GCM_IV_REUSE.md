# Securityfix: Hergebruik van AES-GCM IV in nonce-generatie (Kritiek #1)

**Gefixt in:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Wat was er mis

De `Nonce`-klasse gebruikte **AES-GCM-encryptie met een vaste IV** die bij elke aanroep uit Azure Key Vault werd opgehaald. Het hergebruiken van dezelfde IV met dezelfde AES-GCM-sleutel is een catastrofale cryptografische fout:

* Een aanvaller die twee ciphertexts ziet die met dezelfde IV en sleutel zijn versleuteld, kan ze XOR'en en zo de XOR van de plaintexts terughalen.
* Nog kritieker voor nonce-gebaseerde authenticatietags: IV-hergebruik maakt tag-vervalsing mogelijk, waardoor de integriteitsgarantie van AES-GCM volledig breekt.

Naast deze cryptografische fout voegde encryptie **geen beveiligingsvoordeel** toe voor deze use-case.
Een CSP-nonce heeft alleen twee eigenschappen nodig: **onvoorspelbaar** en **uniek per request**.
Die eigenschappen krijg je direct van een cryptografisch veilige randomgenerator (`RandomNumberGenerator`). Encryptie voegde complexiteit toe zonder extra veiligheid.

### Root-cause code (vóór fix)

```csharp
// Nonce.cs — dezelfde IV uit Key Vault bij elke aanroep
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — één keer opgehaald en hergebruikt over alle requests
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## Wat is gefixt

`Nonce.GenerateSecureNonce()` roept nu direct `RandomNumberGenerator.Fill(byte[])` aan om
16 bytes cryptografisch willekeurige data te genereren, en codeert dat daarna in Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* Er zijn geen Key Vault-calls voor IV of encryptiesleutel meer nodig of aanwezig.
* Er wordt geen AES-GCM of andere cipher meer gebruikt.
* De `Nonce`-constructor accepteert geen `KeyVaultSecret`-parameters meer.

Er is ook een secundaire bug gefixt in `NonceCatalogService.GetANonce`: de methode gebruikte eerder een tweestaps check-then-lookup (`TryGetValue` gevolgd door indexer `[]`), wat niet atomair is en een `KeyNotFoundException` kon geven als een andere thread de key verwijderde tussen die twee aanroepen. De fix gebruikt `TryGetValue` met `out` om in één atomaire stap op te halen.

---

## Hoe je dit gefixt houdt

1. **Voeg nooit een Key Vault-IV of sleutel toe voor nonce-generatie.** Key Vault gebruiken voor andere geheimen is prima, maar nonce-generatie mag nooit afhankelijk zijn van een vaste IV.

2. **Vervang `GenerateSecureNonce` nooit door een AES-GCM- of CBC/CTR-schema** dat een IV of teller over requests hergebruikt.

3. **Houd de nonce minimaal 16 bytes (128 bits).** Minder bytes vergroot botsingskans en verlaagt de entropie voor CSP.

4. **Vervang `RandomNumberGenerator.Fill` niet door `new Random()`** of een andere niet-CSPRNG.

5. **Houd `NonceCatalogService.GetANonce` op `TryGetValue` met `out`.** Het tweestapspatroon (`TryGetValue` + indexer) is niet thread-safe, zelfs niet met `ConcurrentDictionary`.

### Tests die deze fix afdwingen

| Test | Wat het opvangt |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Compileert niet als constructor teruggezet wordt naar `KeyVaultSecret` IV + key |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Faal bij kapotte nonce-generatie of niet-Base64-uitvoer |
| `GenerateSecureNonce_Returns16ByteBase64` | Faal als byte-lengte onder 16 komt |
| `Nonce_SuccessiveGenerations_AreUnique` | Faal als IV-hergebruik dezelfde nonce herhaaldelijk oplevert |
| `Nonce_HasSufficientEntropy` | Faal bij niet-willekeurige entropiebron |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Faal als `ConcurrentDictionary` teruggezet wordt naar `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Faal als TOCTOU-race in `GetANonce` terugkomt |
