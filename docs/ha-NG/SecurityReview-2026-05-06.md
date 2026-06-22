# Bincike na Tsaro — WebAppExperimental266

**Kwanan Wata:** 2026-05-06
**Fadin Bincike:** Cikakken bincike na ma'ajin code (bin sawu na binciken 2026-05-05)
**Mai Bincike:** Tsarin Bincike na Tsaro mai Atomatik

---

## Takaitaccen Bayani na Gudanarwa

Wannan bincike na bin sawu yana tabbatar da cewa duk gano matsalolin tsaro guda goma sha tara (19) da aka gano a lokacin binciken tsaro na 2026-05-05 an gyara su cikin nasara. Wannan bincike kuma ya gano sabon gano matsaloli ko sauran gano abubuwa guda biyar (5) a wannan lokaci. Matsayin tsaron aikace-aikacen ya inganta sosai tun daga bincike na farko.

---

## Matsayin Gano Matsalolin Da Suka Gabata (2026-05-05)

Duk gano matsalolin da suka gabata guda goma sha tara (19) **an tabbatar da an gyara su**:

| # | Gano Matsala | Matakin Tsanani | Matsayi |
|---|--------------|-----------------|---------|
| 1 | Sake amfani da AES-GCM IV a cikin samar da nonce | 🔴 Mai Tsanani Ƙwarai | ✅ An Gyara |
| 2 | An rubuta nonce zuwa ga kundin abubuwa na bayyane | 🔴 Mai Tsanani Ƙwarai | ✅ An Gyara |
| 3 | Haruffan nonce na tsarin da aka rubuta a tsaye | 🔴 Mai Tsanani Ƙwarai | ✅ An Gyara |
| 4 | Ma'ajin nonce na duniya ba shi da amincin zaren | 🟠 Babba | ✅ An Gyara |
| 5 | Tabbatar da asalin mTLS ba a tilasta ba | 🟠 Babba | ✅ An Gyara |
| 6 | Duba soke mTLS ta kasance a kashe ta atomatik | 🟠 Babba | ✅ An Gyara |
| 7 | OCSP kullum yana mayarwa ingantacce (stub) | 🟠 Babba | ✅ An Gyara |
| 8 | Tabbatarwa/Izini ya kasance a kashe ta atomatik a cikin saitin | 🟠 Babba | ✅ An Gyara |
| 9 | An ƙara taken tsaro a ƙarshen sarkar aiki | 🟠 Babba | ✅ An Gyara |
| 10 | Kukis na zama ba su da `Secure` + `SameSite` | 🟡 Matsakaici | ✅ An Gyara |
| 11 | An shigar da taken `Set-Cookie` na duniya | 🟡 Matsakaici | ✅ An Gyara |
| 12 | An rubuta `Content-Type` a tsaye zuwa `text/html` ko'ina | 🟡 Matsakaici | ✅ An Gyara |
| 13 | An saita `AllowedHosts` zuwa alama ta kowane abu | 🟡 Matsakaici | ✅ An Gyara |
| 14 | Ba a haɗa Nonce da alamun `<script>` a cikin tsarin | 🟡 Matsakaici | ✅ An Gyara |
| 15 | Taken `Referrer-Policy` baya nan | 🟡 Matsakaici | ✅ An Gyara |
| 16 | An rubuta PII zuwa ga kundin abubuwa na bayyane | 🔵 Ƙarami | ✅ An Gyara |
| 17 | Haɗe-haɗen haruffa a cikin saƙonni | 🔵 Ƙarami | ✅ An Gyara |
| 18 | Ayyukan Key Vault stubs ne | 🔵 Ƙarami | ✅ An Gyara |
| 19 | `X-XSS-Protection: 1; mode=block` ya tsufa | 🔵 Ƙarami | ✅ An Gyara |

---

## Sabon Gano / Sauran Abubuwa

| # | Wuri | Matakin Tsanani |
|---|------|-----------------|
| 20 | NonceRefresherService yana riƙe da abin da bai dace ba na Key Vault | 🟠 Babba |
| 21 | Ma'ajin ƙwaƙwalwar OcspValidationService yana amfani da Dictionary marar amincin zaren | 🟡 Matsakaici |
| 22 | Stub na tabbatar da OCSP yana nan tukuna — yana rufe idan ya kasa amma ba a aiwatar ba | 🔵 Ƙarami |
| 23 | mTLS tare da AllowedIssuers marar komai ya ƙi duk takardu (yana rufe idan ya kasa, ba a rubuta ba) | 🔵 Ƙarami |
| 24 | Saita atomatik OcspSettings.ServerUnavailableBehavior zuwa "Warn" (yana ba da izinin ci gaba a lokacin kurakurai) | 🔵 Ƙarami |

---

## Bayani Mai Cikakken Bayanai

### ✅ Gyare-gyaren Da Aka Tabbatar Daga 2026-05-05

#### 1. Sake Amfani da AES-GCM IV — An Gyara

**Fayil:** `Models/Main_Objects/Nonce.cs`

An sake rubuta samar da nonce gaba ɗaya daga AES-GCM. `Nonce.GenerateSecureNonce()` yanzu yana kiran `RandomNumberGenerator.Fill(randomBytes)` don bytes na bazuwar 16 kuma yana mayarwa da string na Base64. Babu dogaro da Key Vault, babu IV, babu ɓoye — wannan shi ne hanyar da ta dace don nonce na CSP.

---

#### 2. Darajar Nonce Ba a Rubuta Zuwa Ga Kundin Abubuwa Ba — An Gyara

**Fayiloli:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Fayiloli biyu suna rubuta saƙonni na matsayi kawai (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) kuma ba za su taɓa rubuta darajar nonce kai tsaye ba.

---

#### 3. An Cire Nonces na Tsarin Da Aka Rubuta a Tsaye — An Gyara

**Fayil:** `Services/OptimizedNonceMiddleware.cs`

An maye gurbin haruffan da aka rubuta a tsaye guda uku (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) da kiran `Nonce.GenerateSecureNonce()` a cikin hanyoyin al'ada da nuni guda biyu na kuskure na tsarin.

---

#### 4. Ma'ajin Nonce Mai Amincin Zaren — An Gyara

**Fayil:** `Services/NonceCatalogService.cs`

An canza `Dictionary<string, Nonce>` zuwa `ConcurrentDictionary<string, Nonce>`. `GetANonce` yanzu yana amfani da kira ɗaya na atomatik na `TryGetValue` maimakon duba mataki biyu.

---

#### 5. Tabbatar da Asalin mTLS Yanzu Yana Aiki — An Gyara

**Fayiloli:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

An maye gurbin mahallin tabbatar da asalin da aka rubuta a tsaye da kiran `mtlsSettings.IsIssuerAllowed(issuer)`, wanda ke yin kwatancen haruffa marar kula da harafi manya ko ƙanana akan `AllowedIssuers`. Idan jerin ya zama marar komai (ba a saita ba), hanya tana mayarwa `false`, tana ƙin duk takardu (yana rufe idan ya kasa).

---

#### 6. Duba Soke mTLS Yana Aiki ta Atomatik Yanzu — An Gyara

**Fayil:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` yanzu yana da atomatik `true`. `appsettings.template.json` kuma yana saita `"CheckCertificateRevocation": true`.

---

#### 7. Stub na OCSP Yanzu Yana Rufe Idan Ya Kasa — An Gyara

**Fayil:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` yanzu yana mayarwa `IsValid = false` tare da `OcspStatus.Error` kuma yana rubuta kuskuren, maimakon silently mayarwa `IsValid = true`. Kunna OCSP zai ƙi duk takardu har an sami aiwatarwa ta gaske.

---

#### 8. Tabbatarwa da Izini Yana Aiki ta Atomatik Yanzu — An Gyara

**Fayil:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` da `EnableAuthorization` dukansu yanzu suna da atomatik `true` a cikin ajin `FeatureFlags`. `appsettings.json` kuma yana saita dukansu zuwa `true`.

---

#### 9. An Ƙara Taken Tsaro Da Wuri a Sarkar Aiki — An Gyara

**Fayil:** `Program.cs`

Ana yanzu kiran `UseNonceAndSecurityHeadersAsync` da `UseStandardSecurityHeaders` kafin `UseRouting`, `UseAuthentication`, da `UseAuthorization`. Duk amsoshi, ciki har da amsoshi gajere 401/403, za su sami taken tsaro nasu.

---

#### 10–15. Kukis, Content-Type, AllowedHosts, Nonce a Tsarin, Referrer-Policy — An Gyara

**Fayiloli:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- An saita kukis na zama zuwa `CookieSecurePolicy.Always` da `SameSiteMode.Strict` yanzu.
- An cire taken `Set-Cookie` na duniya da ba a so ba wanda aka shigar.
- An cire sauyin duniya `Content-Type: text/html`.
- `AllowedHosts` a cikin `appsettings.json` yanzu `"localhost;127.0.0.1"`; tsarin yana amfani da `"{{YOUR_HOSTNAME}}"`.
- Duk alamun `<script>` guda uku a cikin `_Layout.cshtml` yanzu suna da `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` `UseStandardSecurityHeaders` ya ƙara.

---

#### 16–19. PII a Kundin Abubuwa, Haɗe-haɗen Haruffa, Key Vault Stubs, X-XSS-Protection — An Gyara

**Fayiloli:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- An HMAC-SHA256 hash duk PII (OID, imel, suna, SID, rawa) ta hanyar `LoggingHelper.HashPii()` kafin rubuta wa kundin abubuwa. Ana iya samar da makullin HMAC mai kwanciyar hankali ta `Logging:PiiHmacKey` a cikin saiti; ana amfani da makullin kowane tsari idan ba a saita ba.
- Sakon Cosmos DB yana tabbatar da haɗin string yana raye kawai (`!string.IsNullOrEmpty`), ba abubuwan da ke ciki ba.
- `AzureKeyVaultCertificateOperations` yanzu yana jefa `InvalidOperationException` a lokacin fara idan takarda ta zama null, maimakon silently mayarwa ƙimar metadata.
- An saita `X-XSS-Protection` zuwa `"0"` (yana kashe tsohuwar tace XSS), daidai da jagororin masu bincike na zamani.

---

## 🟠 Babba

### 20. NonceRefresherService Yana Riƙe da Dogaro da Gine-Gine na Key Vault Marar Amfani

**Fayil:** `Services/NonceRefresherService.cs`

`NonceRefresherService` yana shigar da sigogi na gine-gine don `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, da `IAzureKeyVaultOperationsService`. Tun da an sauƙaƙe samar da nonce don amfani da `RandomNumberGenerator` kai tsaye, ba a yi amfani da waɗannan dogaro ba.

**Matsala:** Lokacin da `EnableNonceServices = true` da `EnableKeyVault = false` (atomatik), ba a yi rajista da waɗannan sabis a cikin akwatin DI ba, suna haifar da `InvalidOperationException` a lokacin fara idan an warware sabis na nonce da farko. Wannan yanayi ne na rashin sabis da saiti na atomatik ya haifar. Ajin `FeatureFlags` yana saita atomatik `EnableNonceServices = true`, don haka kowane yanayi da ke dogaro ne kawai kan ƙimar atomatik na aji (ba tare da canjin `appsettings.json` ba) zai kasa.

**Shawara:** Cire sigogi guda huɗu na gine-gine marasa amfani da filayen sirri ɗaibinsu daga `NonceRefresherService`. Sabis yana buƙatar kawai `ILogger<NonceRefresherService>`, `ILoggerFactory`, da `INonceCatalogService`.

---

## 🟡 Matsakaici

### 21. Ma'ajin Ƙwaƙwalwar OcspValidationService Yana Amfani da Dictionary Marar Amincin Zaren

**Fayil:** `Services/OcspValidationService.cs` (layi 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` ba shi da aminci don karatu da rubutu iri ɗaya. Idan an yi rajista da `OcspValidationService` azaman singleton (ko idan an raba wannan misali a cikin buƙatun), tabbatar da OCSP na lokaci guda na iya lalata ma'aji, wanda ke haifar da rasa shigarwa, jefa ba tsammani, ko mayarwa ƙimomi masu tsufa.

**Shawara:** Maye gurbin `Dictionary<string, CachedOcspResponse>` da `ConcurrentDictionary<string, CachedOcspResponse>`. Sabunta kiran `_cache.Remove` (layi 103) zuwa `_cache.TryRemove`.

---

## 🔵 Ƙarami / Na Bayani

### 22. Stub na Tabbatar da OCSP — Yana Rufe Idan Ya Kasa Amma Ba a Aiwatar Ba

**Fayil:** `Services/OcspValidationService.cs` (layukan 157–173)

`PerformOcspValidationAsync` har yanzu stub ne. Gyaran gano #7 ya canza ɗabi'a daidai daga "kullum ingantacce" zuwa "kullum marar inganci (yana rufe idan ya kasa)". Duk da haka, wannan ba aiwatarwar OCSP ta gaske ba ce. Tare da `EnableOcspValidation = false` (atomatik), babu tasiri kan kasuwanci. Kafin kunna OCSP a kowane yanayi, dole ne a aiwatar da aiwatarwar matsayin OCSP ta gaske.

---

### 23. mTLS Tare da AllowedIssuers Marar Komai Yana Ƙi Duk Takardu na Abokan Ciniki

**Fayil:** `Models/Settings/MtlsSettings.cs`

Lokacin da `ValidateClientCertificateIssuer = true` (atomatik) da `AllowedIssuers` marar komai (kuma atomatik idan ba a saita ba), `IsIssuerAllowed()` yana mayarwa `false`, yana ƙin duk takardu na abokan ciniki. Wannan ɗabi'a ce ta "rufe idan ya kasa" da ake so, amma ba a rubuta ta ba. Masu aiki da ke kunna mTLS ba tare da karanta ƙirar da kyau ba na iya gano cewa an ƙi duk takardu na abokan ciniki ba tare da bayanin bayyananne ba.

**Shawara:** Ƙara gargaɗin kundin abubuwa na fara lokacin da `ValidateClientCertificateIssuer = true` da `AllowedIssuers` marar komai ne.

---

### 24. Saita Atomatik OcspSettings.ServerUnavailableBehavior zuwa "Warn"

**Fayil:** `appsettings.template.json` (layi 134), `Services/OcspValidationService.cs`

Ƙimar atomatik ta `ServerUnavailableBehavior` ita ce `"Warn"` a cikin ƙirar, tana ba da izinin buƙatun ci gaba idan sabis na OCSP ba za a isa ba. Ga yanayi masu tsaro na manyan matakai, wannan ya kamata ya zama `"Fail"` don hana gazawar sabis na OCSP daga yin shuru yin sakaci kan duba soke takarda.

**Shawara:** Rubuta duk zaɓuɓɓuka uku (`Fail`, `Allow`, `Warn`) a sarari a cikin ƙirar kuma la'akari da canza atomatik zuwa `"Fail"` daidai da ka'idar ƙarancin izini.

---

## Duba Taken Tsaro (Matsayi Na Yanzu)

`UseStandardSecurityHeaders` yana ƙara waɗannan taken:

| Taken | Ƙima | Duba |
|-------|------|------|
| `X-Frame-Options` | `DENY` | ✅ Daidai |
| `X-XSS-Protection` | `0` | ✅ Daidai (yana kashe tsohuwar tace XSS) |
| `X-Content-Type-Options` | `nosniff` | ✅ Daidai |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Daidai |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Daidai |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Daidai |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Daidai |
| `Permissions-Policy` | wuri, kyamara, makirufo, interest-cohort an kashe | ✅ Daidai |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Daidai |
| `Content-Security-Policy` | Dogaro da nonce, an haɗa lokacin da aka kunna CSP | ✅ Daidai |
| `Server` | An ɓoye zuwa `"webserver"` | ✅ Daidai |
| `X-Powered-By` | An cire | ✅ Daidai |

---

## Ƙimar Gaba ɗaya

Aikace-aikacen ya gyara duk raunin tsanani mai ƙarfi da babba daga bincike na farko. Gano matsalolin na yanzu an iyakance shi zuwa ɗaya na tsanani mai babba na matsalar saiti/DI (gano #20) da abubuwa na bayani mai ƙarami. Matsayin tsaro ya inganta sosai. Ana ba da shawarar aiki gaggawa don gano #20 (dogaro da DI marasa amfani a cikin NonceRefresherService) tun da yana iya hana aikace-aikacen farawa a ƙarƙashin saiti na atomatik.
