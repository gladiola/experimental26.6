# Ukaguzi wa Usalama — WebAppExperimental266

**Tarehe:** 2026-05-07
**Upeo:** Uchambuzi kamili wa msimbo wa chanzo (ufuatiliaji wa ukaguzi wa 2026-05-06)
**Mkaguzi:** Ukaguzi wa Usalama wa Kiotomatiki

---

## Muhtasari wa Utendaji

Ukaguzi huu wa ufuatiliaji unathibitisha kwamba udhaifu 3 kati ya 5 uliotambuliwa katika ukaguzi wa usalama wa 2026-05-06 umeshughulikiwa kikamilifu, huku 1 ikibaki kushughulikiwa kwa sehemu. Ukaguzi pia unabainisha matokeo 4 mapya. Hali ya jumla ya usalama ya programu inaendelea kuimarika.

---

## Hali ya Matokeo ya Awali (2026-05-06)

| # | Matokeo | Ukali | Hali |
|---|---------|----------|--------|
| 20 | NonceRefresherService inashikilia utegemezi wa mjenzi wa Key Vault usiotumika | 🟠 Juu | ✅ Imerekebisha |
| 21 | Akiba ya ndani ya OcspValidationService inatumia Dictionary ambayo si salama kwa nyuzi | 🟡 Wastani | ✅ Imerekebisha |
| 22 | Kiolezo cha uthibitishaji cha OCSP bado kipo — inashindwa kwa hali iliyofungwa lakini haijatekelezwa | 🔵 Chini | ⚠️ Imekubaliwa (kwa muundo) |
| 23 | mTLS yenye AllowedIssuers tupu inakataa vyeti vyote (fail-closed, haikuandikwa) | 🔵 Chini | ✅ Imerekebisha |
| 24 | OcspSettings.ServerUnavailableBehavior ina chaguo-msingi cha "Warn" (inaruhusu kupita wakati wa hitilafu) | 🔵 Chini | ⚠️ Imerekebisha kwa Sehemu |

---

## Hali ya Kina ya Matokeo ya Awali

### ✅ 20. Utegemezi wa DI Usiotumika wa NonceRefresherService — Imerekebisha

**Faili:** `Services/NonceRefresherService.cs`

Mjenzi wa `NonceRefresherService` sasa anatangaza tu `ILogger<NonceRefresherService>`, `ILoggerFactory`, na `INonceCatalogService`. Utegemezi manne uliotumika awali bila kufaa (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) umeondolewa. Hii inasuluhisha hatari ya kukataa huduma iliyozuia programu kuanza wakati `EnableKeyVault = false` (chaguo-msingi) na `EnableNonceServices = true` (chaguo-msingi).

---

### ✅ 21. Akiba Isiyo Salama kwa Nyuzi ya OcspValidationService — Imerekebisha

**Faili:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` imebadilishwa na `ConcurrentDictionary<string, CachedOcspResponse>`. Simu ya `_cache.Remove` imesasishwa kuwa `_cache.TryRemove`. Akiba sasa ni salama kwa ufikiaji wa wakati mmoja.

---

### ⚠️ 22. Kiolezo cha Uthibitishaji cha OCSP — Imekubaliwa (Kwa Muundo)

**Faili:** `Services/OcspValidationService.cs`

Kiolezo bado kipo lakini kinashindwa kwa usahihi kwa hali iliyofungwa. Kwa sababu `EnableOcspValidation` ina chaguo-msingi la `false`, hii haina athari katika uzalishaji. Hii inakubaliwa kama matokeo ya taarifa yanayosubiri utekelezaji kamili wa OCSP.

---

### ✅ 23. AllowedIssuers Tupu ya mTLS — Imerekebisha

**Faili:** `Extensions/ServiceCollectionExtensions.cs`

Onyo la kuanza sasa linarekodiwa wakati `ValidateClientCertificateIssuer = true` na `AllowedIssuers` ni tupu:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Hii inatoa mwongozo wazi kwa waendeshaji wanaokutana na tabia ya fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Imerekebisha kwa Sehemu

**Faili:** `appsettings.template.json` (imerekebisha), `Models/Settings/OcspSettings.cs` (bado haijarekebisha)

Kiolezo sasa kinabainisha kwa usahihi `"ServerUnavailableBehavior": "Fail"`. Hata hivyo, chaguo-msingi cha darasa la C# katika `OcspSettings.cs` (mstari 39) kinabaki `"Warn"`. Ikiwa mwendeshaji atawasha OCSP na kuacha `ServerUnavailableBehavior` kutoka kwa faili yake ya usanidi, chaguo-msingi la darasa `"Warn"` kinatumika kimya kimya, kikiruhusu kupita wakati wa kukatika kwa seva ya OCSP. Chaguo-msingi la darasa linapaswa kubadilishwa kulingana na mapendekezo ya kiolezo.

---

## Matokeo Mapya

| # | Eneo | Ukali |
|---|------|----------|
| 25 | Chaguo-msingi la darasa la OcspSettings ("Warn") linatofautiana na kiolezo ("Fail") | 🔵 Chini |
| 26 | Ufunguo mmoja wa nonce unaoshirikiwa katika NonceCatalogService unaruhusu mgongano wa nonce kati ya maombi | 🟡 Wastani |
| 27 | Vihesabu vya tuli vya OptimizedNonceMiddleware vinatumia nambari kamili za 32-bit zenye ishara (hatari ya kufurika) | 🔵 Chini |
| 28 | Program.cs inasajili singleton tupu ya ILoggerFactory, inayofunika kirekodiwa cha mfumo | 🟡 Wastani |

---

## 🟡 Wastani

### 26. Ufunguo wa Nonce Unaoshirikiwa wa NonceCatalogService Unaruhusu Mgongano wa Nonce kati ya Maombi

**Faili:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Katalogi ya nonce inahifadhi nonces zote chini ya ufunguo mmoja unaoshirikiwa `"CSPNonce"`. Chini ya mzigo wa wakati mmoja, hali ifuatayo ya mbio inawezekana:

1. Ombi A linaitia `RefreshNonceAsync()` — nonce A1 inahifadhiwa kama `_nonceCollection["CSPNonce"]`.
2. Ombi B linaitia `RefreshNonceAsync()` — nonce B1 inabatilisha `_nonceCollection["CSPNonce"]`.
3. Ombi A linaitia `GetANonce("CSPNonce")` — linapokea B1, si A1.
4. Kichwa cha CSP na nonce ya mpangilio wa Ombi A vyote viwili vina B1.
5. Ombi B pia lina B1.

Majibu mawili yanayofanyika wakati mmoja yanashiriki nonce sawa. Ingawa thamani zote mbili bado ni za nasibu kwa njia ya kriptografia na haziwezi kutabiriwa (hakuna mfuatano uliopigwa msimbo), thamani ile ile ya nonce inaonekana katika majibu mengi ya wakati mmoja, ikidhoofisha dhamana ya kipekee kwa kila ombi inayohitajika na mfumo wa CSP. Mshambuliaji anayeweza kuangalia nonce ya jibu moja ana nonce halali kwa jibu lingine la wakati mmoja angalau.

**Mapendekezo:** Tengeneza nonce moja kwa moja ndani ya middleware kwa kila ombi (mfano, `Nonce.GenerateSecureNonce()`) na ihifadhi tu katika `HttpContext.Items["Nonce"]`, ukipita katalogi inayoshirikiwa kwa nonces za kila ombi. Katalogi inayoshirikiwa itahitajika tu ikiwa nonce lazima ishirikiwe kati ya safu za middleware ndani ya ombi moja, ambayo `HttpContext.Items` inashughulikia tayari kwa asili.

---

### 28. Program.cs Inasajili Singleton Tupu ya ILoggerFactory

**Faili:** `Program.cs` (mstari 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core inasajili kiotomatiki `ILoggerFactory` iliyosanidiwa kikamilifu (pamoja na watoa wote wa uandishi wa kumbukumbu kutoka kwa usanidi wa `builder.Logging`) wakati wa `WebApplication.CreateBuilder`. Usajili huu wa `AddSingleton` wa wazi huongeza mfano wa pili, usiosanikika wa `LoggerFactory` bila watoa wowote. Kwa sababu `GetRequiredService<ILoggerFactory>()` inarudisha utekelezaji uliosajiliwwa hivi karibuni zaidi, huduma zinazopokea `ILoggerFactory` kupitia sindano ya utegemezi (kama `NonceRefresherService`) zitatumia kiwanda hiki tupu na hazitazalisha matokeo ya kumbukumbu yoyote kupitia `_loggerFactory.CreateLogger<T>()`.

**Hatari:** Uandishi wa kumbukumbu kimya ndani ya `NonceRefresherService` — mafanikio na kushindwa kwa uzalishaji wa nonce havipelekwi kwa sinki yoyote ya uandishi wa kumbukumbu iliyosanidiwa. Hii inapunguza uwezo wa kuchunguza programu wakati wa operesheni nyeti za usalama bila kuathiri utendaji.

**Mapendekezo:** Ondoa usajili wa wazi wa `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. `ILoggerFactory` iliyosanidiwa ya mfumo (pamoja na Console na watoa wengine wowote) itapatikana kwa usahihi na huduma zinazotegemea.

---

## 🔵 Chini / Taarifa

### 25. Chaguo-msingi la Darasa la OcspSettings Linatofautiana na Kiolezo

**Faili:** `Models/Settings/OcspSettings.cs` (mstari 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Kiolezo (`appsettings.template.json`) kinabainisha `"ServerUnavailableBehavior": "Fail"`, lakini chaguo-msingi cha darasa cha C# ni `"Warn"`. Ikiwa `ServerUnavailableBehavior` haipo katika faili ya usanidi inayotumika, chaguo-msingi la darasa kinatumika kimya kimya badala ya mapendekezo ya kiolezo. Hii ni mabaki kutoka kwa matokeo #24.

**Mapendekezo:** Badilisha chaguo-msingi la darasa kutoka `"Warn"` hadi `"Fail"` kulingana na kiolezo na kanuni ya upendeleo mdogo.

---

### 27. Vihesabu vya Tuli vya OptimizedNonceMiddleware Vinaweza Kufurika

**Faili:** `Services/OptimizedNonceMiddleware.cs` (mistari 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Vihesabu hivi vya 32-bit vyenye ishara huongezwa kwa njia ya atomiki kupitia `Interlocked.Increment`. Baada ya ongezeko karibu bilioni 2.1, vitarudi kwa `int.MinValue` (−2,147,483,648), na kusababisha hesabu ya ufanisi `(total - generated) * 100.0 / total` kuzalisha matokeo yasiyo sahihi au yasiyo na maana. Kwa maombi 1,000 kwa sekunde, kufurika kunatokea baada ya takriban siku 24.8 za uendeshaji unaoendelea.

**Mapendekezo:** Badilisha aina za uga wa kihesabu kutoka `int` hadi `long` na utumie upakiaji mkubwa `long` wa `Interlocked.Increment` kuzuia kufurika.

---

## Tathmini ya Vichwa vya Usalama (Hali ya Sasa)

Vichwa vifuatavyo vinatumika kupitia `UseStandardSecurityHeaders` — havijabadilika kutoka ukaguzi uliopita:

| Kichwa | Thamani | Tathmini |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Nzuri |
| `X-XSS-Protection` | `0` | ✅ Nzuri (inazima mkaguzi wa zamani) |
| `X-Content-Type-Options` | `nosniff` | ✅ Nzuri |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Nzuri |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Nzuri |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Nzuri |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Nzuri |
| `Permissions-Policy` | eneo la kijiografia, kamera, kipaza sauti, interest-cohort zimezimwa | ✅ Nzuri |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Nzuri |
| `Content-Security-Policy` | Inategemea nonce, inatumika wakati CSP imewashwa | ✅ Nzuri |
| `Server` | Imefichwa hadi `"webserver"` | ✅ Nzuri |
| `X-Powered-By` | Imeondolewa | ✅ Nzuri |

---

## Tathmini ya Jumla

Matokeo yote ya ukali wa juu kutoka ukaguzi uliopita yameshughulikiwa. Matokeo ya sasa yanazuiwa kwa matatizo mawili ya ukali wa wastani (#26 ufunguo wa nonce unaoshirikiwa, #28 ILoggerFactory tupu) na vipengele viwili vya taarifa vya ukali wa chini (#25 kutofautiana kwa chaguo-msingi la darasa, #27 kufurika kwa nambari kamili katika vihesabu). Umakini wa haraka unapendekezwa kwa matokeo #28 (singleton tupu ya ILoggerFactory) kwani inazima kimya kimya uandishi wa kumbukumbu wa uchunguzi unaohusiana na usalama wakati wa operesheni za nonce. Matokeo #26 (ufunguo wa nonce unaoshirikiwa) yanapaswa kushughulikiwa ili kurejesha dhamana ya kipekee cha nonce kwa kila ombi inayohitajika na mfumo wa CSP.
