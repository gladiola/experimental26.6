# Sekuriteitsregstelling: AES-GCM IV-hergebruik in Nonce-generering (Kritiek #1)

**Reggestel in:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Toetse:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Wat Was Fout

Die `Nonce`-klas het **AES-GCM-enkripsie met 'n vaste IV** gebruik wat by elke aanroep van Azure Key Vault gehaal is. Die hergebruik van dieselfde IV met dieselfde AES-GCM-sleutel is 'n katastrofiese kriptografiese fout:

* 'n Aanvaller wat twee sifertekste wat met dieselfde IV en sleutel geënkripteer is, waarneem, kan hulle XOR om die XOR van die twee klartekste te herstel.
* Wat nog kritiekers is vir nonce-gebaseerde verifikasie-etikette: IV-hergebruik laat die vervalssing van verifikasie-etikette toe, wat die integriteitsgaransie van AES-GCM volledig verbreek.

Buite die kriptografiese mislukking het die enkripsie **geen sekuriteitsvoordeel** vir hierdie gebruiksgeval bygevoeg nie. 'n CSP-nonce benodig slegs twee eienskappe: dit moet **onvoorspelbaar** en **uniek per versoek** wees. Hierdie eienskappe word reeds direk verskaf deur 'n kriptografies veilige ewekansigegetal-generator (`RandomNumberGenerator`). Die enkripteer van die waarde het kompleksiteit bygevoeg sonder om sekuriteit by te voeg.

### Oorsprong-kode (voor regstelling)

```csharp
// Nonce.cs — dieselfde IV gehaal van Key Vault by elke aanroep
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — een keer gehaal en hergebruik oor alle versoeke
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## Wat Reggestel Is

`Nonce.GenerateSecureNonce()` roep nou `RandomNumberGenerator.Fill(byte[])` direk aan om 16 grepe kriptografies ewekansige data te produseer, en kodeer dan die resultaat as Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* Geen Key Vault-aanroepe vir IV of enkripsiesleutel is nodig of word gemaak nie.
* Geen AES-GCM of enige ander sifering is betrokke nie.
* Die `Nonce`-konstrukteur aanvaar nie meer `KeyVaultSecret`-parameters nie.

'n Sekondêre fout is ook reggestel in `NonceCatalogService.GetANonce`: die metode het voorheen 'n twee-stap kontroleer-dan-opsoek (`TryGetValue` gevolg deur indekseerder `[]`) gebruik, wat nie atomies is nie en 'n `KeyNotFoundException` kon gooi wanneer 'n ander draad die sleutel tussen die twee aanroepe verwyder het. Die regstelling gebruik `TryGetValue` met die `out`-parameter om die waarde in 'n enkele atomiese bewerking te haal.

---

## Hoe om Dit Reggestel te Hou

1. **Stel nooit 'n Key Vault-IV of -sleutel vir nonce-generering in nie.** As Key Vault vir ander geheime gebruik word, is dit goed — maar nonce-generering moet nooit van 'n vaste IV afhang nie.

2. **Vervang `GenerateSecureNonce` nooit met 'n AES-GCM- of CBC/CTR-skema** wat 'n IV of teller oor versoeke hergebruik nie.

3. **Hou die nonce ten minste 16 grepe (128 bisse).** Die vermindering van die greeplengte verhoog botsingswaarskynlikheid en verminder die entropie beskikbaar vir CSP.

4. **Vervang `RandomNumberGenerator.Fill` nie met `new Random()`** of enige ander nie-CSPRNG nie.

5. **Hou `NonceCatalogService.GetANonce` met `TryGetValue` met die `out`-parameter.** Die twee-stap kontroleer-dan-opsoek patroon (`TryGetValue` + indekseerder) is nie draadveilig nie, selfs nie met `ConcurrentDictionary` nie.

### Toetse wat Hierdie Regstelling Afdwing

| Toets | Wat Dit Vang |
|-------|-------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Misluk om te kompileer as die konstrukteur teruggestel word om `KeyVaultSecret` IV + sleutel te aanvaar |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Misluk as nonce-generering gebreek is of 'n nie-Base64-waarde terugstuur |
| `GenerateSecureNonce_Returns16ByteBase64` | Misluk as greeplengte onder 16 verminder word |
| `Nonce_SuccessiveGenerations_AreUnique` | Misluk as IV-hergebruik veroorsaak dat dieselfde nonce herhaaldelik geproduseer word |
| `Nonce_HasSufficientEntropy` | Misluk as die entropiebron nie-ewekansig is |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Misluk as `ConcurrentDictionary` na `Dictionary` teruggestel word |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Misluk as die TOCTOU-wedrenjag in `GetANonce` herbekendgestel word |
