# Athbhreithniú Slándála — WebAppExperimental266

**Dáta:** 2026-05-05  
**Raon feidhme:** Anailís statach ar an gcódchiste iomlán  

---

## Tábla Achoimre

| # | Réimse | Déine |
|---|------|----------|
| 1 | Athúsáid IV AES-GCM i nginiúint nonce | 🔴 Criticiúil ✅ |
| 2 | Nonce logáilte mar théacs soiléir | 🔴 Criticiúil ✅ |
| 3 | Sreanga nonce cúltaca crua-chódaithe | 🔴 Criticiúil ✅ |
| 4 | Foclóir nonce domhanda neamh-shnáithe-sábháilte | 🟠 Ard |
| 5 | Bailíochtú eisitheora mTLS curtha ar ceal (tráchtáilte) | 🟠 Ard |
| 6 | Seiceáil cúlghairme mTLS as de réir réamhshocraithe | 🟠 Ard |
| 7 | OCSP i gcónaí bailí (stub) | 🟠 Ard |
| 8 | Auth/authz as de réir réamhshocraithe sa chumraíocht | 🟠 Ard |
| 9 | Ceanntásca slándála curtha i bhfeidhm ró-dhéanach sa phíblíne | 🟠 Ard |
| 10 | Fianán seisiúin gan Secure + SameSite | 🟡 Meán |
| 11 | Ceanntásc domhanda Set-Cookie míchumtha | 🟡 Meán |
| 12 | Content-Type iallachaithe mar text/html i ngach áit | 🟡 Meán |
| 13 | AllowedHosts socraithe mar wildcard | 🟡 Meán |
| 14 | Nonce gan cur i bhfeidhm ar chlibeanna `<script>` sa layout | 🟡 Meán |
| 15 | Ceanntásc Referrer-Policy ar iarraidh | 🟡 Meán |
| 16 | PII logáilte mar théacs soiléir | 🔵 Íseal |
| 17 | Cuid de shreang cheangail i logaí | 🔵 Íseal |
| 18 | Oibríochtaí Key Vault mar stubanna | 🔵 Íseal |
| 19 | Ceanntásc X-XSS-Protection atá as dáta | 🔵 Íseal |

---

## 🔴 Criticiúil

### 1. Athúsáid IV AES-GCM — Giniúint Nonce Briste go Cripteagrafach ✅ Ceartaithe i gcomait 45ae31b

**Comhaid:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

Úsáideann criptiú AES-GCM a ghineann nonceanna CSP **IV seasta a fhaightear ó Key Vault ar gach glao**.
Bristear AES-GCM nuair a athúsáidtear an IV leis an eochair chéanna: is féidir le hionsaitheoir a fheiceann
cúpla ciphertext iad a XORáil chun XOR na plainthéacsanna a fháil, agus is féidir clibeanna fíordheimhnithe a bhrionnú.

Tá an ceartú simplí — ní gá criptiú ar chor ar bith do nonce CSP. Níl de dhíth ar nonce CSP ach
a bheith **dothuartha agus uathúil in aghaidh an iarratais**; is leor agus is ceart glao ar
`RandomNumberGenerator.GetBytes(16)` a thiontú go Base64.

---

### 2. Luachanna Nonce Logáilte mar Théacs Soiléir ✅ Ceartaithe i gcomait bb6f27a

**Comhaid:** `Services/NonceMiddleware.cs` (líne 31), `Services/NonceRefresherService.cs` (líne 82)

Bhí nonce CSP ginte logáilte go díreach i logaí na feidhmchláir:

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

Faigheann aon duine a bhfuil rochtain aige ar na logaí nonce bailí agus is féidir leis CSP a sheachaint go héasca
chun script inlíne a instealladh.

---

### 3. Nonceanna Cúltaca Crua-Chódaithe ✅ Ceartaithe i gcomait 11cc9f7

**Comhad:** `Services/OptimizedNonceMiddleware.cs` (línte 53, 78, 92)

Má theipeann ar ghiniúint nonce nó má tá an chatalóg nonce folamh, úsáideann an middleware na sreanga liteartha
`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, agus `"error-fallback-nonce"`.
Tá na luachanna seo sa chód foinse agus ar eolas ag ionsaitheoirí. D'fhéadfadh coinníoll earráide
(m.sh. Key Vault neamh-inrochtana) nonce intuartha agus in-ionsaithe a chur sa cheanntásc CSP.

---

## 🟠 Ard

### 4. Úsáideann NonceCatalogService Dictionary Domhanda Neamh-Shnáithe-Sábháilte ✅ Ceartaithe i gcomait ae2b6c9

**Comhad:** `Services/NonceCatalogService.cs` (líne 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

Níl `Dictionary<TKey, TValue>` snáithe-sábháilte do léamh/scríobh comhuaineach. Faoi ualach,
d'fhéadfadh dhá iarratas a bhíonn ag rásaíocht chun an eochair nonce chéanna a nuashonrú
damáiste sonraí nó eisceachtaí a chruthú. Is singleton é an chatalóg nonce freisin
(domhanlár i bhfeidhm), rud a chiallaíonn gur féidir nonce iarratais amháin a fhorscríobh
ag iarratas eile i lár an tsreafa. Ba cheart `ConcurrentDictionary` a úsáid agus nonceanna a stóráil
in aghaidh an iarratais in `HttpContext.Items` in ionad stór domhanda comhroinnte.

---

### 5. Bailíochtú Eisiúna Deimhnithe mTLS mar Stub ✅ Ceartaithe i gcomait fd3d4fb

**Comhad:** `Extensions/ServiceCollectionExtensions.cs` (línte 305–313)

Tá an socrú `ValidateClientCertificateIssuer` ann agus `true` de réir réamhshocraithe,
ach tá an cód bailíochtaithe iarbhír tráchtáilte amach:

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

Le mTLS cumasaithe, d'fhéadfadh aon deimhniú cliaint ó aon eisitheoir
(a shlabhraíonn go fréamh iontaofa) fíordheimhniú a dhéanamh — níl srianta tionónta/eisitheora i bhfeidhm.

---

### 6. Seiceáil Cúlghairme Deimhnithe mTLS As de Réamhshocrú ✅ Ceartaithe i gcomait fd3d7b3

**Comhaid:** `Models/Settings/MtlsSettings.cs` (líne 26), `appsettings.template.json`

Tá `CheckCertificateRevocation` socraithe go `false` de réir réamhshocraithe sa mhúnla agus sa teimpléad.
D'fhéadfaí deimhnithe cliaint cúlghairthe a úsáid chun fíordheimhniú a dhéanamh go neamh-theoranta.
Le haghaidh mTLS táirgthe, ba cheart seiceáil cúlghairme a chumasú de réir réamhshocraithe.

---

### 7. OCSP mar Stub a Fhilleann Bailí i gCónaí ✅ Ceartaithe i gcomait b4c3807

**Comhad:** `Services/OcspValidationService.cs` (línte 149–163)

Is "template implementation" é `PerformOcspValidationAsync` go sainráite a fhilleann i gcónaí
`IsValid = true` tar éis `Task.Delay(100)`. Má chumasaítear OCSP sa chumraíocht, rithfidh sé
gach deimhniú mar bhailí go ciúin — lena n-áirítear cinn chúlghairthe — agus ní bheidh ach rabhadh logála ann.

---

### 8. Fíordheimhniú agus Údarú As de Réamhshocrú ✅ Ceartaithe i gcomait b392c47

**Comhad:** `appsettings.json` (línte 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

Seoltar an chumraíocht réamhshocraithe gan fíordheimhniú ná údarú. D'fhéadfadh forbróir a chóipeálann
`appsettings.template.json` (ina bhfuil sé seo as freisin) feidhmchlár oscailte a imscaradh mura dtugann sé faoi deara.
Ba cheart go mbeadh réamhshocruithe an teimpléid bunaithe ar rogha amach d'aon ghnó, ní ar rogha isteach.

---

### 9. Ceanntásca Slándála Curtha i bhFeidhm i ndiaidh Routing/Auth ✅ Ceartaithe i gcomait 016e57c

**Comhad:** `Program.cs` (línte 130–152)

Glaotar `UseNonceAndSecurityHeadersAsync` agus `UseStandardSecurityHeaders` i ndiaidh
`UseRouting`, `UseAuthentication`, agus `UseAuthorization`.
D'fhéadfadh freagraí a ghearrtar gearr sa phíblíne roimh sin (m.sh. atreoruithe 401, diúltuithe 403)
ceanntásca slándála a chailleadh. Ba cheart ceanntásca slándála a chur chomh luath agus is féidir sa phíblíne.

---

## 🟡 Meán

### 10. Fianán Seisiúin Gan `Secure` agus `SameSite` ✅ Ceartaithe i gcomait 8f2223c

**Comhad:** `Extensions/ServiceCollectionExtensions.cs` (línte 41–46)

Socraíonn an fianán seisiúin `HttpOnly = true` agus `IsEssential = true`, ach fágtar
`Cookie.SecurePolicy = CookieSecurePolicy.Always` agus `Cookie.SameSite = SameSiteMode.Strict` ar lár.
D'fhéadfadh an fianán dul thar HTTP soiléir (mura bhfuil an atreorú tar éis tarlú fós)
nó a bheith seolta tras-láithreán.

---

### 11. Ceanntásc Domhanda `Set-Cookie` Míchumtha ✅ Ceartaithe i gcomait 8f2223c

**Comhad:** `Extensions/ApplicationBuilderExtensions.cs` (líne 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

Cuireann sé seo ceanntásc `Set-Cookie` gan ainm ná luach le gach freagra.
Tá sé seo neamhbhailí agus d'fhéadfadh brabhsálaithe neamhaird a dhéanamh de (nó é a dhiúltú).
Ba cheart slándáil fianán a chumrú i roghanna an fhianáin ar leith,
ní mar cheanntásc amh domhanda.

---

### 12. `Content-Type` Iallachaithe mar `text/html` do Gach Freagra ✅ Ceartaithe i gcomait 8f2223c

**Comhad:** `Extensions/ApplicationBuilderExtensions.cs` (líne 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

Forscríobhann sé seo an Content-Type do gach freagra — API, JSON, íoslódálacha dénártha,
agús comhaid statacha. Téann sé seo salach ar `X-Content-Type-Options: nosniff`,
a choisceann an brabhsálaí ó chineál an ábhair a fhísiú thar an dearbhú.

---

### 13. `AllowedHosts` mar Wildcard ✅ Ceartaithe i gcomait 8f2223c

**Comhaid:** `appsettings.json` (líne 11), `appsettings.template.json` (líne 36)

```json
"AllowedHosts": "*"
```

Múchann sé seo bailíochtú ceanntásc ósta ionsuite ASP.NET Core.
Is féidir ionsaithe nimhiú taisce, nimhiú nasc athshocraithe pasfhocail,
agús atreoruithe oscailte a chumasú le hionsaí ceanntásc ósta.
Ba cheart an luach a shocrú do na fearainn shonracha amháin.

---

### 14. Ní Chuireann an Layout Nonce ar Chlibeanna `<script>` ✅ Ceartaithe i gcomait 8f2223c

**Comhad:** `Views/Shared/_Layout.cshtml`

Luchtaíonn an layout roinnt comhad JavaScript (`jquery.min.js`, `bootstrap.bundle.min.js`, `site.js`)
ach níl `nonce="@Context.Items["Nonce"]"` ar aon cheann de na clibeanna `<script>`.
Má chumasaítear CSP le nonceanna, bhlocálfadh an brabhsálaí na scripteanna seo.
Tá cur i bhfeidhm nonce ceangailte sa middleware ach gan úsáid sna radhairc,
rud a fhágann an córas nonce CSP neamhéifeachtach.

---

### 15. Ceanntásc Referrer-Policy ar Iarraidh ✅ Ceartaithe i gcomait 8f2223c

**Comhad:** `Extensions/ApplicationBuilderExtensions.cs`

Ní chuimsíonn na ceanntásca slándála caighdeánacha `Referrer-Policy`.
Gan é seo, seolann an brabhsálaí an URL iomlán sa cheanntásc `Referer`
chuig acmhainní tríú páirtí (m.sh. an CDN ArcGIS sa CSP),
a d'fhéadfadh cosáin seisiúin fhíordheimhnithe a sceitheadh.

---

## 🔵 Íseal / Faisnéiseach

### 16. PII Logáilte mar Théacs Soiléir ✅ Ceartaithe i gcomait 93bb4e9

**Comhad:** `Services/LoggingHelper.cs` (línte 85, 105)

Bhí OID úsáideora, ríomhphost, ainm, ID seisiúin, agus róil logáilte go díreach ar gach iarratas fíordheimhnithe:

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

Ag brath ar rialacháin phríobháideachais (GDPR, CCPA, HIPAA), d'fhéadfadh sé seo a bheith ina shaincheist chomhlíonta.
Moltar aitheantóirí a mhaisc nó a haisiú in aschur loga agus logaí ina bhfuil PII a sheoladh
go stór rialaithe cuí. Is féidir athchruthú seisiúin fhóiréinseach a choinneáil
trí hais HMAC-SHA256 chomhsheasmhach de na haitheantóirí a logáil.

---

### 17. Cuid de Shreang Cheangail i Logaí ✅ Ceartaithe i gcomait 93bb4e9

**Comhad:** `Extensions/ServiceCollectionExtensions.cs` (líne 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

Ní dea-chleachtas é fiú cuid de rún a logáil. Ina ionad sin,
ba cheart don logáil a dheimhniú go bhfuil sreang cheangail i láthair (neamhfholamh)
gan aon chuid di a nochtadh.

---

### 18. Oibríochtaí Key Vault mar Stubanna ✅ Ceartaithe i gcomait 93bb4e9

**Comhad:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

Is stubanna teimpléid iad `GetCertificateFromKeyVault` agus `GetSecretFromKeyVault`
a fhilleann `null`/luachanna samplacha. Le Key Vault cumasaithe,
fillfidh `GetCertificateFromKeyVault` `null`, rud a chruthaíonn `InvalidOperationException`
ag tosú — teip-thapa maith, ach ciallaíonn sé freisin nach bhfuil comhtháthú Key Vault iarbhír ann
le hiniúchadh rúin.

---

### 19. `X-XSS-Protection: 1; mode=block` as Dáta ✅ Ceartaithe i gcomait 93bb4e9

**Comhad:** `Extensions/ApplicationBuilderExtensions.cs` (líne 70)

Tá tacaíocht do `X-XSS-Protection` bainte ag brabhsálaithe nua-aimseartha.
Níl an ceanntásc díobhálach de ghnáth, ach cruthaíonn sé braistint bhréagach slándála.
Moltar CSP láidir a úsáid ina ionad. Uaireanta meastar `0` (an t-auditor XSS a mhúchadh)
níos sábháilte ná `1; mode=block` i mbrabhsálaithe níos sine mar gheall ar iompraíochtaí in-ionsaithe san auditor féin.
