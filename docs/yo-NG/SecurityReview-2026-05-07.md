# Àtúnyẹ̀wò Ààbò — WebAppExperimental266

**Ọjọ́:** 2026-05-07
**Iwọn:** Ìtúpalẹ̀ kóòdù dídúró tó pé (tẹ̀lé àtúnyẹ̀wò 2026-05-06)
**Olùtúnyẹ̀wò:** Ètò Àtúnyẹ̀wò Ààbò Àdáṣe

---

## Àkójọpọ̀ Ìfọ̀rọ̀wánilẹ́nuwò

Àtúnyẹ̀wò tẹ̀lé yìí fi hàn pé àwọn àkóbí 3 nínú 5 tí a rí nínú àtúnyẹ̀wò ààbò 2026-05-06 ti jẹ́ ìtúnṣe tó pé, pẹ̀lú 1 tí ó ṣì wà ní ìtúnṣe àárọ̀. Àtúnyẹ̀wò náà tún rí àwọn àkóbí 4 tuntun. Ipò ààbò gbogbogbò ti ètò náà ń tẹ̀síwájú láti dára sí i.

---

## Ipò Àwọn Àkóbí Àtẹ́yìn (2026-05-06)

| # | Àkóbí | Ìlọsíwájú | Ipò |
|---|---------|----------|--------|
| 20 | NonceRefresherService ń pa àwọn ìgbẹ́kẹ̀lé afọ́jú Key Vault tí a kò lò | 🟠 Gíga | ✅ Tí a Ṣe |
| 21 | Àpótí tó wà nínú OcspValidationService ń lo Dictionary tí kò fẹsẹ̀ múlẹ̀ fún àwọn ọ̀nà | 🟡 Àárọ̀ | ✅ Tí a Ṣe |
| 22 | Àkóbí ìfọwọ́sí OCSP ṣì wà — kùnà ní ìpade ṣùgbọ́n kò fún dáadáa | 🔵 Kékeré | ⚠️ Tí a Gbà (nípasẹ̀ ìpinnu) |
| 23 | mTLS pẹ̀lú AllowedIssuers òfìfo kọ àwọn ìwé-ẹ̀rí fún gbogbo (fail-closed, tí kò ṣàlàyé) | 🔵 Kékeré | ✅ Tí a Ṣe |
| 24 | OcspSettings.ServerUnavailableBehavior ń ṣe "Warn" ní títọ̀nà (n fayè gba gbigbéjáde nígbà àṣìṣe) | 🔵 Kékeré | ⚠️ Àárọ̀ Tí a Ṣe |

---

## Àlàyé Ipò Àwọn Àkóbí Àtẹ́yìn

### ✅ 20. NonceRefresherService Àwọn DI Tí a Kò Lò — Tí a Ṣe

**Fáìlì:** `Services/NonceRefresherService.cs`

Olùkọ́ `NonceRefresherService` ní báyìí ń kéde `ILogger<NonceRefresherService>`, `ILoggerFactory` àti `INonceCatalogService` nìkan. Àwọn ìgbẹ́kẹ̀lé mẹ́rin tí a kò lò tẹ́lẹ̀ (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) ni a yọkúrò. Èyí mú ìṣòro ìkọ̀sílẹ̀ iṣẹ́ kan tí ó ń dídùn ẹ̀rọ náà kí ó ṣẹ̀rẹ̀ nígbà tí `EnableKeyVault = false` (ìpinnu) àti `EnableNonceServices = true` (ìpinnu).

---

### ✅ 21. OcspValidationService Àpótí Tí Kò Fẹsẹ̀ Múlẹ̀ Fún Àwọn Ọ̀nà — Tí a Ṣe

**Fáìlì:** `Services/OcspValidationService.cs`

A rọ́pò `Dictionary<string, CachedOcspResponse> _cache` pẹ̀lú `ConcurrentDictionary<string, CachedOcspResponse>`. Ìpèpè `_cache.Remove` ti jẹ́ mímú sínú `_cache.TryRemove`. Àpótí ní báyìí wà ní àbò fún ìwọlé ìgbakejì.

---

### ⚠️ 22. Àkóbí Ìfọwọ́sí OCSP — Tí a Gbà (Nípasẹ̀ Ìpinnu)

**Fáìlì:** `Services/OcspValidationService.cs`

Àkóbí ṣì wà ṣùgbọ́n ó kùnà ní ìpade tó ṣe dáadáa. Níwọ̀n bí `EnableOcspValidation` ń ṣe ìpinnu sí `false`, kò ní ìpínlẹ̀ iṣẹ́. A gbà á gẹ́gẹ́ bí àkóbí alàyé tó ń dúró de ìfúnwọ́ OCSP tó pé.

---

### ✅ 23. mTLS AllowedIssuers Òfìfo — Tí a Ṣe

**Fáìlì:** `Extensions/ServiceCollectionExtensions.cs`

Ìkìlọ̀ ìbẹ̀rẹ̀pẹ̀lẹ̀ ti jẹ́ títú sílẹ̀ ní báyìí nígbà tí `ValidateClientCertificateIssuer = true` àti `AllowedIssuers` wà ní òfìfo:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Èyí ń fún àwọn olùṣiṣẹ́ tí wọn bá pàdé ìdẹsẹ fail-closed ní ìtọsọ́nà tó mọ́.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Àárọ̀ Tí a Ṣe

**Àwọn Fáìlì:** `appsettings.template.json` (tí a ṣe), `Models/Settings/OcspSettings.cs` (tí kò tíì ṣe)

Àwòṣe ní báyìí ń pàsẹ `"ServerUnavailableBehavior": "Fail"` dáadáa. Bí ó ti wà, ìpinnu C# ní `OcspSettings.cs` (ìlà 39) ṣì jẹ́ `"Warn"`. Bí olùṣiṣẹ́ kan bá ṣe àánù OCSP àti pé kò fi `ServerUnavailableBehavior` sínú fáìlì ètò rẹ, ìpinnu ìkàásù `"Warn"` ni a ló ní àìdájẹ́, tí yóò fayè gba gbigbéjáde nígbà àwọn ìdẹsẹ olùpèsè OCSP. Ìpinnu ìkàásù gbọ́dọ̀ jẹ́ ìyípadà láti báramu pẹ̀lú ìmọ̀ràn àwòṣe.

---

## Àwọn Àkóbí Tuntun

| # | Agbègbè | Ìlọsíwájú |
|---|------|----------|
| 25 | Ìpinnu ìkàásù OcspSettings ("Warn") yàtọ̀ sí àwòṣe ("Fail") | 🔵 Kékeré |
| 26 | Bọ́tìnnì nonce tí a pín ọ̀kan nínú NonceCatalogService ń jẹ́ kí ìjà nonce àáárẹ̀-ìbéèrè wáyé | 🟡 Àárọ̀ |
| 27 | Àwọn iṣiro dídúró nínú OptimizedNonceMiddleware ń lo awọn integer 32-bit tí a fẹ̀sẹ̀ (ewu àṣejù) | 🔵 Kékeré |
| 28 | Program.cs ń dàfidí ILoggerFactory singleton òfìfo, tí ó ń bo ológìn ìlànà | 🟡 Àárọ̀ |

---

## 🟡 Àárọ̀

### 26. Bọ́tìnnì Nonce Tí a Pín ọ̀kan Nínú NonceCatalogService Ń Jẹ́ Kí Ìjà Nonce Àáárẹ̀-Ìbéèrè Wáyé

**Àwọn Fáìlì:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Àkọsílẹ̀ nonce ń tọ́jú àwọn nonce fún gbogbo ní abẹ bọ́tìnnì tí a pín ọ̀kan `"CSPNonce"`. Nínú ìgbéjáde ìgbákejì, ìdẹsẹ ìjẹré yìí ṣeéṣe:

1. Ìbéèrè A pè `RefreshNonceAsync()` — nonce A1 ni a tọ́jú gẹ́gẹ́ bí `_nonceCollection["CSPNonce"]`.
2. Ìbéèrè B pè `RefreshNonceAsync()` — nonce B1 kọjá `_nonceCollection["CSPNonce"]`.
3. Ìbéèrè A pè `GetANonce("CSPNonce")` — ó gbà B1, kì í ṣe A1.
4. Àkọlé CSP àti nonce ìlínlínlín Ìbéèrè A ni wọ́n jẹ B1 méjèèjì.
5. Ìbéèrè B tún jẹ B1.

Àwọn ìdáhùn ìgbákejì méjì pín nonce kan ṣoṣo. Bí ó tilẹ̀ jẹ́ pé àwọn iye méjì ṣì wà ní àìdájẹ́ cryptographic àti àìlọ̀yún (kò sí okun hardcoded), iye nonce kan ṣoṣo fara hàn nínú àwọn ìdáhùn ìgbákejì lọ́pọ̀lọ́pọ̀, tí ó ń pa ìdánilójú ìlọ́pọ̀lọ́pọ̀ nonce-fun-ìbéèrè kúrò tí àpèjúwe CSP béèrè. Ọ̀tá tí ó le ṣàkíyèsí nonce ìdáhùn kan ní nonce tó tọ́ fún ó kéré jù ìdáhùn ìgbákejì ọ̀kan mìíràn.

**Ìmọ̀ràn:** Ṣe ipò nonce tó ṣí nínú middleware fún ìbéèrè kọọkan (fun àpẹrẹ `Nonce.GenerateSecureNonce()`) àti tọ́jú sínú `HttpContext.Items["Nonce"]` nìkan, yíkiri àkọsílẹ̀ tí a pín fún nonces fun ìbéèrè. Àkọsílẹ̀ tí a pín yóò ṣì nílò nikan bí nonce bá nílò láti pín kâákájú àwọn ohun tẹẹlẹ middleware nínú ìbéèrè kan ṣoṣo, ẹnití `HttpContext.Items` ti mú ṣiṣẹ́ ní aṣa.

---

### 28. Program.cs Ń Dàfidí ILoggerFactory Singleton Òfìfo

**Fáìlì:** `Program.cs` (ìlà 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core ń dàfidí `ILoggerFactory` tó pé ní ìtọ́sọ́nà (pẹ̀lú gbogbo àwọn olùpèsè ìwé ìròyìn láti ìtọ́sọ́nà `builder.Logging`) ní aládùúgbò nígbà `WebApplication.CreateBuilder`. Àwọn ìforúkọsílẹ̀ `AddSingleton` tó gbangba yìí ń fi àpẹẹrẹ `LoggerFactory` kejì, tó ní àìtọ́sọ́nà àti láìsí àwọn olùpèsè. Níwọ̀n bí `GetRequiredService<ILoggerFactory>()` ń darí ìmúṣẹ tó kẹ̀yìn tí a forúkọsílẹ̀, àwọn iṣẹ́ tí ó gbà `ILoggerFactory` nípasẹ̀ DI (bíi `NonceRefresherService`) yóò lo ilé-iṣẹ́ òfìfo yìí àti pé kò ní ṣe ìṣelọpọ ìdahùn ìwé ìròyìn nípasẹ̀ `_loggerFactory.CreateLogger<T>()`.

**Ewu:** Ìwé ìròyìn àìdájẹ́ nínú `NonceRefresherService` — àwọn àṣeyọrí àti ìkùnà ìpilẹ̀ṣẹ nonce kò jẹ́ fífún sínú àwọn àpótí ìwé ìròyìn tó wà ní ìtọ́sọ́nà. Èyí ń dín àwọ̀ àkíyèsí ẹ̀rọ náà kù nígbà àwọn iṣẹ́ tó ní àbò ìpinnu lái fọwọ́ kan iṣẹ́ṣẹ.

**Ìmọ̀ràn:** Yọ ìforúkọsílẹ̀ gbangba `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` kúrò. `ILoggerFactory` tó ní ìtọ́sọ́nà ìlànà (pẹ̀lú Console àti àwọn olùpèsè mìíràn) yóò ṣe ìdásílẹ̀ dáadáa nípasẹ̀ àwọn iṣẹ́ tó gbára lé e.

---

## 🔵 Kékeré / Alàyé

### 25. Ìpinnu Ìkàásù OcspSettings Yàtọ̀ Sí Àwòṣe

**Fáìlì:** `Models/Settings/OcspSettings.cs` (ìlà 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Àwòṣe (`appsettings.template.json`) ń pàsẹ `"ServerUnavailableBehavior": "Fail"`, ṣùgbọ́n ìpinnu ìkàásù C# jẹ́ `"Warn"`. Bí `ServerUnavailableBehavior` kò bá wà nínú fáìlì ìtọ́sọ́nà tó ń ṣiṣẹ́, ìpinnu ìkàásù ni a lo ní àìdájẹ́ dípò ìmọ̀ràn àwòṣe. Èyí jẹ́ ìkókó tí ó ṣẹ́ kù láti àkóbí #24.

**Ìmọ̀ràn:** Yí ìpinnu ìkàásù padà láti `"Warn"` sí `"Fail"` láti báramu pẹ̀lú àwòṣe àti ìlànà ẹ̀tọ́ kéréeste.

---

### 27. Àwọn Iṣiro Dídúró Nínú OptimizedNonceMiddleware Le Kọjú Oṣù

**Fáìlì:** `Services/OptimizedNonceMiddleware.cs` (àwọn ìlà 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Àwọn iṣiro 32-bit tí a fẹ̀sẹ̀ wọ̀nyí jẹ́ ìgbéjáde atomiki nípasẹ̀ `Interlocked.Increment`. Lẹ́yìn ìgbéjáde tí ó lé ní 2.1 bílíọ̀nù iye, wọn yóò yípadà padà sí `int.MinValue` (−2,147,483,648), tí yóò jẹ́ kí ìṣàpẹẹrẹ iṣẹ́ṣẹ `(total - generated) * 100.0 / total` ṣe àwọn ìyọrísí àìtọ́ tàbí àìhẹ́dùn. Ní ìbéèrè 1,000 fún ìṣẹ́jú kan, àṣejù wáyé lẹ́yìn ọjọ́ 24.8 ti àdáṣe tí kò dáwọ́.

**Ìmọ̀ràn:** Yí iru àwọn àpótí iṣiro padà láti `int` sí `long` àti lo `long` overload ti `Interlocked.Increment` láti dènà àṣejù.

---

## Ìgbeyẹ̀wò Àkọlé Ààbò (Ipò Lọwọlọwọ)

Àwọn àkọlé wọnyi ni a lo nípasẹ̀ `UseStandardSecurityHeaders` — kò yí padà láti àtúnyẹ̀wò àtẹ́yìn:

| Àkọlé | Iye | Ìgbeyẹ̀wò |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Dára |
| `X-XSS-Protection` | `0` | ✅ Dára (ń pa àgbàgbàde àtijọ́ run) |
| `X-Content-Type-Options` | `nosniff` | ✅ Dára |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Dára |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Dára |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Dára |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Dára |
| `Permissions-Policy` | geolocation, camera, microphone, interest-cohort di-mú | ✅ Dára |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Dára |
| `Content-Security-Policy` | Tí o da lé Nonce, ni a lo nígbà tí CSP bá ṣíṣẹ́ | ✅ Dára |
| `Server` | Ti farasin sí `"webserver"` | ✅ Dára |
| `X-Powered-By` | Ti yọkúrò | ✅ Dára |

---

## Ìgbeyẹ̀wò Gbogbogbò

Gbogbo àwọn àkóbí ìlọsíwájú gíga láti àtúnyẹ̀wò àtẹ́yìn ti jẹ́ ìtúnṣe. Àwọn àkóbí lọwọlọwọ jẹ́ díwọ̀n sí àwọn ọ̀ràn ìlọsíwájú àárọ̀ méjì (#26 bọ́tìnnì nonce tí a pín, #28 ILoggerFactory òfìfo) àti àwọn ohun alàyé ìlọsíwájú kékeré méjì (#25 àìbáramu ìpinnu ìkàásù, #27 integer overflow nínú àwọn iṣiro). Àfiyèsí lẹ́sẹ̀kẹsẹ̀ ni a gbà nímọ̀ràn fún àkóbí #28 (ILoggerFactory singleton òfìfo) níwọ̀n bí ó ṣe ń pa ìwé ìròyìn aṣàwárí tó ní ààbò nígbà àwọn iṣẹ́ nonce run ní àìdájẹ́. Àkóbí #26 (bọ́tìnnì nonce tí a pín) gbọ́dọ̀ jẹ́ ìmúlẹ̀ láti mú ìdánilójú ìlọ́pọ̀lọ́pọ̀ nonce fún ìbéèrè padà tí àpèjúwe CSP béèrè.
