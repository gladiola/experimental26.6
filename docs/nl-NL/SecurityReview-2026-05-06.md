# Beveiligingsbeoordeling — WebAppExperimental266

**Datum:** 2026-05-06
**Reikwijdte:** Volledige code-repository-audit (opvolging van de 2026-05-05-beoordeling)
**Beoordelaar:** Automatische Beveiligingsbeoordeling

---

## Managementsamenvatting

Deze opvolgbeoordeling bevestigt dat alle negentien (19) beveiligingsbevindingen die zijn geïdentificeerd tijdens de 2026-05-05-beveiligingsbeoordeling met succes zijn verholpen. Deze beoordeling identificeert ook vijf (5) nieuwe of resterende bevindingen die op dit moment zijn ontdekt. De algehele beveiligingspositie van de applicatie is aanzienlijk verbeterd ten opzichte van de vorige beoordeling.

---

## Status van Vorige Bevindingen (2026-05-05)

Alle negentien (19) vorige bevindingen **zijn bevestigd als verholpen**:

| # | Bevinding | Ernst | Status |
|---|-----------|-------|--------|
| 1 | Hergebruik van AES-GCM IV binnen nonce-generatie | 🔴 Kritiek | ✅ Verholpen |
| 2 | Nonce geschreven naar plaintext logs | 🔴 Kritiek | ✅ Verholpen |
| 3 | Hardgecodeerde nonce-opzetstrings | 🔴 Kritiek | ✅ Verholpen |
| 4 | Globale nonce-opslag niet threadsafe | 🟠 Hoog | ✅ Verholpen |
| 5 | mTLS-clientcertificaat uitgeversverificaties niet afgedwongen | 🟠 Hoog | ✅ Verholpen |
| 6 | mTLS-intrekkingscontrole standaard uitgeschakeld | 🟠 Hoog | ✅ Verholpen |
| 7 | OCSP retourneert altijd geldig (stub) | 🟠 Hoog | ✅ Verholpen |
| 8 | Authenticatie/Autorisatie standaard uitgeschakeld in functieschakelaars | 🟠 Hoog | ✅ Verholpen |
| 9 | Beveiligingsheaders laat toegevoegd in de pipeline | 🟠 Hoog | ✅ Verholpen |
| 10 | Sessiecookies missen `Secure` + `SameSite` | 🟡 Medium | ✅ Verholpen |
| 11 | Globale `Set-Cookie`-header geïnjecteerd | 🟡 Medium | ✅ Verholpen |
| 12 | `Content-Type` gehard gecodeerd naar `text/html` overal | 🟡 Medium | ✅ Verholpen |
| 13 | `AllowedHosts` ingesteld op wildcard-token | 🟡 Medium | ✅ Verholpen |
| 14 | Nonce niet gekoppeld aan `<script>`-tags in sjabloon | 🟡 Medium | ✅ Verholpen |
| 15 | `Referrer-Policy`-header ontbreekt | 🟡 Medium | ✅ Verholpen |
| 16 | PII geschreven naar plaintext logs | 🔵 Laag | ✅ Verholpen |
| 17 | String-samenvoeging in berichten | 🔵 Laag | ✅ Verholpen |
| 18 | Key Vault-bewerkingen zijn stubs | 🔵 Laag | ✅ Verholpen |
| 19 | `X-XSS-Protection: 1; mode=block` verouderd | 🔵 Laag | ✅ Verholpen |

---

## Nieuwe / Resterende Bevindingen

| # | Locatie | Ernst |
|---|---------|-------|
| 20 | NonceRefresherService behoudt onnodige Key Vault-constructorafhankelijkheden | 🟠 Hoog |
| 21 | OcspValidationService-geheugencache gebruikt niet-threadsafe Dictionary | 🟡 Medium |
| 22 | OCSP-verificatiestub nog aanwezig — fail-closed maar niet geïmplementeerd | 🔵 Laag |
| 23 | mTLS met lege AllowedIssuers weigert alle clientcertificaten (fail-closed, niet gedocumenteerd) | 🔵 Laag |
| 24 | OcspSettings.ServerUnavailableBehavior standaard ingesteld op "Warn" (staat doorgang toe bij fouten) | 🔵 Laag |

---

## Gedetailleerde Bevindingen

### ✅ Bevestigde Oplossingen van 2026-05-05

#### 1. Hergebruik van AES-GCM IV — Verholpen

**Bestand:** `Models/Main_Objects/Nonce.cs`

De nonce-generatie is volledig herschreven van AES-GCM. `Nonce.GenerateSecureNonce()` roept nu `RandomNumberGenerator.Fill(randomBytes)` aan voor 16 willekeurige bytes en retourneert een Base64-string. Geen Key Vault-afhankelijkheid, geen IV, geen versleuteling — dit is de correcte aanpak voor een CSP-nonce.

---

#### 2. Nonce-waarde niet naar Logs geschreven — Verholpen

**Bestanden:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Beide bestanden schrijven alleen statusberichten (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) en nooit de nonce-waarde zelf.

---

#### 3. Hardgecodeerde Opzetnonces Verwijderd — Verholpen

**Bestand:** `Services/OptimizedNonceMiddleware.cs`

Drie hardgecodeerde strings (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) zijn vervangen door aanroepen naar `Nonce.GenerateSecureNonce()` in normale paden en twee opzetfoutaanwijzers.

---

#### 4. Threadsafe Nonce-opslag — Verholpen

**Bestand:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` is gewijzigd naar `ConcurrentDictionary<string, Nonce>`. `GetANonce` gebruikt nu één atomaire `TryGetValue`-aanroep in plaats van een tweestapscontrole.

---

#### 5. mTLS-clientcertificaat Uitgeversverificaties Werken Nu — Verholpen

**Bestanden:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

De hardgecodeerde uitgeversvericatiecontext is vervangen door een `mtlsSettings.IsIssuerAllowed(issuer)`-aanroep, die een hoofdletter-onafhankelijke stringvergelijking uitvoert tegen `AllowedIssuers`. Als de lijst leeg is (niet geconfigureerd), retourneert de methode `false`, waardoor alle certificaten worden geweigerd (fail-closed).

---

#### 6. mTLS-intrekkingscontrole Standaard Ingeschakeld — Verholpen

**Bestand:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` is nu standaard `true`. `appsettings.template.json` stelt ook `"CheckCertificateRevocation": true` in.

---

#### 7. OCSP-stub Faalt Nu Gesloten — Verholpen

**Bestand:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` retourneert nu `IsValid = false` met `OcspStatus.Error` en logt de fout, in plaats van stilletjes `IsValid = true` te retourneren. OCSP-activering weigert alle certificaten totdat een echte implementatie beschikbaar is.

---

#### 8. Authenticatie en Autorisatie Standaard Ingeschakeld — Verholpen

**Bestand:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` en `EnableAuthorization` zijn nu beide standaard `true` in de `FeatureFlags`-klasse. `appsettings.json` stelt beide ook in op `true`.

---

#### 9. Beveiligingsheaders Vroeg in Pipeline Toegevoegd — Verholpen

**Bestand:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` en `UseStandardSecurityHeaders` worden nu aangeroepen vóór `UseRouting`, `UseAuthentication` en `UseAuthorization`. Alle antwoorden, inclusief short-circuit 401/403-antwoorden, ontvangen hun beveiligingsheaders.

---

#### 10–15. Cookies, Content-Type, AllowedHosts, Nonce in Sjabloon, Referrer-Policy — Verholpen

**Bestanden:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Sessiecookies zijn nu ingesteld op `CookieSecurePolicy.Always` en `SameSiteMode.Strict`.
- De ongewenste globale `Set-Cookie`-header is verwijderd.
- De globale `Content-Type: text/html`-variabele is verwijderd.
- `AllowedHosts` in `appsettings.json` is nu `"localhost;127.0.0.1"`; het sjabloon gebruikt `"{{YOUR_HOSTNAME}}"`.
- Alle drie `<script>`-tags in `_Layout.cshtml` hebben nu `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` is toegevoegd door `UseStandardSecurityHeaders`.

---

#### 16–19. PII in Logs, String-samenvoeging, Key Vault Stubs, X-XSS-Protection — Verholpen

**Bestanden:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Alle PII (OID, e-mail, naam, SID, rollen) worden HMAC-SHA256 gehasht via `LoggingHelper.HashPii()` vóór het schrijven naar logs. De stabiele HMAC-sleutel kan worden opgegeven via `Logging:PiiHmacKey` in de configuratie; een per-processleutel wordt gebruikt als dit niet geconfigureerd is.
- De Cosmos DB-logboodschap verifieert alleen dat de samenvoegstring bestaat (`!string.IsNullOrEmpty`), niet de inhoud.
- `AzureKeyVaultCertificateOperations` gooit nu een `InvalidOperationException` bij opstarten als het certificaat null is, in plaats van stilletjes metadata-waarden te retourneren.
- `X-XSS-Protection` is nu ingesteld op `"0"` (schakelt verouderd XSS-filter uit), in overeenstemming met richtlijnen van moderne browsers.

---

## 🟠 Hoog

### 20. NonceRefresherService Behoudt Onnodige Key Vault-Constructorafhankelijkheden

**Bestand:** `Services/NonceRefresherService.cs`

`NonceRefresherService` injecteert constructorparameters voor `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService` en `IAzureKeyVaultOperationsService`. Omdat nonce-generatie is vereenvoudigd om `RandomNumberGenerator` direct te gebruiken, worden geen van deze afhankelijkheden gebruikt.

**Probleem:** Wanneer `EnableNonceServices = true` en `EnableKeyVault = false` (standaard), zijn deze services niet geregistreerd in de DI-container, wat een `InvalidOperationException` bij opstarten veroorzaakt wanneer de nonce-service voor het eerst wordt opgelost. Dit is een servicebeschikbaarheidsconditie die wordt veroorzaakt door de standaardconfiguratie. De `FeatureFlags`-klasse stelt standaard `EnableNonceServices = true` in, dus elke omgeving die alleen vertrouwt op klasse-standaardwaarden (zonder `appsettings.json`-wijzigingen) zal mislukken.

**Aanbeveling:** Verwijder de vier ongebruikte constructorparameters en hun corresponderende privévelden uit `NonceRefresherService`. De service heeft alleen `ILogger<NonceRefresherService>`, `ILoggerFactory` en `INonceCatalogService` nodig.

---

## 🟡 Medium

### 21. OcspValidationService-Geheugencache Gebruikt Niet-Threadsafe Dictionary

**Bestand:** `Services/OcspValidationService.cs` (regel 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` is niet veilig voor gelijktijdig lezen en schrijven. Als `OcspValidationService` is geregistreerd als singleton (of als dezelfde instantie wordt gedeeld over verzoeken), kunnen gelijktijdige OCSP-verificaties de cache beschadigen, wat leidt tot verloren vermeldingen, onverwachte uitzonderingen of het retourneren van verouderde gegevens.

**Aanbeveling:** Vervang `Dictionary<string, CachedOcspResponse>` door `ConcurrentDictionary<string, CachedOcspResponse>`. Werk de `_cache.Remove`-aanroep (regel 103) bij naar `_cache.TryRemove`.

---

## 🔵 Laag / Informatief

### 22. OCSP-Verificatiestub — Fail-Closed Maar Niet Geïmplementeerd

**Bestand:** `Services/OcspValidationService.cs` (regels 157–173)

`PerformOcspValidationAsync` is nog steeds een stub. De bevinding #7-oplossing heeft het gedrag correct gewijzigd van "altijd geldig" naar "altijd ongeldig (fail-closed)". Het is echter geen echte OCSP-implementatie. Met `EnableOcspValidation = false` (standaard) is er geen bedrijfsimpact. Voordat OCSP in een omgeving wordt ingeschakeld, moet een echte OCSP-statusimplementatie worden geïmplementeerd.

---

### 23. mTLS met Lege AllowedIssuers Weigert Alle Clientcertificaten

**Bestand:** `Models/Settings/MtlsSettings.cs`

Wanneer `ValidateClientCertificateIssuer = true` (standaard) en `AllowedIssuers` leeg is (ook standaard als niet geconfigureerd), retourneert `IsIssuerAllowed()` `false`, waardoor alle clientcertificaten worden geweigerd. Dit is gewenst fail-closed gedrag, maar is niet duidelijk gedocumenteerd. Operators die mTLS inschakelen zonder het sjabloon zorgvuldig te lezen, kunnen ontdekken dat alle clientcertificaten worden geweigerd zonder duidelijke uitleg.

**Aanbeveling:** Voeg een opstartlog-waarschuwing toe wanneer `ValidateClientCertificateIssuer = true` en `AllowedIssuers` leeg is.

---

### 24. OcspSettings.ServerUnavailableBehavior Standaard ingesteld op "Warn"

**Bestand:** `appsettings.template.json` (regel 134), `Services/OcspValidationService.cs`

De standaardwaarde van `ServerUnavailableBehavior` is `"Warn"` in het sjabloon, waardoor verzoeken worden doorgegeven wanneer de OCSP-service niet bereikbaar is. Voor omgevingen met hoge beveiliging moet dit `"Fail"` zijn om te voorkomen dat OCSP-serviceuitval de certificaatintrekkingscontrole stil omzeilt.

**Aanbeveling:** Documenteer alle drie opties (`Fail`, `Allow`, `Warn`) duidelijk in het sjabloon en overweeg de standaardwaarde te wijzigen naar `"Fail"` in overeenstemming met het principe van minimale rechten.

---

## Beoordeling Beveiligingsheaders (Huidige Status)

Deze headers worden toegevoegd door `UseStandardSecurityHeaders`:

| Header | Waarde | Beoordeling |
|--------|--------|-------------|
| `X-Frame-Options` | `DENY` | ✅ Goed |
| `X-XSS-Protection` | `0` | ✅ Goed (schakelt verouderd XSS-filter uit) |
| `X-Content-Type-Options` | `nosniff` | ✅ Goed |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Goed |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Goed |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Goed |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Goed |
| `Permissions-Policy` | geolocatie, camera, microfoon, interest-cohort uitgeschakeld | ✅ Goed |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Goed |
| `Content-Security-Policy` | Nonce-gebaseerd, inbegrepen wanneer CSP ingeschakeld | ✅ Goed |
| `Server` | Verborgen als `"webserver"` | ✅ Goed |
| `X-Powered-By` | Verwijderd | ✅ Goed |

---

## Algemene Beoordeling

De applicatie heeft alle kritieke en hoge-ernst kwetsbaarheden van de vorige beoordeling verholpen. De huidige bevindingen zijn beperkt tot één hoge-ernst configuratie-/DI-probleem (bevinding #20) en lage-ernst informatieve items. De beveiligingspositie is aanzienlijk verbeterd. Urgente actie wordt aanbevolen voor bevinding #20 (ongebruikte DI-afhankelijkheden in NonceRefresherService) omdat het de applicatie kan verhinderen op te starten onder standaardconfiguratie.
