# Sekuriteitsoorsig — WebAppExperimental266

**Datum:** 2026-05-07
**Omvang:** Volledige statiese analise van kodebasis (opvolg van 2026-05-06 oorsig)
**Beoordelaar:** Geoutomatiseerde Sekuriteitsoorsig

---

## Uitvoerende Opsomming

Hierdie opvolgoorsig bevestig dat 3 van die 5 kwesbaarhede wat in die sekuriteitsoorsig van 2026-05-06 geïdentifiseer is, volledig herstel is, met 1 wat gedeeltelik herstel bly. Die oorsig identifiseer ook 4 nuwe bevindings. Die algehele sekuriteitsposisie van die toepassing verbeter steeds.

---

## Status van Vorige Bevindings (2026-05-06)

| # | Bevinding | Erns | Status |
|---|---------|----------|--------|
| 20 | NonceRefresherService behou ongebruikte Key Vault konstrukteurafhanklikhede | 🟠 Hoog | ✅ Herstel |
| 21 | OcspValidationService se interne kas gebruik nie-draadveilige Dictionary | 🟡 Medium | ✅ Herstel |
| 22 | OCSP-valideringsstub is steeds teenwoordig — misluk gesluit maar nie geïmplementeer nie | 🔵 Laag | ⚠️ Aanvaar (deur ontwerp) |
| 23 | mTLS met leë AllowedIssuers verwerp alle sertifikate (fail-closed, ongedokumenteerd) | 🔵 Laag | ✅ Herstel |
| 24 | OcspSettings.ServerUnavailableBehavior verstek na "Warn" (laat deurgang toe by fout) | 🔵 Laag | ⚠️ Gedeeltelik Herstel |

---

## Gedetailleerde Status van Vorige Bevindings

### ✅ 20. NonceRefresherService Ongebruikte DI-afhanklikhede — Herstel

**Lêer:** `Services/NonceRefresherService.cs`

Die `NonceRefresherService` konstrukteur verklaar nou slegs `ILogger<NonceRefresherService>`, `ILoggerFactory` en `INonceCatalogService`. Die vier voorheen ongebruikte afhanklikhede (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) is verwyder. Dit los die diensweiering-risiko op wat verhoed het dat die toepassing begin wanneer `EnableKeyVault = false` (die verstek) en `EnableNonceServices = true` (die verstek).

---

### ✅ 21. OcspValidationService Nie-draadveilige Kas — Herstel

**Lêer:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` is vervang met `ConcurrentDictionary<string, CachedOcspResponse>`. Die `_cache.Remove` aanroep is opgedateer na `_cache.TryRemove`. Die kas is nou veilig vir gelyktydige toegang.

---

### ⚠️ 22. OCSP-valideringsstub — Aanvaar (Deur Ontwerp)

**Lêer:** `Services/OcspValidationService.cs`

Die stub is steeds teenwoordig maar misluk korrek gesluit. Aangesien `EnableOcspValidation` verstek na `false`, het dit geen produksie-impak nie. Dit word aanvaar as 'n inligtingsbevinding hangende 'n volledige OCSP-implementering.

---

### ✅ 23. mTLS Leë AllowedIssuers — Herstel

**Lêer:** `Extensions/ServiceCollectionExtensions.cs`

'n Aanvangswaarskuwing word nou aangeteken wanneer `ValidateClientCertificateIssuer = true` en `AllowedIssuers` leeg is:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Dit bied duidelike leiding aan operateurs wat die fail-closed gedrag teëkom.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Gedeeltelik Herstel

**Lêers:** `appsettings.template.json` (herstel), `Models/Settings/OcspSettings.cs` (nog nie herstel nie)

Die sjabloon spesifiseer nou korrek `"ServerUnavailableBehavior": "Fail"`. Die C#-klasse-verstek in `OcspSettings.cs` (reël 39) bly egter `"Warn"`. As 'n operateur OCSP aktiveer en `ServerUnavailableBehavior` uit sy konfigurasielêer weglaat, word die klasse-verstek `"Warn"` stil toegepas, wat deurgang by OCSP-bediener-onderbrekings toelaat. Die klasse-verstek moet verander word om by die sjabloonaanbeveling te pas.

---

## Nuwe Bevindings

| # | Gebied | Erns |
|---|------|----------|
| 25 | OcspSettings klasse-verstek ("Warn") wyk af van sjabloon ("Fail") | 🔵 Laag |
| 26 | NonceCatalogService se enkele gedeelde nonce-sleutel laat kruisversoek nonce-botsing toe | 🟡 Medium |
| 27 | OptimizedNonceMiddleware statiese tellers gebruik getekende 32-bis heelgetalle (oorvloei-risiko) | 🔵 Laag |
| 28 | Program.cs registreer leë ILoggerFactory singleton, wat die raamwerk-logger oorskadu | 🟡 Medium |

---

## 🟡 Medium

### 26. NonceCatalogService Gedeelde Nonce-sleutel Laat Kruisversoek Nonce-botsing Toe

**Lêers:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Die nonce-katalogus stoor alle nonces onder 'n enkele gedeelde sleutel `"CSPNonce"`. Onder gelyktydige las is die volgende wedlooptoestand moontlik:

1. Versoek A roep `RefreshNonceAsync()` aan — nonce A1 word gestoor as `_nonceCollection["CSPNonce"]`.
2. Versoek B roep `RefreshNonceAsync()` aan — nonce B1 skryf `_nonceCollection["CSPNonce"]` oor.
3. Versoek A roep `GetANonce("CSPNonce")` aan — ontvang B1, nie A1 nie.
4. Versoek A se CSP-kopskrif en uitleg-nonce bevat albei B1.
5. Versoek B bevat ook B1.

Twee gelyktydige antwoorde deel dieselfde nonce. Hoewel albei waardes steeds kriptografies ewekansig en onvoorspelbaar is (geen hardekodte string nie), verskyn dieselfde nonce-waarde in verskeie gelyktydige antwoorde, wat die per-versoek-uniekheidsgaransie wat deur die CSP-spesifikasie vereis word, verswak. 'n Aanvaller wat die nonce van een antwoord kan waarneem, het 'n geldige nonce vir ten minste een ander gelyktydige antwoord.

**Aanbeveling:** Genereer die nonce direk binne die middleware per versoek (bv. `Nonce.GenerateSecureNonce()`) en stoor dit slegs in `HttpContext.Items["Nonce"]`, deur die gedeelde katalogus vir per-versoek-nonces te omseil. Die gedeelde katalogus sal dan slegs nodig wees as 'n nonce tussen middleware-lae binne 'n enkele versoek gedeel moet word, wat `HttpContext.Items` reeds inheems hanteer.

---

### 28. Program.cs Registreer Leë ILoggerFactory Singleton

**Lêer:** `Program.cs` (reël 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core registreer outomaties 'n volledig gekonfigureerde `ILoggerFactory` (met alle aanteken-verskaffers uit die `builder.Logging` konfigurasie) tydens `WebApplication.CreateBuilder`. Hierdie eksplisiete `AddSingleton` registrasie voeg 'n tweede, ongekonfigureerde `LoggerFactory` instansie sonder verskaffers by. Aangesien `GetRequiredService<ILoggerFactory>()` die mees onlangs geregistreerde implementering terugstuur, sal dienste wat `ILoggerFactory` via afhanklikheidsinspuiting ontvang (soos `NonceRefresherService`) hierdie leë fabriek gebruik en geen aanteken-uitvoer produseer via `_loggerFactory.CreateLogger<T>()` nie.

**Risiko:** Stil aantekening in `NonceRefresherService` — nonce-generering suksesse en mislukkings word nie na enige gekonfigureerde aanteken-put uitgegee nie. Dit verminder die toepassing se waarneembaarheid tydens sekuriteitsgevoelige bewerkings sonder om funksionaliteit te beïnvloed.

**Aanbeveling:** Verwyder die eksplisiete `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` registrasie. Die raamwerk se gekonfigureerde `ILoggerFactory` (met Console en enige ander verskaffers) sal dan korrek deur dienste wat daarvan afhang opgelos word.

---

## 🔵 Laag / Inligtend

### 25. OcspSettings Klasse-verstek Wyk af van Sjabloon

**Lêer:** `Models/Settings/OcspSettings.cs` (reël 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Die sjabloon (`appsettings.template.json`) spesifiseer `"ServerUnavailableBehavior": "Fail"`, maar die C#-klasse-verstek is `"Warn"`. As `ServerUnavailableBehavior` afwesig is uit die aktiewe konfigurasielêer, word die klasse-verstek stil toegepas eerder as die sjabloonaanbeveling. Dit is 'n oorblyfsel van bevinding #24.

**Aanbeveling:** Verander die klasse-verstek van `"Warn"` na `"Fail"` om met die sjabloon en die beginsel van minste voorreg te ooreenstem.

---

### 27. OptimizedNonceMiddleware Statiese Tellers Kan Oorvloei

**Lêer:** `Services/OptimizedNonceMiddleware.cs` (reëls 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Hierdie getekende 32-bis tellers word atomies deur `Interlocked.Increment` vermeerder. Na ongeveer 2.1 miljard vermeerderinge sal hulle na `int.MinValue` (−2,147,483,648) terugrol, wat die doeltreffendheidsberekening `(total - generated) * 100.0 / total` verkeerde of betekenislose resultate laat produseer. By 1,000 versoeke per sekonde gebeur oorvloei na ongeveer 24.8 dae van deurlopende werking.

**Aanbeveling:** Verander die tellerveldtipes van `int` na `long` en gebruik die `long` oorlading van `Interlocked.Increment` om oorvloei te voorkom.

---

## Sekuriteitskopskrif-assessering (Huidige Toestand)

Die volgende kopskrifte word toegepas via `UseStandardSecurityHeaders` — onveranderd van die vorige oorsig:

| Kopskrif | Waarde | Assessering |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Goed |
| `X-XSS-Protection` | `0` | ✅ Goed (deaktiveer verouderde ouditeur) |
| `X-Content-Type-Options` | `nosniff` | ✅ Goed |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Goed |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Goed |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Goed |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Goed |
| `Permissions-Policy` | geolokasie, kamera, mikrofoon, interest-cohort gedeaktiveer | ✅ Goed |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Goed |
| `Content-Security-Policy` | Nonce-gebaseerd, toegepas wanneer CSP geaktiveer is | ✅ Goed |
| `Server` | Gemaskeer na `"webserver"` | ✅ Goed |
| `X-Powered-By` | Verwyder | ✅ Goed |

---

## Algehele Assessering

Alle hoë-erns bevindings van vorige oorsig is herstel. Die huidige bevindings is beperk tot twee medium-erns kwessies (#26 gedeelde nonce-sleutel, #28 leë ILoggerFactory) en twee lae-erns inligtingsitems (#25 klasse-verstek-wanpassing, #27 heelgetal-oorvloei in tellers). Onmiddellike aandag word aanbeveel vir bevinding #28 (leë ILoggerFactory singleton) aangesien dit sekuriteitsverwante diagnostiese aantekening tydens nonce-bewerkings stil onderdruk. Bevinding #26 (gedeelde nonce-sleutel) moet aangespreek word om die per-versoek nonce-uniekheidsgaransie wat deur die CSP-spesifikasie vereis word te herstel.
