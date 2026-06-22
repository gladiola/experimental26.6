# Revizyon Sekirite — WebAppExperimental266

**Dat:** 2026-05-07
**Portèy:** Analiz estatik konplè nan tout kòd baz la (swivi revizyon 2026-05-06)
**Revizè:** Revizyon Sekirite Otomatize

---

## Rezime Egzekitif

Revizyon swivi sa a konfime ke 3 nan 5 vilnerabilite yo te idantifye nan revizyon sekirite 2026-05-06 yo te korijé nèt, avèk 1 ki rete korijenan pasyèlman. Revizyon an idantifye tou 4 nouvo konklizyon. Estati sekirite jeneral aplikasyon an ap kontinye amelyore.

---

## Estati Konklizyon Anvan yo (2026-05-06)

| # | Konklizyon | Grav | Estati |
|---|---------|----------|--------|
| 20 | NonceRefresherService kenbe depandans konstriktè Key Vault ki pa itilize | 🟠 Wo | ✅ Korije |
| 21 | Kache entèn OcspValidationService itilize Dictionary ki pa an sekirite pou fil | 🟡 Mwayen | ✅ Korije |
| 22 | Stub validasyon OCSP toujou prezan — echwe fèmen men pa aplike | 🔵 Ba | ⚠️ Aksepte (pa konsepsyon) |
| 23 | mTLS avèk AllowedIssuers vid rejte tout sètifika (fail-closed, pa dokimante) | 🔵 Ba | ✅ Korije |
| 24 | OcspSettings.ServerUnavailableBehavior default a "Warn" (pèmèt pase-a-travè lè gen erè) | 🔵 Ba | ⚠️ Korije Pasyèlman |

---

## Estati Detaye Konklizyon Anvan yo

### ✅ 20. Depandans DI ki Pa Itilize nan NonceRefresherService — Korije

**Fichye:** `Services/NonceRefresherService.cs`

Konstriktè `NonceRefresherService` kounye a deklare sèlman `ILogger<NonceRefresherService>`, `ILoggerFactory`, ak `INonceCatalogService`. Kat depandans ki pa t itilize anvan yo (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) yo te retire. Sa rezoud risk refize sèvis ki te anpeche aplikasyon an kòmanse lè `EnableKeyVault = false` (default la) ak `EnableNonceServices = true` (default la).

---

### ✅ 21. Kache ki Pa An Sekirite pou Fil nan OcspValidationService — Korije

**Fichye:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` te ranplase ak `ConcurrentDictionary<string, CachedOcspResponse>`. Apèl `_cache.Remove` te mete ajou a `_cache.TryRemove`. Kache a kounye a an sekirite pou aksè an paralèl.

---

### ⚠️ 22. Stub Validasyon OCSP — Aksepte (Pa Konsepsyon)

**Fichye:** `Services/OcspValidationService.cs`

Stub la toujou prezan men li echwe fèmen kòrèkteman. Paske `EnableOcspValidation` defaulte a `false`, sa pa gen okenn enpak nan pwodiksyon. Sa aksepte kòm yon konklizyon enfòmasyon an atandan yon apliasyon OCSP konplè.

---

### ✅ 23. AllowedIssuers Vid nan mTLS — Korije

**Fichye:** `Extensions/ServiceCollectionExtensions.cs`

Yon avètisman demaraj kounye a anrejistre lè `ValidateClientCertificateIssuer = true` ak `AllowedIssuers` vid:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Sa bay operatè ki rankontre konpòtman fail-closed la konsèy klè.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Korije Pasyèlman

**Fichye yo:** `appsettings.template.json` (korije), `Models/Settings/OcspSettings.cs` (pa korije ankò)

Modèl la kounye a espesifye kòrèkteman `"ServerUnavailableBehavior": "Fail"`. Sepandan, default klas C# nan `OcspSettings.cs` (liy 39) rete `"Warn"`. Si yon operatè aktive OCSP epi omet `ServerUnavailableBehavior` nan fichye konfigirasyon li, default klas `"Warn"` aplike an silans, pèmèt pase-a-travè pandan pàn sèvè OCSP. Default klas la dwe chanje pou koresponn ak rekòmandasyon modèl la.

---

## Nouvo Konklizyon

| # | Zòn | Grav |
|---|------|----------|
| 25 | Default klas OcspSettings ("Warn") divèje de modèl ("Fail") | 🔵 Ba |
| 26 | Sèl kle nonce pataje nan NonceCatalogService pèmèt kolizyon nonce ant demann yo | 🟡 Mwayen |
| 27 | Kontè estatik OptimizedNonceMiddleware itilize antye 32-bit siyen (risk dèbòdman) | 🔵 Ba |
| 28 | Program.cs anrejistre singleton ILoggerFactory vid, ki kouvri loggè framework la | 🟡 Mwayen |

---

## 🟡 Mwayen

### 26. Kle Nonce Pataje nan NonceCatalogService Pèmèt Kolizyon Nonce ant Demann yo

**Fichye yo:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Katalòg nonce a estoke tout nonces anba yon sèl kle pataje `"CSPNonce"`. Anba chaj paralèl, kondisyon kous sa a posib:

1. Demann A rele `RefreshNonceAsync()` — nonce A1 estoke kòm `_nonceCollection["CSPNonce"]`.
2. Demann B rele `RefreshNonceAsync()` — nonce B1 ekrase `_nonceCollection["CSPNonce"]`.
3. Demann A rele `GetANonce("CSPNonce")` — resevwa B1, pa A1.
4. Antèt CSP ak nonce layout Demann A yo tou de gen B1.
5. Demann B genyen B1 tou.

De repons paralèl pataje menm nonce a. Malgre ke tou de valè yo toujou kriptografikman aleatwa ak enprediksib (pa gen chaîne codée an di), menm valè nonce a parèt nan plizyè repons sinikè, ki febli garanti iniksite pa-demann ke espesifikasyon CSP mande. Yon atak ki ka obsève nonce yon repons gen yon nonce valid pou omwen yon lòt repons paralèl.

**Rekòmandasyon:** Jenere nonce la dirèkteman anndan middleware la pa demann (pa egzanp, `Nonce.GenerateSecureNonce()`) epi estoke l sèlman nan `HttpContext.Items["Nonce"]`, kontourne katalòg pataje pou nonces pa-demann. Katalòg pataje a ta sèlman nesesè si yon nonce dwe pataje atravè kouch middleware anndan yon sèl demann, ke `HttpContext.Items` deja jere nativman.

---

### 28. Program.cs Anrejistre Singleton Vide ILoggerFactory

**Fichye:** `Program.cs` (liy 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core otomatikman anrejistre yon `ILoggerFactory` konplètman konfigire (ak tout founisè logging yo soti nan konfigirasyon `builder.Logging`) pandan `WebApplication.CreateBuilder`. Anrejistreman `AddSingleton` eksplis sa a ajoute yon dezyèm, `LoggerFactory` enstan ki pa konfigire san founisè. Paske `GetRequiredService<ILoggerFactory>()` retounen apliasyon ki pi resamman anrejistre a, sèvis ki resevwa `ILoggerFactory` via injeksyon depandans (tankou `NonceRefresherService`) pral itilize faktori vid sa a epi pa pwoduire okenn rezilta log via `_loggerFactory.CreateLogger<T>()`.

**Risk:** Logging silansye nan `NonceRefresherService` — siksè ak echèk jenerasyon nonce pa voye nan okenn pyas logging konfigire. Sa redui kapasite obsèvasyon aplikasyon an pandan operasyon sensib an sekirite san afekte fonksyonalite.

**Rekòmandasyon:** Retire anrejistreman eksplis `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. `ILoggerFactory` konfigire framework la (avèk Console ak nenpòt lòt founisè) pral rezoud kòrèkteman pa sèvis ki depann sou li.

---

## 🔵 Ba / Enfòmasyon

### 25. Default Klas OcspSettings Divèje de Modèl la

**Fichye:** `Models/Settings/OcspSettings.cs` (liy 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Modèl la (`appsettings.template.json`) espesifye `"ServerUnavailableBehavior": "Fail"`, men default klas C# la se `"Warn"`. Si `ServerUnavailableBehavior` absan nan fichye konfigirasyon aktif la, default klas la aplike an silans olye rekòmandasyon modèl la. Sa se yon rès de konklizyon #24.

**Rekòmandasyon:** Chanje default klas la de `"Warn"` a `"Fail"` pou aliyen ak modèl la ak prensip piti privilèj la.

---

### 27. Kontè Estatik OptimizedNonceMiddleware Ka Dèbòde

**Fichye:** `Services/OptimizedNonceMiddleware.cs` (liy 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Kontè 32-bit siyen sa yo ogmante atomikman via `Interlocked.Increment`. Apre anviwon 2.1 milyad ogmantasyon, yo pral anvlope a `int.MinValue` (−2,147,483,648), ki fè kalkil efikasite `(total - generated) * 100.0 / total` pwodui rezilta enkòrèk oswa san siyifikasyon. Nan 1,000 demann pa segonn, dèbòdman rive apre anviwon 24.8 jou operasyon kontinyèl.

**Rekòmandasyon:** Chanje tip chan kontè yo soti nan `int` a `long` epi itilize chaj `long` nan `Interlocked.Increment` pou anpeche dèbòdman.

---

## Evalyasyon Antèt Sekirite (Eta Aktyèl)

Antèt sa yo aplike via `UseStandardSecurityHeaders` — san chanjman depi revizyon anvan an:

| Antèt | Valè | Evalyasyon |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bon |
| `X-XSS-Protection` | `0` | ✅ Bon (dezaktive oditè depase a) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bon |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bon |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bon |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bon |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bon |
| `Permissions-Policy` | jewolokasyon, kamera, mikwofòn, interest-cohort dezaktive | ✅ Bon |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bon |
| `Content-Security-Policy` | Baze sou nonce, aplike lè CSP aktive | ✅ Bon |
| `Server` | Kache a `"webserver"` | ✅ Bon |
| `X-Powered-By` | Retire | ✅ Bon |

---

## Evalyasyon Jeneral

Tout konklizyon grav segondè soti nan revizyon anvan yo korije. Konklizyon aktyèl yo limite a de pwoblèm grav mwayen (#26 kle nonce pataje, #28 ILoggerFactory vid) ak de atik enfòmasyon grav ba (#25 dezakò default klas, #27 dèbòdman antye nan kontè yo). Atansyon imedya rekòmande pou konklizyon #28 (singleton ILoggerFactory vid) paske li siprime an silans logging dyagnostik ki gen rapò ak sekirite pandan operasyon nonce. Konklizyon #26 (kle nonce pataje) dwe trete pou retabli garanti iniksite nonce pa-demann ke espesifikasyon CSP mande.
