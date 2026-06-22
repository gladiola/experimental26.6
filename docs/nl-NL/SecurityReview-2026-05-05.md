# Beveiligingsbeoordeling — WebAppExperimental266

**Datum:** 2026-05-05  
**Reikwijdte:** Volledige statische analyse van de codebase

---

## Samenvattingstabel

| # | Gebied | Ernst |
|---|------|----------|
| 1 | Hergebruik van AES-GCM IV in nonce-generatie | 🔴 Kritiek ✅ |
| 2 | Nonce gelogd in plaintext | 🔴 Kritiek ✅ |
| 3 | Hardcoded fallback nonce-strings | 🔴 Kritiek ✅ |
| 4 | Niet-threadsafe globale nonce-dictionary | 🟠 Hoog |
| 5 | mTLS issuer-validatie uitgecommentarieerd | 🟠 Hoog |
| 6 | mTLS intrekkingscontrole standaard uit | 🟠 Hoog |
| 7 | OCSP retourneert altijd geldig (stub) | 🟠 Hoog |
| 8 | Auth/authz standaard uit in config | 🟠 Hoog |
| 9 | Security headers te laat in pipeline | 🟠 Hoog |
| 10 | Sessiecookie mist Secure + SameSite | 🟡 Gemiddeld |
| 11 | Foutieve globale Set-Cookie-header | 🟡 Gemiddeld |
| 12 | Content-Type overal geforceerd naar text/html | 🟡 Gemiddeld |
| 13 | AllowedHosts is wildcard | 🟡 Gemiddeld |
| 14 | Nonce niet toegepast op `<script>`-tags in layout | 🟡 Gemiddeld |
| 15 | Referrer-Policy-header ontbreekt | 🟡 Gemiddeld |
| 16 | PII gelogd in plaintext | 🔵 Laag |
| 17 | Gedeeltelijke connection string in logs | 🔵 Laag |
| 18 | Key Vault-ops zijn stubs | 🔵 Laag |
| 19 | Verouderde X-XSS-Protection-header | 🔵 Laag |

---

## 🔴 Kritiek

### 1. Hergebruik van AES-GCM IV — nonce-generatie cryptografisch kapot ✅ Gefixt in commit 45ae31b

**Bestanden:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

De AES-GCM-encryptie die CSP-nonces genereert gebruikt een **vaste IV die bij elke aanroep uit Key Vault wordt opgehaald**. AES-GCM breekt als de IV wordt hergebruikt met dezelfde sleutel: een aanvaller die twee ciphertexts ziet kan ze XOR'en om de XOR van de plaintexts te krijgen, en authenticatietags kunnen vervalst worden.

De fix is eenvoudig — CSP-nonces hebben geen encryptie nodig. Een CSP-nonce hoeft alleen **onvoorspelbaar en uniek per request** te zijn; `RandomNumberGenerator.GetBytes(16)` naar Base64 is voldoende en correct.

---

### 2. Nonce-waarden gelogd in plaintext ✅ Gefixt in commit bb6f27a

**Bestanden:** `Services/NonceMiddleware.cs` (regel 31), `Services/NonceRefresherService.cs` (regel 82)

De gegenereerde CSP-nonce werd letterlijk in applicatielogs geschreven:

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

Iedereen met logtoegang krijgt hiermee een geldige nonce en kan CSP triviaal omzeilen om inline scripts te injecteren.

---

### 3. Hardcoded fallback-nonces ✅ Gefixt in commit 11cc9f7

**Bestand:** `Services/OptimizedNonceMiddleware.cs` (regels 53, 78, 92)

Als nonce-generatie faalde of de nonce-catalogus leeg was, viel de middleware terug op de stringliterals `"bootstrap-nonce-placeholder"`, `"fallback-nonce"` en `"error-fallback-nonce"`. Deze waarden staan in sourcecode en zijn voorspelbaar voor aanvallers. Een foutconditie (bijv. Key Vault onbeschikbaar) zou daardoor een voorspelbare, exploiteerbare nonce in de CSP-header zetten.

---

## 🟠 Hoog

### 4. NonceCatalogService gebruikt niet-threadsafe statische dictionary ✅ Gefixt in commit ae2b6c9

**Bestand:** `Services/NonceCatalogService.cs` (regel 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` is niet thread-safe voor gelijktijdig lezen/schrijven. Onder load kunnen requests racen op dezelfde nonce-key, met datacorruptie of exceptions als gevolg. De nonce-catalogus is ook singleton (effectief globaal), waardoor de nonce van één request door een andere overschreven kan worden. Gebruik `ConcurrentDictionary` en sla nonces per request op in `HttpContext.Items` i.p.v. in een gedeelde globale opslag.

---

### 5. mTLS-certificaat-issuer-validatie was uitgezet ✅ Gefixt in commit fd3d4fb

**Bestand:** `Extensions/ServiceCollectionExtensions.cs` (regels 305–313)

De instelling `ValidateClientCertificateIssuer` bestaat en staat standaard op `true`, maar de daadwerkelijke validatiecode was uitgecommentarieerd:

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

Met mTLS aan kon elk clientcertificaat van elke issuer (die op een vertrouwde root chain't) authenticeren — er was geen tenant/issuer-beperking.

---

### 6. mTLS-intrekkingscontrole standaard uit ✅ Gefixt in commit fd3d7b3

**Bestanden:** `Models/Settings/MtlsSettings.cs` (regel 26), `appsettings.template.json`

`CheckCertificateRevocation` stond standaard op `false` in model en template. Ingetrokken clientcertificaten konden daardoor onbeperkt blijven authenticeren. Voor productie-mTLS moet intrekkingscontrole standaard aan staan.

---

### 7. OCSP-validatie was stub die altijd geldig retourneert ✅ Gefixt in commit b4c3807

**Bestand:** `Services/OcspValidationService.cs` (regels 149–163)

De methode `PerformOcspValidationAsync` was expliciet een "template implementation" die altijd `IsValid = true` teruggeeft na een `Task.Delay(100)`. Als OCSP in config werd aangezet, zouden ook ingetrokken certificaten stilzwijgend als geldig passeren.

---

### 8. Authenticatie en autorisatie standaard uit ✅ Gefixt in commit b392c47

**Bestand:** `appsettings.json` (regels 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

De standaardconfiguratie leverde zonder auth/authz. Wie `appsettings.template.json` kopieerde zonder goed te lezen, kon een open applicatie deployen. Template-defaults moeten een bewuste opt-out vereisen, geen opt-in.

---

### 9. Security headers toegepast ná routing/auth ✅ Gefixt in commit 016e57c

**Bestand:** `Program.cs` (regels 130–152)

`UseNonceAndSecurityHeadersAsync` en `UseStandardSecurityHeaders` werden na `UseRouting`, `UseAuthentication` en `UseAuthorization` aangeroepen. Responses die eerder kortsluiten (bijv. 401-redirects, 403-denials) konden security headers missen. Security headers horen zo vroeg mogelijk in de pipeline.

---

## 🟡 Gemiddeld

### 10. Sessiecookie mist `Secure` en `SameSite` ✅ Gefixt in commit 8f2223c

**Bestand:** `Extensions/ServiceCollectionExtensions.cs` (regels 41–46)

De sessiecookie had `HttpOnly = true` en `IsEssential = true`, maar miste `Cookie.SecurePolicy = CookieSecurePolicy.Always` en `Cookie.SameSite = SameSiteMode.Strict`. De cookie kon over plain HTTP meegestuurd worden (als redirect nog niet was gebeurd) of cross-site verzonden worden.

---

### 11. Ongeldige globale `Set-Cookie`-header ✅ Gefixt in commit 8f2223c

**Bestand:** `Extensions/ApplicationBuilderExtensions.cs` (regel 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

Dit voegde op elke response een naamloze/waardeloze `Set-Cookie` toe. Ongeldig en onverwacht gedrag in statische bestanden, JSON-responses en health checks. Cookiebeveiliging moet via cookie-opties worden gezet, niet via globale ruwe headers.

---

### 12. `Content-Type` geforceerd naar `text/html` voor alle responses ✅ Gefixt in commit 8f2223c

**Bestand:** `Extensions/ApplicationBuilderExtensions.cs` (regel 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

Dit overschreef Content-Type voor alle responses — API, JSON, binaire downloads en static files claimden allemaal `text/html`. Dat botst met `X-Content-Type-Options: nosniff`.

---

### 13. `AllowedHosts` op wildcard ✅ Gefixt in commit 8f2223c

**Bestanden:** `appsettings.json` (regel 11), `appsettings.template.json` (regel 36)

```json
"AllowedHosts": "*"
```

Dit schakelde ingebouwde host-headervalidatie van ASP.NET Core uit. Host-headerinjectie kan cache poisoning, password-reset-link poisoning en open redirects mogelijk maken. Zet dit op specifieke domeinen.

---

### 14. Layout past nonce niet toe op `<script>`-tags ✅ Gefixt in commit 8f2223c

**Bestand:** `Views/Shared/_Layout.cshtml`

De layout laadde meerdere JavaScript-bestanden (`jquery.min.js`, `bootstrap.bundle.min.js`, `site.js`), maar zonder `nonce="@Context.Items["Nonce"]"`. Met CSP-nonces aan zouden deze scripts door de browser geblokkeerd worden. De nonce-infrastructuur zat in middleware maar werd niet in views gebruikt.

---

### 15. Referrer-Policy-header ontbrak ✅ Gefixt in commit 8f2223c

**Bestand:** `Extensions/ApplicationBuilderExtensions.cs`

Standaard security headers bevatten geen `Referrer-Policy`. Zonder deze header stuurt de browser de volledige URL in `Referer` naar third-party resources (zoals ArcGIS CDN in CSP), wat sessiepadinformatie kan lekken.

---

## 🔵 Laag / Informatief

### 16. PII gelogd in plaintext ✅ Gefixt in commit 93bb4e9

**Bestand:** `Services/LoggingHelper.cs` (regels 85, 105)

User OID, e-mail, naam, sessie-ID en rollen werden letterlijk gelogd op elke geauthenticeerde request:

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

Afhankelijk van regelgeving (GDPR, CCPA, HIPAA) kan dit compliance-risico zijn. Overweeg maskeren of hashen van identifiers en stuur PII-logs naar streng gecontroleerde sinks.

---

### 17. Gedeeltelijke connection string in logs ✅ Gefixt in commit 93bb4e9

**Bestand:** `Extensions/ServiceCollectionExtensions.cs` (regel 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

Zelfs gedeeltelijke secrets in logs zijn slechte praktijk. Log liever alleen dat een connection string aanwezig is (niet-leeg).

---

### 18. Key Vault-operations zijn stubs ✅ Gefixt in commit 93bb4e9

**Bestand:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

`GetCertificateFromKeyVault` en `GetSecretFromKeyVault` waren template-stubs die `null`/dummywaarden teruggaven. Met Key Vault aan gaf `GetCertificateFromKeyVault` `null` terug, wat een `InvalidOperationException` bij startup triggerde.

---

### 19. `X-XSS-Protection: 1; mode=block` is verouderd ✅ Gefixt in commit 93bb4e9

**Bestand:** `Extensions/ApplicationBuilderExtensions.cs` (regel 70)

Moderne browsers ondersteunen `X-XSS-Protection` niet meer. De header is niet direct schadelijk, maar geeft vals veiligheidsgevoel. Een sterke CSP is de aanbevolen aanpak.
