# Sekuriteitsoorsiening — WebAppExperimental266

**Datum:** 2026-05-05  
**Omvang:** Volledige kodebaseer statiese analise  

---

## Opsommingstabel

| # | Area | Erns |
|---|------|------|
| 1 | AES-GCM IV-hergebruik in nonce-generering | 🔴 Kritiek ✅ |
| 2 | Nonce in klarteks aangeteken | 🔴 Kritiek ✅ |
| 3 | Hardgekodeerde terugval-nonce-strings | 🔴 Kritiek ✅ |
| 4 | Nie-draadveilige globale nonce-woordeboek | 🟠 Hoog |
| 5 | mTLS-uitreiker-validasie uitgekommentarieer | 🟠 Hoog |
| 6 | mTLS-herroepingskontrolering standaard af | 🟠 Hoog |
| 7 | OCSP gee altyd geldig terug (stub) | 🟠 Hoog |
| 8 | Verifikasie/Magtiging standaard af in konfigurasie | 🟠 Hoog |
| 9 | Sekuriteitsopskrifte te laat in pyplyn toegepas | 🟠 Hoog |
| 10 | Sessiekoekie mis `Secure` + `SameSite`-attribute | 🟡 Medium |
| 11 | Misvormde globale `Set-Cookie`-opskrif | 🟡 Medium |
| 12 | `Content-Type` gedwing na `text/html` oral | 🟡 Medium |
| 13 | `AllowedHosts` is wildkaart | 🟡 Medium |
| 14 | Nonce nie op `<script>`-etikette in uitleg toegepas nie | 🟡 Medium |
| 15 | `Referrer-Policy`-opskrif ontbreek | 🟡 Medium |
| 16 | PII in klarteks aangeteken | 🔵 Laag |
| 17 | Gedeeltelike verbindingstring in logs | 🔵 Laag |
| 18 | Key Vault-bewerkings is stubs | 🔵 Laag |
| 19 | Verouderde `X-XSS-Protection`-opskrif | 🔵 Laag |

---

## 🔴 Kritiek

### 1. AES-GCM IV-hergebruik — Nonce-generering is Kriptografies Gebreek ✅ Reggestel in pleeg 45ae31b

**Lêers:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

Die AES-GCM-enkripsie wat CSP-nonces genereer, gebruik 'n **vaste IV wat by elke aanroep van Key Vault gehaal word**. AES-GCM breek wanneer die IV met dieselfde sleutel hergebruik word: 'n aanvaller wat twee sifertekste waarneem, kan hulle XOR om die XOR van die klartekste te herstel, en verifikasie-etikette kan vervals word.

Die regstelling is eenvoudig — CSP-nonces het hoegenaamd nie enkripsie nodig nie. 'n CSP-nonce moet slegs **onvoorspelbaar en uniek per versoek** wees; 'n aanroep na `RandomNumberGenerator.GetBytes(16)` omgeskakel na Base64 is voldoende en korrek.

---

### 2. Nonce-waardes in Klarteks Aangeteken ✅ Reggestel in pleeg bb6f27a

**Lêers:** `Services/NonceMiddleware.cs` (reël 31), `Services/NonceRefresherService.cs` (reël 82)

Die gegenereerde CSP-nonce word woordeliks in die toepassingslogs aangeteken:

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

Enigeen met toegang tot die logs kry 'n geldige nonce en kan CSP trivial omseil om inlynskrifte in te spuit.

---

### 3. Hardgekodeerde Terugval-Nonces ✅ Reggestel in pleeg 11cc9f7

**Lêer:** `Services/OptimizedNonceMiddleware.cs` (reëls 53, 78, 92)

As nonce-generering misluk of die nonce-katalogus leeg is, val die middleware terug na die stringleterale `"bootstrap-nonce-placeholder"`, `"fallback-nonce"` en `"error-fallback-nonce"`. Hierdie strings word na bronkode gepleeg en is bekend aan aanvallers. 'n Foutsituasie (bv. Key Vault onbeskikbaar) sou 'n voorspelbare, uitbuitbare nonce in die CSP-opskrif plaas.

---

## 🟠 Hoog

### 4. NonceCatalogService Gebruik 'n Nie-Draadveilige Statiese Woordeboek ✅ Reggestel in pleeg ae2b6c9

**Lêer:** `Services/NonceCatalogService.cs` (reël 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` is nie draadveilig vir gelyktydige lees en skryf nie. Onder las kan twee versoeke wat om dieselfde nonce-sleutel wedywer, datavervuiling of uitgeworpe uitsonderings veroorsaak. Die nonce-katalogus is ook 'n enkelvoud (effektief 'n globale), wat beteken een versoek se nonce deur 'n ander versoek mid-vlug oorskryf kan word — 'n nonce-botsing tussen versoeke. Gebruik `ConcurrentDictionary` en stoor nonces per-versoek in `HttpContext.Items` eerder as in 'n gedeelde globale.

---

### 5. mTLS-Sertifikaat-Uitreiker-Validasie is Uitgeskakel ✅ Reggestel in pleeg fd3d4fb

**Lêer:** `Extensions/ServiceCollectionExtensions.cs` (reëls 305–313)

Die `ValidateClientCertificateIssuer`-instelling bestaan en is standaard `true`, maar die werklike valideringskode is uitgekommentarieer:

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

Met mTLS geaktiveer, kan enige kliëntsertifikaat van enige uitreiker (wat na 'n vertroude wortel ketting) verifieer — geen huurder/uitreiker-beperking geld nie.

---

### 6. mTLS-Sertifikaat-Herroepingskontrolering Standaard Gedeaktiveer ✅ Reggestel in pleeg fd3d7b3

**Lêers:** `Models/Settings/MtlsSettings.cs` (reël 26), `appsettings.template.json`

`CheckCertificateRevocation` stel standaard op `false` in beide die model en die sjabloon. Herroepde kliëntsertifikate kan onbepaald gebruik word om te verifieer. Vir produksie-mTLS moet herroepingskontrolering standaard geaktiveer wees.

---

### 7. OCSP-Validasie is 'n Stub wat Altyd Geldig Terugstuur ✅ Reggestel in pleeg b4c3807

**Lêer:** `Services/OcspValidationService.cs` (reëls 149–163)

Die `PerformOcspValidationAsync`-metode is uitdruklik 'n "sjabloon-implementasie" wat altyd `IsValid = true` terugstuur na 'n `Task.Delay(100)`. As OCSP-validasie ooit in konfigurasie geaktiveer word, sal dit stilswyend alle sertifikate — insluitend herroepde sertifikate — as geldig deurgee, terwyl dit 'n waarskuwing aanteken wat maklik gemis kan word.

---

### 8. Verifikasie en Magtiging Standaard Gedeaktiveer ✅ Reggestel in pleeg b392c47

**Lêer:** `appsettings.json` (reëls 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

Die standaardkonfigurasie word gestuur sonder verifikasie of magtiging. 'n Ontwikkelaar wat `appsettings.template.json` kopieer (wat ook hierdie af het) sonder om die dokumentasie noukeurig te lees, sal 'n oop toepassing ontplooi. Die sjabloonststandaarde moet 'n doelbewuste keuse uit vereis, nie keuse in nie.

---

### 9. Sekuriteitsopskrifte Na Roetebepaling/Verifikasie Toegepas ✅ Reggestel in pleeg 016e57c

**Lêer:** `Program.cs` (reëls 130–152)

`UseNonceAndSecurityHeadersAsync` en `UseStandardSecurityHeaders` word na `UseRouting`, `UseAuthentication` en `UseAuthorization` geroep. Responsse wat die pyplyn kortsluit voor hierdie middleware bereik (bv. 401-herleidings, 403-weierings) ontvang moontlik nie sekuriteitsopskrifte nie. Sekuriteitsopskrifte moet so vroeg in die pyplyn as moontlik wees.

---

## 🟡 Medium

### 10. Sessiekoekie Mis `Secure` en `SameSite`-attribute ✅ Reggestel in pleeg 8f2223c

**Lêer:** `Extensions/ServiceCollectionExtensions.cs` (reëls 41–46)

Die sessiekoekie stel `HttpOnly = true` en `IsEssential = true`, maar laat `Cookie.SecurePolicy = CookieSecurePolicy.Always` en `Cookie.SameSite = SameSiteMode.Strict` weg. Die koekie kan oor gewone HTTP gestuur word (as die herleiing nog nie gevuur het nie) of kruissite gestuur word.

---

### 11. Misvormde Globale `Set-Cookie`-Opskrif ✅ Reggestel in pleeg 8f2223c

**Lêer:** `Extensions/ApplicationBuilderExtensions.cs` (reël 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

Dit voeg 'n naamloos, waardeloos `Set-Cookie`-opskrif by elke respons. Dit is ongeldig en sal deur blaaiers geïgnoreer (of verwerp) word, maar dit produseer verrassende artefakte in alle responsse insluitend statiese lêers, JSON API-responsse en gesondheidskontroles. Koekie-sekuriteit moet in die koekie-opsies van die spesifieke koekie wat gekonfigureer word, gestel word, nie as 'n rou opskrif globaal ingespuit word nie.

---

### 12. `Content-Type` Gedwing na `text/html` vir Alle Responsse ✅ Reggestel in pleeg 8f2223c

**Lêer:** `Extensions/ApplicationBuilderExtensions.cs` (reël 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

Dit oorskryf Content-Type vir elke respons — API-eindpunte, JSON, binêre aflaaie en statiese lêers sal almal beweer dat hulle `text/html` is. Dit is in konflik met `X-Content-Type-Options: nosniff`, wat blaaiers verhoed om die verklaarde inhoudstype te oorskry.

---

### 13. `AllowedHosts` Gestel op Wildkaart ✅ Reggestel in pleeg 8f2223c

**Lêers:** `appsettings.json` (reël 11), `appsettings.template.json` (reël 36)

```json
"AllowedHosts": "*"
```

Dit deaktiveer ASP.NET Core se ingeboude gasheer-opskrif-validasie. Gasheerskop-inspuiting-aanvalle laat kasberging-vergiftiging, wagwoord-herstel-skakel-vergiftiging en oop herleidings toe. Dit moet op die spesifieke domein(e) gestel word waarop die toepassing bedien word.

---

### 14. Uitleg Pas Nonce Nie op `<script>`-Etikette toe nie ✅ Reggestel in pleeg 8f2223c

**Lêer:** `Views/Shared/_Layout.cshtml`

Die uitleg laai verskeie JavaScript-lêers (`jquery.min.js`, `bootstrap.bundle.min.js`, `site.js`) maar geen van die `<script>`-etikette sluit `nonce="@Context.Items["Nonce"]"` in nie. As CSP met nonces geaktiveer is, sal die blaaier hierdie skrifte blokkeer. Die nonce-implementasie is in middleware bedraad maar nie in aansigte verbruik nie, wat die CSP-nonce-stelsel ondoeltreffend maak.

---

### 15. `Referrer-Policy`-Opskrif Ontbreek ✅ Reggestel in pleeg 8f2223c

**Lêer:** `Extensions/ApplicationBuilderExtensions.cs`

Die standaard sekuriteitsopskrifte sluit nie `Referrer-Policy` in nie. Sonder dit stuur die blaaier die volledige URL in die `Referer`-opskrif na derdepartybronne (bv. die ArcGIS CDN wat in die CSP ingesluit is), wat geverifieerde sessie-paaie kan laat uitlek.

---

## 🔵 Laag / Inligtend

### 16. PII in Klarteks Aangeteken ✅ Reggestel in pleeg 93bb4e9

**Lêer:** `Services/LoggingHelper.cs` (reëls 85, 105)

Gebruiker OID, e-pos, naam, sessie-ID en rolle word woordeliks by elke geverifieerde versoek aangeteken:

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

Afhangende van toepaslike privaatheidregulasies (GDPR, CCPA, HIPAA) kan dit 'n voldoeningskwessie wees. Oorweeg om identifiseerders in loguitvoer te masker of te hash en PII-bevattende logs na 'n toepaslik beheerde wasbak te stuur.

---

### 17. Gedeeltelike Verbindingstring in Logs ✅ Reggestel in pleeg 93bb4e9

**Lêer:** `Extensions/ServiceCollectionExtensions.cs` (reël 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

Selfs 'n gedeeltelike geheim in logs is nie beste praktyk nie. Die logstelling moet eerder bevestig dat 'n verbindingstring teenwoordig (nie-leeg) is eerder as om enige gedeelte daarvan aan te teken.

---

### 18. Key Vault-Bewerkings is Stubs ✅ Reggestel in pleeg 93bb4e9

**Lêer:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

Beide `GetCertificateFromKeyVault` en `GetSecretFromKeyVault` is sjabloonstubs wat `null`/dummy-waardes terugstuur. Met Key Vault geaktiveer, stuur `GetCertificateFromKeyVault` `null` terug, wat 'n `InvalidOperationException` by opstart veroorsaak — 'n goeie misluk-vinnig, maar dit beteken ook dat daar geen werklike Key Vault-integrasie is om vir geheime-hantering te oudit nie.

---

### 19. `X-XSS-Protection: 1; mode=block` is Verouderd ✅ Reggestel in pleeg 93bb4e9

**Lêer:** `Extensions/ApplicationBuilderExtensions.cs` (reël 70)

Moderne blaaiers het ondersteuning vir `X-XSS-Protection` verwyder. Die opskrif is nie skadelik nie, maar dit gee 'n valse sekuriteitsgevoel. Die aanbevole benadering is om eerder op 'n sterk CSP staat te maak. Die `0`-waarde (deaktiveer die XSS-ouditor) word soms as veiliger beskou as `1; mode=block` vir ouer blaaiers omdat die ouditor self uitbuitbare gedrag gehad het.
