# Mapitio ya Usalama — WebAppExperimental266

**Tarehe:** 2026-05-05  
**Wigo:** Uchambuzi tuli wa codebase nzima  

---

## Jedwali la Muhtasari

| # | Eneo | Ukali |
|---|------|----------|
| 1 | Kutumia tena IV ya AES-GCM katika uzalishaji wa nonce | 🔴 Muhimu Sana ✅ |
| 2 | Nonce imeandikwa kwenye kumbukumbu kama plaintext | 🔴 Muhimu Sana ✅ |
| 3 | Hardcoded fallback nonce strings | 🔴 Muhimu Sana ✅ |
| 4 | Global nonce dictionary isiyo thread-safe | 🟠 Juu |
| 5 | Uthibitishaji wa issuer wa mTLS umewekwa kama comment | 🟠 Juu |
| 6 | Ukaguzi wa revocation wa mTLS umezimwa kwa chaguo-msingi | 🟠 Juu |
| 7 | OCSP daima hurudisha valid (stub) | 🟠 Juu |
| 8 | Auth/authz zimezimwa kwa chaguo-msingi katika config | 🟠 Juu |
| 9 | Security headers zinatumika kuchelewa sana katika pipeline | 🟠 Juu |
| 10 | Session cookie haina Secure + SameSite | 🟡 Wastani |
| 11 | Global `Set-Cookie` header yenye muundo mbovu | 🟡 Wastani |
| 12 | `Content-Type` inalazimishwa kuwa `text/html` kila mahali | 🟡 Wastani |
| 13 | `AllowedHosts` ni wildcard | 🟡 Wastani |
| 14 | Nonce haitumiki kwenye `<script>` tags katika layout | 🟡 Wastani |
| 15 | Kichwa cha Referrer-Policy hakipo | 🟡 Wastani |
| 16 | PII imeandikwa kwenye kumbukumbu kama plaintext | 🔵 Chini |
| 17 | Sehemu ya connection string iko kwenye kumbukumbu | 🔵 Chini |
| 18 | Operesheni za Key Vault ni stubs | 🔵 Chini |
| 19 | Kichwa cha `X-XSS-Protection` kimepitwa na wakati | 🔵 Chini |

---

## 🔴 Muhimu Sana

### 1. Kutumia Tena IV ya AES-GCM — Uzalishaji wa Nonce Umevunjika Kicryptography ✅ Imerekebishwa katika commit 45ae31b

**Faili:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

Usimbaji wa AES-GCM unaozalisha CSP nonces hutumia **IV isiyobadilika inayorejeshwa kutoka Key Vault kwa kila mwito**. AES-GCM huvunjika IV inapotumika tena na key ileile: mshambuliaji anayeona ciphertext mbili anaweza kufanya XOR yao ili kupata XOR ya plaintexts, na authentication tags zinaweza kughushiwa.

Marekebisho ni ya moja kwa moja — CSP nonces hazihitaji usimbaji hata kidogo. Nonce ya CSP inahitaji tu kuwa **isiyotabirika na ya kipekee kwa kila request**; mwito wa `RandomNumberGenerator.GetBytes(16)` unaobadilishwa kuwa Base64 unatosha na ni sahihi.

---

### 2. Thamani za Nonce Ziliandikwa Kwenye Kumbukumbu kama Plaintext ✅ Imerekebishwa katika commit bb6f27a

**Faili:** `Services/NonceMiddleware.cs` (mstari wa 31), `Services/NonceRefresherService.cs` (mstari wa 82)

Nonce ya CSP iliyozalishwa huandikwa moja kwa moja kwenye kumbukumbu za programu:

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

Mtu yeyote mwenye ufikiaji wa kumbukumbu hupata nonce halali na anaweza kupita CSP kwa urahisi ili kuingiza inline scripts.

---

### 3. Hardcoded Fallback Nonces ✅ Imerekebishwa katika commit 11cc9f7

**File:** `Services/OptimizedNonceMiddleware.cs` (mistari ya 53, 78, 92)

Ikiwa uzalishaji wa nonce unashindwa au nonce catalog iko tupu, middleware hurudi kwenye string literals `"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, na `"error-fallback-nonce"`. Strings hizi zimewekwa kwenye source code na zinajulikana kwa washambuliaji. Hali ya kosa (kwa mfano, Key Vault isipatikane) ingeweka nonce inayotabirika na inayoweza kutumiwa vibaya kwenye kichwa cha CSP.

---

## 🟠 Juu

### 4. `NonceCatalogService` Hutumia Static Dictionary Isiyo Thread-Safe ✅ Imerekebishwa katika commit ae2b6c9

**File:** `Services/NonceCatalogService.cs` (mstari wa 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` si thread-safe kwa usomaji na uandishi wa wakati mmoja. Chini ya mzigo, requests mbili zinazoshindania kusasisha nonce key ileile zinaweza kusababisha uharibifu wa data au exceptions. Nonce catalog pia ni singleton (kimsingi ni global), ikimaanisha nonce ya request moja inaweza kuandikwa juu yake na request nyingine katikati ya safari — nonce collision kati ya requests. Tumia `ConcurrentDictionary` na hifadhi nonces kwa kila request katika `HttpContext.Items` badala ya global inayoshirikiwa.

---

### 5. Uthibitishaji wa Issuer wa Cheti cha mTLS Umewekwa kama Stub ✅ Imerekebishwa katika commit fd3d4fb

**File:** `Extensions/ServiceCollectionExtensions.cs` (mistari ya 305–313)

Setting ya `ValidateClientCertificateIssuer` ipo na ni `true` kwa chaguo-msingi, lakini msimbo halisi wa uthibitishaji umewekwa kama comment:

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

mTLS inapowezeshwa, client certificate yoyote kutoka issuer yeyote (inayochaini hadi trusted root) inaweza kuthibitishwa — hakuna kizuizi cha tenant/issuer kinachotumika.

---

### 6. Ukaguzi wa Revocation wa Cheti cha mTLS Umezimwa kwa Chaguo-Msingi ✅ Imerekebishwa katika commit fd3d7b3

**Faili:** `Models/Settings/MtlsSettings.cs` (mstari wa 26), `appsettings.template.json`

`CheckCertificateRevocation` ina default ya `false` katika model na template zote mbili. Client certificates zilizofutwa zinaweza kutumiwa kuthibitisha kwa muda usiojulikana. Kwa production mTLS, ukaguzi wa revocation unapaswa kuwezeshwa kwa chaguo-msingi.

---

### 7. Uthibitishaji wa OCSP ni Stub Inayorudisha Valid Daima ✅ Imerekebishwa katika commit b4c3807

**File:** `Services/OcspValidationService.cs` (mistari ya 149–163)

Method ya `PerformOcspValidationAsync` imeelezwa wazi kuwa ni "template implementation" inayorudisha `IsValid = true` baada ya `Task.Delay(100)`. Uthibitishaji wa OCSP ukiwezeshwa kwenye config, utapitisha kimya kimya vyeti vyote — hata vilivyofutwa — kama halali, huku ukiandika onyo ambalo ni rahisi kukosa.

---

### 8. Authentication na Authorization Zimezimwa kwa Chaguo-Msingi ✅ Imerekebishwa katika commit b392c47

**File:** `appsettings.json` (mistari ya 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

Usanidi wa default husafirishwa bila authentication au authorization. Msanidi anayenakili `appsettings.template.json` (ambayo pia imezima mambo haya) bila kusoma hati kwa makini atasambaza programu iliyo wazi. Defaults za template zinapaswa kuhitaji opt-out ya makusudi, si opt-in.

---

### 9. Security Headers Zinatumika Baada ya Routing/Auth ✅ Imerekebishwa katika commit 016e57c

**File:** `Program.cs` (mistari ya 130–152)

`UseNonceAndSecurityHeadersAsync` na `UseStandardSecurityHeaders` zinaitwa baada ya `UseRouting`, `UseAuthentication`, na `UseAuthorization`. Responses zinazokatisha pipeline kabla ya kufika middleware hizi (kwa mfano, 401 redirects, 403 denials) zinaweza kutopokea security headers. Security headers zinapaswa kuwa mapema iwezekanavyo katika pipeline.

---

## 🟡 Wastani

### 10. Session Cookie Haina `Secure` na `SameSite` Attributes ✅ Imerekebishwa katika commit 8f2223c

**File:** `Extensions/ServiceCollectionExtensions.cs` (mistari ya 41–46)

Session cookie huweka `HttpOnly = true` na `IsEssential = true`, lakini huacha `Cookie.SecurePolicy = CookieSecurePolicy.Always` na `Cookie.SameSite = SameSiteMode.Strict`. Cookie inaweza kutumwa kupitia HTTP ya kawaida (ikiwa redirect bado haijafanyika) au kutumwa cross-site.

---

### 11. Global `Set-Cookie` Header Yenye Muundo Mbovu ✅ Imerekebishwa katika commit 8f2223c

**File:** `Extensions/ApplicationBuilderExtensions.cs` (mstari wa 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

Hii huongeza kichwa cha `Set-Cookie` kisicho na jina wala value kwenye kila response. Hii si halali na itapuuzwa (au kukataliwa) na browsers, lakini inazalisha vitu vya kushangaza katika responses zote ikijumuisha faili statiki, JSON API responses, na health checks. Usalama wa cookie unapaswa kuwekwa katika cookie options za cookie mahususi inayosanidiwa, si kudungwa kama raw header kimataifa.

---

### 12. `Content-Type` Imewekwa kwa Lazima kuwa `text/html` kwa Responses Zote ✅ Imerekebishwa katika commit 8f2223c

**File:** `Extensions/ApplicationBuilderExtensions.cs` (mstari wa 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

Hii huandika juu ya Content-Type kwa kila response — API endpoints, JSON, binary downloads, na faili statiki zote zitasema ni `text/html`. Hii inakinzana na `X-Content-Type-Options: nosniff`, ambayo huzuia browsers kubadilisha content type iliyotangazwa.

---

### 13. `AllowedHosts` Imewekwa kwa Wildcard ✅ Imerekebishwa katika commit 8f2223c

**Faili:** `appsettings.json` (mstari wa 11), `appsettings.template.json` (mstari wa 36)

```json
"AllowedHosts": "*"
```

Hii huzima uthibitishaji wa built-in wa host header katika ASP.NET Core. Mashambulizi ya host header injection huruhusu cache poisoning, password-reset link poisoning, na open redirects. Hii inapaswa kuwekwa kwa domain(s) mahususi ambazo programu inatolewa nazo.

---

### 14. Layout Haitumii Nonce kwenye `<script>` Tags ✅ Imerekebishwa katika commit 8f2223c

**File:** `Views/Shared/_Layout.cshtml`

Layout hupakia faili kadhaa za JavaScript (`jquery.min.js`, `bootstrap.bundle.min.js`, `site.js`) lakini hakuna hata moja ya `<script>` tags yenye `nonce="@Context.Items["Nonce"]"`. CSP yenye nonces ikiwezeshwa, scripts hizi zingezuiwa na browser. Utekelezaji wa nonce umeunganishwa katika middleware lakini hautumiki kwenye views, na kufanya mfumo wa CSP nonce usiwe na maana.

---

### 15. Kichwa cha Referrer-Policy Hakipo ✅ Imerekebishwa katika commit 8f2223c

**File:** `Extensions/ApplicationBuilderExtensions.cs`

Security headers za kawaida hazijumuishi `Referrer-Policy`. Bila hii, browser hutuma URL kamili katika kichwa cha `Referer` kwa third-party resources (kwa mfano, ArcGIS CDN iliyojumuishwa katika CSP), jambo ambalo linaweza kufichua paths za authenticated session.

---

## 🔵 Chini / Taarifa

### 16. PII Imeandikwa Kwenye Kumbukumbu kama Plaintext ✅ Imerekebishwa katika commit 93bb4e9

**File:** `Services/LoggingHelper.cs` (mistari ya 85, 105)

OID ya mtumiaji, email, jina, session ID, na roles huandikwa moja kwa moja kwenye kumbukumbu kwa kila authenticated request:

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

Kulingana na kanuni za faragha zinazotumika (GDPR, CCPA, HIPAA), hili linaweza kuwa tatizo la compliance. Fikiria kuficha au kufanya hash ya vitambulisho katika output ya kumbukumbu na kuelekeza kumbukumbu zenye PII kwenye sink yenye udhibiti unaofaa. Lengo la forensic session reconstruction linaweza kuhifadhiwa kwa kuandika HMAC-SHA256 hashes thabiti za vitambulisho badala ya plaintext values zake.

---

### 17. Sehemu ya Connection String Iko Kwenye Kumbukumbu ✅ Imerekebishwa katika commit 93bb4e9

**File:** `Extensions/ServiceCollectionExtensions.cs` (mstari wa 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

Hata sehemu ya secret kwenye kumbukumbu si mbinu bora. Kauli ya kumbukumbu inapaswa badala yake kuthibitisha kuwa connection string ipo (sio tupu) badala ya kuandika sehemu yoyote yake.

---

### 18. Operesheni za Key Vault ni Stubs ✅ Imerekebishwa katika commit 93bb4e9

**File:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

`GetCertificateFromKeyVault` na `GetSecretFromKeyVault` zote ni template stubs zinazorejesha `null`/dummy values. Key Vault ikiwezeshwa, `GetCertificateFromKeyVault` hurudisha `null`, jambo linalosababisha `InvalidOperationException` wakati wa startup — kushindwa mapema ni vizuri, lakini pia inamaanisha hakuna ushirikiano halisi wa Key Vault wa kukaguliwa kwa secret handling.

---

### 19. `X-XSS-Protection: 1; mode=block` Imepitwa na Wakati ✅ Imerekebishwa katika commit 93bb4e9

**File:** `Extensions/ApplicationBuilderExtensions.cs` (mstari wa 70)

Browsers za kisasa zimeondoa msaada kwa `X-XSS-Protection`. Kichwa hiki si hatari, lakini hutoa hisia ya uongo ya usalama. Mbinu inayopendekezwa ni kutegemea CSP imara badala yake. Value ya `0` (kuzima XSS auditor) wakati mwingine huchukuliwa kuwa salama zaidi kuliko `1; mode=block` kwa browsers za zamani kwa sababu auditor yenyewe ilikuwa na tabia zinazoweza kutumiwa vibaya.

