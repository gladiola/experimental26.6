# Athbhreithniú Slándála — WebAppExperimental266

**Dáta:** 2026-05-07
**Raon:** Anailís stataiceach iomlán ar an gcódbhunachar (leantacht ar athbhreithniú 2026-05-06)
**Athbhreithneoirí:** Athbhreithniú Slándála Uathoibrithe

---

## Achoimre Feidhmiúcháin

Deimhníonn an t-athbhreithniú leantachta seo go bhfuil 3 de na 5 leochaileacht a sainaithníodh san athbhreithniú slándála 2026-05-06 leigheasta go hiomlán, agus go bhfuil 1 fós leigheasta go páirteach. Sainaithnítear 4 tátail nua san athbhreithniú freisin. Tá dearcadh slándála foriomlán an fheidhmchláir ag feabhsú go seasta.

---

## Stádas Tátail Roimhe (2026-05-06)

| # | Tátal | Déine | Stádas |
|---|---------|----------|--------|
| 20 | Coimeádann NonceRefresherService spleáchais thógálaithe Key Vault neamhúsáidte | 🟠 Ard | ✅ Leigheasta |
| 21 | Úsáideann taisce inmheánach OcspValidationService Dictionary nach bhfuil slán do shnáitheanna | 🟡 Meánach | ✅ Leigheasta |
| 22 | Tá stub fíorúcháin OCSP fós i láthair — fail-closed ach gan cur i bhfeidhm | 🔵 Íseal | ⚠️ Glactha (de réir dearaidh) |
| 23 | Diúltaíonn mTLS le AllowedIssuers folamh gach deimhniú (fail-closed, gan doiciméadú) | 🔵 Íseal | ✅ Leigheasta |
| 24 | Réamhshocrú OcspSettings.ServerUnavailableBehavior go "Warn" (ceadaíonn dul tríd ar earráid) | 🔵 Íseal | ⚠️ Leigheasta Go Páirteach |

---

## Stádas Mionsonraithe Tátail Roimhe

### ✅ 20. Spleáchais DI Neamhúsáidte NonceRefresherService — Leigheasta

**Comhad:** `Services/NonceRefresherService.cs`

Dearbhaíonn tógálaí `NonceRefresherService` anois `ILogger<NonceRefresherService>`, `ILoggerFactory` agus `INonceCatalogService` amháin. Baineadh na ceithre spleáchas neamhúsáidte roimhe (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`). Réitíonn sé seo an baol diúltú seirbhíse a chuir cosc ar an bhfeidhmchlár tosú nuair a bhí `EnableKeyVault = false` (réamhshocrú) agus `EnableNonceServices = true` (réamhshocrú).

---

### ✅ 21. Taisce Neamhshlán do Shnáitheanna OcspValidationService — Leigheasta

**Comhad:** `Services/OcspValidationService.cs`

Cuireadh `ConcurrentDictionary<string, CachedOcspResponse>` in ionad `Dictionary<string, CachedOcspResponse> _cache`. Nuashonraíodh an glao `_cache.Remove` go `_cache.TryRemove`. Tá an taisce slán anois do rochtain chomhuaineach.

---

### ⚠️ 22. Stub Fíorúcháin OCSP — Glactha (De Réir Dearaidh)

**Comhad:** `Services/OcspValidationService.cs`

Tá an stub fós i láthair ach tá sé fail-closed i gceart. Ós rud é go bhfuil réamhshocrú `EnableOcspValidation` go `false`, níl aon tionchar táirgthe aige. Glactar leis mar thátal faisnéise ar feadh cur i bhfeidhm iomlán OCSP.

---

### ✅ 23. AllowedIssuers Folamh mTLS — Leigheasta

**Comhad:** `Extensions/ServiceCollectionExtensions.cs`

Logáiltear rabhadh tosaithe anois nuair a bhíonn `ValidateClientCertificateIssuer = true` agus `AllowedIssuers` folamh:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Soláthraíonn sé seo treoir shoiléir d'oibreoirí a thagann ar an iompar fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Leigheasta Go Páirteach

**Comhaid:** `appsettings.template.json` (leigheasta), `Models/Settings/OcspSettings.cs` (gan leigheas fós)

Sonraíonn an teimpléad `"ServerUnavailableBehavior": "Fail"` i gceart anois. Mar sin féin, fanann réamhshocrú ranga C# in `OcspSettings.cs` (líne 39) mar `"Warn"`. Má chuireann oibreoir OCSP ar siúl agus má fhágann sé `ServerUnavailableBehavior` ar lár ó chomhad cumraíochta, cuirtear an réamhshocrú ranga `"Warn"` i bhfeidhm go ciúin, rud a cheadaíonn dul tríd i rith briseadh freastalaí OCSP. Ní mór an réamhshocrú ranga a athrú chun teacht le moladh an teimpléid.

---

## Tátail Nua

| # | Réimse | Déine |
|---|------|----------|
| 25 | Réamhshocrú ranga OcspSettings ("Warn") tagann ó theimpléad ("Fail") | 🔵 Íseal |
| 26 | Eochair nonce roinnte aonair NonceCatalogService ceadaíonn imbhualadh nonce tras-iarratais | 🟡 Meánach |
| 27 | Cuntóirí stataiceacha OptimizedNonceMiddleware úsáideann slánuimhreacha comharthacha 32-ghiotán (baol ró-shreafa) | 🔵 Íseal |
| 28 | Cláraíonn Program.cs ILoggerFactory singleton folamh, ag clúdach an logálaí creatlaí | 🟡 Meánach |

---

## 🟡 Meánach

### 26. Eochair Nonce Roinnte Aonair NonceCatalogService Ceadaíonn Imbhualadh Nonce Tras-Iarratais

**Comhaid:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Stórálann an catalóg nonce gach nonce faoi eochair roinnte aonair `"CSPNonce"`. Faoi ualach comhuaineach, tá an coinníoll iomaíochta seo a leanas féideartha:

1. Glaonn Iarratas A ar `RefreshNonceAsync()` — stóráiltear nonce A1 mar `_nonceCollection["CSPNonce"]`.
2. Glaonn Iarratas B ar `RefreshNonceAsync()` — scríobhann nonce B1 thar `_nonceCollection["CSPNonce"]`.
3. Glaonn Iarratas A ar `GetANonce("CSPNonce")` — faigheann sé B1, ní A1.
4. Tá B1 i gceann CSP agus nonce inlíne Iarratais A araon.
5. Tá B1 in Iarratas B freisin.

Roinneann dhá fhreagra comhuaineacha an nonce céanna. Cé go bhfuil luachanna araon fós randamach go criptaigrafach agus dothaispeánta (gan teaghrán hardcoded), feictear an luach nonce céanna i bhfreagraí comhuaineacha iomadúla, rud a lagaíonn an ráthaíocht aonúlachta in aghaidh iarratais a éilíonn sonraíocht CSP. Tá nonce bailí ag ionsaitheoirí a bhreathnaíonn ar nonce freagra amháin do freagra comhuaineach eile ar a laghad.

**Moladh:** Gin an nonce go díreach laistigh den middleware do gach iarratas (m.sh. `Nonce.GenerateSecureNonce()`) agus stóráil in `HttpContext.Items["Nonce"]` amháin, ag dul timpeall ar an gcatalóg roinnte do nonces in aghaidh iarratais. Bheadh gá leis an gcatalóg roinnte ach amháin má theastaíonn nonce a roinnt trasna sraitheanna middleware laistigh d'iarratas amháin, rud a láimhseálann `HttpContext.Items` go dúchasach cheana féin.

---

### 28. Cláraíonn Program.cs ILoggerFactory Singleton Folamh

**Comhad:** `Program.cs` (líne 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

Cláraíonn ASP.NET Core `ILoggerFactory` iomlán cumraithe go huathoibríoch (le gach soláthraí logála ó chumraíocht `builder.Logging`) le linn `WebApplication.CreateBuilder`. Cuireann an clárú `AddSingleton` sainráite seo cás `LoggerFactory` dara, neamhchumraithe gan soláthróirí leis. Ós rud é go dtugann `GetRequiredService<ILoggerFactory>()` an cur i bhfeidhm is déanaí cláraithe ar ais, bainfidh seirbhísí a fhaigheann `ILoggerFactory` trí DI (cosúil le `NonceRefresherService`) úsáid as an monarcha folamh seo agus ní tháirgfidh siad aon aschur logála tríd an `_loggerFactory.CreateLogger<T>()`.

**Baol:** Logáil chiúin in `NonceRefresherService` — ní eisítear rathúlachtaí agus teipeanna giniúna nonce chuig doirtil logála cumraithe. Laghdaíonn sé seo infheictheacht an fheidhmchláir le linn oibríochtaí íogaire slándála gan tionchar ar fheidhmiúlacht.

**Moladh:** Bain an clárú sainráite `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. Réiteofar `ILoggerFactory` cumraithe na creatlaí (le Console agus aon soláthróirí eile) i gceart ansin ag seirbhísí a bhíonn ag brath air.

---

## 🔵 Íseal / Faisnéiseach

### 25. Tagann Réamhshocrú Ranga OcspSettings ó Theimpléad

**Comhad:** `Models/Settings/OcspSettings.cs` (líne 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Sonraíonn an teimpléad (`appsettings.template.json`) `"ServerUnavailableBehavior": "Fail"`, ach is é `"Warn"` an réamhshocrú ranga C#. Má tá `ServerUnavailableBehavior` as láthair ón gcomhad cumraíochta gníomhach, cuirtear réamhshocrú an ranga i bhfeidhm go ciúin in ionad mholadh an teimpléid. Is iarsma é seo ó thátal #24.

**Moladh:** Athraigh réamhshocrú an ranga ó `"Warn"` go `"Fail"` chun teacht le teimpléad agus prionsabal na pribhléide is lú.

---

### 27. Is Féidir le Cuntóirí Stataiceacha OptimizedNonceMiddleware Ró-shreabhadh

**Comhad:** `Services/OptimizedNonceMiddleware.cs` (línte 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Méadaítear na cuntóirí comharthacha 32-ghiotán seo go adamhach ag `Interlocked.Increment`. Tar éis thart ar 2.1 billiún méadú, rollálfaidh siad ar ais go `int.MinValue` (−2,147,483,648), rud a fhágfaidh go dtáirgeann an ríomh éifeachtúlachta `(total - generated) * 100.0 / total` torthaí mícheart nó gan brí. Ag 1,000 iarratas in aghaidh an tsoicind, tarlaíonn ró-shreabhadh tar éis thart ar 24.8 lá d'oibríocht leanúnach.

**Moladh:** Athraigh cineálacha réimse cuntóra ó `int` go `long` agus úsáid ró-luchtú `long` de `Interlocked.Increment` chun ró-shreabhadh a chosc.

---

## Measúnú Ceanntásca Slándála (Staid Reatha)

Cuirtear na ceanntásca seo a leanas i bhfeidhm trí `UseStandardSecurityHeaders` — gan athrú ón athbhreithniú roimhe:

| Ceanntásc | Luach | Measúnú |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Maith |
| `X-XSS-Protection` | `0` | ✅ Maith (díchumasaíonn iniúchóir as dáta) |
| `X-Content-Type-Options` | `nosniff` | ✅ Maith |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Maith |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Maith |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Maith |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Maith |
| `Permissions-Policy` | geolocation, camera, microphone, interest-cohort díchumasaithe | ✅ Maith |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Maith |
| `Content-Security-Policy` | Bunaithe ar Nonce, i bhfeidhm nuair atá CSP cumasaithe | ✅ Maith |
| `Server` | Mascaithe mar `"webserver"` | ✅ Maith |
| `X-Powered-By` | Bainte | ✅ Maith |

---

## Measúnú Foriomlán

Leigheasta gach tátal déine ard ón athbhreithniú roimhe. Tá na tátail reatha teoranta do dhá cheist déine meánaigh (#26 eochair nonce roinnte, #28 ILoggerFactory folamh) agus dhá mhír faisnéiseach déine íseal (#25 mí-mheaitseáil réamhshocrú ranga, #27 slánuimhir ró-shreafa i gcuntóirí). Moltar aird láithreach ar thátal #28 (ILoggerFactory singleton folamh) ós rud é go gcuireann sé logáil dhiagnóiseach a bhaineann le slándáil i rith oibríochtaí nonce i dtost go ciúin. Ba cheart tátal #26 (eochair nonce roinnte) a réiteach chun an ráthaíocht aonúlachta nonce in aghaidh iarratais a éilíonn sonraíocht CSP a athbhunú.
