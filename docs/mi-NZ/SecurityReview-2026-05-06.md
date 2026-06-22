# Arotake Haumaru — WebAppExperimental266

**Rā:** 2026-05-06
**Whānuitanga:** Arotake katoa o te rārangi waehere (i muri i te arotake o te 2026-05-05)
**Kaitirotiro:** Arotake Aunoa Haumaru

---

## Whakarāpopototanga Whakahaere

E whakaū ana tēnei arotake whai muri ko ngā whakatikanga mō ngā take haumaru tekau mā iwa (19) i kitea i te arotake haumaru o te 2026-05-05 kua oti te whakatika. E whakaatu ana hoki tēnei arotake i ngā hua hou e rima (5) i kitea i tēnei wā. Kua nui ake te pai o te haumaru o te tūmau atahua mai i te arotake o mua.

---

## Āhua o Ngā Kitenga o Mua (2026-05-05)

Ko ngā kitenga o mua tekau mā iwa (19) **kua whakaūhia kua tika**:

| # | Kitea | Taumaha | Āhua |
|---|-------|---------|------|
| 1 | Te whakamahi anō i te IV AES-GCM i roto i te mahinga nonce | 🔴 Tino Nui | ✅ Kua Tika |
| 2 | Nonce tuhia ki ngā taka mārama | 🔴 Tino Nui | ✅ Kua Tika |
| 3 | Ngā aho nonce hoahoa tūāhanga i tuhia mō ake | 🔴 Tino Nui | ✅ Kua Tika |
| 4 | Toa nonce ā-ao ehara i te haumaru mō ngā kaupeka | 🟠 Nui | ✅ Kua Tika |
| 5 | Ngā tohu kaiwhakarato mTLS me mutu | 🟠 Nui | ✅ Kua Tika |
| 6 | Tirohanga huri mTLS e whakamonohia ana e te tautuhinga paerewa | 🟠 Nui | ✅ Kua Tika |
| 7 | OCSP e hoki tonu ana ki te mea tika (stub) | 🟠 Nui | ✅ Kua Tika |
| 8 | Whakamōtiti/Mana i whakamonohia ana e te tautuhinga i roto i ngā tautuhinga | 🟠 Nui | ✅ Kua Tika |
| 9 | Ngā pane haumaru e tāpiria ana i te mutunga o te ara kōrero | 🟠 Nui | ✅ Kua Tika |
| 10 | Ngā kuki kaupeka kore `Secure` + `SameSite` | 🟡 Waenga | ✅ Kua Tika |
| 11 | Pane `Set-Cookie` ā-ao i tāirihia | 🟡 Waenga | ✅ Kua Tika |
| 12 | `Content-Type` i ūtohua ki `text/html` i ngā wāhi katoa | 🟡 Waenga | ✅ Kua Tika |
| 13 | `AllowedHosts` i tautuhia ki te tohu matarua | 🟡 Waenga | ✅ Kua Tika |
| 14 | Kāore te Nonce i honoa ki ngā tūtohu `<script>` i roto i ngā tīpako | 🟡 Waenga | ✅ Kua Tika |
| 15 | Pane `Referrer-Policy` ngaro | 🟡 Waenga | ✅ Kua Tika |
| 16 | PII tuhia ki ngā taka mārama | 🔵 Iti | ✅ Kua Tika |
| 17 | Hononga aho tāpiri i roto i ngā kōrero | 🔵 Iti | ✅ Kua Tika |
| 18 | Ngā mahi Key Vault he mau stubs | 🔵 Iti | ✅ Kua Tika |
| 19 | `X-XSS-Protection: 1; mode=block` tawhito | 🔵 Iti | ✅ Kua Tika |

---

## Ngā Kitenga Hou / Toe

| # | Wāhi | Taumaha |
|---|------|---------|
| 20 | Ko NonceRefresherService e pupuri ana i ngā hono hanganga Key Vault e kore e whakamahia | 🟠 Nui |
| 21 | Ko te kete memiori o OcspValidationService e whakamahi ana i Dictionary ehara i te haumaru mō ngā kaupeka | 🟡 Waenga |
| 22 | Stub whakaū OCSP e noho tonu ana — huri mutu engari kāore i whakatinanahia | 🔵 Iti |
| 23 | mTLS me AllowedIssuers ōpātia e whakakore ana i ngā tiwhikete katoa (mutu-katau, kāore i tuhia) | 🔵 Iti |
| 24 | OcspSettings.ServerUnavailableBehavior tautuhia paerewa ki "Warn" (e āhei ana ki te haere ina hē) | 🔵 Iti |

---

## Ngā Kitenga Taipitopito

### ✅ Ngā Whakatikanga Kua Whakaūhia mai i te 2026-05-05

#### 1. Whakamahi Anō i te IV AES-GCM — Kua Tika

**Kōnae:** `Models/Main_Objects/Nonce.cs`

Kua whakakāhoretia katoa te mahinga nonce i roto i AES-GCM. Ko `Nonce.GenerateSecureNonce()` e karanga ana ki `RandomNumberGenerator.Fill(randomBytes)` mō ngā repe 16 tōpū ā e hoki ana ki te aho Base64. Kāore he hono Key Vault, kāore he IV, kāore he whakamau — ko te ara tika tēnei mō te nonce CSP.

---

#### 2. Kāore Ngā Uara Nonce i Tuhia ki Ngā Taka — Kua Tika

**Ngā Kōnae:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Ko ēnei kōnae e rua e tuhi ana i ngā karere āhua (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) anake ā kāore rawa i te uara nonce ake.

---

#### 3. Ngā Nonce Hoahoa Tūāhanga Tuhia Mō Ake Kua Tangohia — Kua Tika

**Kōnae:** `Services/OptimizedNonceMiddleware.cs`

Ko ngā aho tūāhanga toru (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) i whakakapi ki ngā karanga ki `Nonce.GenerateSecureNonce()` i ngā ara māori ā ngā tohu hapa e rua i mōhiotia.

---

#### 4. Toa Nonce Haumaru mō Ngā Kaupeka — Kua Tika

**Kōnae:** `Services/NonceCatalogService.cs`

Kua huri `Dictionary<string, Nonce>` ki `ConcurrentDictionary<string, Nonce>`. Ko `GetANonce` e whakamahi ana i tētahi karanga atomic kotahi `TryGetValue` hei wāhi o tētahi tirohanga rua-arawhata.

---

#### 5. Ko Ngā Tohu Kaiwhakarato mTLS e Mahi Ana Ināianei — Kua Tika

**Ngā Kōnae:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Kua huri te horopaki whakaū kaiwhakarato i ūtohua ki tētahi karanga `mtlsSettings.IsIssuerAllowed(issuer)`, e whakamahi ana i tētahi whakarite aho kore-tōkeke ki `AllowedIssuers`. Ina ōpātia te rārangi (kāore i tautuhia), ko te tikanga e hoki ana ki `false`, e whakakore ana i ngā tiwhikete katoa (mutu-katau).

---

#### 6. Ko te Tirohanga Huri mTLS e Whakahohea Ana e te Tautuhinga Paerewa — Kua Tika

**Kōnae:** `Models/Settings/MtlsSettings.cs`

Ko `CheckCertificateRevocation` ināianei `true` hei paerewa. Ko `appsettings.template.json` hoki e tautuhia ana `"CheckCertificateRevocation": true`.

---

#### 7. Ko te Stub OCSP e Huri Mutu Ana Ināianei — Kua Tika

**Kōnae:** `Services/OcspValidationService.cs`

Ko `PerformOcspValidationAsync` ināianei e hoki ana ki `IsValid = false` me `OcspStatus.Error` ā e tuhi ana i te hapa, hei wāhi o te hoki mārie ki `IsValid = true`. Ko te whakahohe o OCSP e whakakore ana i ngā tiwhikete katoa ā tae noa ki te whakatinanatanga tūturu.

---

#### 8. Ko Whakamōtiti me Mana e Whakahohea Ana e te Tautuhinga Paerewa — Kua Tika

**Kōnae:** `Models/Settings/FeatureFlags.cs`

Ko `EnableAzureAd` me `EnableAuthorization` tōrua ināianei `true` hei paerewa i roto i te akomanga `FeatureFlags`. Ko `appsettings.json` hoki e tautuhia ana ko ngā mea e rua ki `true`.

---

#### 9. Ko Ngā Pane Haumaru e Tāpiria Ana i Mua o te Ara Kōrero — Kua Tika

**Kōnae:** `Program.cs`

Ko `UseNonceAndSecurityHeadersAsync` me `UseStandardSecurityHeaders` e karangatia ana i mua o `UseRouting`, `UseAuthentication`, me `UseAuthorization`. Ko ngā whakautu katoa, tae atu ki ngā 401/403 e tino poto ana, ka whiwhi i ō rātou pane haumaru.

---

#### 10–15. Kuki, Content-Type, AllowedHosts, Nonce i Ngā Tīpako, Referrer-Policy — Kua Tika

**Ngā Kōnae:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Ko ngā kuki kaupeka ināianei e tautuhia ana ki `CookieSecurePolicy.Always` me `SameSiteMode.Strict`.
- Kua tangohia te pane `Set-Cookie` ā-ao i tāirihia.
- Kua tangohia te taurangi ā-ao `Content-Type: text/html`.
- Ko `AllowedHosts` i `appsettings.json` ināianei ko `"localhost;127.0.0.1"`; ko te tīpako e whakamahi ana `"{{YOUR_HOSTNAME}}"`.
- Ko ngā tūtohu `<script>` toru i roto i `_Layout.cshtml` ināianei kei a rātou `nonce="@Context.Items["Nonce"]"`.
- Ko `Referrer-Policy: strict-origin-when-cross-origin` i tāpirihia e `UseStandardSecurityHeaders`.

---

#### 16–19. PII i Ngā Taka, Hononga Aho Tāpiri, Ngā Stub Key Vault, X-XSS-Protection — Kua Tika

**Ngā Kōnae:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Ko ngā PII katoa (OID, īmēra, ingoa, SID, ngā mana) i HMAC-SHA256 hash-tia mā `LoggingHelper.HashPii()` i mua o te tuhituhi ki ngā taka. Ka taea te tuku i te kī HMAC tūāhanga mā `Logging:PiiHmacKey` i roto i ngā tautuhinga; ka whakamahia he kī ōrite mō ia tīkanga ina kāore i tautuhia.
- Ko te karere Cosmos DB e whakaū ana i ora noa te aho hononga (`!string.IsNullOrEmpty`), ehara i te mea i roto.
- Ko `AzureKeyVaultCertificateOperations` ināianei e huri ana i tētahi `InvalidOperationException` i te wā tīmata ina kore te tiwhikete, hei wāhi o te hoki mārie ki ngā uara metatōkena.
- Ko `X-XSS-Protection` ināianei e tautuhia ana ki `"0"` (e whakamoumou ana i te kaiwhakatika XSS tawhito), e hāngai ana ki ngā tohutohu kaiwāwāo hou.

---

## 🟠 Nui

### 20. Ko NonceRefresherService e Pupuri Ana i Ngā Hono Hanganga Key Vault e Kore e Whakamahia

**Kōnae:** `Services/NonceRefresherService.cs`

Ko `NonceRefresherService` e kōkuhu ana i ngā waahanga hanganga mō `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, me `IAzureKeyVaultOperationsService`. Nō reira i māmā ake ai te mahinga nonce ki te whakamahi tika i `RandomNumberGenerator`, kāore ētahi o ngā hono e whakamahia ana.

**Take:** Ina `EnableNonceServices = true` me `EnableKeyVault = false` (paerewa), kāore ēnei ratonga i rēhitatia i roto i te kete DI, e ara ake ana i tētahi `InvalidOperationException` i te wā whakatū ina whakatikahia tuatahi te ratonga nonce. He āhua mutu-ratonga tēnei i whakaohatia e te tautuhinga paerewa. Ko te akomanga `FeatureFlags` e tautuhia ana `EnableNonceServices = true` hei paerewa, nō reira ka hinga ngā taiao katoa e whakawhirinaki ana i ngā paerewa akomanga anake (me te kore i huri ngā `appsettings.json`).

**Tūtohu:** Tangohia ngā waahanga hanganga e whā e kore e whakamahia ana me ō rātou ngā tāhuhu tinana hono mai i `NonceRefresherService`. Ko te ratonga me `ILogger<NonceRefresherService>`, `ILoggerFactory`, me `INonceCatalogService` anake.

---

## 🟡 Waenga

### 21. Ko te Kete Memiori o OcspValidationService e Whakamahi Ana i Dictionary Ehara i te Haumaru mō Ngā Kaupeka

**Kōnae:** `Services/OcspValidationService.cs` (raina 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

Ko `Dictionary<TKey, TValue>` ehara i te haumaru mō ngā pānui me ngā tuhituhi ōrite. Ina rēhitatia `OcspValidationService` hei singleton (me te toha i taua mahinga i ngā tono), ka āhua pakaru ngā whakaū OCSP ōrite i te kete, e ara ake ana i ngā ngaro, ngā huri kore e tūmanakohia, me te hoki o ngā raraunga tawhito.

**Tūtohu:** Hurihia `Dictionary<string, CachedOcspResponse>` ki `ConcurrentDictionary<string, CachedOcspResponse>`. Whakahoungia te karanga `_cache.Remove` (raina 103) ki `_cache.TryRemove`.

---

## 🔵 Iti / Mōhiotia

### 22. Stub Whakaū OCSP — Huri Mutu Engari Kāore i Whakatinanahia

**Kōnae:** `Services/OcspValidationService.cs` (ngā raina 157–173)

He stub tonu `PerformOcspValidationAsync`. Ko te whakatikanga o te kitea #7 i tika pai te mahinga mai "haumaru tonu" ki "kore haumaru tonu (mutu-katau)". Heoi anō, ehara i te whakatinanatanga OCSP tūturu tēnei mahinga. Ina `EnableOcspValidation = false` (paerewa), kāore he pānga ki te mahi pakihi. I mua o te whakahohe i OCSP i tētahi taiao, me whakatinanahia tētahi mahi whakaū OCSP tūturu.

---

### 23. mTLS me AllowedIssuers Ōpātia e Whakakore Ana i Ngā Tiwhikete Tono Katoa

**Kōnae:** `Models/Settings/MtlsSettings.cs`

Ina `ValidateClientCertificateIssuer = true` (paerewa) me `AllowedIssuers` ōpātia (paerewa anō ina kāore i tautuhia), ko `IsIssuerAllowed()` e hoki ana ki `false`, e whakakore ana i ngā tiwhikete tono katoa. He tikanga mutu-katau tēnei e hiahiatia ana, engari kāore i tuhia me te āhua. Ka taea e ngā kaitautuhia e whakahohea ai mTLS me te kore i pānui pai i te tīpako te kite i ngā tono katoa i whakakore ā kāore he whakamarama mārama.

**Tūtohu:** Tāpirihia tētahi karere reo tūāhu i te wā tīmata ina `ValidateClientCertificateIssuer = true` me `AllowedIssuers` ōpātia.

---

### 24. OcspSettings.ServerUnavailableBehavior Paerewa ki "Warn"

**Kōnae:** `appsettings.template.json` (raina 134), `Services/OcspValidationService.cs`

Ko te paerewa o `ServerUnavailableBehavior` ko `"Warn"` i roto i te tīpako, e āhei ana i ngā tono haere ake ina kāore e taea te tae ki te ratonga OCSP. Mō ngā taiao haumaru teitei, me huri tēnei ki `"Fail"` kia ārai i te rahunga o te ratonga OCSP mai te whakatūkino mārie i te tirohanga huri tiwhikete.

**Tūtohu:** Me tuhia māramatia ngā kōwhiringa e toru (`Fail`, `Allow`, `Warn`) i roto i te tīpako ā me whiwhi whakaaro ki te huri i te paerewa ki `"Fail"` e ai ki te mātāpono mana iti rawa.

---

## Arotake o Ngā Pane Haumaru (Āhua Ināianei)

Ko ēnei pane e tāpiria ana e `UseStandardSecurityHeaders`:

| Pane | Uara | Arotake |
|------|------|---------|
| `X-Frame-Options` | `DENY` | ✅ Pai |
| `X-XSS-Protection` | `0` | ✅ Pai (e whakamoumou ana i te kaitiaki XSS tawhito) |
| `X-Content-Type-Options` | `nosniff` | ✅ Pai |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Pai |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Pai |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Pai |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Pai |
| `Permissions-Policy` | wāhi, kāmera, hopuoro, interest-cohort i whakamomohia | ✅ Pai |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Pai |
| `Content-Security-Policy` | E hono ana ki nonce, e tāpiria ana ina whakahohea CSP | ✅ Pai |
| `Server` | E hunaia ana ki `"webserver"` | ✅ Pai |
| `X-Powered-By` | E tangohia ana | ✅ Pai |

---

## Arotake Whakakapi

Kua whakatikahia e te tūmau atahua ngā ngoikoretanga nui me ngā nui katoa mai i te arotake o mua. Ko ngā kitenga ināianei e herea ana ki tētahi take tautuhinga/DI nui kotahi (kitea #20) me ngā mōhiotia iti. Kua nui ake te haumaru. Ka tūtohungia te mahi tere mō te kitea #20 (ngā hono DI e kore e whakamahia ana i roto i NonceRefresherService) nō reira ka āhei te tūmau ki te tīmata i raro i te tautuhinga paerewa.
