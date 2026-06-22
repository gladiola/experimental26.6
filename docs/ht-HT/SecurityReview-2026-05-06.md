# Revizyon Sekirite — WebAppExperimental266

**Dat:** 2026-05-06
**Pòte:** Analiz estatik konplè baz kòd la (swivi revizyon 2026-05-05)
**Revizè:** Revizyon Sekirite Otomatik

---

## Rezime Egzekitif

Revizyon swivi sa a konfime ke tout 19 fay ki te idantifye nan revizyon sekirite 2026-05-05 yo te korije. Revizyon an idantifye tou 5 nouvèl rezilta oswa rezidyèl ki te dekouvri pandan sesyon sa a. Estati sekirite jeneral aplikasyon an amelyore konsiderableman depi revizyon anvan an.

---

## Estati Rezilta Anvan yo (2026-05-05)

Tout 19 rezilta anvan yo **konfime kòm korije**:

| # | Rezilta | Severite | Estati |
|---|---------|----------|--------|
| 1 | Reutilizasyon IV AES-GCM nan jenerasyon nonce | 🔴 Kritik | ✅ Korije |
| 2 | Nonce anrejistre nan tèks klè | 🔴 Kritik | ✅ Korije |
| 3 | Chèn nonce repons anba yo kode di | 🔴 Kritik | ✅ Korije |
| 4 | Diksyonè nonce global ki pa sekirize pou fil | 🟠 Wo | ✅ Korije |
| 5 | Validasyon emètè mTLS kòmante | 🟠 Wo | ✅ Korije |
| 6 | Tchèk revokasyon mTLS dezaktive pa defò | 🟠 Wo | ✅ Korije |
| 7 | OCSP toujou retounen valid (stub) | 🟠 Wo | ✅ Korije |
| 8 | Otantifikasyon/otorisasyon dezaktive pa defò nan konfigirasyon | 🟠 Wo | ✅ Korije |
| 9 | Antèt sekirite aplike twò ta nan pipeline a | 🟠 Wo | ✅ Korije |
| 10 | Cookie sesyon manke `Secure` + `SameSite` | 🟡 Mwayèn | ✅ Korije |
| 11 | Antèt `Set-Cookie` global mal fòmate | 🟡 Mwayèn | ✅ Korije |
| 12 | `Content-Type` fòse a `text/html` toupatou | 🟡 Mwayèn | ✅ Korije |
| 13 | `AllowedHosts` mete sou wildcard | 🟡 Mwayèn | ✅ Korije |
| 14 | Nonce pa aplike sou baliz `<script>` nan layout a | 🟡 Mwayèn | ✅ Korije |
| 15 | Antèt `Referrer-Policy` manke | 🟡 Mwayèn | ✅ Korije |
| 16 | PII anrejistre nan tèks klè | 🔵 Ba | ✅ Korije |
| 17 | Chèn koneksyon pasyèl nan jounal yo | 🔵 Ba | ✅ Korije |
| 18 | Operasyon Key Vault yo se stubs | 🔵 Ba | ✅ Korije |
| 19 | `X-XSS-Protection: 1; mode=block` ki depase | 🔵 Ba | ✅ Korije |

---

## Nouvèl Rezilta / Rezidyèl

| # | Zòn | Severite |
|---|-----|----------|
| 20 | NonceRefresherService kenbe depandans konstriktè Key Vault ki pa itilize | 🟠 Wo |
| 21 | Kach entèn OcspValidationService itilize Dictionary ki pa sekirize pou fil | 🟡 Mwayèn |
| 22 | Stub validasyon OCSP toujou la — echwe fèmen men pa enplemante | 🔵 Ba |
| 23 | mTLS ak AllowedIssuers vid rejte tout sètifika (fail-closed, pa dokimante) | 🔵 Ba |
| 24 | OcspSettings.ServerUnavailableBehavior defò sou "Warn" (pèmèt pase lè gen erè) | 🔵 Ba |

---

## Rezilta Detaye

### ✅ Koreksyon Konfime depi 2026-05-05

#### 1. Reutilizasyon IV AES-GCM — Korije

**Fichye:** `Models/Main_Objects/Nonce.cs`

Jenerasyon nonce ki baze sou AES-GCM ranplase konplètman. `Nonce.GenerateSecureNonce()` rele kounye a `RandomNumberGenerator.Fill(randomBytes)` sou 16 byte aleatwa epi retounen yon chèn Base64. Pa gen depandans Key Vault, pa gen IV, pa gen chifraj — egzakteman bon apwòch pou yon nonce CSP.

---

#### 2. Valè Nonce Pa Anrejistre Ankò — Korije

**Fichye:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

De fichye yo kounye a sèlman anrejistre mesaj estati (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) epi pa janm valè nonce tèt li.

---

#### 3. Nonces Repons Anba Kode Di Retire — Korije

**Fichye:** `Services/OptimizedNonceMiddleware.cs`

Twa chèn literal kode di yo (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) ranplase ak rele `Nonce.GenerateSecureNonce()` nan de chemen nòmal ak repons eksepsyon yo.

---

#### 4. Diksyonè Nonce Sekirize pou Fil — Korije

**Fichye:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` ranplase ak `ConcurrentDictionary<string, Nonce>`. `GetANonce` kounye a itilize yon sèl rele atomik `TryGetValue` olye de yon verifikasyon de etap.

---

#### 5. Validasyon Emètè mTLS Kounye a Fonksyonèl — Korije

**Fichye:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Blòk validasyon emètè kòmante a ranplase pa yon rele `mtlsSettings.IsIssuerAllowed(issuer)`, ki fè yon matche sou-chèn ki pa sansib ak majiskil/miniskil kont `AllowedIssuers`. Lè lis la vid (pa konfigire), metòd la retounen `false`, rejte tout sètifika (fail-closed).

---

#### 6. Tchèk Revokasyon mTLS Aktive pa Defò — Korije

**Fichye:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` kounye a defò sou `true`. `appsettings.template.json` espesifye tou `"CheckCertificateRevocation": true`.

---

#### 7. Stub OCSP Kounye a Echwe Fèmen — Korije

**Fichye:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` kounye a retounen `IsValid = false` ak `OcspStatus.Error` epi anrejistre yon erè, olye retounen silansyèzman `IsValid = true`. Aktive OCSP nan konfigirasyon kounye a va rejte tout sètifika jiskaske yon enplemantasyon reyèl ba yo.

---

#### 8. Otantifikasyon ak Otorisasyon Aktive pa Defò — Korije

**Fichye:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` ak `EnableAuthorization` tou de kounye a defò sou `true` nan klas `FeatureFlags` la. `appsettings.json` mete tou de sou `true` tou.

---

#### 9. Antèt Sekirite Aplike Anvan Routage — Korije

**Fichye:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` ak `UseStandardSecurityHeaders` kounye a rele anvan `UseRouting`, `UseAuthentication`, ak `UseAuthorization`. Tout repons, enkli kout-sikwi 401/403, resevwa antèt sekirite yo.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce nan Layout, Referrer-Policy — Korije

**Fichye:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Cookie sesyon kounye a mete `CookieSecurePolicy.Always` ak `SameSiteMode.Strict`.
- Antèt `Set-Cookie` san non ki mal fòmate a retire.
- Ranplasman global `Content-Type: text/html` retire.
- `AllowedHosts` nan `appsettings.json` kounye a se `"localhost;127.0.0.1"`; modèl la itilize `"{{YOUR_HOSTNAME}}"`.
- Twa baliz `<script>` yo nan `_Layout.cshtml` kounye a gen `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` kounye a ajoute pa `UseStandardSecurityHeaders`.

---

#### 16–19. Jounal PII, Jounal Chèn Koneksyon, Stubs Key Vault, X-XSS-Protection — Korije

**Fichye:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Tout PII (OID, imèl, non, SID, wòl) kounye a hash HMAC-SHA256 via `LoggingHelper.HashPii()` anvan yo ekri nan jounal yo. Yon kle HMAC estab ka ba yo via `Logging:PiiHmacKey` nan konfigirasyon; yon kle aleatwa pa pwosesis itilize lè pa konfigire.
- Deklarasyon jounal Cosmos DB kounye a sèlman konfime si yon chèn koneksyon prezan (`!string.IsNullOrEmpty`), pa kontni li.
- `AzureKeyVaultCertificateOperations` kounye a voye `InvalidOperationException` nan demaraj lè sètifika a nil, olye retounen silansyèzman valè faktis.
- `X-XSS-Protection` kounye a mete sou `"0"` (dezaktive oditè XSS ki depase a), konsistan ak gid navigatè modèn yo.

---

## 🟠 Wo

### 20. NonceRefresherService Kenbe Depandans Konstriktè Key Vault ki Pa Itilize

**Fichye:** `Services/NonceRefresherService.cs`

`NonceRefresherService` toujou deklare paramèt konstriktè pou `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, ak `IAzureKeyVaultOperationsService`. Depi jenerasyon nonce simpliye pou itilize `RandomNumberGenerator` dirèkteman, okenn nan depandans sa yo itilize.

**Risk:** Lè `EnableNonceServices = true` ak `EnableKeyVault = false` (defò a), sèvis sa yo pa anrejistre nan konteyné DI a, ki koze yon `InvalidOperationException` nan tan kouri lè sèvis nonce a rezoud pou premye fwa. Sa se efektivman yon kondisyon refize-sèvis deklanché pa konfigirasyon defò a. Klas `FeatureFlags` la defò `EnableNonceServices = true`, kidonk nenpòt anviwonnman ki konte sèlman sou defò klas yo (san ranplasman `appsettings.json`) ta echwe pou kòmanse.

**Rekòmandasyon:** Retire kat paramèt konstriktè ki pa itilize yo ak champ prive ki koresponn yo nan `NonceRefresherService`. Sèvis la sèlman bezwen `ILogger<NonceRefresherService>`, `ILoggerFactory`, ak `INonceCatalogService`.

---

## 🟡 Mwayèn

### 21. Kach Entèn OcspValidationService Itilize Dictionary ki Pa Sekirize pou Fil

**Fichye:** `Services/OcspValidationService.cs` (liy 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` pa sekirize pou fil pou lekti ak ekriti simultan yo. Si `OcspValidationService` anrejistre kòm yon singleton (oswa si menm enstans lan pataje ant demann pa nenpòt lòt mekanis), validasyon OCSP simultan ka koronp kach la, ki koze antena pèdi, eksepsyon voye, oswa done ki depase retounen.

**Rekòmandasyon:** Ranplase `Dictionary<string, CachedOcspResponse>` ak `ConcurrentDictionary<string, CachedOcspResponse>`. Mete ajou rele `_cache.Remove` (liy 103) a `_cache.TryRemove`.

---

## 🔵 Ba / Enfòmasyon

### 22. Stub Validasyon OCSP — Echwe Fèmen men Pa Enplemante

**Fichye:** `Services/OcspValidationService.cs` (liy 157–173)

`PerformOcspValidationAsync` toujou yon stub. Koreksyon rezilta #7 a chanje kòrèkteman konpòtman an soti nan "toujou valid" pou vin "toujou envalid (fail-closed)". Sepandan, metòd la toujou pa yon enplemantasyon OCSP reyèl. Toutotan `EnableOcspValidation = false` (defò), sa pa gen okenn enpak sou pwodiksyon. Anvan pou aktive OCSP nan nenpòt anviwonnman, yon kliyan OCSP kalite pwodiksyon dwe enplemante.

---

### 23. mTLS ak AllowedIssuers Vid Rejte Tout Sètifika Kliyan

**Fichye:** `Models/Settings/MtlsSettings.cs`

Lè `ValidateClientCertificateIssuer = true` (defò) ak `AllowedIssuers` vid (tou defò lè pa konfigire), `IsIssuerAllowed()` retounen `false`, ki koze tout sètifika kliyan rejte. Sa se bon konpòtman fail-closed, men li pa dokimante yon fason enpòtan. Operatè ki aktive mTLS san li modèl la ak anpil atansyon ka jwenn tout koneksyon kliyan rejte san eksplikasyon evidan.

**Rekòmandasyon:** Ajoute yon mesaj jounal avètisman nan demaraj lè `ValidateClientCertificateIssuer = true` ak `AllowedIssuers` vid.

---

### 24. OcspSettings.ServerUnavailableBehavior Defò sou "Warn"

**Fichye:** `appsettings.template.json` (liy 134), `Services/OcspValidationService.cs`

Paramèt `ServerUnavailableBehavior` defò sou `"Warn"` nan modèl la, ki pèmèt demann pase lè sèvè OCSP a pa ka jwenn. Pou anviwonnman wo sekirite, sa ta dwe `"Fail"` pou pannes sèvè OCSP pa degradé silansyèzman verifikasyon revokasyon sètifika.

**Rekòmandasyon:** Dokimante twa opsyon yo (`Fail`, `Allow`, `Warn`) klèman nan modèl la ak konsidere chanje defò a sou `"Fail"` pou matche ak prensip mwens privilèj.

---

## Evalyasyon Antèt Sekirite (Eta Aktyèl)

Antèt sa yo kounye a aplike via `UseStandardSecurityHeaders`:

| Antèt | Valè | Evalyasyon |
|-------|------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bon |
| `X-XSS-Protection` | `0` | ✅ Bon (dezaktive oditè ki depase a) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bon |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bon |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bon |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bon |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bon |
| `Permissions-Policy` | jeolokalizasyon, kamera, mikwofòn, interest-cohort dezaktive | ✅ Bon |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bon |
| `Content-Security-Policy` | Baze sou Nonce, aplike lè CSP aktive | ✅ Bon |
| `Server` | Maske sou `"webserver"` | ✅ Bon |
| `X-Powered-By` | Retire | ✅ Bon |

---

## Evalyasyon Jeneral

Aplikasyon an adrese tout fayblès grav ak wo-severite nan revizyon anvan an. Rezilta aktyèl yo limite a yon sèl pwoblèm konfigirasyon/DI wo-severite (rezilta #20) ak eleman enfòmasyon ba-severite. Pozisyon sekirite amelyore konsiderableman. Aksyon imedya rekòmande pou rezilta #20 (depandans DI ki pa itilize nan NonceRefresherService) paske li ka anpeche aplikasyon an kòmanse sou konfigirasyon defò.
