# Ceartú Slándála: Athúsáid IV AES-GCM i nGiniúint Nonce (Criticiúil #1)

**Ceartaithe i:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Tástálacha:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Cad a bhí mícheart

Bhain an rang `Nonce` úsáid as **criptiú AES-GCM le IV seasta** a fuarthas ó Azure Key Vault ar gach glao.
Is earráid chripteagrafach an-tromchúiseach í an IV céanna a athúsáid leis an eochair AES-GCM chéanna:

* Is féidir le hionsaitheoir a fheiceann dhá ciphertext atá criptithe leis an IV agus eochair chéanna iad a XORáil chun XOR an dá phlainthéacs a fháil.
* Níos measa fós do chlibeanna fíordheimhnithe bunaithe ar nonce, ceadaíonn athúsáid IV brionnú clibeanna fíordheimhnithe, rud a bhriseann sláine AES-GCM go hiomlán.

Seachas an teip chripteagrafach, níor chuir an criptiú **aon bhuntáiste slándála** ar fáil don chás úsáide seo.
Níl ach dhá airí de dhíth ar nonce CSP: caithfidh sé a bheith **dothuartha** agus **uathúil in aghaidh an iarratais**.
Soláthraítear na hairíonna sin cheana féin go díreach le gineadóir randamach cripteagrafach (`RandomNumberGenerator`).
Chuir criptiú castacht leis gan slándáil bhreise a chur leis.

### Cód bunchúise (roimh an gceartú)

```csharp
// Nonce.cs — an IV céanna á fháil ó Key Vault ar gach glao
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — faighte uair amháin agus athúsáidte thar gach iarratas
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## Cad a ceartaíodh

Glaonn `Nonce.GenerateSecureNonce()` anois go díreach ar `RandomNumberGenerator.Fill(byte[])` chun
16 bheart de shonraí randamacha cripteagrafacha a tháirgeadh, ansin ionchódaíonn sé an toradh mar Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* Níl gá le glaonna Key Vault le haghaidh IV ná eochair chriptithe.
* Níl AES-GCM ná aon chifra eile i gceist.
* Ní ghlacann tógálaí `Nonce` paraiméadair `KeyVaultSecret` a thuilleadh.

Ceartaíodh fabht tánaisteach i `NonceCatalogService.GetANonce` freisin: d'úsáid an modh patrún dhá chéim roimhe seo
(`TryGetValue` agus ansin an t-innéacsóir `[]`), nach bhfuil adamhach agus a d'fhéadfadh `KeyNotFoundException` a chaitheamh
nuair a bhain snáithe eile an eochair idir an dá ghlao. Úsáideann an ceartú `TryGetValue` leis an bparaiméadar `out`
chun an luach a fháil in aon oibríocht adamhach amháin.

---

## Conas é seo a choinneáil ceartaithe

1. **Ná tabhair isteach IV ná eochair Key Vault riamh do ghiniúint nonce.** Má úsáidtear Key Vault do rúin eile,
   tá sin ceart go leor — ach níor cheart go mbraithfeadh giniúint nonce ar IV seasta riamh.

2. **Ná cuir scéim AES-GCM ná CBC/CTR in ionad `GenerateSecureNonce`** a athúsáideann IV nó cuntar thar iarratais.

3. **Coinnigh an nonce ar a laghad 16 bheart (128 giotán).** Méadaíonn laghdú fad na mbeart dóchúlacht imbhuailte
   agus laghdaíonn sé eantrópacht atá ar fáil do CSP.

4. **Ná hathraigh `RandomNumberGenerator.Fill` go `new Random()`** ná go foinse neamh-CSPRNG eile.

5. **Coinnigh `NonceCatalogService.GetANonce` ag úsáid `TryGetValue` leis an bparaiméadar `out`.** Ní patrún
   snáithe-sábháilte é an cur chuige dhá chéim (`TryGetValue` + innéacsóir) fiú le `ConcurrentDictionary`.

### Tástálacha a fhorfheidhmíonn an ceartú seo

| Tástáil | Cad a aimsíonn sí |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Teipeann ar thiomsú má chuirtear tógálaí ar ais chun `KeyVaultSecret` IV + eochair a ghlacadh |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Teipeann má bhristear giniúint nonce nó má fhilleann sí luach neamh-Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | Teipeann má laghdaítear fad na mbeart faoi bhun 16 |
| `Nonce_SuccessiveGenerations_AreUnique` | Teipeann má chuireann athúsáid IV an nonce céanna ar fáil arís agus arís |
| `Nonce_HasSufficientEntropy` | Teipeann má tá foinse na heantrópachta neamh-randamach |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Teipeann má chuirtear `Dictionary` in áit `ConcurrentDictionary` ar ais |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Teipeann má thugtar an rás TOCTOU i `GetANonce` isteach arís |
