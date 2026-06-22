# Àyẹ̀wò Ààbò — WebAppExperimental266

**Ọjọ́:** 2026-05-06
**Ìwọ̀n:** Àyẹ̀wò kíkún ti àkójọpọ̀ kóòdù (ìtẹ̀síwájú àyẹ̀wò 2026-05-05)
**Olùàyẹ̀wò:** Àyẹ̀wò Ààbò Àifọwọ́tọ

---

## Ìsọníṣokí Ìṣàkóso

Àyẹ̀wò ìtẹ̀síwájú yìí jẹ́rìísí pé gbogbo ìdámọ̀ ìṣòro ààbò mọ́kàndínlógún (19) tí a mọ̀ ní àyẹ̀wò ààbò 2026-05-05 ni a ti ṣàtúnṣe pẹ̀lú àṣeyọrí. Àyẹ̀wò yìí tún dámọ̀ àwọn ìdámọ̀ tuntun tàbí tó kù márùn-ún (5) tí a rí ní àkókò yìí. Ipò ààbò àpapọ̀ ti ohun èlò náà ti dára sí i lọ nìṣó láti àyẹ̀wò tẹ́lẹ̀.

---

## Ipò Àwọn Ìdámọ̀ Àtijọ́ (2026-05-05)

Gbogbo ìdámọ̀ àtijọ́ mọ́kàndínlógún (19) **ti jẹ́ jẹ́rìísí pé a ti ṣàtúnṣe wọn**:

| # | Ìdámọ̀ | Ìjìnnà | Ipò |
|---|--------|--------|-----|
| 1 | Lílo AES-GCM IV lẹ̀ẹ̀kan sí i nínú ìṣẹ̀dá nonce | 🔴 Pàtàkì | ✅ Ti Ṣàtúnṣe |
| 2 | Nonce tí a kọ sí àwọn ìgbàsilẹ̀ àwọn ọ̀rọ̀ tààrà | 🔴 Pàtàkì | ✅ Ti Ṣàtúnṣe |
| 3 | Àwọn ọ̀rọ̀ àkíndìí nonce àdìtú tí a kọ lọ́nà pàtó | 🔴 Pàtàkì | ✅ Ti Ṣàtúnṣe |
| 4 | Àtọjọ nonce àgbáyé kò ní ìdáàbòbò fún àwọn ẹ̀ka | 🟠 Gíga | ✅ Ti Ṣàtúnṣe |
| 5 | Àwọn jẹ́rìísí onísèlú mTLS kò ní ìmúsìnlẹ̀ | 🟠 Gíga | ✅ Ti Ṣàtúnṣe |
| 6 | Ìgbógun ìfagile mTLS dáwọ́dúró ní ìpele àìpéye | 🟠 Gíga | ✅ Ti Ṣàtúnṣe |
| 7 | OCSP máa ń padà pẹ̀lú òdódó títí (stub) | 🟠 Gíga | ✅ Ti Ṣàtúnṣe |
| 8 | Jẹ́rìísí/Àṣẹ dáwọ́dúró ní ìpele àìpéye nínú àwọn àpéjọ | 🟠 Gíga | ✅ Ti Ṣàtúnṣe |
| 9 | Àwọn àkọlé ààbò tí a fi kún ní òpin àlàfo ìṣẹ̀dá | 🟠 Gíga | ✅ Ti Ṣàtúnṣe |
| 10 | Àwọn kúkì iṣẹ̀ aláìní `Secure` + `SameSite` | 🟡 Àárín | ✅ Ti Ṣàtúnṣe |
| 11 | Àkọlé `Set-Cookie` àgbáyé tí a sọ gbẹ̀ sínú | 🟡 Àárín | ✅ Ti Ṣàtúnṣe |
| 12 | `Content-Type` tí a kọ pàtó sí `text/html` níbikíbi | 🟡 Àárín | ✅ Ti Ṣàtúnṣe |
| 13 | `AllowedHosts` tí a ṣètò sí àmì àgbásọ | 🟡 Àárín | ✅ Ti Ṣàtúnṣe |
| 14 | Nonce kò so mọ́ àwọn àmì `<script>` nínú àdàkọ | 🟡 Àárín | ✅ Ti Ṣàtúnṣe |
| 15 | Àkọlé `Referrer-Policy` pàdánù | 🟡 Àárín | ✅ Ti Ṣàtúnṣe |
| 16 | PII tí a kọ sí àwọn ìgbàsilẹ̀ àwọn ọ̀rọ̀ tààrà | 🔵 Kékeré | ✅ Ti Ṣàtúnṣe |
| 17 | Ìpọ̀pọ̀ ọ̀rọ̀ nínú àwọn ìfiránṣẹ̀ | 🔵 Kékeré | ✅ Ti Ṣàtúnṣe |
| 18 | Àwọn iṣẹ́ Key Vault jẹ́ stubs | 🔵 Kékeré | ✅ Ti Ṣàtúnṣe |
| 19 | `X-XSS-Protection: 1; mode=block` àtijọ́ | 🔵 Kékeré | ✅ Ti Ṣàtúnṣe |

---

## Àwọn Ìdámọ̀ Tuntun / Tó Kù

| # | Ìpò | Ìjìnnà |
|---|-----|--------|
| 20 | NonceRefresherService ń pa àwọn ìgbọràn àkọ̀tọ Key Vault tí a kò lò mọ́ | 🟠 Gíga |
| 21 | Àpamọ́ irántí OcspValidationService ń lò Dictionary tó kò ní ìdáàbòbò fún àwọn ẹ̀ka | 🟡 Àárín |
| 22 | Stub jẹ́rìísí OCSP ṣì wà — kò ṣí bí ó bá kùnà ṣùgbọ́n kò ní ìmúṣẹ | 🔵 Kékeré |
| 23 | mTLS pẹ̀lú AllowedIssuers òfo kọ gbogbo ìwé-ẹ̀rí (kò ṣí bí ó bá kùnà, a kò kọ rẹ̀ sílẹ̀) | 🔵 Kékeré |
| 24 | OcspSettings.ServerUnavailableBehavior àìpéye "Warn" (gba ọ̀nà bí àṣìṣe bá wà) | 🔵 Kékeré |

---

## Àwọn Ìdámọ̀ Pípẹ̀kọ

### ✅ Àwọn Àtúnṣe Tí A Jẹ́rìísí Láti 2026-05-05

#### 1. Lílo AES-GCM IV Lẹ̀ẹ̀kan Sí I — Ti Ṣàtúnṣe

**Fáìlì:** `Models/Main_Objects/Nonce.cs`

A ti tún kọ ìṣẹ̀dá nonce pátápátá kúrò ní AES-GCM. `Nonce.GenerateSecureNonce()` ń pe `RandomNumberGenerator.Fill(randomBytes)` fún 16 bytes àkóbátan báyìí, ó sì padà pẹ̀lú ọ̀rọ̀ Base64. Kò sí ìgbọràn Key Vault, kò sí IV, kò sí ìsọdipúpọ̀ — èyí jẹ́ ọ̀nà tó tọ́ fún nonce CSP.

---

#### 2. Iye Nonce Kò Si Kọ Sí Àwọn Ìgbàsilẹ̀ — Ti Ṣàtúnṣe

**Àwọn Fáìlì:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Àwọn fáìlì méjèéjì ń kọ àwọn ìfiránṣẹ̀ ipò nìkan (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) wọn kò ì kọ iye nonce fúnrarẹ̀ rárá.

---

#### 3. Àwọn Nonce Àdìtú Àkíndìí Tí A Yọ Kúrò — Ti Ṣàtúnṣe

**Fáìlì:** `Services/OptimizedNonceMiddleware.cs`

Àwọn ọ̀rọ̀ pàtó mẹ́ta (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) ti rọ́pò pẹ̀lú àwọn ìpè sí `Nonce.GenerateSecureNonce()` nínú àwọn ọ̀nà tó ṣe déédéé àti àwọn àmì àṣìṣe àdìtú méjì.

---

#### 4. Àtọjọ Nonce Tó Ní Ìdáàbòbò Fún Àwọn Ẹ̀ka — Ti Ṣàtúnṣe

**Fáìlì:** `Services/NonceCatalogService.cs`

A ti yí `Dictionary<string, Nonce>` padà sí `ConcurrentDictionary<string, Nonce>`. `GetANonce` ń lò ìpè `TryGetValue` ọ̀kan tó jẹ́ àdánilọ́kàn nísinsìnyí dípò ìgbógun ìgbésẹ̀-méjì.

---

#### 5. Àwọn Jẹ́rìísí Onísèlú mTLS Ń Ṣiṣẹ́ Báyìí — Ti Ṣàtúnṣe

**Àwọn Fáìlì:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

A ti rọ́pò ìyípadà jẹ́rìísí onísèlú tí a kọ pàtó pẹ̀lú ìpè `mtlsSettings.IsIssuerAllowed(issuer)`, èyí tó ń ṣe ìfiwéra ọ̀rọ̀ tó kò ṣàánú ìhun-jẹjẹ sí `AllowedIssuers`. Tí àkójọ bá ṣòfò (kò ṣètò), ọ̀nà padà pẹ̀lú `false`, kò gba gbogbo ìwé-ẹ̀rí (kò ṣí bí ó bá kùnà).

---

#### 6. Ìgbógun Ìfagile mTLS Ṣiṣẹ́ Nípa Àìpéye Báyìí — Ti Ṣàtúnṣe

**Fáìlì:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` jẹ́ àìpéye `true` báyìí. `appsettings.template.json` tún ṣètò `"CheckCertificateRevocation": true`.

---

#### 7. Stub OCSP Ṣì Ní Kúnà Báyìí — Ti Ṣàtúnṣe

**Fáìlì:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` ń padà pẹ̀lú `IsValid = false` àti `OcspStatus.Error` báyìí ó sì ń kọ àṣìṣe náà, dípò ipadà ìdákẹ́jẹ́ẹ́ sí `IsValid = true`. Ìmúṣiṣẹ́ OCSP yóò kọ gbogbo ìwé-ẹ̀rí títí tí ìmúṣẹ gidi bá wà.

---

#### 8. Jẹ́rìísí àti Àṣẹ Ṣiṣẹ́ Nípa Àìpéye Báyìí — Ti Ṣàtúnṣe

**Fáìlì:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` àti `EnableAuthorization` méjèéjì jẹ́ àìpéye `true` nísinsìnyí nínú kíláàsì `FeatureFlags`. `appsettings.json` tún ṣètò méjèéjì sí `true`.

---

#### 9. Àwọn Àkọlé Ààbò Ti Wọ Sínú Àlàfo Ìṣẹ̀dá Tóótọ́ — Ti Ṣàtúnṣe

**Fáìlì:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` àti `UseStandardSecurityHeaders` ń pè báyìí kí `UseRouting`, `UseAuthentication`, àti `UseAuthorization` tó pè. Gbogbo àwọn ìdáhùn, títí pẹ̀lú àwọn ìdáhùn 401/403 tó kúrú, yóò gba àwọn àkọlé ààbò wọn.

---

#### 10–15. Àwọn Kúkì, Content-Type, AllowedHosts, Nonce nínú Àdàkọ, Referrer-Policy — Ti Ṣàtúnṣe

**Àwọn Fáìlì:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Àwọn kúkì iṣẹ̀ ti ṣètò sí `CookieSecurePolicy.Always` àti `SameSiteMode.Strict` báyìí.
- Àkọlé `Set-Cookie` àgbáyé tí a kò fẹ́ tí a fi sọ gbẹ̀ sínú ti yọ kúrò.
- Ìyípadà àgbáyé `Content-Type: text/html` ti yọ kúrò.
- `AllowedHosts` nínú `appsettings.json` jẹ́ `"localhost;127.0.0.1"` báyìí; àdàkọ ń lò `"{{YOUR_HOSTNAME}}"`.
- Gbogbo àwọn àmì `<script>` mẹ́ta nínú `_Layout.cshtml` ní `nonce="@Context.Items["Nonce"]"` báyìí.
- `Referrer-Policy: strict-origin-when-cross-origin` ti fi kún nípasẹ̀ `UseStandardSecurityHeaders`.

---

#### 16–19. PII nínú Àwọn Ìgbàsilẹ̀, Ìpọ̀pọ̀ Ọ̀rọ̀, Key Vault Stubs, X-XSS-Protection — Ti Ṣàtúnṣe

**Àwọn Fáìlì:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Gbogbo PII (OID, íméèlì, orúkọ, SID, ipa) ti jẹ́ HMAC-SHA256 hash nípasẹ̀ `LoggingHelper.HashPii()` ṣáájú kíkọ sí àwọn ìgbàsilẹ̀. Kọnkọ HMAC tó dúró tán lè pèsè nípasẹ̀ `Logging:PiiHmacKey` nínú àdàpò; kọnkọ fún ọ̀kọ̀ọ̀kan àkọ̀lé ni a lò tí a kò bá ṣètò.
- Ìfiránṣẹ̀ ìgbàsilẹ̀ Cosmos DB ń jẹ́rìísí pé ọ̀rọ̀ ìpọ̀pọ̀ wà nìkan (`!string.IsNullOrEmpty`), kì í ṣe ohun tó wà nínú.
- `AzureKeyVaultCertificateOperations` ń jù `InvalidOperationException` nísinsìnyí nígbà ìmúṣẹ̀lú tí ìwé-ẹ̀rí jẹ́ null, dípò ìpadà ìdákẹ́jẹ́ẹ́ sí àwọn iye metadata.
- `X-XSS-Protection` ti ṣètò sí `"0"` (gbádùn tó ń kó fíltà XSS àtijọ́ dúró), tó bá àwọn ìtọ́sọ́nà ìmọ̀ẹ̀rọ̀ tuntun mu.

---

## 🟠 Gíga

### 20. NonceRefresherService Ń Pa Àwọn Ìgbọràn Àkọ̀tọ Key Vault Tí A Kò Lò

**Fáìlì:** `Services/NonceRefresherService.cs`

`NonceRefresherService` ń fi àwọn ìjókòó àkọ̀tọ fún `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, àti `IAzureKeyVaultOperationsService`. Nítorí ìṣẹ̀dá nonce ti jẹ́ ìrọrùn fún lílo `RandomNumberGenerator` tààrà, kò sí ìgbọràn tí a ń lò.

**Ìṣòro:** Nígbà `EnableNonceServices = true` àti `EnableKeyVault = false` (àìpéye), àwọn iṣẹ́ wọ̀nyí kò ṣe àdéhùn nínú apoti DI, tó ń fa `InvalidOperationException` nígbà ìbẹ̀rẹ̀ tí iṣẹ́ nonce bá ṣe yanjú. Ipò àìrí-iṣẹ́ yìí ni àdàpò àìpéye ń fa. Kíláàsì `FeatureFlags` ṣètò àìpéye `EnableNonceServices = true`, nítorí náà àyíká tó bá gbẹ́kẹ̀lé àwọn àìpéye kíláàsì nìkan (láìsí àwọn àyípadà `appsettings.json`) yóò kùnà.

**Ìfọkànsín:** Yọ àwọn ìjókòó àkọ̀tọ mẹ́rin tí a kò lò àti àwọn pápá àkọ̀kọ̀ tó bá wọn mu kúrò ní `NonceRefresherService`. Iṣẹ́ náà nílò `ILogger<NonceRefresherService>`, `ILoggerFactory`, àti `INonceCatalogService` nìkan.

---

## 🟡 Àárín

### 21. Àpamọ́ Irántí OcspValidationService Ń Lò Dictionary Tó Kò Ní Ìdáàbòbò Fún Àwọn Ẹ̀ka

**Fáìlì:** `Services/OcspValidationService.cs` (ìlà 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` kò ní ìdáàbòbò fún kíkà àti kíkọ ní àkókò kan náà. Tí `OcspValidationService` bá ṣe àdéhùn gẹ́gẹ́ bi singleton (tàbí tí ìṣẹ̀lẹ̀ kan náà bá pín káàkiri àwọn ìbéèrè), àwọn jẹ́rìísí OCSP alájọgbẹpọ̀ lè bà àpamọ́ jẹ́, tó ń fa àwọn ìwọlé tí a pàdánù, àwọn ìjù tí a kò retí, tàbí ìpadà àwọn ìsọfúnni àtijọ́.

**Ìfọkànsín:** Rọ́pò `Dictionary<string, CachedOcspResponse>` pẹ̀lú `ConcurrentDictionary<string, CachedOcspResponse>`. Ṣe ìmúdójúìwọ̀n ìpè `_cache.Remove` (ìlà 103) sí `_cache.TryRemove`.

---

## 🔵 Kékeré / Ìsọfúnni

### 22. Stub Jẹ́rìísí OCSP — Kò Ṣí Bí Ó Bá Kùnà Ṣùgbọ́n Kò Ní Ìmúṣẹ

**Fáìlì:** `Services/OcspValidationService.cs` (àwọn ìlà 157–173)

`PerformOcspValidationAsync` ṣì jẹ́ stub. Àtúnṣe ìdámọ̀ #7 yí ìhùwàsí padà lásàn láti "ní òdódó nígbà gbogbo" sí "àìní òdódó nígbà gbogbo (kò ṣí bí ó bá kùnà)". Bí ó ti wà, kò ṣe ìmúṣẹ gidi OCSP. Pẹ̀lú `EnableOcspValidation = false` (àìpéye), kò sí ipa lórí ìṣòwò. Ṣáájú ìmúṣiṣẹ́ OCSP ní àyíká kankan, gbọdọ̀ ṣe ìmúṣẹ gidi ìpò OCSP.

---

### 23. mTLS Pẹ̀lú AllowedIssuers Òfo Ń Kọ Gbogbo Àwọn Ìwé-Ẹ̀rí Aláàbò

**Fáìlì:** `Models/Settings/MtlsSettings.cs`

Nígbà tó bá jẹ́ `ValidateClientCertificateIssuer = true` (àìpéye) àti `AllowedIssuers` ṣòfò (tún jẹ́ àìpéye tí a kò bá ṣètò), `IsIssuerAllowed()` ń padà pẹ̀lú `false`, kò gba gbogbo ìwé-ẹ̀rí aláàbò. Ìhùwàsí kò-ṣí-bí-ó-bá-kùnà tó fẹ́ ni èyí, ṣùgbọ́n a kò kọ rẹ̀ síjọ. Àwọn oníṣiṣẹ́ tó ń mú mTLS ṣiṣẹ́ láìkà àdàkọ fínnífínní lè rí pé a kọ gbogbo ìwé-ẹ̀rí aláàbò láìsí ìṣàlàyé tó hàn.

**Ìfọkànsín:** Fi ìkilọ̀ ìgbàsilẹ̀ ìbẹ̀rẹ̀ kún nígbà `ValidateClientCertificateIssuer = true` àti `AllowedIssuers` ṣòfò.

---

### 24. OcspSettings.ServerUnavailableBehavior Àìpéye "Warn"

**Fáìlì:** `appsettings.template.json` (ìlà 134), `Services/OcspValidationService.cs`

Iye àìpéye `ServerUnavailableBehavior` jẹ́ `"Warn"` nínú àdàkọ, tó gba àwọn ìbéèrè láàyè láti gba ọ̀nà tí iṣẹ́ OCSP kò bá lè rí. Fún àwọn àyíká ààbò gíga, èyí gbọdọ̀ jẹ́ `"Fail"` láti ṣe ìdènà àìrí-iṣẹ́ OCSP láti yí ìgbógun ìfagile ìwé-ẹ̀rí padà ní ìdákẹ́jẹ́ẹ́.

**Ìfọkànsín:** Ṣàkọsílẹ̀ gbogbo àwọn àṣàyàn mẹ́ta (`Fail`, `Allow`, `Warn`) ní kedere nínú àdàkọ àti gbé ìyípadà àìpéye sí `"Fail"` lé nínú ọkàn ní ìbamu pẹ̀lú ìlànà ẹ̀tọ́ tó kéré jù.

---

## Ìṣirò Àwọn Àkọlé Ààbò (Ipò Lọ́wọ́lọ́wọ́)

`UseStandardSecurityHeaders` fi àwọn àkọlé wọ̀nyí kún:

| Àkọlé | Iye | Ìṣirò |
|-------|-----|--------|
| `X-Frame-Options` | `DENY` | ✅ Dára |
| `X-XSS-Protection` | `0` | ✅ Dára (gbádùn fíltà XSS àtijọ́) |
| `X-Content-Type-Options` | `nosniff` | ✅ Dára |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Dára |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Dára |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Dára |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Dára |
| `Permissions-Policy` | geolocation, camera, microphone, interest-cohort gbádùn | ✅ Dára |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Dára |
| `Content-Security-Policy` | Gíga-Nonce, tí a fi kún nígbà CSP mú ṣiṣẹ́ | ✅ Dára |
| `Server` | Pa mọ́ sí `"webserver"` | ✅ Dára |
| `X-Powered-By` | Yọ kúrò | ✅ Dára |

---

## Ìṣirò Àpapọ̀

Ohun èlò náà ti ṣàtúnṣe gbogbo àwọn ailera pàtàkì àti gíga-ìjìnnà láti àyẹ̀wò tẹ́lẹ̀. Àwọn ìdámọ̀ lọ́wọ́lọ́wọ́ ti dín mọ́ ìṣòro kan ṣoṣo àdàpò/DI gíga-ìjìnnà (ìdámọ̀ #20) àti àwọn nkan ìsọfúnni kékeré. Ipò ààbò ti dára sí i lọ nìṣó. A ń gbani níyànjú fún ìgbésẹ̀ tó yára fún ìdámọ̀ #20 (àwọn ìgbọràn DI tí a kò lò nínú NonceRefresherService) nítorí pé ó lè gbógun ìbẹ̀rẹ̀ ohun èlò náà lábẹ́ àdàpò àìpéye.
