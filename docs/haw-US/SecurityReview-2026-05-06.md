# Nānā ʻAno Palekana — WebAppExperimental266

**Lā:** 2026-05-06
**Palena:** Nānā pākuʻi piha o ka waihona code (hahai ana i ka nānā o 2026-05-05)
**Mea Nānā:** Nānā Palekana ʻAutomatic

---

## Hōʻuluʻulu Hoʻokele

Hōʻoia kēia nānā hahai ʻana ua hoʻoponopono ʻia nā puka palekana ʻumi kumamaiwa (19) a pau i ʻike ʻia ma ka nānā palekana o 2026-05-05. Hōʻike pū kēia nānā i ʻelima (5) hua hou a i ʻole koena i loaʻa i kēia wā. Ua hoʻomaikaʻi nui ʻia ka palekana o ka polokalamu mai ka nānā mua aku.

---

## Ke Kūlana o Nā Hua Mua (2026-05-05)

Nā hua mua ʻumi kumamaiwa (19) **ua hōʻoia ʻia ua hoʻoponopono ʻia**:

| # | Hua | Koʻikoʻi | Kūlana |
|---|-----|----------|--------|
| 1 | Hoʻohana hou ʻia ka IV AES-GCM i ka hana nonce | 🔴 Koʻikoʻi Nui | ✅ Hoʻoponopono ʻia |
| 2 | Nonce kākau ʻia ma ka palapala maʻamau | 🔴 Koʻikoʻi Nui | ✅ Hoʻoponopono ʻia |
| 3 | Nā kaula nonce hoʻolālā paʻa i kākau paʻa ʻia | 🔴 Koʻikoʻi Nui | ✅ Hoʻoponopono ʻia |
| 4 | Puke hua nonce honua ʻaʻole palekana no nā aho | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 5 | Nā hōʻoia mea hāʻawi mTLS i hoʻokaʻawale ʻia | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 6 | Ka nānā hoʻololi mTLS hoʻopau ʻia ma ke ʻano maʻamau | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 7 | OCSP hoʻihoʻi mau i ka mea kūpono (stub) | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 8 | ʻOiaʻiʻo/ʻāpono hoʻopau ʻia ma ke ʻano maʻamau ma ka hoʻonohonoho | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 9 | Nā poʻo palekana hoʻopili ʻia ma hope loa i ka pipeline | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 10 | Nāpua cookie pūʻali ʻaʻole `Secure` + `SameSite` | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 11 | Poʻo `Set-Cookie` honua i ʻīnea ʻia | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 12 | `Content-Type` koi ʻia i `text/html` ma nā wahi a pau | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 13 | `AllowedHosts` hoʻonoho ʻia i ka hōʻailona wildcard | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 14 | ʻAʻole hoʻopili ʻia ka Nonce i nā lēpili `<script>` ma ka hoʻolālā | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 15 | Poʻo `Referrer-Policy` nalowale | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 16 | PII kākau ʻia ma ka palapala maʻamau | 🔵 Haʻahaʻa | ✅ Hoʻoponopono ʻia |
| 17 | Kaula hoʻohui hapa ma nā moʻolelo | 🔵 Haʻahaʻa | ✅ Hoʻoponopono ʻia |
| 18 | Nā hana Key Vault he mau stubs | 🔵 Haʻahaʻa | ✅ Hoʻoponopono ʻia |
| 19 | `X-XSS-Protection: 1; mode=block` kahiko | 🔵 Haʻahaʻa | ✅ Hoʻoponopono ʻia |

---

## Nā Hua Hou / Koena

| # | Wahi | Koʻikoʻi |
|---|------|----------|
| 20 | NonceRefresherService mālama ana i nā hilinaʻi mea kūkulu Key Vault i hoʻohana ʻole ʻia | 🟠 Kiʻekiʻe |
| 21 | Ka waihona waena o OcspValidationService hoʻohana ana i Dictionary ʻaʻole palekana no nā aho | 🟡 Waena |
| 22 | Stub hōʻoia OCSP aia nō — hāʻule pani akā ʻaʻole i hoʻokō ʻia | 🔵 Haʻahaʻa |
| 23 | mTLS me AllowedIssuers hakahaka hōʻole i nā palapala hōʻoia a pau (fail-closed, ʻaʻole i kākau ʻia) | 🔵 Haʻahaʻa |
| 24 | OcspSettings.ServerUnavailableBehavior maʻamau i "Warn" (ʻae i ke hele ʻana i ka wā o ka hewa) | 🔵 Haʻahaʻa |

---

## Nā Hua Kikoʻī

### ✅ Nā Hoʻoponopono i Hōʻoia ʻia mai 2026-05-05

#### 1. Hoʻohana hou ʻia ka IV AES-GCM — Hoʻoponopono ʻia

**Waihona:** `Models/Main_Objects/Nonce.cs`

Ua hoʻololi piha ʻia ka hana nonce ma AES-GCM. Ke kāhea nei ʻo `Nonce.GenerateSecureNonce()` i `RandomNumberGenerator.Fill(randomBytes)` ma 16 mau byte ʻokoʻa a hoʻihoʻi i ka kaula Base64. ʻAʻohe hilinaʻi Key Vault, ʻaʻohe IV, ʻaʻohe hoʻopaʻa — ʻo ia ka ala kūpono loa no ka nonce CSP.

---

#### 2. ʻAʻole i Kākau Hou ʻia Nā Waiwai Nonce — Hoʻoponopono ʻia

**Mau Waihona:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Nā waihona ʻelua kākau wale i nā leka kūlana (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) a ʻaʻole loa i ka waiwai nonce ponoʻī.

---

#### 3. Nā Nonce Hoʻolālā Kākau Paʻa i Wehe ʻia — Hoʻoponopono ʻia

**Waihona:** `Services/OptimizedNonceMiddleware.cs`

Nā kaula palapala kākau paʻa ʻekolu (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) i hoʻololi ʻia i nā kāhea `Nonce.GenerateSecureNonce()` ma nā ala maʻamau a hoʻolālā kuhi hewa ʻelua.

---

#### 4. Puke Hua Nonce Palekana no Nā Aho — Hoʻoponopono ʻia

**Waihona:** `Services/NonceCatalogService.cs`

Ua hoʻololi ʻia `Dictionary<string, Nonce>` i `ConcurrentDictionary<string, Nonce>`. Ke hoʻohana nei ʻo `GetANonce` i kahi kāhea atomic hoʻokahi `TryGetValue` ma kahi o ka nānā ʻelua-kaʻina.

---

#### 5. Ka Hōʻoia Mea Hāʻawi mTLS Hana Ana Kēia Manawa — Hoʻoponopono ʻia

**Mau Waihona:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Ua hoʻololi ʻia ka pōʻaiapili hōʻoia mea hāʻawi i hoʻokaʻawale ʻia me kahi kāhea `mtlsSettings.IsIssuerAllowed(issuer)`, e hana ana i ka hoʻopili kaula hapa ʻaʻole mālie i ka huapalapala nui/iki e kūʻē iā `AllowedIssuers`. Ke hakahaka ana ka papa inoa (ʻaʻole i hoʻonohonoho ʻia), hoʻihoʻi ka hana i `false`, hōʻole i nā palapala hōʻoia a pau (fail-closed).

---

#### 6. Ka Nānā Hoʻololi mTLS Hana ʻia ma ke ʻAno Maʻamau — Hoʻoponopono ʻia

**Waihona:** `Models/Settings/MtlsSettings.cs`

ʻO `CheckCertificateRevocation` ke ʻano maʻamau ʻo ia `true` kēia manawa. Hoʻonoho pū ʻo `appsettings.template.json` i `"CheckCertificateRevocation": true`.

---

#### 7. Ka Stub OCSP Hāʻule Pani Kēia Manawa — Hoʻoponopono ʻia

**Waihona:** `Services/OcspValidationService.cs`

Ke hoʻihoʻi nei ʻo `PerformOcspValidationAsync` i `IsValid = false` me `OcspStatus.Error` a kākau i ka hewa, ma kahi o ka hoʻihoʻi mālie i `IsValid = true`. E hōʻole ana ka hoʻā ʻana o OCSP i nā palapala hōʻoia a pau a hiki i ke kū ʻana o ka hoʻokō maoli.

---

#### 8. Ka ʻOiaʻiʻo a me ka ʻĀpono Hana ʻia ma ke ʻAno Maʻamau — Hoʻoponopono ʻia

**Waihona:** `Models/Settings/FeatureFlags.cs`

ʻO `EnableAzureAd` a me `EnableAuthorization` ʻelua ʻano maʻamau ʻo ia `true` kēia manawa ma ka papa `FeatureFlags`. Hoʻonoho pū ʻo `appsettings.json` i nā mea ʻelua i `true`.

---

#### 9. Nā Poʻo Palekana Hoʻopili ʻia ma Mua o ka Routing — Hoʻoponopono ʻia

**Waihona:** `Program.cs`

Kāhea ʻia ʻo `UseNonceAndSecurityHeadersAsync` a me `UseStandardSecurityHeaders` ma mua o `UseRouting`, `UseAuthentication`, a me `UseAuthorization`. Nā pane a pau, me nā pōkole-kaapuni 401/403, loaʻa iā lākou nā poʻo palekana.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce ma ka Hoʻolālā, Referrer-Policy — Hoʻoponopono ʻia

**Mau Waihona:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Ke hoʻonoho nei ka cookie pūʻali i `CookieSecurePolicy.Always` a me `SameSiteMode.Strict`.
- Ua wehe ʻia ka poʻo `Set-Cookie` ʻaʻole inoa i ʻīnea ʻia.
- Ua wehe ʻia ka hoʻololi honua `Content-Type: text/html`.
- ʻO `AllowedHosts` ma `appsettings.json` ʻo ia kēia manawa `"localhost;127.0.0.1"`; hoʻohana ka papa kauoha i `"{{YOUR_HOSTNAME}}"`.
- ʻO nā lēpili `<script>` ʻekolu ma `_Layout.cshtml` loaʻa iā lākou kēia manawa `nonce="@Context.Items["Nonce"]"`.
- Hoʻohui ʻia `Referrer-Policy: strict-origin-when-cross-origin` e `UseStandardSecurityHeaders`.

---

#### 16–19. Kākau PII, Moʻolelo Kaula Hoʻohui, Nā Stub Key Vault, X-XSS-Protection — Hoʻoponopono ʻia

**Mau Waihona:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Nā PII a pau (OID, leka uila, inoa, SID, nā kuleana) i hash HMAC-SHA256 ʻia ma o `LoggingHelper.HashPii()` ma mua o ke kākau ʻana i nā moʻolelo. Hiki ke hāʻawi ʻia ka kī HMAC paʻa ma o `Logging:PiiHmacKey` i ka hoʻonohonoho; hoʻohana ʻia ka kī ʻokoʻa no kēlā me kēia kaʻina hana ke ʻaʻole i hoʻonohonoho ʻia.
- Hōʻoia wale nō ka ʻōlelo moʻolelo Cosmos DB i ola ana ka kaula hoʻohui (`!string.IsNullOrEmpty`), ʻaʻole ka mea i loko.
- Ke kiola nei ʻo `AzureKeyVaultCertificateOperations` i ka `InvalidOperationException` i ka wā hoʻomaka ke null ka palapala hōʻoia, ma kahi o ka hoʻihoʻi mālie i nā waiwai moʻomeheu.
- ʻO `X-XSS-Protection` ke hoʻonoho ʻia nei i `"0"` (hoʻopau i ka mea nānā XSS kahiko), e like me ke alakaʻi o nā kāloloʻike hou.

---

## 🟠 Kiʻekiʻe

### 20. NonceRefresherService Mālama ana i Nā Hilinaʻi Mea Kūkulu Key Vault i Hoʻohana ʻole ʻia

**Waihona:** `Services/NonceRefresherService.cs`

Ke haʻi nei ʻo `NonceRefresherService` i nā ʻāpana mea kūkulu no `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, a me `IAzureKeyVaultOperationsService`. No ka mea ua maʻalahi ʻia ka hana nonce e hoʻohana pololei iā `RandomNumberGenerator`, ʻaʻole hoʻohana ʻia kekahi o nā hilinaʻi.

**Pilikia:** Ke `EnableNonceServices = true` a me `EnableKeyVault = false` (ke ʻano maʻamau), ʻaʻole i kākau inoa ʻia kēia mau lawelawe i loko o ka pahu DI, e hoʻāla ana i ka `InvalidOperationException` i ka wā holo ke hoʻoponopono mua ʻia ka lawelawe nonce. He kūlana hōʻole-lawelawe kēia i hoʻāla ʻia e ka hoʻonohonoho maʻamau. Ke hoʻonoho nei ka papa `FeatureFlags` ma ke ʻano maʻamau i `EnableNonceServices = true`, no laila e hāʻule ana kēlā me kēia kaiapuni e hilinaʻi wale ana i nā ʻano maʻamau papa (me ka ʻole o nā hoʻololi `appsettings.json`).

**Manaʻo:** E wehe i nā ʻāpana mea kūkulu ʻehā i hoʻohana ʻole ʻia a me kā lākou mau kahua pilikino e pili ana mai `NonceRefresherService`. Pono ka lawelawe i `ILogger<NonceRefresherService>`, `ILoggerFactory`, a me `INonceCatalogService` wale nō.

---

## 🟡 Waena

### 21. Ka Waihona Waena o OcspValidationService Hoʻohana ana i Dictionary ʻAʻole Palekana no Nā Aho

**Waihona:** `Services/OcspValidationService.cs` (laina 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

ʻAʻole palekana ʻo `Dictionary<TKey, TValue>` no nā heluhelu a kākau like ʻana. Inā kākau inoa ʻia ʻo `OcspValidationService` ma ke ʻano singleton (a i ʻole kaʻana like ʻia kēlā ʻano hana hoʻokahi ma waena o nā noi), hiki i nā hōʻoia OCSP like ʻana ke poino i ka waihona, e hoʻāla ana i nā komo nalowale, nā kiola kuhi hewa, a i ʻole ka hoʻihoʻi ʻana o nā ʻikepili kahiko.

**Manaʻo:** E hoʻololi i `Dictionary<string, CachedOcspResponse>` i `ConcurrentDictionary<string, CachedOcspResponse>`. E hoʻohou i ka kāhea `_cache.Remove` (laina 103) i `_cache.TryRemove`.

---

## 🔵 Haʻahaʻa / Hōʻike Wale

### 22. Stub Hōʻoia OCSP — Hāʻule Pani Akā ʻAʻole i Hoʻokō ʻia

**Waihona:** `Services/OcspValidationService.cs` (nā laina 157–173)

He stub nō ʻo `PerformOcspValidationAsync`. Ua hoʻololi kūpono ka hoʻoponopono o ka hua #7 i ka hana mai "kūpono mau" i "ʻaʻole kūpono mau (fail-closed)". Akā naʻe, ʻaʻole i ka hoʻokō maoli OCSP ka hana kēia manawa. ʻO `EnableOcspValidation = false` (ke ʻano maʻamau) ke kū nei, ʻaʻohe hopena i ka hana ʻoihana. Ma mua o ka hoʻā ʻana o OCSP ma kēlā me kēia kaiapuni, pono e hoʻokō ʻia kahi mea kūlana OCSP maikaʻi.

---

### 23. mTLS me AllowedIssuers Hakahaka Hōʻole i Nā Palapala Hōʻoia Kūʻokoʻa a Pau

**Waihona:** `Models/Settings/MtlsSettings.cs`

Ke `ValidateClientCertificateIssuer = true` (ke ʻano maʻamau) a me `AllowedIssuers` hakahaka (ʻano maʻamau pū ʻole ke hoʻonohonoho ʻia), hoʻihoʻi ʻo `IsIssuerAllowed()` i `false`, e hōʻole ana i nā palapala hōʻoia kūʻokoʻa a pau. He hana kūpono fail-closed kēia, akā ʻaʻole i kākau ʻia me ka nani. Hiki i nā mea hoʻonoho e hoʻā ana i mTLS me ka ʻole o ka heluhelu ʻana pono i ka papa kauoha ke ʻike i nā hoʻohui kūʻokoʻa a pau i hōʻole ʻia me ka ʻole o ka wehewehe maʻalahi.

**Manaʻo:** E hoʻohui i kahi leka ʻōlelo aʻo i ka wā hoʻomaka ke `ValidateClientCertificateIssuer = true` a me `AllowedIssuers` hakahaka.

---

### 24. OcspSettings.ServerUnavailableBehavior Maʻamau i "Warn"

**Waihona:** `appsettings.template.json` (laina 134), `Services/OcspValidationService.cs`

ʻO ke ʻano maʻamau o `ServerUnavailableBehavior` ʻo ia `"Warn"` ma ka papa kauoha, e ʻae ana i nā noi ke hele ʻole i ka lawelawe OCSP. No nā kaiapuni palekana kiʻekiʻe, pono ia e lilo i `"Fail"` i mea e pale ai i ka hāʻule ʻana o ka lawelawe OCSP mai ka hoʻopau mālie i ka nānā hoʻololi palapala hōʻoia.

**Manaʻo:** E kākau pono i nā koho ʻekolu (`Fail`, `Allow`, `Warn`) ma ka papa kauoha a e noʻonoʻo i ka hoʻololi ʻana o ke ʻano maʻamau i `"Fail"` e like me ke kumu o ka pono liʻiliʻi.

---

## Ka Loiloi o Nā Poʻo Palekana (Ke Kūlana Kēia Manawa)

Hoʻopili ʻia nā poʻo nei ma o `UseStandardSecurityHeaders`:

| Poʻo | Waiwai | Loiloi |
|------|--------|--------|
| `X-Frame-Options` | `DENY` | ✅ Maikaʻi |
| `X-XSS-Protection` | `0` | ✅ Maikaʻi (hoʻopau i ka mea nānā kahiko) |
| `X-Content-Type-Options` | `nosniff` | ✅ Maikaʻi |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Maikaʻi |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Maikaʻi |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Maikaʻi |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Maikaʻi |
| `Permissions-Policy` | nā ʻāina, ka maka kiʻi, ka leo, interest-cohort hoʻopau ʻia | ✅ Maikaʻi |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Maikaʻi |
| `Content-Security-Policy` | Nonce-like, hoʻopili ʻia ke hoʻā ʻia CSP | ✅ Maikaʻi |
| `Server` | Hūnā ʻia i `"webserver"` | ✅ Maikaʻi |
| `X-Powered-By` | Wehe ʻia | ✅ Maikaʻi |

---

## Ka Loiloi Huina

Ua hoʻoponopono ka polokalamu i nā nāwaliwali koʻikoʻi nui a kiʻekiʻe a pau mai ka nānā mua. Ua kaupalena ʻia nā hua o kēia manawa i kahi pilikia hoʻonohonoho/DI kiʻekiʻe hoʻokahi (hua #20) a me nā mea ʻike haʻahaʻa. Ua hoʻomaikaʻi nui ʻia ka palekana. Kauoha ʻia ka hana koke no ka hua #20 (nā hilinaʻi DI i hoʻohana ʻole ʻia ma NonceRefresherService) no ka mea hiki ke pale i ka polokalamu mai ka hoʻomaka ʻana ma lalo o ka hoʻonohonoho maʻamau.
