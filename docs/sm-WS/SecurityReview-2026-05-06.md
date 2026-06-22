# Iloiloga Saogalemu — WebAppExperimental266

**Aso:** 2026-05-06
**Avanoa:** Iloiloga atoa o le falesii code (mulimuli i le iloiloga o le 2026-05-05)
**Iloiloina e:** Iloiloga Otometi Saogalemu

---

## Ao Faafomai Pulega

Faamaonia lenei iloiloga mulimuli o saosaoa fa'alavelave sefulu-iva (19) uma na maua i le iloiloga saogalemu o le 2026-05-05 ua sola manuia. Fa'ailoa fo'i lenei iloiloga i lima (5) mea fou pe totoe o le taimi nei. Ua fa'aleleia tele le saogalemu o le polokalama mai le iloiloga muamua.

---

## Tulaga o Mea Na Maua Muamua (2026-05-05)

Mea na maua muamua sefulu-iva (19) **ua faamaonia ua sa'o**:

| # | Mea na Maua | Maualuga | Tulaga |
|---|-------------|----------|--------|
| 1 | Toe fa'aogaina o le IV AES-GCM i totonu o le faiga nonce | 🔴 Maualuga Tele | ✅ Sa'o |
| 2 | Nonce tusia i totonu o lautogia | 🔴 Maualuga Tele | ✅ Sa'o |
| 3 | Nonce fa'atulagaga tusia tumau fa'amautu | 🔴 Maualuga Tele | ✅ Sa'o |
| 4 | Faleoloa nonce lautele e le saogalemu mo ulutala | 🟠 Maualuga | ✅ Sa'o |
| 5 | Aitalafu fa'amaonu mTLS le fa'atumau | 🟠 Maualuga | ✅ Sa'o |
| 6 | Siaki fa'afolau mTLS faʻaumatiaina e le tulaga masani | 🟠 Maualuga | ✅ Sa'o |
| 7 | OCSP fa'afo'i mau i mea talafeagai (stub) | 🟠 Maualuga | ✅ Sa'o |
| 8 | Fa'amaonia/Pule faʻaumatiaina e tulaga masani i totonu o fa'atulagaga | 🟠 Maualuga | ✅ Sa'o |
| 9 | Ulutala saogalemu fa'aofi i le aofai mulimuli o le pipeline | 🟠 Maualuga | ✅ Sa'o |
| 10 | Cookie auau e le ni `Secure` + `SameSite` | 🟡 Ogatotonu | ✅ Sa'o |
| 11 | Ulutala `Set-Cookie` lautele e le manino | 🟡 Ogatotonu | ✅ Sa'o |
| 12 | `Content-Type` tumauina i `text/html` i nofoaga uma | 🟡 Ogatotonu | ✅ Sa'o |
| 13 | `AllowedHosts` fa'atulagaina i fa'ailoga wildcard | 🟡 Ogatotonu | ✅ Sa'o |
| 14 | Nonce e le o fa'aofi i `<script>` tags i totonu o fa'atulagaga | 🟡 Ogatotonu | ✅ Sa'o |
| 15 | Ulutala `Referrer-Policy` ua mole | 🟡 Ogatotonu | ✅ Sa'o |
| 16 | PII tusia i totonu o lautogia | 🔵 Maualalo | ✅ Sa'o |
| 17 | Feso'otaiga string fa'aopopo i totonu o tala | 🔵 Maualalo | ✅ Sa'o |
| 18 | Galuega Key Vault o stub | 🔵 Maualalo | ✅ Sa'o |
| 19 | `X-XSS-Protection: 1; mode=block` tuai | 🔵 Maualalo | ✅ Sa'o |

---

## Mea Fou / Totoe

| # | Nofoaga | Maualuga |
|---|---------|----------|
| 20 | NonceRefresherService tago atu i fa'alagolago fausiaina Key Vault e le fa'aogaina | 🟠 Maualuga |
| 21 | Kesi o luga o OcspValidationService fa'aogaina Dictionary e le saogalemu mo ulutala | 🟡 Ogatotonu |
| 22 | Stub fa'amaonia OCSP o iai pea — fafo'i umi ae e le mafai | 🔵 Maualalo |
| 23 | mTLS ma AllowedIssuers gaogao faia fa'aitiitia tautatala uma (fail-closed, e le tusia) | 🔵 Maualalo |
| 24 | OcspSettings.ServerUnavailableBehavior fa'aletonu i "Warn" (fa'atagaina alu i le taimi o le sese) | 🔵 Maualalo |

---

## Mea Manino

### ✅ Sa'oga Faamaonia mai le 2026-05-05

#### 1. Toe Fa'aogaina o le IV AES-GCM — Sa'o

**Faila:** `Models/Main_Objects/Nonce.cs`

Ua suia atoatoa le faiga nonce i AES-GCM. E valaau nei `Nonce.GenerateSecureNonce()` i `RandomNumberGenerator.Fill(randomBytes)` i luga o 16 bytes e eseese ma fa'afo'i i le string Base64. E leai se fa'alagolago Key Vault, e leai se IV, e leai se fa'amaumau — o le auala sili lea mo le CSP nonce.

---

#### 2. Aoga o Nonce E Le O Tusia i Lautogia — Sa'o

**Faila:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

E tusi le faila e lua ia na faila na'o faamatalaga tulaga (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) ae leiloa le tau nonce lava ia.

---

#### 3. Nonce Fa'atulagaga Tusia Tumau Aveese — Sa'o

**Faila:** `Services/OptimizedNonceMiddleware.cs`

String tumau e tolu (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) ua sui i valaau atu i `Nonce.GenerateSecureNonce()` i auala masani ma le lua o fa'ailoga sese manatuaina.

---

#### 4. Faleoloa Nonce Saogalemu mo Ulutala — Sa'o

**Faila:** `Services/NonceCatalogService.cs`

Ua suia `Dictionary<string, Nonce>` i `ConcurrentDictionary<string, Nonce>`. E fa'aogaina nei `GetANonce` se valaau atomic e tasi `TryGetValue` nai lo le siaki lua-laasaga.

---

#### 5. Fa'amaona Mea Auina mTLS e Galue Nei — Sa'o

**Faila:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Ua fa'aliliu le fa'amoemoe fa'amaona mea auina fa'atumauina i le valaau `mtlsSettings.IsIssuerAllowed(issuer)`, e gaosia ai le fa'atusatusaga string le feoa'i i luga/lalo e faasaga i `AllowedIssuers`. A gaogao le lisi (e le fa'atulagaina), e toe fo'i le metotia i `false`, e taofia fa'amanuiaga uma (fail-closed).

---

#### 6. Siaki Fa'afolau mTLS Fa'aagaina nei e le Tulaga Masani — Sa'o

**Faila:** `Models/Settings/MtlsSettings.cs`

O le `CheckCertificateRevocation` ua masani nei i `true`. Fa'atulagaina fo'i e `appsettings.template.json` i `"CheckCertificateRevocation": true`.

---

#### 7. Stub OCSP Fafo'i Umi Nei — Sa'o

**Faila:** `Services/OcspValidationService.cs`

E toe fo'i nei `PerformOcspValidationAsync` i `IsValid = false` ma `OcspStatus.Error` ma tusia le sese, nai lo le toe fo'i filemu i `IsValid = true`. O le fa'aagaina o le OCSP o le a taofia fa'amanuiaga uma seia o'o i le fa'atinoga moni.

---

#### 8. Fa'amaonia ma Pule Fa'aagaina nei e le Tulaga Masani — Sa'o

**Faila:** `Models/Settings/FeatureFlags.cs`

O le `EnableAzureAd` ma `EnableAuthorization` ua masani nei i `true` i totonu o le vasega `FeatureFlags`. Fa'atulagaina fo'i e `appsettings.json` mea e lua i `true`.

---

#### 9. Ulutala Saogalemu Fa'aofi muamua i Routing — Sa'o

**Faila:** `Program.cs`

E valaau atu `UseNonceAndSecurityHeadersAsync` ma `UseStandardSecurityHeaders` a o le'i valaau `UseRouting`, `UseAuthentication`, ma `UseAuthorization`. Tali uma, e aofia ai tali pupuu 401/403, o le a maua ai o latou ulutala saogalemu.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce i Fa'atulagaga, Referrer-Policy — Sa'o

**Faila:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- E fa'atulagaina nei le cookie auau i `CookieSecurePolicy.Always` ma `SameSiteMode.Strict`.
- Ua aveese le ulutala `Set-Cookie` e le o manino na fa'apipi'i ai.
- Ua aveese le fesuiaiga lautele `Content-Type: text/html`.
- O le `AllowedHosts` i `appsettings.json` o lea `"localhost;127.0.0.1"`; e fa'aogaina e le template i `"{{YOUR_HOSTNAME}}"`.
- O le tolu `<script>` tags i totonu o `_Layout.cshtml` ua maua nei `nonce="@Context.Items["Nonce"]"`.
- Ua fa'aofi `Referrer-Policy: strict-origin-when-cross-origin` e `UseStandardSecurityHeaders`.

---

#### 16–19. PII i Lautogia, Feso'otaiga String Fa'aopopo, Stub Key Vault, X-XSS-Protection — Sa'o

**Faila:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- PII uma (OID, imeli, igoa, SID, tulafono) ua HMAC-SHA256 hash-ina e ala i `LoggingHelper.HashPii()` a o le'i tusia i tala. E mafai ona fa'aauau atu le ki HMAC fa'amautu e ala i `Logging:PiiHmacKey` i le fa'atulagaga; e fa'aogaina se ki e eseese mo ia sailiga a leai se fa'atulagaga.
- E fa'amaonia na'o le fa'amatalaga Cosmos DB e ola le string fa'aopopo (`!string.IsNullOrEmpty`), ae le o mea o iai.
- E togi nei `AzureKeyVaultCertificateOperations` se `InvalidOperationException` i le amata a null le fa'amaonia, nai lo le toe fo'i filemu i aoga fa'amatalaga.
- O le `X-XSS-Protection` ua fa'atulagaina nei i `"0"` (fa'amoumou le fa'amata XSS tuai), e tusa ma fautuaga o su'esu'ega fou.

---

## 🟠 Maualuga

### 20. NonceRefresherService Tago atu i Fa'alagolago Fausiaina Key Vault e le Fa'aogaina

**Faila:** `Services/NonceRefresherService.cs`

E fa'alavelaveina e `NonceRefresherService` vaega fausiaina mo `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, ma `IAzureKeyVaultOperationsService`. Talu ai ua faafaigofie le faiga nonce e fa'aogaina sa'o `RandomNumberGenerator`, e leai se fa'alagolago e fa'aaogaina.

**Fa'afitauli:** Pe a `EnableNonceServices = true` ma `EnableKeyVault = false` (masani), e le resitalaina nei auaunaga i le pusa DI, ma gaosia ai se `InvalidOperationException` i le taimi talimalo pe a fa'aleleia muamua le auaunaga nonce. O se tulaga faato'a-umi na mafua mai i le fa'atulagaga masani. E fa'atulagaina e le vasega `FeatureFlags` e masani `EnableNonceServices = true`, o le a faatafunaina ai le siosiomaga uma e faalagolago na'o le masani o le vasega (e aunoa ma le suiga o `appsettings.json`).

**Fautuaga:** E aveese le vaega fausiaina e fa o le a le fa'aogaina ma a latou matagi faalilolilo e felagolagomai mai `NonceRefresherService`. E manaʻomia na'o le auaunaga `ILogger<NonceRefresherService>`, `ILoggerFactory`, ma `INonceCatalogService`.

---

## 🟡 Ogatotonu

### 21. Kesi o Luga o OcspValidationService Fa'aogaina Dictionary E le Saogalemu mo Ulutala

**Faila:** `Services/OcspValidationService.cs` (laina 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

O le `Dictionary<TKey, TValue>` e le saogalemu mo faitau ma tusi faatasi. Afai o le `OcspValidationService` resitalaina e tusa ma singleton (pe faasoa le faiga e tasi i ulutala), e mafai ona fa'aleagaina faitau OCSP faatasi le kesi, ma gaosia ai mea leiloa, togi e le mafai ona fautuaina, pe toe fo'i o faʻamatalaga tuai.

**Fautuaga:** Sui `Dictionary<string, CachedOcspResponse>` i `ConcurrentDictionary<string, CachedOcspResponse>`. Fa'afouina le valaau `_cache.Remove` (laina 103) i `_cache.TryRemove`.

---

## 🔵 Maualalo / Fa'amatalaga

### 22. Stub Fa'amaona OCSP — Fafo'i Umi ae E le Mafai

**Faila:** `Services/OcspValidationService.cs` (laina 157–173)

O le `PerformOcspValidationAsync` o se stub. Ua fa'aleleia lelei le sa'oga #7 le faiga mai "saogalemu mau" i "le saogalemu mau (fail-closed)". Ae peita'i, e le o le fa'atinoga OCSP moni le faiga nei. Ina ia `EnableOcspValidation = false` (masani), e leai se aafiaga i le faiga pisinisi. A o le'i fa'aagaina OCSP i so'o se siosiomaga, e tatau ona fa'atinoina se fa'atinoga OCSP lelei.

---

### 23. mTLS ma AllowedIssuers Gaogao Taofia Fa'amanuiaga Fa'aaoga Uma

**Faila:** `Models/Settings/MtlsSettings.cs`

Pe a `ValidateClientCertificateIssuer = true` (masani) ma `AllowedIssuers` gaogao (fo'i masani pe a le fa'atulagaina), e toe fo'i `IsIssuerAllowed()` i `false`, e taofia ai fa'amanuiaga fa'aaoga uma. O se faiga fail-closed manaʻomia, ae e le o tusia ma le manino. E mafai e tagata fa'atulagaga e fa'aagaina ai mTLS e aunoa ma le faitauina lelei o le template ona maua ai fa'amanuiaga uma e taofia e aunoa ma fa'amatalaga manino.

**Fautuaga:** E fa'aopopo se faitauga lautogia lapisi pe a `ValidateClientCertificateIssuer = true` ma `AllowedIssuers` gaogao i le amata.

---

### 24. OcspSettings.ServerUnavailableBehavior Masani i "Warn"

**Faila:** `appsettings.template.json` (laina 134), `Services/OcspValidationService.cs`

O le masani o `ServerUnavailableBehavior` o `"Warn"` i totonu o le template, e fa'atalanoa ai talosaga i le le mafai ona ausia le auaunaga OCSP. Mo siosiomaga saogalemu maualuga, e tatau ona suia i `"Fail"` e puipuia ai le le mafai o le auaunaga OCSP mai le faifaipea le faamauina o le togi fa'amaona.

**Fautuaga:** E tusia manino aʻoaʻoga e tolu (`Fail`, `Allow`, `Warn`) i totonu o le template ma mafaufau e suia le masani i `"Fail"` e tusa ma le mataupu faavae o le lafo maualalo.

---

## Iloiloga o Ulutala Saogalemu (Tulaga Nei)

E fa'aofi nei ulutala nei e `UseStandardSecurityHeaders`:

| Ulutala | Aoga | Iloiloga |
|---------|------|---------|
| `X-Frame-Options` | `DENY` | ✅ Lelei |
| `X-XSS-Protection` | `0` | ✅ Lelei (fa'amoumou fa'amata tuai) |
| `X-Content-Type-Options` | `nosniff` | ✅ Lelei |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Lelei |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Lelei |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Lelei |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Lelei |
| `Permissions-Policy` | geolocation, camera, microphone, interest-cohort fa'amoumouina | ✅ Lelei |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Lelei |
| `Content-Security-Policy` | Fa'alagolago nonce, fa'aofi pe a CSP fa'aagaina | ✅ Lelei |
| `Server` | Natia i `"webserver"` | ✅ Lelei |
| `X-Powered-By` | Aveese | ✅ Lelei |

---

## Iloiloga Aotelega

Ua sa'o le polokalama i nesi maualuga ma maualuga uma mai le iloiloga muamua. O mea ua toe iai nei o lo'o fa'atapulaaina i le fa'afitauli fa'atulagaga/DI maualuga e tasi (mea #20) ma mea fa'ailo maualalo. Ua fa'aleleia tele le saogalemu. O le gaoioiga vave e fautuaina mo le mea #20 (fa'alagolago DI e le fa'aogaina i totonu o NonceRefresherService) talu ai e mafai ona taofia le polokalama mai le amata i lalo o le fa'atulagaga masani.
