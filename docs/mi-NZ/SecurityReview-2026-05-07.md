# Arotake Haumarutanga — WebAppExperimental266

**Rā:** 2026-05-07
**Whānuitanga:** Tātaritanga tū-atu katoa o ngā waehere (aronga ki muri mai i te arotake 2026-05-06)
**Kaiaro:** Arotake Haumarutanga Aunoa

---

## Whakarāpopototanga Rangatira

E whakatūturu ana tēnei arotake aronga ki muri i whai muri ake ko 3 o ngā 5 āhuatanga ngāwari i tautuhi i roto i te arotake haumarutanga 2026-05-06 kua tino āreruia, me tētahi e toe ana kia āreruia ā-āpure. Ka tautuhi anō te arotake i ngā kitenga hou e 4. E haere tonu ana te whakapai ake o te tūāhuatanga haumarutanga whānui o te tono.

---

## Āhuatanga o Ngā Kitenga o Mua (2026-05-06)

| # | Kitea | Taumaha | Āhuatanga |
|---|---------|----------|--------|
| 20 | Ko NonceRefresherService e pupuri ana i ngā whirinaki hanganga Key Vault e kāore i whakamahia | 🟠 Teitei | ✅ Kua Āreruia |
| 21 | Ko te rāhina ō-roto o OcspValidationService e whakamahi ana i tētahi Dictionary ehara i te haumaru-mōrearea | 🟡 Waenganui | ✅ Kua Āreruia |
| 22 | Ko te stub whakamana OCSP kei reira tonu — ka hinga kati engari kāore i whakatinanahia | 🔵 Tāhūnui | ⚠️ Whakaaetia (nā te hoahoa) |
| 23 | Ko te mTLS me te AllowedIssuers kau e whakakāhore ana i ngā tiwhikete katoa (fail-closed, kāore i tuhia) | 🔵 Tāhūnui | ✅ Kua Āreruia |
| 24 | Ko OcspSettings.ServerUnavailableBehavior ōrite ki "Warn" (e aro ana ki te tūnga hapa) | 🔵 Tāhūnui | ⚠️ Kua Āreruia ā-Āpure |

---

## Āhuatanga Whānui o Ngā Kitenga o Mua

### ✅ 20. Ngā Whirinaki DI Kāore i Whakamahia o NonceRefresherService — Kua Āreruia

**Kōnae:** `Services/NonceRefresherService.cs`

Ko te hanganga `NonceRefresherService` ināianei e kī ana anake ki `ILogger<NonceRefresherService>`, `ILoggerFactory`, me `INonceCatalogService`. Ko ngā whirinaki e whā o mua i kāore i whakamahia (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) kua mukua. Ka oti i tēnei te tūraru whakakāhore ratonga i kāpina i te tono mai i te tīmata ina `EnableKeyVault = false` (te taunoa) me `EnableNonceServices = true` (te taunoa).

---

### ✅ 21. Ko te Rāhina Ehara i te Haumaru-Mōrearea o OcspValidationService — Kua Āreruia

**Kōnae:** `Services/OcspValidationService.cs`

Ko te `Dictionary<string, CachedOcspResponse> _cache` kua anō ki `ConcurrentDictionary<string, CachedOcspResponse>`. Ko te karanga `_cache.Remove` kua whakahoutia ki `_cache.TryRemove`. He haumaru ināianei te rāhina mō te whai wāhi tūāhuru.

---

### ⚠️ 22. Ko te Stub Whakamana OCSP — Whakaaetia (Nā te Hoahoa)

**Kōnae:** `Services/OcspValidationService.cs`

Ko te stub kei reira tonu engari ka hinga tika kati. Nā te mea ko te `EnableOcspValidation` taunoa he `false`, kāore he pānga ki te hua. Ka whakaaetia tēnei hei kitea pārongo e tatari ana ki te whakatinanahia katoa o OCSP.

---

### ✅ 23. Ko te AllowedIssuers Kau o mTLS — Kua Āreruia

**Kōnae:** `Extensions/ServiceCollectionExtensions.cs`

Ka rekoata tētahi whakatūpato tīmata ināianei ina `ValidateClientCertificateIssuer = true` ā ka kau te `AllowedIssuers`:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Ka hoatu tēnei i ngā aratohu mārama ki ngā kaiwhakahaere e tūtaki ana ki te whanonga fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Kua Āreruia ā-Āpure

**Ngā Kōnae:** `appsettings.template.json` (kua āreruia), `Models/Settings/OcspSettings.cs` (kāore anō i āreruia)

Ko te tūāhua ināianei e tohu tika ana ki `"ServerUnavailableBehavior": "Fail"`. Heoi, ko te taunoa akomanga C# i `OcspSettings.cs` (raina 39) e toe ana ki `"Warn"`. Ki te whakahohe kaiwhakahau i OCSP ā tōna korenga o te `ServerUnavailableBehavior` mai tōna kōnae whirihoranga, ka tautohua mārie te taunoa akomanga `"Warn"`, e aro ana ki te tūnga mō ngā hapa tūmau o te tūmau OCSP. Me huri te taunoa akomanga kia rite ki tūtohinga tūāhua.

---

## Ngā Kitenga Hou

| # | Wāhi | Taumaha |
|---|------|----------|
| 25 | Ko te taunoa akomanga OcspSettings ("Warn") e rereketia ana i te tūāhua ("Fail") | 🔵 Tāhūnui |
| 26 | Ko te kī nonce kotahi i tohatoha i NonceCatalogService e aro ana ki te haumi nonce i waenga i ngā tono | 🟡 Waenganui |
| 27 | Ko ngā kaute tū o OptimizedNonceMiddleware e whakamahi ana i ngā tau 32-bit tohu (tūraru taupeke) | 🔵 Tāhūnui |
| 28 | Ko te Program.cs e rēhita ana i tētahi singleton ILoggerFactory kau, e huna ana i te kaiwhakamarama o te anga | 🟡 Waenganui |

---

## 🟡 Waenganui

### 26. Ko te Kī Nonce i Tohatoha o NonceCatalogService e Aro ana ki te Haumi Nonce i Waenga i Ngā Tono

**Ngā Kōnae:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Ko te kātaka nonce e tiaki ana i ngā nonce katoa i raro i tētahi kī kotahi i tohatoha `"CSPNonce"`. I raro i te uta tūāhuru, ka taea te tūāhuatanga oma e whai ake nei:

1. Ko te Tono A e karanga ana ki `RefreshNonceAsync()` — ko te nonce A1 e tiakina ana hei `_nonceCollection["CSPNonce"]`.
2. Ko te Tono B e karanga ana ki `RefreshNonceAsync()` — ko te nonce B1 e takahi ana i `_nonceCollection["CSPNonce"]`.
3. Ko te Tono A e karanga ana ki `GetANonce("CSPNonce")` — ka riro B1, kāore ko A1.
4. Ko te pane CSP me te nonce whakatakotoranga o Tono A ā rāua ko B1.
5. Ko te Tono B anō ōna B1.

E rua ngā whakautu tūāhuru e tohatoha ana i te nonce kotahi. Ahakoa ko ngā uara e rua e tūāhuru tonu ana i te pūnaha huna me te kāore e taea te matapae (kāore he aho tūāhura), ka puta te uara nonce kotahi ki roto i ngā whakautu tūāhuru maha, ka ngoikore ai te āhei motuhake mō ia tono e hiahiatia ana e ngā tohu CSP. Ka whai te kaiwhakamuka e taea ana te mātakitaki i te nonce o tētahi whakautu i tētahi nonce tōtika mō tētahi atu whakautu tūāhuru.

**Tūtohinga:** Whakaputahia te nonce tika i roto i te middleware mō ia tono (hei tauira, `Nonce.GenerateSecureNonce()`) ā tiakina anake ki `HttpContext.Items["Nonce"]`, ka karo i te kātaka i tohatoha mō ngā nonce ā-tono. Ko te kātaka i tohatoha ka hiahiatia anake ki te hiahia ki te tohatoha nonce i ngā paepae middleware i roto i tētahi tono kotahi, tērā e whakahaere tonu ana e `HttpContext.Items`.

---

### 28. Ko te Program.cs e Rēhita ana i tētahi Singleton ILoggerFactory Kau

**Kōnae:** `Program.cs` (raina 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

Ka rēhita aunoa a ASP.NET Core i tētahi `ILoggerFactory` i whakaritea katoa (me ngā kaiwhakarato katoa o te whakaurunga mai i te whirihoranga `builder.Logging`) i te wā o `WebApplication.CreateBuilder`. Ko tēnei rēhitatanga `AddSingleton` tū ka tāpiri i tētahi tūtanga `LoggerFactory` tuarua, kāore i whirihoratia me ōna kaiwhakarato. Nā te mea ka hoki mai a `GetRequiredService<ILoggerFactory>()` i te whakatinanahia o rōpū hou rawa atu i rēhitatia, ko ngā ratonga e riro ana i `ILoggerFactory` mā te tuku whirinaki (pērā `NonceRefresherService`) ka whakamahi i tēnei whare kau ā kāore e hanga ai i tētahi putanga rekoata mā `_loggerFactory.CreateLogger<T>()`.

**Tūraru:** Ko te rekoata mārie i `NonceRefresherService` — kāore ngā angitu me ngā raukore hanga nonce e tukuna ana ki tētahi hipanga rekoata i whirihoratia. Ka whakaitia tēnei i te āhei mātakitaki o te tono i ngā wā mahi haumaru-aro me te kore e pā ki te mahi.

**Tūtohinga:** Mukua te rēhitatanga tū `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. Ka whakaotia tika ai te `ILoggerFactory` i whirihoratia o te anga (me te Console me ōna kaiwhakarato) e ngā ratonga e whirinaki ana ki a ia.

---

## 🔵 Tāhūnui / Pārongo

### 25. Ko te Taunoa Akomanga OcspSettings e Rereketia ana i te Tūāhua

**Kōnae:** `Models/Settings/OcspSettings.cs` (raina 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Ko te tūāhua (`appsettings.template.json`) e tohu ana ki `"ServerUnavailableBehavior": "Fail"`, engari ko te taunoa akomanga C# he `"Warn"`. Ki te ngaro te `ServerUnavailableBehavior` mai te kōnae whirihoranga hohe, ka tautohua mārie te taunoa akomanga engari kāore ko te tūtohinga tūāhua. He toenga tēnei o te kitenga #24.

**Tūtohinga:** Hurihia te taunoa akomanga mai `"Warn"` ki `"Fail"` kia rite ki te tūāhua me te mātāpono o ngā tūāhuatanga iti rawa.

---

### 27. Ko Ngā Kaute Tū o OptimizedNonceMiddleware E Taea ana te Taupeke

**Kōnae:** `Services/OptimizedNonceMiddleware.cs` (raina 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Ko ēnei kaute 32-bit tohu e tāpirihia atomically mā `Interlocked.Increment`. I muri mai i tata ki ngā tāpiritanga 2.1 piriona, ka huri ki `int.MinValue` (−2,147,483,648), ka hanga ai te tatau tōtika `(total - generated) * 100.0 / total` i ngā hua hē, kāore rānei he tikanga. I te 1,000 tono ia hēkona, ka puta te taupeke i muri mai i tata 24.8 rā o te mahi tonu.

**Tūtohinga:** Hurihia ngā momo kūiti kaute mai `int` ki `long` ā whakamahia te nui `long` o `Interlocked.Increment` kia ārai i te taupeke.

---

## Aromatawai o Ngā Pane Haumarutanga (Āhuatanga o Ināianei)

Ko ngā pane e whai ake nei e tautohua ana mā `UseStandardSecurityHeaders` — kāore i huri mai i te arotake o mua:

| Pane | Uara | Aromatawai |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Pai |
| `X-XSS-Protection` | `0` | ✅ Pai (e whakakore ana i te kaitiaki tāhūtia) |
| `X-Content-Type-Options` | `nosniff` | ✅ Pai |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Pai |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Pai |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Pai |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Pai |
| `Permissions-Policy` | tūrangawaewae, kāmera, hopukorero, interest-cohort whakakore | ✅ Pai |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Pai |
| `Content-Security-Policy` | Ā-nonce, ka tautohua ina whakahohea te CSP | ✅ Pai |
| `Server` | Ka hunaia ki `"webserver"` | ✅ Pai |
| `X-Powered-By` | Ka mukua | ✅ Pai |

---

## Aromatawai Whānui

Kua āreruia ngā kitenga taumaha-teitei katoa mai i ngā arotake o mua. Ko ngā kitenga o ināianei e aukatia ana ki ngā take taumaha-waenganui e rua (#26 kī nonce i tohatoha, #28 ILoggerFactory kau) me ngā āhuatanga pārongo taumaha-haʻahaʻa e rua (#25 rereketanga taunoa akomanga, #27 taupeke tau i ngā kaute). Ka tūtohu kia aro tūāhuri ki te kitenga #28 (singleton ILoggerFactory kau) nā te mea ka huna mārie i te rekoata mōhiohio e hāngai ana ki te haumarutanga i ngā wā mahi nonce. Me whakaoti te kitenga #26 (kī nonce i tohatoha) kia whakahokia te āhei motuhake nonce ā-tono e hiahiatia ana e ngā tohu CSP.
