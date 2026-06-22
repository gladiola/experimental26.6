# Nānā ʻia ka Palekana — WebAppExperimental266

**Lā:** 2026-05-07
**Palena:** Nānā piha ʻia ka code base ma ke ʻano staika (hahai ana i ka nānā ʻana o 2026-05-06)
**Mea nānā:** Nānā Palekana Aunoa

---

## Hōʻuluʻulu Hoʻokele

Hōʻike ka nānā hahai nei ua hoʻoponopono piha ʻia 3 o 5 nā popilikia i ʻike ʻia ma ka nānā palekana o 2026-05-06, me 1 i hoʻoponopono ʻia ma kekahi ʻāpana. Hōʻike pū ka nānā i 4 mau loaʻa hou. Ke hoʻomaikaʻi mau nei ka palekana holoʻokoʻa o ka polokalamu.

---

## Ke Kūlana o Nā Loaʻa Mua (2026-05-06)

| # | Loaʻa | Koʻikoʻi | Kūlana |
|---|---------|----------|--------|
| 20 | Ke giʻi nei o NonceRefresherService nā hilinaʻi kūkulu Key Vault i hoʻohana ʻole ʻia | 🟠 Kiʻekiʻe | ✅ Hoʻoponopono ʻia |
| 21 | Ka hoʻoahu māloko o OcspValidationService e hoʻohana ana i ka Dictionary ʻaʻole palekana no nā lālau | 🟡 Waena | ✅ Hoʻoponopono ʻia |
| 22 | Ke waiho nei ka stub hoʻopaʻapaʻa OCSP — hāʻule pani ʻia akā ʻaʻole hoʻokō ʻia | 🔵 Haʻahaʻa | ⚠️ ʻae ʻia (ma ke ʻano hoʻolālā) |
| 23 | Ka mTLS me ka AllowedIssuers hakahaka e hōʻole ana i nā palapala hōʻoia a pau (fail-closed, ʻaʻole kākau ʻia) | 🔵 Haʻahaʻa | ✅ Hoʻoponopono ʻia |
| 24 | Ke paʻa nei ka OcspSettings.ServerUnavailableBehavior i "Warn" (e ʻae ana i ka hele pono ma ka hewa) | 🔵 Haʻahaʻa | ⚠️ Hoʻoponopono ʻia ma Kekahi ʻĀpana |

---

## Ke Kūlana Kiʻekiʻe o Nā Loaʻa Mua

### ✅ 20. Nā Hilinaʻi DI i Hoʻohana ʻole ʻia o NonceRefresherService — Hoʻoponopono ʻia

**Faila:** `Services/NonceRefresherService.cs`

Ke haʻi aku nei ka hale kūkulu o `NonceRefresherService` i `ILogger<NonceRefresherService>`, `ILoggerFactory`, a me `INonceCatalogService` wale nō. Ua wehe ʻia nā hilinaʻi ʻehā i hoʻohana ʻole ʻia mua aku (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`). Hoʻonā kēia i ka pilikia hōʻole lawelawe i keʻe i ka polokalamu mai ka hoʻomaka ʻana i ka wā `EnableKeyVault = false` (ka paʻamau) a me `EnableNonceServices = true` (ka paʻamau).

---

### ✅ 21. Ka Hoʻoahu ʻAʻole Palekana no Nā Lālau o OcspValidationService — Hoʻoponopono ʻia

**Faila:** `Services/OcspValidationService.cs`

Ua pani ʻia ka `Dictionary<string, CachedOcspResponse> _cache` me ka `ConcurrentDictionary<string, CachedOcspResponse>`. Ua hoʻohou ʻia ke keʻa `_cache.Remove` i `_cache.TryRemove`. He palekana kēia hoʻoahu no ka komo like ʻana.

---

### ⚠️ 22. Ka Stub Hoʻopaʻapaʻa OCSP — ʻae ʻia (Ma ke ʻAno Hoʻolālā)

**Faila:** `Services/OcspValidationService.cs`

Ke waiho mau nei ka stub akā e hāʻule pani pono ana. No ka mea, he `false` ka paʻamau o `EnableOcspValidation`, ʻaʻohe hopena i ka hana nui. Ua ʻae ʻia kēia ma ke ʻano he loaʻa hoʻomaopopo e kali ana i ka hoʻokō piha ʻana o OCSP.

---

### ✅ 23. Ka AllowedIssuers Hakahaka o mTLS — Hoʻoponopono ʻia

**Faila:** `Extensions/ServiceCollectionExtensions.cs`

Ke kākau ʻia nei kekahi aʻo hoʻomaka i ka wā `ValidateClientCertificateIssuer = true` a me ka `AllowedIssuers` hakahaka:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Hāʻawi kēia i nā alakaʻi maopopo i nā mea hoʻohana e hālāwai ana me ka hana fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Hoʻoponopono ʻia ma Kekahi ʻĀpana

**Nā faila:** `appsettings.template.json` (hoʻoponopono ʻia), `Models/Settings/OcspSettings.cs` (ʻaʻole hoʻoponopono ʻia)

Ke hoʻike pono nei ka papa hoʻolālā i `"ServerUnavailableBehavior": "Fail"`. Akā naʻe, ke paʻa mau nei ka paʻamau o ka papa C# ma `OcspSettings.cs` (laina 39) i `"Warn"`. Inā e hoʻoikaika kekahi mea hoʻohana i OCSP a e haʻalele i ka `ServerUnavailableBehavior` mai kāna faila hoʻonohonoho, e hoʻopili ʻolelo ʻole ʻia ka paʻamau papa `"Warn"`, e ʻae ana i ka hele pono ma nā wā pōpilikia o ka kikowaena OCSP. Pono e hoʻololi ʻia ka paʻamau papa e kūlike me ka paipai papa hoʻolālā.

---

## Nā Loaʻa Hou

| # | Wahi | Koʻikoʻi |
|---|------|----------|
| 25 | Ka paʻamau papa OcspSettings ("Warn") e kaʻawale ana me ka papa hoʻolālā ("Fail") | 🔵 Haʻahaʻa |
| 26 | Ka kī nonce hoʻokahi i kaʻana like ʻia ma NonceCatalogService e ʻae ana i ka haʻina nonce ma waena o nā noi | 🟡 Waena |
| 27 | Nā helu paʻa o OptimizedNonceMiddleware e hoʻohana ana i nā helu pilina 32-bit me ka hōʻailona (pilikia nui loa) | 🔵 Haʻahaʻa |
| 28 | Ke hoʻopaʻa nei o Program.cs i kahi singleton ILoggerFactory hakahaka, e uhi ana i ka loggā o ka ʻōnaehana | 🟡 Waena |

---

## 🟡 Waena

### 26. Ke Kī Nonce i Kaʻana Like ʻia o NonceCatalogService e ʻAe ana i ka Haʻina Nonce ma Waena o Nā Noi

**Nā faila:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Ke mālama nei ka kataloka nonce i nā nonce a pau ma lalo o kahi kī hoʻokahi i kaʻana like ʻia `"CSPNonce"`. Ma lalo o ka ukali like, hiki ke hana aku ka moʻolelo heihei e aʻo ʻia nei:

1. Ke keʻa nei ka Noi A i `RefreshNonceAsync()` — ua mālama ʻia ka nonce A1 ma ke ʻano he `_nonceCollection["CSPNonce"]`.
2. Ke keʻa nei ka Noi B i `RefreshNonceAsync()` — ke kāpī nei ka nonce B1 i `_nonceCollection["CSPNonce"]`.
3. Ke keʻa nei ka Noi A i `GetANonce("CSPNonce")` — loaʻa ia B1, ʻaʻole A1.
4. Aia nō me ka poʻo CSP a me ka nonce hoʻolālā o ka Noi A i B1.
5. Aia pū me ka Noi B i B1.

Kaʻana like ʻia ka nonce hoʻokahi ma nā pane ʻelua i hana like ʻia. ʻOiai he oihana cryptographic wale nō a ʻaʻole hiki ke wānana ʻia nā waiwai ʻelua (ʻaʻohe kaula hardcode ʻia), ke ʻike ʻia nei ka nonce like i nā pane like ʻia he nui, e nawaliwali ana i ka hōʻoia kūʻokoʻa o kēlā me kēia noi i koi ʻia e ka palapala CSP. He nonce kūpono ko ka mea e ʻimi e hōʻike i ka nonce o kekahi pane no hoʻokahi pane like ʻia ʻē aʻe.

**Paipai:** E hana i ka nonce ma loko o ka middleware pono no kēlā me kēia noi (e laʻa me `Nonce.GenerateSecureNonce()`) a e mālama i kahi wale nō ma `HttpContext.Items["Nonce"]`, e kiʻi ana i ka kataloka i kaʻana like ʻia no nā nonce o kēlā me kēia noi. Pono wale nō ka kataloka i kaʻana like ʻia inā pono e kaʻana like ʻia ka nonce ma waena o nā pae middleware ma loko o kahi noi hoʻokahi, kahi a `HttpContext.Items` i hana nō me ia.

---

### 28. Ke Hoʻopaʻa nei o Program.cs i Kahi Singleton ILoggerFactory Hakahaka

**Faila:** `Program.cs` (laina 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

Ke hoʻopaʻa aunoa nei o ASP.NET Core i kahi `ILoggerFactory` i hoʻonohonoho piha ʻia (me nā mea hāʻawi logging a pau mai ka hoʻonohonoho `builder.Logging`) i ka wā `WebApplication.CreateBuilder`. Ke hoʻopili nei kēia hoʻopaʻa `AddOutright` i kahi lua, `LoggerFactory` kahua ʻole me nā mea hāʻawi ʻole. No ka mea, ke hoʻihoʻi nei o `GetRequiredService<ILoggerFactory>()` i ka hoʻokō i hoʻopaʻa ʻia moʻi loa, e hoʻohana ana nā lawelawe e loaʻa ai ka `ILoggerFactory` ma ke ʻano he hāʻawi hilinaʻi (e like me `NonceRefresherService`) i kēia waihona hakahaka a ʻaʻole e hana i kahi hoʻopuka log ma `_loggerFactory.CreateLogger<T>()`.

**Popilikia:** Ka logging mālie ma `NonceRefresherService` — ʻaʻole e hoʻouna ʻia nā kūleʻa a me nā hāʻule o ka hana nonce i kahi sinki logging i hoʻonohonoho ʻia. Ke hōʻemi nei kēia i ka hiki ke nānā i ka polokalamu i nā wā hana koʻikoʻi no ka palekana me ka hoʻololi ʻole i ka hana.

**Paipai:** E wehe i ka hoʻopaʻa maopopo `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. E hoʻoponopono pono ʻia ka `ILoggerFactory` i hoʻonohonoho ʻia o ka ʻōnaehana (me ka Console a me nā mea hāʻawi ʻē aʻe) e nā lawelawe e hilinaʻi aku ana iā ia.

---

## 🔵 Haʻahaʻa / Hoʻomaopopo

### 25. Ka Paʻamau Papa OcspSettings e Kaʻawale ana me ka Papa Hoʻolālā

**Faila:** `Models/Settings/OcspSettings.cs` (laina 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Ke hoʻike nei ka papa hoʻolālā (`appsettings.template.json`) i `"ServerUnavailableBehavior": "Fail"`, akā ʻo `"Warn"` ka paʻamau o ka papa C#. Inā ʻaʻole i loaʻa ka `ServerUnavailableBehavior` ma ka faila hoʻonohonoho kūpono, e hoʻopili ʻolelo ʻole ʻia ka paʻamau papa ma kahi o ka paipai papa hoʻolālā. He koena kēia mai ka loaʻa #24.

**Paipai:** E hoʻololi i ka paʻamau papa mai `"Warn"` a i `"Fail"` e kūlike me ka papa hoʻolālā a me ke kuleana liʻiliʻi loa.

---

### 27. Hiki i Nā Helu Paʻa o OptimizedNonceMiddleware ke Piʻi Loa

**Faila:** `Services/OptimizedNonceMiddleware.cs` (nā laina 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Ke hoʻonui atomica ʻia nei kēia mau helu 32-bit i hoʻohōʻailona ʻia ma `Interlocked.Increment`. Ma hope o ka hoʻonui ʻana ma kahi o 2.1 biliona, e hoʻihoʻi lākou i `int.MinValue` (−2,147,483,648), e hana ai ka helu pono `(total - generated) * 100.0 / total` i nā hopena hewa a ʻole i ka manaʻo. Ma 1,000 mau noi i kēlā me kēia sekona, ke hiki mai ka piʻi ʻana loa ma hope o ka 24.8 mau lā o ka hana mau.

**Paipai:** E hoʻololi i nā ʻano kahua o nā helu mai `int` a i `long` a e hoʻohana i ka hoʻololi `long` o `Interlocked.Increment` e pale ai i ka piʻi loa.

---

## Ka Loiloi o Nā Poʻo Palekana (Ke Kūlana o Kēia Manawa)

Ke hoʻopili ʻia nei nā poʻo aʻe ma `UseStandardSecurityHeaders` — ʻaʻole i hoʻololi ʻia mai ka nānā mua:

| Poʻo | Waiwai | Loiloi |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Maika'i |
| `X-XSS-Protection` | `0` | ✅ Maikaʻi (e hoʻōki ana i ka mea nānā kahiko) |
| `X-Content-Type-Options` | `nosniff` | ✅ Maikaʻi |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Maikaʻi |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Maikaʻi |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Maikaʻi |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Maikaʻi |
| `Permissions-Policy` | hoʻopae ʻāina, pahu kiʻi, mikewileka, interest-cohort hoʻōki ʻia | ✅ Maikaʻi |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Maikaʻi |
| `Content-Security-Policy` | Makamua i ka nonce, hoʻopili ʻia ke hoʻoikaika ʻia ka CSP | ✅ Maikaʻi |
| `Server` | Hūnā ʻia i `"webserver"` | ✅ Maikaʻi |
| `X-Powered-By` | Wehe ʻia | ✅ Maikaʻi |

---

## Ka Loiloi Holoʻokoʻa

Ua hoʻoponopono ʻia nā loaʻa koʻikoʻe kiʻekiʻe a pau mai nā nānā mua. Paʻa nā loaʻa o kēia manawa i ʻelua pilikia koʻikoʻi waena (#26 kī nonce i kaʻana like ʻia, #28 ILoggerFactory hakahaka) a me ʻelua mea hoʻomaopopo koʻikoʻi haʻahaʻa (#25 ka paʻamau papa ʻokoʻa, #27 ka nui loa o nā helu pilina i nā helu). Paipai ʻia ka nānā koke no ka loaʻa #28 (singleton ILoggerFactory hakahaka) no ka mea ke hoʻōki mālie nei ia i ka logging kelepona palekana i nā wā hana nonce. Pono e hoʻoponopono ʻia ka loaʻa #26 (kī nonce i kaʻana like ʻia) e hoʻihoʻi ai i ka hōʻoia kūʻokoʻa o ka nonce no kēlā me kēia noi i koi ʻia e ka palapala CSP.
