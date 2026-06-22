# Ukaguzi wa Usalama — WebAppExperimental266

**Tarehe:** 2026-05-06
**Upeo:** Uchambuzi wa kistatic wa msimbo wote (ufuatiliaji wa ukaguzi wa 2026-05-05)
**Mkaguzi:** Ukaguzi wa Usalama Otometi

---

## Muhtasari wa Kiutendaji

Ukaguzi huu wa ufuatiliaji unathibitisha kwamba udhaifu wote 19 uliotambuliwa katika ukaguzi wa usalama wa 2026-05-05 umeshughulikiwa. Ukaguzi pia unabainisha matokeo 5 mapya au yaliyobaki ambayo yaligunduliwa wakati wa kikao hiki. Hali ya usalama ya jumla ya programu imeboreshwa kwa kiasi kikubwa tangu ukaguzi uliopita.

---

## Hali ya Matokeo ya Awali (2026-05-05)

Matokeo yote 19 ya awali **yamethibitishwa kuwa yamesahihishwa**:

| # | Ugunduzi | Ukali | Hali |
|---|----------|-------|------|
| 1 | Kutumia tena IV ya AES-GCM katika uzalishaji wa nonce | 🔴 Muhimu | ✅ Imesahihishwa |
| 2 | Nonce imeandikwa kwenye kumbukumbu kwa maandishi wazi | 🔴 Muhimu | ✅ Imesahihishwa |
| 3 | Mifuatano ya nonce ya chelezo iliyowekwa moja kwa moja | 🔴 Muhimu | ✅ Imesahihishwa |
| 4 | Kamusi ya nonce ya kimataifa isiyofaa kwa nyuzi | 🟠 Juu | ✅ Imesahihishwa |
| 5 | Uthibitishaji wa mtoa mTLS umefutwa maoni | 🟠 Juu | ✅ Imesahihishwa |
| 6 | Ukaguzi wa kufutwa kwa mTLS umezimwa kwa chaguo-msingi | 🟠 Juu | ✅ Imesahihishwa |
| 7 | OCSP inarudisha halali kila wakati (stub) | 🟠 Juu | ✅ Imesahihishwa |
| 8 | Uthibitishaji/idhini zimezimwa kwa chaguo-msingi katika mipangilio | 🟠 Juu | ✅ Imesahihishwa |
| 9 | Vichwa vya usalama vimetumika baadaye sana katika mstari wa usindikaji | 🟠 Juu | ✅ Imesahihishwa |
| 10 | Kidakuzi cha kikao hakina `Secure` + `SameSite` | 🟡 Wastani | ✅ Imesahihishwa |
| 11 | Kichwa cha `Set-Cookie` cha kimataifa kilichoharibiwa | 🟡 Wastani | ✅ Imesahihishwa |
| 12 | `Content-Type` imelazimishwa kuwa `text/html` kila mahali | 🟡 Wastani | ✅ Imesahihishwa |
| 13 | `AllowedHosts` imewekwa kama herufi ya kiunganishi | 🟡 Wastani | ✅ Imesahihishwa |
| 14 | Nonce haikutumika kwenye lebo za `<script>` katika mpangilio | 🟡 Wastani | ✅ Imesahihishwa |
| 15 | Kichwa cha `Referrer-Policy` hakipo | 🟡 Wastani | ✅ Imesahihishwa |
| 16 | PII imeandikwa kwenye kumbukumbu kwa maandishi wazi | 🔵 Chini | ✅ Imesahihishwa |
| 17 | Mfuatano wa muunganisho wa sehemu kwenye kumbukumbu | 🔵 Chini | ✅ Imesahihishwa |
| 18 | Shughuli za Key Vault ni stubs | 🔵 Chini | ✅ Imesahihishwa |
| 19 | `X-XSS-Protection: 1; mode=block` iliyopitwa na wakati | 🔵 Chini | ✅ Imesahihishwa |

---

## Matokeo Mapya / Yaliyobaki

| # | Eneo | Ukali |
|---|------|-------|
| 20 | NonceRefresherService inadumisha utegemezi wa mjenzi wa Key Vault usiotumika | 🟠 Juu |
| 21 | Hifadhi ya ndani ya OcspValidationService inatumia Dictionary isiyofaa kwa nyuzi | 🟡 Wastani |
| 22 | Stub ya uthibitishaji wa OCSP bado ipo — inashindwa kwa hali iliyofungwa lakini haijatekelezwa | 🔵 Chini |
| 23 | mTLS yenye AllowedIssuers tupu inakataa vyeti vyote (fail-closed, haijadokumentwa) | 🔵 Chini |
| 24 | OcspSettings.ServerUnavailableBehavior ina chaguo-msingi cha "Warn" (inaruhusu kupita wakati wa hitilafu) | 🔵 Chini |

---

## Matokeo ya Kina

### ✅ Marekebisho Yaliyothibitishwa kutoka 2026-05-05

#### 1. Kutumia Tena IV ya AES-GCM — Imesahihishwa

**Faili:** `Models/Main_Objects/Nonce.cs`

Uzalishaji wa nonce unaotegemea AES-GCM umebadilishwa kabisa. `Nonce.GenerateSecureNonce()` sasa inaita `RandomNumberGenerator.Fill(randomBytes)` kwa baiti 16 za nasibu na kurudisha mfuatano wa Base64. Hakuna utegemezi wa Key Vault, hakuna IV, hakuna usimbaji fiche — hii ndiyo mbinu sahihi kabisa kwa nonce ya CSP.

---

#### 2. Maadili ya Nonce Hayaandikiwi Tena Kwenye Kumbukumbu — Imesahihishwa

**Faili:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Faili zote mbili sasa zinaandika tu ujumbe wa hali (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) na kamwe si thamani ya nonce yenyewe.

---

#### 3. Nonces za Chelezo Zilizowekwa Moja kwa Moja Zimeondolewa — Imesahihishwa

**Faili:** `Services/OptimizedNonceMiddleware.cs`

Mifuatano yote mitatu ya herufi iliyowekwa moja kwa moja (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) imebadilishwa na wito wa `Nonce.GenerateSecureNonce()` katika njia zote za kawaida na za chelezo za makosa.

---

#### 4. Kamusi ya Nonce Salama kwa Nyuzi — Imesahihishwa

**Faili:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` imebadilishwa na `ConcurrentDictionary<string, Nonce>`. `GetANonce` sasa inatumia wito mmoja wa atomiki wa `TryGetValue` badala ya ukaguzi wa hatua mbili wa kuthibitisha kisha kutafuta.

---

#### 5. Uthibitishaji wa Mtoa wa mTLS Sasa Unafanya Kazi — Imesahihishwa

**Faili:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Kizuizi cha uthibitishaji wa mtoa kilichofutwa maoni kimebadilishwa na wito wa `mtlsSettings.IsIssuerAllowed(issuer)`, ambao unafanya ulinganifu wa mfuatano mdogo usioathiriwa na herufi kubwa/ndogo dhidi ya `AllowedIssuers`. Wakati orodha ipo wazi (haijasanidiwa), mbinu inarudisha `false`, ikikataa vyeti vyote (fail-closed).

---

#### 6. Ukaguzi wa Kufutwa kwa mTLS Umewashwa kwa Chaguo-msingi — Imesahihishwa

**Faili:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` sasa ina chaguo-msingi cha `true`. `appsettings.template.json` pia inabainisha `"CheckCertificateRevocation": true`.

---

#### 7. Stub ya OCSP Sasa Inashindwa kwa Hali Iliyofungwa — Imesahihishwa

**Faili:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` sasa inarudisha `IsValid = false` na `OcspStatus.Error` na kuandika kosa, badala ya kurudisha kimya kimya `IsValid = true`. Kuwasha OCSP katika mipangilio sasa kutakataa vyeti vyote hadi utekelezaji wa kweli utolewe, badala ya kukubali kimya kimya.

---

#### 8. Uthibitishaji na Idhini Zimewashwa kwa Chaguo-msingi — Imesahihishwa

**Faili:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` na `EnableAuthorization` zote sasa zina chaguo-msingi cha `true` katika darasa la `FeatureFlags`. `appsettings.json` pia inaweka zote mbili kuwa `true`.

---

#### 9. Vichwa vya Usalama Vimetumika Kabla ya Uelekezaji — Imesahihishwa

**Faili:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` na `UseStandardSecurityHeaders` sasa zinaитwa kabla ya `UseRouting`, `UseAuthentication`, na `UseAuthorization`. Majibu yote, ikijumuisha njia za mkato za 401/403, yanapokea vichwa vya usalama.

---

#### 10–15. Kidakuzi, Content-Type, AllowedHosts, Nonce katika Mpangilio, Referrer-Policy — Imesahihishwa

**Faili:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Kidakuzi cha kikao sasa kinaweka `CookieSecurePolicy.Always` na `SameSiteMode.Strict`.
- Kichwa cha `Set-Cookie` kilichoharibiwa bila jina kimeondolewa.
- Ubadilishaji wa kimataifa wa `Content-Type: text/html` umeondolewa.
- `AllowedHosts` katika `appsettings.json` sasa ni `"localhost;127.0.0.1"`; kiolezo kinatumia `"{{YOUR_HOSTNAME}}"`.
- Lebo zote tatu za `<script>` katika `_Layout.cshtml` sasa zinajumuisha `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` sasa inaongezwa na `UseStandardSecurityHeaders`.

---

#### 16–19. Uandishi wa PII Kwenye Kumbukumbu, Kumbukumbu ya Mfuatano wa Muunganisho, Stubs za Key Vault, X-XSS-Protection — Imesahihishwa

**Faili:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- PII yote (OID, barua pepe, jina, SID, majukumu) sasa inafanyiwa heshi ya HMAC-SHA256 kupitia `LoggingHelper.HashPii()` kabla ya kuandikwa kwenye kumbukumbu. Ufunguo thabiti wa HMAC unaweza kutolewa kupitia `Logging:PiiHmacKey` katika mipangilio; ufunguo wa nasibu kwa kila mchakato unatumika wakati haujasanidiwa.
- Taarifa ya kumbukumbu ya Cosmos DB sasa inathibitisha tu kama mfuatano wa muunganisho upo (`!string.IsNullOrEmpty`), si maudhui yake.
- `AzureKeyVaultCertificateOperations` sasa inaitupa `InvalidOperationException` wakati wa kuanza wakati cheti ni null, badala ya kurudisha kimya kimya maadili ya bandia.
- `X-XSS-Protection` sasa imewekwa kwa `"0"` (kuzima mkaguzi wa XSS uliopitwa na wakati), ikizingatiwa na mwongozo wa vivinjari vya kisasa.

---

## 🟠 Juu

### 20. NonceRefresherService Inadumisha Utegemezi wa Mjenzi wa Key Vault Usiotumika

**Faili:** `Services/NonceRefresherService.cs`

`NonceRefresherService` bado inabainisha vigezo vya mjenzi kwa `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, na `IAzureKeyVaultOperationsService`. Kwa kuwa uzalishaji wa nonce umefanywa rahisi kutumia `RandomNumberGenerator` moja kwa moja, hakuna kati ya utegemezi huu unaotumika.

**Hatari:** Wakati `EnableNonceServices = true` na `EnableKeyVault = false` (chaguo-msingi), huduma hizi hazijasajiliwa katika chombo cha DI, ikisababisha `InvalidOperationException` wakati wa uendeshaji wakati huduma ya nonce itatuliwa kwa mara ya kwanza. Hii kimsingi ni hali ya kukataa huduma iliyochochewa na mipangilio ya chaguo-msingi. Darasa la `FeatureFlags` kwa chaguo-msingi linaweka `EnableNonceServices = true`, kwa hivyo mazingira yoyote yanayotegemea peke yake maadili ya chaguo-msingi ya darasa (bila kubadilisha `appsettings.json`) yangeweza kushindwa kuanza.

**Mapendekezo:** Ondoa vigezo vinne vya mjenzi visivyotumika na sehemu zao za faragha zinazolingana kutoka `NonceRefresherService`. Huduma inahitaji tu `ILogger<NonceRefresherService>`, `ILoggerFactory`, na `INonceCatalogService`.

---

## 🟡 Wastani

### 21. Hifadhi ya Ndani ya OcspValidationService Inatumia Dictionary Isiyofaa kwa Nyuzi

**Faili:** `Services/OcspValidationService.cs` (mstari 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` haifai kwa nyuzi kwa usomaji na uandishi unaofanana. Ikiwa `OcspValidationService` imesajiliwa kama singleton (au ikiwa mfano sawa unashirikiwa kati ya maombi na utaratibu mwingine wowote), uthibitishaji wa OCSP unaofanana unaweza kuharibu hifadhi, na kusababisha ingizo zilizopotea, makosa yaliyotupwa, au data iliyopitwa na wakati kurudishwa.

**Mapendekezo:** Badilisha `Dictionary<string, CachedOcspResponse>` na `ConcurrentDictionary<string, CachedOcspResponse>`. Sasisha wito wa `_cache.Remove` (mstari 103) kuwa `_cache.TryRemove`.

---

## 🔵 Chini / Taarifa

### 22. Stub ya Uthibitishaji wa OCSP — Inashindwa kwa Hali Iliyofungwa lakini Haijatekelezwa

**Faili:** `Services/OcspValidationService.cs` (mistari 157–173)

`PerformOcspValidationAsync` bado ni stub. Marekebisho kutoka kwa ugunduzi wa #7 yalibadilisha kwa usahihi tabia kutoka "halali kila wakati" hadi "batili kila wakati (fail-closed)". Hata hivyo, mbinu bado si utekelezaji wa kweli wa OCSP. Mradi `EnableOcspValidation = false` (chaguo-msingi), hii haina athari yoyote kwa uzalishaji. Kabla ya kuwasha OCSP katika mazingira yoyote, mteja wa OCSP wenye ubora wa uzalishaji lazima utekelezwe.

---

### 23. mTLS yenye AllowedIssuers Tupu Inakataa Vyeti Vyote vya Mteja

**Faili:** `Models/Settings/MtlsSettings.cs`

Wakati `ValidateClientCertificateIssuer = true` (chaguo-msingi) na `AllowedIssuers` ipo wazi (pia chaguo-msingi wakati haijasanidiwa), `IsIssuerAllowed()` inarudisha `false`, ikisababisha vyeti vyote vya mteja kukataliwa. Hii ni tabia sahihi ya fail-closed, lakini haijadokumentwa kwa umaarufu. Wasimamizi wanaowasha mTLS bila kusoma kiolezo kwa makini wanaweza kukuta miunganisho yote ya mteja ikikataliwa bila maelezo wazi.

**Mapendekezo:** Ongeza ujumbe wa kumbukumbu ya onyo wakati wa kuanza wakati `ValidateClientCertificateIssuer = true` na `AllowedIssuers` ipo wazi.

---

### 24. OcspSettings.ServerUnavailableBehavior ina Chaguo-msingi cha "Warn"

**Faili:** `appsettings.template.json` (mstari 134), `Services/OcspValidationService.cs`

Mipangilio ya `ServerUnavailableBehavior` ina chaguo-msingi cha `"Warn"` katika kiolezo, ambayo inaruhusu maombi kupita wakati seva ya OCSP haiwezi kufikiwa. Kwa mazingira ya usalama wa juu, hii inapaswa kuwa `"Fail"` ili kukatizwa kwa seva ya OCSP kusidhoofishe ukaguzi wa kufutwa kwa cheti kimya kimya.

**Mapendekezo:** Dokumeti chaguo tatu (`Fail`, `Allow`, `Warn`) wazi katika kiolezo na fikiria kubadilisha chaguo-msingi kuwa `"Fail"` kulingana na kanuni ya ruhusa ndogo zaidi.

---

## Tathmini ya Vichwa vya Usalama (Hali ya Sasa)

Vichwa vifuatavyo sasa vinatumika kupitia `UseStandardSecurityHeaders`:

| Kichwa | Thamani | Tathmini |
|--------|---------|---------|
| `X-Frame-Options` | `DENY` | ✅ Nzuri |
| `X-XSS-Protection` | `0` | ✅ Nzuri (inazima mkaguzi uliopitwa na wakati) |
| `X-Content-Type-Options` | `nosniff` | ✅ Nzuri |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Nzuri |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Nzuri |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Nzuri |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Nzuri |
| `Permissions-Policy` | eneo kijiografia, kamera, maikrofoni, interest-cohort zimezimwa | ✅ Nzuri |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Nzuri |
| `Content-Security-Policy` | Inategemea Nonce, inatumika OCSP inapowashwa | ✅ Nzuri |
| `Server` | Imefichwa kwa `"webserver"` | ✅ Nzuri |
| `X-Powered-By` | Imeondolewa | ✅ Nzuri |

---

## Tathmini ya Jumla

Programu imeshughulikia udhaifu wote muhimu na wa ukali wa juu kutoka kwa ukaguzi wa awali. Matokeo ya sasa yanazuiliwa kwa tatizo moja la usanidi/DI la ukali wa juu (ugunduzi #20) na vipengele vya taarifa vya ukali wa chini. Hali ya usalama imeboreshwa kwa kiasi kikubwa. Hatua ya haraka inapendekezwa kwa ugunduzi #20 (utegemezi wa DI usiotumika katika NonceRefresherService) kwani inaweza kuzuia programu kuanza chini ya usanidi wa chaguo-msingi.
