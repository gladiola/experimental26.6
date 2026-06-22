# Nazarin Tsaro — WebAppExperimental266

**Kwanan Wata:** 2026-05-07
**Iyaka:** Cikakken nazarin tsayataccen lambar shirye-shirye (biyo bayan nazarin 2026-05-06)
**Mai Dubawa:** Tsarin Nazarin Tsaro na Atomatik

---

## Taƙaitaccen Bayani

Wannan nazarin biyo bayan yana tabbatar da cewa an gyara abubuwa 3 daga cikin 5 na rauni da aka gano a cikin nazarin tsaro na 2026-05-06 gaba ɗaya, tare da 1 wanda har yanzu ya kasance an gyara shi a wani ɓangare. Nazarin ya kuma gano sabbin bincike 4. Matsayin tsaro na gaba ɗaya na aikace-aikacen yana ci gaba da inganta.

---

## Matsayin Binciken Da Ya Gabata (2026-05-06)

| # | Bincike | Tsanani | Matsayi |
|---|---------|----------|--------|
| 20 | NonceRefresherService yana riƙe da dogaro da Key Vault mai gini maras amfani | 🟠 Babba | ✅ An Gyara |
| 21 | OcspValidationService ma'ajiyar ciki tana amfani da Dictionary maras aminci ga zaren | 🟡 Matsakaici | ✅ An Gyara |
| 22 | Matattarar OCSP tana nan har yanzu — ta kasa a rufe amma ba a aiwatar da ita ba | 🔵 Ƙanƙanta | ⚠️ An Karɓa (ta tsarin) |
| 23 | mTLS tare da AllowedIssuers fanko yana ƙin duk takaddun shaida (fail-closed, ba a rubuta ba) | 🔵 Ƙanƙanta | ✅ An Gyara |
| 24 | OcspSettings.ServerUnavailableBehavior ta taso zuwa "Warn" (tana barin wuce gona da iri a kuskure) | 🔵 Ƙanƙanta | ⚠️ An Gyara A Wani Ɓangare |

---

## Cikakken Matsayin Binciken Da Ya Gabata

### ✅ 20. NonceRefresherService Dogaro Maras Amfani na DI — An Gyara

**Fayil:** `Services/NonceRefresherService.cs`

Mai ginin `NonceRefresherService` yanzu yana bayyana kawai `ILogger<NonceRefresherService>`, `ILoggerFactory` da `INonceCatalogService`. An cire dogaro huɗu da ba a yi amfani da su ba (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`). Wannan yana warware haɗarin ƙin bada aiki wanda ya hana aikace-aikacen farawa lokacin da `EnableKeyVault = false` (tsoho) da `EnableNonceServices = true` (tsoho).

---

### ✅ 21. OcspValidationService Ma'ajiyar Maras Aminci ga Zaren — An Gyara

**Fayil:** `Services/OcspValidationService.cs`

An maye gurbin `Dictionary<string, CachedOcspResponse> _cache` da `ConcurrentDictionary<string, CachedOcspResponse>`. An sabunta kiran `_cache.Remove` zuwa `_cache.TryRemove`. Ma'ajiyar yanzu ta fi aminci ga samun dama ta lokaci guda.

---

### ⚠️ 22. Matattarar OCSP — An Karɓa (Ta Tsarin)

**Fayil:** `Services/OcspValidationService.cs`

Matattarar tana nan har yanzu amma ta kasa a rufe yadda ya kamata. Tunda `EnableOcspValidation` ta taso zuwa `false`, ba ta da tasiri na samarwa. An karɓa ta a matsayin bincike na bayanai yayin jiran cikakken aiwatar da OCSP.

---

### ✅ 23. mTLS AllowedIssuers Fanko — An Gyara

**Fayil:** `Extensions/ServiceCollectionExtensions.cs`

Ana rubuta gargaɗin farawa yanzu lokacin da `ValidateClientCertificateIssuer = true` da `AllowedIssuers` ya zama fanko:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Wannan yana ba da jagora mai bayyanawa ga masu aiki waɗanda suka gamu da halin fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — An Gyara A Wani Ɓangare

**Fayiloli:** `appsettings.template.json` (an gyara), `Models/Settings/OcspSettings.cs` (har yanzu ba a gyara ba)

Samfurin yanzu yana fayyace `"ServerUnavailableBehavior": "Fail"` yadda ya kamata. Duk da haka, tsohon C# a `OcspSettings.cs` (layi 39) ya kasance `"Warn"`. Idan wani mai aiki ya kunna OCSP kuma ya bar `ServerUnavailableBehavior` daga fayilin tsarinsa, ana amfani da tsoho na aji `"Warn"` a shiru, wanda ke ba da damar wuce gona da iri a hutu na uwar garken OCSP. Tsoho na aji dole ne a canza shi don ya daidaita da shawarar samfurin.

---

## Sabbin Bincike

| # | Yanki | Tsanani |
|---|------|----------|
| 25 | Tsoho na aji OcspSettings ("Warn") ya ɓaci daga samfurin ("Fail") | 🔵 Ƙanƙanta |
| 26 | Mabuɗin nonce ɗaya da aka raba na NonceCatalogService yana ba da damar karo nonce tsakanin buƙatun | 🟡 Matsakaici |
| 27 | Kirga na atomatik na OptimizedNonceMiddleware suna amfani da lambobi masu sanya hannu na 32-bit (haɗarin ambaliya) | 🔵 Ƙanƙanta |
| 28 | Program.cs yana rajista ILoggerFactory singleton fanko, wanda ke toshe logger na tsarin | 🟡 Matsakaici |

---

## 🟡 Matsakaici

### 26. Mabuɗin Nonce ɗaya da aka Raba na NonceCatalogService Yana Ba da Damar Karo Nonce Tsakanin Buƙatun

**Fayiloli:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Ma'ajiyar nonce tana adana duk nonces ƙarƙashin mabuɗi ɗaya da aka raba `"CSPNonce"`. A ƙarƙashin nauyi mai lokaci guda, zage-zage mai zuwa yana yiwuwa:

1. Buƙata A tana kiran `RefreshNonceAsync()` — ana adana nonce A1 a matsayin `_nonceCollection["CSPNonce"]`.
2. Buƙata B tana kiran `RefreshNonceAsync()` — nonce B1 ta rubuta `_nonceCollection["CSPNonce"]` a kai.
3. Buƙata A tana kiran `GetANonce("CSPNonce")` — tana karɓar B1, ba A1 ba.
4. Taken CSP na Buƙata A da nonce cikin gida duk sun ƙunshi B1.
5. Buƙata B kuma tana ƙunshe da B1.

Amsa biyu na lokaci guda suna raba nonce ɗaya. Ko da yake duk ƙimomi suna kasancewa masu kusurwa ta hanyar crypto da ba za a iya hasashe ba (babu kirtani mai wuya), ƙimon nonce ɗaya ya bayyana a cikin amsa da yawa na lokaci guda, wanda ke raunana tabbacin keɓancewa na kowane buƙata da ƙayyadaddun ƙayyadaddun CSP suka buƙata. Wani mai kai hari wanda zai iya lura da nonce daga wata amsa yana da ingantaccen nonce don aƙalla wata amsa ɗaya ta lokaci guda.

**Shawarwari:** Samar da nonce kai tsaye a cikin middleware kowane buƙata (misali `Nonce.GenerateSecureNonce()`) kuma adana shi kawai a `HttpContext.Items["Nonce"]`, yin bel ɗin ma'ajiyar da aka raba don nonces na kowane buƙata. Ma'ajiyar da aka raba za ta kasance ana buƙatar ta kawai idan ya zama dole a raba nonce tsakanin yadudduka na middleware a cikin buƙata ɗaya, wanda `HttpContext.Items` ke sarrafa shi a halitta.

---

### 28. Program.cs Yana Rajista ILoggerFactory Singleton Fanko

**Fayil:** `Program.cs` (layi 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core yana rajista ta atomatik `ILoggerFactory` mai cikakken tsari (tare da duk masu ba da rajista daga tsarin `builder.Logging`) a lokacin `WebApplication.CreateBuilder`. Wannan rajista na bayyane `AddSingleton` yana ƙara misalin `LoggerFactory` na biyu, ba tare da masu ba da rajista ba. Tunda `GetRequiredService<ILoggerFactory>()` yana mayar da aiwatarwa da aka rajista na baya-bayan nan, sabis ɗin da ke karɓar `ILoggerFactory` ta hanyar DI (kamar `NonceRefresherService`) zai yi amfani da wannan masana'anta fanko kuma ba za su samar da fitarwar rajista ba ta `_loggerFactory.CreateLogger<T>()`.

**Haɗari:** Rajista a shiru a `NonceRefresherService` — nasarorin samar da nonce da gazawar ba a bayar da su ga wurin ajiyar rajista da aka tsara ba. Wannan yana rage iya ganin aikace-aikacen a lokacin ayyukan masu tsaro na aminci ba tare da shafar aiki ba.

**Shawarwari:** Cire rajista ta bayyane `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. `ILoggerFactory` da aka tsara na tsarin (tare da Console da sauran masu ba da rajista) za ta warware yadda ya kamata ta sabis ɗin da suka dogara a kai.

---

## 🔵 Ƙanƙanta / Bayani

### 25. Tsoho na Aji OcspSettings Ya Ɓaci Daga Samfurin

**Fayil:** `Models/Settings/OcspSettings.cs` (layi 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Samfurin (`appsettings.template.json`) yana fayyace `"ServerUnavailableBehavior": "Fail"`, amma tsoho na aji C# shine `"Warn"`. Idan `ServerUnavailableBehavior` ya ɓace daga fayilin tsari mai aiki, ana amfani da tsoho na aji a shiru maimakon shawarar samfurin. Wannan ya rage daga bincike #24.

**Shawarwari:** Canza tsoho na aji daga `"Warn"` zuwa `"Fail"` don daidaitawa da samfurin da ka'idar ƙarancin hakkoki.

---

### 27. Kirga na Atomatik na OptimizedNonceMiddleware Na Iya Ƙwarara

**Fayil:** `Services/OptimizedNonceMiddleware.cs` (layuka 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

An ƙara waɗannan kirga masu sanya hannu na 32-bit ta hanyar atomatik ta `Interlocked.Increment`. Bayan kimanin ƙididdigar 2.1 biliyan, za su juyawa zuwa `int.MinValue` (−2,147,483,648), wanda ke sa ƙididdiga ta inganci `(total - generated) * 100.0 / total` ta samar da sakamako mara daidai ko mara ma'ana. A buƙatun 1,000 a daƙiƙa, ambaliyar ruwa ta faru bayan kusan kwanaki 24.8 na aiki mai ci gaba.

**Shawarwari:** Canza nau'in filin kirga daga `int` zuwa `long` kuma yi amfani da `long` overload na `Interlocked.Increment` don hana ambaliyar ruwa.

---

## Kimanta Taken Tsaro (Yanayin Yanzu)

Ana amfani da taken masu zuwa ta `UseStandardSecurityHeaders` — ba a canza ba daga nazarin da ya gabata:

| Taken | Ƙima | Kimanta |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Mai Kyau |
| `X-XSS-Protection` | `0` | ✅ Mai Kyau (yana kashe mai duba tsofaffi) |
| `X-Content-Type-Options` | `nosniff` | ✅ Mai Kyau |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Mai Kyau |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Mai Kyau |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Mai Kyau |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Mai Kyau |
| `Permissions-Policy` | geolocation, kyamara, microphone, interest-cohort an kashe | ✅ Mai Kyau |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Mai Kyau |
| `Content-Security-Policy` | Tushen Nonce, ana amfani lokacin da aka kunna CSP | ✅ Mai Kyau |
| `Server` | An lullube zuwa `"webserver"` | ✅ Mai Kyau |
| `X-Powered-By` | An cire | ✅ Mai Kyau |

---

## Kimantawar Gaba ɗaya

An gyara duk binciken masu tsanani na baya daga nazarin da ya gabata. Binciken yanzu an iyakance su zuwa batutuwa biyu na matsakaicin tsanani (#26 mabuɗin nonce da aka raba, #28 ILoggerFactory fanko) da abubuwa biyu na ƙarancin tsanani (#25 tsoho na aji ba daidai ba, #27 ambaliyar lamba mai sanya hannu a kirga). Ana ba da shawarar kulawa nan da nan ga bincike #28 (ILoggerFactory singleton fanko) tunda yana toshe rajista na bincike masu tsaro da alaƙa a lokacin ayyukan nonce a shiru. Ya kamata a magance bincike #26 (mabuɗin nonce da aka raba) don maido da tabbacin keɓancewa na nonce na kowane buƙata da ƙayyadaddun ƙayyadaddun CSP suka buƙata.
