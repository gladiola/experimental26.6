# Iloiloga o le Saogalemu — WebAppExperimental266

**Aso:** 2026-05-07
**Faʻaavanoa:** Suʻesuʻega atoatoa o le code base (faasolosolo mai le iloiloga o le 2026-05-06)
**Tagata Iloiloga:** Iloiloga o le Saogalemu Otometi

---

## Aotelega a le Faatinoga

Faamaonia e lenei iloiloga faasolosolo e 3 o le 5 vaivai na maua i le iloiloga o le saogalemu o le 2026-05-06 ua toe faaleleia atoatoa, ma le 1 o loʻo tumau pea le faaleleia faʻatasi. Faailoa foi e le iloiloga mauaina fou e 4. O loʻo aumaia pea le lelei o le tulaga o le saogalemu o le polokalama.

---

## Tulaga o Mauaina Tuai (2026-05-06)

| # | Mauaina | Lavelave | Tulaga |
|---|---------|----------|--------|
| 20 | E taofia e NonceRefresherService fautuaga o le faatulagaga Key Vault e le faaupuupuina | 🟠 Maualuga | ✅ Faaleleia |
| 21 | O le cache totonu o le OcspValidationService e faʻaaogā ai se Dictionary e le saogalemu mo le filo | 🟡 Fafati | ✅ Faaleleia |
| 22 | Stub faamaonia OCSP o loʻo i ai pea — tali aunoa faapitoa peitaʻi le faatinoina | 🔵 Maualalo | ⚠️ Talia (i le mamanuina) |
| 23 | mTLS e maua ai le AllowedIssuers avanoa e teena ai seti uma (fail-closed, e le faamauina) | 🔵 Maualalo | ✅ Faaleleia |
| 24 | O le tulaga faatonu o OcspSettings.ServerUnavailableBehavior o "Warn" (e faatagaina le solitulafono i le sese) | 🔵 Maualalo | ⚠️ Faaleleia Faatasi |

---

## Tulaga Faʻamaopoopo o Mauaina Tuai

### ✅ 20. Fautuaga DI E Le Faaupuupuina o NonceRefresherService — Faaleleia

**Faila:** `Services/NonceRefresherService.cs`

O le faatulagaga `NonceRefresherService` o loʻo faasilasila nei naʻo le `ILogger<NonceRefresherService>`, `ILoggerFactory`, ma le `INonceCatalogService`. Na aveesea fautuaga e fa na muamua le faʻaaogaina (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`). O lenei e fofoʻi ai le lamatiaga o le teena o le auaunaga na taofia ai le polokalama mai le amata pe a `EnableKeyVault = false` (le tulaga faatonu) ma `EnableNonceServices = true` (le tulaga faatonu).

---

### ✅ 21. Cache E Le Saogalemu mo Filo o OcspValidationService — Faaleleia

**Faila:** `Services/OcspValidationService.cs`

Na sui le `Dictionary<string, CachedOcspResponse> _cache` i le `ConcurrentDictionary<string, CachedOcspResponse>`. Na faʻafoʻi le valaʻau `_cache.Remove` i le `_cache.TryRemove`. O loʻo saogalemu nei le cache mo le avanoa tutusa.

---

### ⚠️ 22. Stub Faamaonia OCSP — Talia (I le Mamanuina)

**Faila:** `Services/OcspValidationService.cs`

O loʻo i ai pea le stub peitaʻi e tali aunoa saʻo. Talu ai o le `EnableOcspValidation` tulaga faatonu e `false`, e leai se aafiaga i le galuega. O lenei mauaina e talia o se mauaina faailoa i le faatali ina ia faatino atoatoa le OCSP.

---

### ✅ 23. AllowedIssuers Avanoa o mTLS — Faaleleia

**Faila:** `Extensions/ServiceCollectionExtensions.cs`

O loʻo faamauina nei se lapataʻiga amata pe a `ValidateClientCertificateIssuer = true` ma le `AllowedIssuers` avanoa:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

E maua ai faʻatonuga manino mo tagata faʻaogaʻau e feagai ma le amioga fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Faaleleia Faatasi

**Faila:** `appsettings.template.json` (faaleleia), `Models/Settings/OcspSettings.cs` (leʻi faaleleia)

O loʻo faasilasila saʻo nei le faataʻitaʻiga i `"ServerUnavailableBehavior": "Fail"`. Peita'i, o le tulaga faatonu o le vaega C# i le `OcspSettings.cs` (laina 39) o loʻo tumau pea i le `"Warn"`. Afai e faʻagaoioia e se tagata faʻaogaʻau le OCSP ma aveese le `ServerUnavailableBehavior` mai lana faila faʻatulagaga, o le tulaga faatonu `"Warn"` e faʻaaogā filemu, faatagaina le solitulafono i le faʻalavelaveina o le server OCSP. E tatau ona suia le tulaga faatonu o le vaega e fetaui ma fautuaga o le faataʻitaʻiga.

---

## Mauaina Fou

| # | Vaega | Lavelave |
|---|------|----------|
| 25 | O le tulaga faatonu o le vaega OcspSettings ("Warn") e ese mai le faataʻitaʻiga ("Fail") | 🔵 Maualalo |
| 26 | O le ki nonce tutusa toʻatasi i le NonceCatalogService e faatagaina le faafesoʻotaʻi nonce i le va o talosaga | 🟡 Fafati |
| 27 | O se fuainumera faʻamolemole o le OptimizedNonceMiddleware e faʻaaogā ai numera 32-bit sainia (lamatiaga o le totoʻo) | 🔵 Maualalo |
| 28 | E lesitala le Program.cs i le singleton ILoggerFactory avanoa, e ufiufi ai le loggā famanatuga | 🟡 Fafati |

---

## 🟡 Fafati

### 26. O le Ki Nonce Tutusa o NonceCatalogService e Faʻatagaina le Faafesoʻotaʻi Nonce i le Va o Talosaga

**Faila:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

O le tusi nonce e teu ai nonce uma i lalo o le ki tutusa toʻatasi `"CSPNonce"`. I lalo o le avega faʻatasi, e mafai ona tupu le tulaga o le tausoʻo:

1. O le Talosaga A e valaʻau i le `RefreshNonceAsync()` — o le nonce A1 e teu o ia e pei o le `_nonceCollection["CSPNonce"]`.
2. O le Talosaga B e valaʻau i le `RefreshNonceAsync()` — o le nonce B1 e suia le `_nonceCollection["CSPNonce"]`.
3. O le Talosaga A e valaʻau i le `GetANonce("CSPNonce")` — mauaina B1, ae le o A1.
4. O le ulutala CSP ma le nonce o le faʻasologa o le Talosaga A e iai uma B1.
5. O le Talosaga B e iai foi le B1.

E faʻaaogā le nonce lava e tasi e tali e lua faʻatasi. E ui lava ina o loʻo faʻamaʻi tumau le faiʻai faʻatasi ma le tauaso e le faʻatulafonoina (e leai se manoa hardcode), o le nonce taua lava e foʻi mai i tali faʻatasi e tele, faʻaitiitia ai le tautinoga faʻapitoa mo talosaga taʻitasi e manaʻomia e le faʻatonuga CSP. O se tagata osofaʻiga e mafai ona matamata i le nonce o se tali e iai lava se nonce avanoa mo le itiiti ifo ma le tali faʻatasi e tasi.

**Fautuaga:** Faʻatupuina le nonce saʻo i totonu o le middleware mo talosaga taʻitasi (e.g., `Nonce.GenerateSecureNonce()`) ma teuina naʻo i le `HttpContext.Items["Nonce"]`, e alo ai le tusi tutusa mo nonce o talosaga taʻitasi. O le tusi tutusa e manaʻomia naʻo pe afai e manaʻomia ona faʻaaogāina e le nonce i luga o vaega o middleware i totonu o se talosaga toʻatasi, lea e faʻaaogā saʻo ai le `HttpContext.Items`.

---

### 28. Program.cs e Lesitala ai le Singleton ILoggerFactory Avanoa

**Faila:** `Program.cs` (laina 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

E lesitala otometi e le ASP.NET Core se `ILoggerFactory` faʻatulafonoina atoatoa (faʻatasi ma faʻamatalaga uma o le mana faʻamauina mai le faʻatulagaga `builder.Logging`) i le taimi o le `WebApplication.CreateBuilder`. O lenei lesitalaga manino `AddSingleton` e faʻaopoopoina ai le lua, `LoggerFactory` faletioa e le faʻatulafonoina e aunoa ma ni mea fuafuaina. Talu ai o le `GetRequiredService<ILoggerFactory>()` e toe foʻi ai le faatinoina e talu ai lata nei lesitalaina, o auaunaga e mauaina le `ILoggerFactory` e ala i le faʻafitia o fautuaga (e pei o le `NonceRefresherService`) o le a faʻaaogā lenei fale e aunoa ma ni mea e faʻatupuina ai se faʻamaumauga uma e ala i le `_loggerFactory.CreateLogger<T>()`.

**Lamatiaga:** Faʻamauina filemu i le `NonceRefresherService` — o manuia ma faʻalavelave o le faʻatumuina o nonce e le auina atu i luga o se sinki faʻamauina faʻatulafonoina. O lenei e faʻaitiitia ai le mafai ona matamata i le polokalama i le taimi o gaoioiga e nafa ma le saogalemu e aunoa ma le aafia o le galuega.

**Fautuaga:** Aveeseina le lesitalaga manino `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. O le `ILoggerFactory` faʻatulafonoina o le famanatuga (faʻatasi ma le Console ma isi mea faʻatino) o le a mauaina saʻo e auaunaga e fautua i ai.

---

## 🔵 Maualalo / Faailoa

### 25. O le Tulaga Faatonu o le Vaega OcspSettings e Ese mai le Faataʻitaʻiga

**Faila:** `Models/Settings/OcspSettings.cs` (laina 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

O le faataʻitaʻiga (`appsettings.template.json`) e faʻasilasila i le `"ServerUnavailableBehavior": "Fail"`, peitaʻi o le tulaga faatonu o le vaega C# o le `"Warn"`. Afai o le `ServerUnavailableBehavior` e leai i le faila faʻatulagaga faʻagaioioina, o le tulaga faatonu o le vaega e faʻaaogā filemu nai lo le fautuaga o le faataʻitaʻiga. O lenei se totoe mai le mauaina #24.

**Fautuaga:** Suia le tulaga faatonu o le vaega mai `"Warn"` i le `"Fail"` e fetaui ma le faataʻitaʻiga ma le mataupu faavae o le faʻapuʻupuʻu lava le avanoa.

---

### 27. E Mafai ona Totoʻo Fuainumera Faʻamolemole o OptimizedNonceMiddleware

**Faila:** `Services/OptimizedNonceMiddleware.cs` (laina 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

O nei fuainumera sainia 32-bit e faʻaopoopoina atomically e ala i le `Interlocked.Increment`. Ina ua mae'a le sili atu i le 2.1 piliona faʻaopoopo, o le a faʻafo'i i le `int.MinValue` (−2,147,483,648), mafua ai ona faʻatupuina le fuainumera faʻaauau `(total - generated) * 100.0 / total` i fa'ai'uga sese poʻo se uiga leai. I le 1,000 talosaga i le sekone, e tupu le totoʻo pe a mae'a le 24.8 aso o le faʻaogaina faʻaauau.

**Fautuaga:** Suia ituaiga o le fanua faʻafuainumera mai le `int` i le `long` ma faʻaaogā le faʻateleina `long` o le `Interlocked.Increment` e taofia ai le totoʻo.

---

## Iloiloga o Ulutala Saogalemu (Tulaga o Lenei Taimi)

O ulutala nei e faʻaaogā e ala i le `UseStandardSecurityHeaders` — e le suia mai le iloiloga tuai:

| Ulutala | Taua | Iloiloga |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Lelei |
| `X-XSS-Protection` | `0` | ✅ Lelei (e taofia ai le siakiina tuai) |
| `X-Content-Type-Options` | `nosniff` | ✅ Lelei |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Lelei |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Lelei |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Lelei |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Lelei |
| `Permissions-Policy` | geolocation, kamera, microphone, interest-cohort faʻaumia | ✅ Lelei |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Lelei |
| `Content-Security-Policy` | Faʻavae i le nonce, faʻaaogā pe a faʻagaoioia le CSP | ✅ Lelei |
| `Server` | Natia i le `"webserver"` | ✅ Lelei |
| `X-Powered-By` | Aveese | ✅ Lelei |

---

## Iloiloga Aoao

O mauaina uma maualuga-lavelave mai iloiloga muamua ua faaleleia. O mauaina o lenei taimi e faʻatapulaʻaina i faʻafitauli e lua fafati-lavelave (#26 ki nonce tutusa, #28 ILoggerFactory avanoa) ma mea faʻailoa e lua maualalo-lavelave (#25 eseesega o tulaga faatonu vaega, #27 totoʻo numera i fuainumera). E faʻaaloalo le māloloina vave mo le mauaina #28 (singleton ILoggerFactory avanoa) ona e faʻatounōtia filemu ai le faʻamauina o faʻatinoga e fesoʻotaʻi ma le saogalemu i le taimi o gaoioiga nonce. O le mauaina #26 (ki nonce tutusa) e tatau ona faʻatinoina e toe faʻafo'i ai le tautinoga faʻapitoa o nonce mo talosaga taʻitasi e manaʻomia e le faʻatonuga CSP.
