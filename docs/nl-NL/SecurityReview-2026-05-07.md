# Beveiligingsoverzicht — WebAppExperimental266

**Datum:** 2026-05-07
**Reikwijdte:** Volledige statische analyse van codebase (opvolg van overzicht 2026-05-06)
**Beoordelaar:** Geautomatiseerde Beveiligingsoverzicht

---

## Samenvatting

Dit opvolgend overzicht bevestigt dat 3 van de 5 kwetsbaarheden die zijn geïdentificeerd in het beveiligingsoverzicht van 2026-05-06 volledig zijn hersteld, met 1 die gedeeltelijk hersteld blijft. Het overzicht identificeert ook 4 nieuwe bevindingen. De algehele beveiligingshouding van de applicatie verbetert aanhoudend.

---

## Status van Vorige Bevindingen (2026-05-06)

| # | Bevinding | Ernst | Status |
|---|---------|----------|--------|
| 20 | NonceRefresherService behoudt ongebruikte Key Vault constructor-afhankelijkheden | 🟠 Hoog | ✅ Hersteld |
| 21 | OcspValidationService intern cache gebruikt niet-threadveilig Dictionary | 🟡 Gemiddeld | ✅ Hersteld |
| 22 | OCSP-validatiestub is nog aanwezig — mislukt gesloten maar niet geïmplementeerd | 🔵 Laag | ⚠️ Geaccepteerd (by ontwerp) |
| 23 | mTLS met lege AllowedIssuers weigert alle certificaten (fail-closed, ongedocumenteerd) | 🔵 Laag | ✅ Hersteld |
| 24 | OcspSettings.ServerUnavailableBehavior standaard naar "Warn" (staat doorgang toe bij fout) | 🔵 Laag | ⚠️ Gedeeltelijk Hersteld |

---

## Gedetailleerde Status van Vorige Bevindingen

### ✅ 20. NonceRefresherService Ongebruikte DI-afhankelijkheden — Hersteld

**Bestand:** `Services/NonceRefresherService.cs`

De `NonceRefresherService` constructor declareert nu alleen `ILogger<NonceRefresherService>`, `ILoggerFactory` en `INonceCatalogService`. De vier eerder ongebruikte afhankelijkheden (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) zijn verwijderd. Dit lost het risico op weigering van dienst op dat de applicatie verhinderde te starten wanneer `EnableKeyVault = false` (de standaard) en `EnableNonceServices = true` (de standaard).

---

### ✅ 21. OcspValidationService Niet-threadveilige Cache — Hersteld

**Bestand:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` is vervangen door `ConcurrentDictionary<string, CachedOcspResponse>`. De `_cache.Remove` aanroep is bijgewerkt naar `_cache.TryRemove`. De cache is nu veilig voor gelijktijdige toegang.

---

### ⚠️ 22. OCSP-validatiestub — Geaccepteerd (Bij Ontwerp)

**Bestand:** `Services/OcspValidationService.cs`

De stub is nog aanwezig maar mislukt correct gesloten. Aangezien `EnableOcspValidation` standaard naar `false`, heeft het geen productie-impact. Het wordt geaccepteerd als een informationele bevinding in afwachting van een volledige OCSP-implementatie.

---

### ✅ 23. mTLS Lege AllowedIssuers — Hersteld

**Bestand:** `Extensions/ServiceCollectionExtensions.cs`

Een opstartswaarschuwing wordt nu gelogd wanneer `ValidateClientCertificateIssuer = true` en `AllowedIssuers` leeg is:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Dit biedt duidelijke begeleiding voor operators die het fail-closed gedrag tegenkomen.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Gedeeltelijk Hersteld

**Bestanden:** `appsettings.template.json` (hersteld), `Models/Settings/OcspSettings.cs` (nog niet hersteld)

De sjabloon specificeert nu correct `"ServerUnavailableBehavior": "Fail"`. De C#-klasse-standaard in `OcspSettings.cs` (regel 39) blijft echter `"Warn"`. Als een operator OCSP inschakelt en `ServerUnavailableBehavior` weglaat uit zijn configuratiebestand, wordt de klasse-standaard `"Warn"` stilletjes toegepast, waardoor doorgang wordt toegestaan bij OCSP-server-onderbrekingen. De klasse-standaard moet worden gewijzigd om overeen te komen met de sjabloonsanbeveling.

---

## Nieuwe Bevindingen

| # | Gebied | Ernst |
|---|------|----------|
| 25 | OcspSettings klasse-standaard ("Warn") wijkt af van sjabloon ("Fail") | 🔵 Laag |
| 26 | NonceCatalogService enkelvoudige gedeelde nonce-sleutel maakt cross-request nonce-botsing mogelijk | 🟡 Gemiddeld |
| 27 | OptimizedNonceMiddleware statische tellers gebruiken getekende 32-bits integers (overlooprisico) | 🔵 Laag |
| 28 | Program.cs registreert lege ILoggerFactory singleton waardoor het raamwerk-logger wordt overschaduwd | 🟡 Gemiddeld |

---

## 🟡 Gemiddeld

### 26. NonceCatalogService Gedeelde Nonce-sleutel Maakt Cross-request Nonce-botsing Mogelijk

**Bestanden:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

De nonce-catalogus slaat alle nonces op onder een enkele gedeelde sleutel `"CSPNonce"`. Onder gelijktijdige belasting is de volgende race-conditie mogelijk:

1. Verzoek A roept `RefreshNonceAsync()` aan — nonce A1 wordt opgeslagen als `_nonceCollection["CSPNonce"]`.
2. Verzoek B roept `RefreshNonceAsync()` aan — nonce B1 overschrijft `_nonceCollection["CSPNonce"]`.
3. Verzoek A roept `GetANonce("CSPNonce")` aan — ontvangt B1, niet A1.
4. Verzoek A's CSP-header en inline-nonce bevatten beide B1.
5. Verzoek B bevat ook B1.

Twee gelijktijdige reacties delen dezelfde nonce. Hoewel beide waarden nog steeds cryptografisch willekeurig en onvoorspelbaar zijn (geen hardgecodeerde string), verschijnt dezelfde nonce-waarde in meerdere gelijktijdige reacties, waardoor de per-verzoek-uniciteitsgarantie vereist door de CSP-specificatie wordt verzwakt. Een aanvaller die de nonce van één reactie kan observeren, heeft een geldige nonce voor ten minste één andere gelijktijdige reactie.

**Aanbeveling:** Genereer de nonce direct in de middleware per verzoek (bijv. `Nonce.GenerateSecureNonce()`) en sla het alleen op in `HttpContext.Items["Nonce"]`, waarbij de gedeelde catalogus wordt omzeild voor per-verzoek-nonces. De gedeelde catalogus zou dan alleen nodig zijn als een nonce moet worden gedeeld tussen middleware-lagen binnen een enkel verzoek, wat `HttpContext.Items` al native afhandelt.

---

### 28. Program.cs Registreert Lege ILoggerFactory Singleton

**Bestand:** `Program.cs` (regel 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core registreert automatisch een volledig geconfigureerde `ILoggerFactory` (met alle logproviders van de `builder.Logging` configuratie) tijdens `WebApplication.CreateBuilder`. Deze expliciete `AddSingleton` registratie voegt een tweede, ongeconfigureerde `LoggerFactory` instantie toe zonder providers. Aangezien `GetRequiredService<ILoggerFactory>()` de meest recent geregistreerde implementatie retourneert, zullen services die `ILoggerFactory` ontvangen via afhankelijkheidsinjectie (zoals `NonceRefresherService`) deze lege fabriek gebruiken en geen loguitvoer produceren via `_loggerFactory.CreateLogger<T>()`.

**Risico:** Stille logging in `NonceRefresherService` — nonce-generering successen en mislukkingen worden niet uitgegeven naar geconfigureerde log-sinks. Dit vermindert de observeerbaarheid van de applicatie tijdens beveiligingsgevoelige operaties zonder functionaliteit te beïnvloeden.

**Aanbeveling:** Verwijder de expliciete `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` registratie. Het raamwerk's geconfigureerde `ILoggerFactory` (met Console en andere providers) zal dan correct worden opgelost door services die ervan afhankelijk zijn.

---

## 🔵 Laag / Informationeel

### 25. OcspSettings Klasse-standaard Wijkt af van Sjabloon

**Bestand:** `Models/Settings/OcspSettings.cs` (regel 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

De sjabloon (`appsettings.template.json`) specificeert `"ServerUnavailableBehavior": "Fail"`, maar de C#-klasse-standaard is `"Warn"`. Als `ServerUnavailableBehavior` ontbreekt in het actieve configuratiebestand, wordt de klasse-standaard stilletjes toegepast in plaats van de sjabloonsanbeveling. Dit is een overblijfsel van bevinding #24.

**Aanbeveling:** Wijzig de klasse-standaard van `"Warn"` naar `"Fail"` om overeen te komen met de sjabloon en het principe van minste privilege.

---

### 27. OptimizedNonceMiddleware Statische Tellers Kunnen Overlopen

**Bestand:** `Services/OptimizedNonceMiddleware.cs` (regels 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Deze getekende 32-bits tellers worden atomisch verhoogd door `Interlocked.Increment`. Na ongeveer 2,1 miljard verhogingen zullen ze terugrollen naar `int.MinValue` (−2.147.483.648), waardoor de efficiëntieberekening `(total - generated) * 100.0 / total` onjuiste of betekenisloze resultaten produceert. Bij 1.000 verzoeken per seconde treedt overloop op na ongeveer 24,8 dagen continu gebruik.

**Aanbeveling:** Wijzig de tellerveldtypes van `int` naar `long` en gebruik de `long` overload van `Interlocked.Increment` om overloop te voorkomen.

---

## Beveiligingsheader-beoordeling (Huidige Staat)

De volgende headers worden toegepast via `UseStandardSecurityHeaders` — ongewijzigd ten opzichte van het vorige overzicht:

| Header | Waarde | Beoordeling |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Goed |
| `X-XSS-Protection` | `0` | ✅ Goed (schakelt verouderde auditor uit) |
| `X-Content-Type-Options` | `nosniff` | ✅ Goed |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Goed |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Goed |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Goed |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Goed |
| `Permissions-Policy` | geolocatie, camera, microfoon, interest-cohort uitgeschakeld | ✅ Goed |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Goed |
| `Content-Security-Policy` | Nonce-gebaseerd, toegepast wanneer CSP ingeschakeld | ✅ Goed |
| `Server` | Gemaskeerd naar `"webserver"` | ✅ Goed |
| `X-Powered-By` | Verwijderd | ✅ Goed |

---

## Algehele Beoordeling

Alle bevindingen met hoge ernst uit het vorige overzicht zijn hersteld. De huidige bevindingen zijn beperkt tot twee kwesties met gemiddelde ernst (#26 gedeelde nonce-sleutel, #28 lege ILoggerFactory) en twee informatieve items met lage ernst (#25 klasse-standaard-mismatch, #27 integer-overloop in tellers). Onmiddellijke aandacht wordt aanbevolen voor bevinding #28 (lege ILoggerFactory singleton) omdat het beveiligingsgerelateerde diagnostische logging tijdens nonce-operaties stil onderdrukt. Bevinding #26 (gedeelde nonce-sleutel) moet worden aangepakt om de per-verzoek nonce-uniciteitsgarantie te herstellen die vereist is door de CSP-specificatie.
