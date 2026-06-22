# Sekuriteitsoorsiening — WebAppExperimental266

**Datum:** 2026-05-06
**Omvang:** Volledige kode-opslagplaas-oudit (opvolging van die 2026-05-05-hersiening)
**Resensent:** Outomatiese Sekuriteitshersiening

---

## Uitvoerende Opsomming

Hierdie opvolghersiening bevestig dat al negentien (19) sekuriteitsbevindinge wat tydens die 2026-05-05-sekuriteitshersiening geïdentifiseer is, suksesvol reggestel is. Hierdie hersiening identifiseer ook vyf (5) nuwe of oorblyfselbevindinge wat op hierdie stadium opgespoor is. Die algehele sekuriteitsposisie van die toepassing het aansienlik verbeter sedert die vorige hersiening.

---

## Status van Vorige Bevindinge (2026-05-05)

Al negentien (19) vorige bevindinge **is bevestig as reggestel**:

| # | Bevinding | Erns | Status |
|---|-----------|------|--------|
| 1 | Hergebruik van AES-GCM IV binne nonce-generering | 🔴 Kritiek | ✅ Reggestel |
| 2 | Nonce geskryf na kaaltetse logs | 🔴 Kritiek | ✅ Reggestel |
| 3 | Hardekoduierte nonce-opsetstring | 🔴 Kritiek | ✅ Reggestel |
| 4 | Globale nonce-stoor nie draadveilig nie | 🟠 Hoog | ✅ Reggestel |
| 5 | mTLS-klientverifikasie verifikasies nie versterk nie | 🟠 Hoog | ✅ Reggestel |
| 6 | mTLS-herroepingskontrole standaard gedeaktiveer | 🟠 Hoog | ✅ Reggestel |
| 7 | OCSP gee altyd geldig terug (stub) | 🟠 Hoog | ✅ Reggestel |
| 8 | Verifikasie/Magtiging standaard gedeaktiveer in konfigurasieskakelaars | 🟠 Hoog | ✅ Reggestel |
| 9 | Sekuriteitsopskrifte laat bygevoeg in die pyplyn | 🟠 Hoog | ✅ Reggestel |
| 10 | Sessiekoekies mis `Secure` + `SameSite` | 🟡 Medium | ✅ Reggestel |
| 11 | Globale `Set-Cookie`-opskrif ingespuit | 🟡 Medium | ✅ Reggestel |
| 12 | `Content-Type` gehardekodeer na `text/html` oral | 🟡 Medium | ✅ Reggestel |
| 13 | `AllowedHosts` gestel op wildkaart-token | 🟡 Medium | ✅ Reggestel |
| 14 | Nonce nie aan `<script>`-etikette in sjabloon geheg nie | 🟡 Medium | ✅ Reggestel |
| 15 | `Referrer-Policy`-opskrif ontbreek | 🟡 Medium | ✅ Reggestel |
| 16 | PBI geskryf na kaaltetse logs | 🔵 Laag | ✅ Reggestel |
| 17 | String-samevoeging in boodskappe | 🔵 Laag | ✅ Reggestel |
| 18 | Key Vault-bewerkings is stubs | 🔵 Laag | ✅ Reggestel |
| 19 | `X-XSS-Protection: 1; mode=block` verouderd | 🔵 Laag | ✅ Reggestel |

---

## Nuwe / Oorblyfsel Bevindinge

| # | Ligging | Erns |
|---|---------|------|
| 20 | NonceRefresherService hou onnodige Key Vault-konstrukteur-afhanklikhede | 🟠 Hoog |
| 21 | OcspValidationService-geheue gebruik Dictionary nie draadveilig nie | 🟡 Medium |
| 22 | OCSP-verifikasie-stub nog aanwesig — misluk-geslote maar nie geïmplementeer nie | 🔵 Laag |
| 23 | mTLS met leë AllowedIssuers weier alle kliëntsertifikate (misluk-geslote, nie gedokumenteer nie) | 🔵 Laag |
| 24 | OcspSettings.ServerUnavailableBehavior verstekwaarde na "Warn" (laat deurgang toe tydens foute) | 🔵 Laag |

---

## Gedetailleerde Bevindinge

### ✅ Bevestigde Regstellings van 2026-05-05

#### 1. Hergebruik van AES-GCM IV — Reggestel

**Lêer:** `Models/Main_Objects/Nonce.cs`

Die nonce-generering is volledig herwerk van AES-GCM af. `Nonce.GenerateSecureNonce()` roep nou `RandomNumberGenerator.Fill(randomBytes)` vir 16 willekeurige grepe en gee 'n Base64-string terug. Geen Key Vault-afhanklikheid, geen IV, geen enkriptasie — dit is die korrekte benadering vir 'n CSP-nonce.

---

#### 2. Nonce-waarde nie na Logs geskryf nie — Reggestel

**Lêers:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Beide lêers skryf slegs statusboodskappe (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) en nooit die nonce-waarde self nie.

---

#### 3. Hardekoduierte Opstetnonces Verwyder — Reggestel

**Lêer:** `Services/OptimizedNonceMiddleware.cs`

Drie hardekoduierte strings (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) is vervang deur aanroepe na `Nonce.GenerateSecureNonce()` in normale paaie en twee opsetfout-aanwysers.

---

#### 4. Draadveilige Nonce-stoor — Reggestel

**Lêer:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` is na `ConcurrentDictionary<string, Nonce>` verander. `GetANonce` gebruik nou 'n enkele atomiese `TryGetValue`-aanroep in plaas van 'n twee-stap-kontrole.

---

#### 5. mTLS-klientverifikasie Verifikasies Werk Nou — Reggestel

**Lêers:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Die hardekoduierte uitreiker-verifikasie-konteks is vervang deur 'n `mtlsSettings.IsIssuerAllowed(issuer)`-aanroep, wat 'n hoofletter-onsensitiewe string-vergelyking teen `AllowedIssuers` uitvoer. As die lys leeg is (nie gekonfigureer nie), gee die metode `false` terug, en weier alle sertifikate (misluk-geslote).

---

#### 6. mTLS-herroepingskontrole Standaard Geaktiveer — Reggestel

**Lêer:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` is nou standaard `true`. `appsettings.template.json` stel ook `"CheckCertificateRevocation": true`.

---

#### 7. OCSP-stub Misluk Nou Geslote — Reggestel

**Lêer:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` gee nou `IsValid = false` met `OcspStatus.Error` terug en log die fout, in plaas van stil `IsValid = true` terug te gee. OCSP-aktivering sal alle sertifikate weier totdat 'n werklike implementasie beskikbaar is.

---

#### 8. Verifikasie en Magtiging Standaard Geaktiveer — Reggestel

**Lêer:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` en `EnableAuthorization` is beide nou standaard `true` in die `FeatureFlags`-klas. `appsettings.json` stel ook beide na `true`.

---

#### 9. Sekuriteitsopskrifte Vroeg in Pyplyn Bygevoeg — Reggestel

**Lêer:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` en `UseStandardSecurityHeaders` word nou aangeroep vóór `UseRouting`, `UseAuthentication`, en `UseAuthorization`. Alle reaksies, insluitend kortbaankring 401/403-reaksies, sal hul sekuriteitsopskrifte kry.

---

#### 10–15. Koekies, Content-Type, AllowedHosts, Nonce in Sjabloon, Referrer-Policy — Reggestel

**Lêers:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Sessiekoekies is nou na `CookieSecurePolicy.Always` en `SameSiteMode.Strict` gestel.
- Die ongesoekte globale `Set-Cookie`-opskrif is verwyder.
- Die globale `Content-Type: text/html`-veranderlike is verwyder.
- `AllowedHosts` in `appsettings.json` is nou `"localhost;127.0.0.1"`; die sjabloon gebruik `"{{YOUR_HOSTNAME}}"`.
- Al drie `<script>`-etikette in `_Layout.cshtml` het nou `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` is deur `UseStandardSecurityHeaders` bygevoeg.

---

#### 16–19. PBI in Logs, String-Samevoeging, Key Vault Stubs, X-XSS-Protection — Reggestel

**Lêers:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Alle PBI (OID, e-pos, naam, SID, rolle) word HMAC-SHA256 gehash via `LoggingHelper.HashPii()` voor log-skrywing. Die stabiele HMAC-sleutel kan via `Logging:PiiHmacKey` in konfigurasie verskaf word; 'n per-proses-sleutel word gebruik as dit nie gekonfigureer is nie.
- Die Cosmos DB-logboodskap verifieer slegs dat die samesnoerstring lewe (`!string.IsNullOrEmpty`), nie die inhoud nie.
- `AzureKeyVaultCertificateOperations` gooi nou 'n `InvalidOperationException` tydens opstart as die sertifikaat nul is, in plaas van stil metadata-waardes terug te gee.
- `X-XSS-Protection` is nou `"0"` (deaktiveer verouderde XSS-filter), in ooreenstemming met riglyne van moderne blaaiers.

---

## 🟠 Hoog

### 20. NonceRefresherService Hou Onnodige Key Vault-Konstrukteur-Afhanklikhede

**Lêer:** `Services/NonceRefresherService.cs`

`NonceRefresherService` spuit konstrukteurparameters in vir `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, en `IAzureKeyVaultOperationsService`. Aangesien nonce-generering vereenvoudig is om `RandomNumberGenerator` direk te gebruik, word geen van hierdie afhanklikhede gebruik nie.

**Probleem:** Wanneer `EnableNonceServices = true` en `EnableKeyVault = false` (verstekwaarde), is hierdie dienste nie in die DI-houer geregistreer nie, wat 'n `InvalidOperationException` by opstart veroorsaak wanneer die nonce-diens eers opgelos word. Dit is 'n diensonbeskikbaarheidstoestand wat deur die verstekonfigurasie veroorsaak word. Die `FeatureFlags`-klas stel standaard `EnableNonceServices = true`, so enige omgewing wat net op klasverstekwaardes staatmaak (sonder `appsettings.json`-wysigings) sal misluk.

**Aanbeveling:** Verwyder die vier ongebruikte konstrukteurparameters en hul ooreenstemmende privaatvelde van `NonceRefresherService`. Die diens benodig slegs `ILogger<NonceRefresherService>`, `ILoggerFactory`, en `INonceCatalogService`.

---

## 🟡 Medium

### 21. OcspValidationService-Geheue Gebruik Nie-Draadveilige Dictionary

**Lêer:** `Services/OcspValidationService.cs` (reël 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` is nie veilig vir gelyktydige lees en skryf nie. As `OcspValidationService` as singleton geregistreer is (of as dieselfde instansie oor versoeke gedeel word), kan gelyktydige OCSP-verifikasies die geheue korrupteer, wat tot verlore inskrywings, onverwagte uitsonderings, of terugkeer van verouderde data kan lei.

**Aanbeveling:** Vervang `Dictionary<string, CachedOcspResponse>` met `ConcurrentDictionary<string, CachedOcspResponse>`. Werk die `_cache.Remove`-aanroep (reël 103) op na `_cache.TryRemove`.

---

## 🔵 Laag / Inligtingsgewys

### 22. OCSP-Verifikasie-Stub — Misluk-Geslote Maar Nie Geïmplementeer Nie

**Lêer:** `Services/OcspValidationService.cs` (reëls 157–173)

`PerformOcspValidationAsync` is steeds 'n stub. Die bevinding #7-regstelling het die gedrag korrek verander van "altyd geldig" na "altyd ongeldig (misluk-geslote)". Nietemin is dit nie 'n werklike OCSP-implementasie nie. Met `EnableOcspValidation = false` (verstekwaarde) is daar geen besigheidsinvloed nie. Voordat OCSP in enige omgewing geaktiveer word, moet 'n werklike OCSP-statusimplementasie geïmplementeer word.

---

### 23. mTLS met Leë AllowedIssuers Weier Alle Kliëntsertifikate

**Lêer:** `Models/Settings/MtlsSettings.cs`

Wanneer `ValidateClientCertificateIssuer = true` (verstekwaarde) en `AllowedIssuers` leeg is (ook standaard as nie gekonfigureer nie), gee `IsIssuerAllowed()` `false` terug, wat alle kliëntsertifikate weier. Dit is 'n gewenste misluk-geslote gedrag, maar is nie duidelik gedokumenteer nie. Operateurs wat mTLS aktiveer sonder om die sjabloon noukeurig te lees, kan vind dat alle kliëntsertifikate geweier word sonder 'n duidelike verduideliking.

**Aanbeveling:** Voeg 'n opstartlogs-waarskuwing by wanneer `ValidateClientCertificateIssuer = true` en `AllowedIssuers` leeg is.

---

### 24. OcspSettings.ServerUnavailableBehavior Verstekwaarde na "Warn"

**Lêer:** `appsettings.template.json` (reël 134), `Services/OcspValidationService.cs`

Die verstekwaarde van `ServerUnavailableBehavior` is `"Warn"` in die sjabloon, wat versoeke toelaat om deur te gaan as die OCSP-diens onbereikbaar is. Vir hoësekuriteitsomgewings moet dit `"Fail"` wees om te voorkom dat OCSP-diensuitval sertifikaatherroepingskontrole stil omseil.

**Aanbeveling:** Dokumenteer al drie opsies (`Fail`, `Allow`, `Warn`) duidelik in die sjabloon en oorweeg om die verstekwaarde na `"Fail"` te verander in ooreenstemming met die beginsel van minste voorreg.

---

## Sekuriteitsopskrifbeoordeling (Huidige Status)

Hierdie opskrifte word deur `UseStandardSecurityHeaders` bygevoeg:

| Opskrif | Waarde | Beoordeling |
|---------|--------|-------------|
| `X-Frame-Options` | `DENY` | ✅ Goed |
| `X-XSS-Protection` | `0` | ✅ Goed (deaktiveer verouderde XSS-filter) |
| `X-Content-Type-Options` | `nosniff` | ✅ Goed |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Goed |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Goed |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Goed |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Goed |
| `Permissions-Policy` | ligging, kamera, mikrofoon, interest-cohort gedeaktiveer | ✅ Goed |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Goed |
| `Content-Security-Policy` | Nonce-gebaseer, ingesluit wanneer CSP geaktiveer | ✅ Goed |
| `Server` | Verberg na `"webserver"` | ✅ Goed |
| `X-Powered-By` | Verwyder | ✅ Goed |

---

## Algehele Beoordeling

Die toepassing het al die kritiese en hoëerens kwesbaarhede van die vorige hersiening reggestel. Die huidige bevindinge is beperk tot een hoëerens konfigurasie/DI-probleem (bevinding #20) en lae-erns inligtings-items. Die sekuriteitsposisie het aansienlik verbeter. Dringende aksie word aanbeveel vir bevinding #20 (ongebruikte DI-afhanklikhede in NonceRefresherService) aangesien dit die toepassing kan verhinder om onder verstekonfigurasie te begin.
