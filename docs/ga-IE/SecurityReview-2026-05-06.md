# Athbhreithniú Slándála — WebAppExperimental266

**Dáta:** 2026-05-06
**Raon:** Iniúchadh iomlán ar an stór cód (leantach ar an athbhreithniú 2026-05-05)
**Athbhreithneoir:** Athbhreithniú Slándála Uathoibríoch

---

## Achoimre Feidhmiúcháin

Deimhníonn an t-athbhreithniú leantach seo gur ceartaíodh go rathúil na naoi mbuairt slándála déag (19) a aithníodh le linn an athbhreithnithe slándála 2026-05-05. Aithníonn an t-athbhreithniú seo freisin cúig (5) mbuairt nua nó iarmhartach a fuarthas ag an am seo. Tá feabhas suntasach tagtha ar staid slándála fhoriomlán an fheidhmchláir ón athbhreithniú roimhe.

---

## Stádas na mBuairtí Roimhe Seo (2026-05-05)

Tá na naoi mbuairt déag (19) roimhe seo **deimhnithe mar cheartaithe**:

| # | Buairt | Tromchúis | Stádas |
|---|--------|-----------|--------|
| 1 | Athúsáid AES-GCM IV laistigh de ghiniúint nonce | 🔴 Criticiúil | ✅ Ceartaithe |
| 2 | Nonce scríofa chuig logaí gnáthtéacs | 🔴 Criticiúil | ✅ Ceartaithe |
| 3 | Teaghráin socraithe nonce códaithe go crua | 🔴 Criticiúil | ✅ Ceartaithe |
| 4 | Stór nonce domhanda nach bhfuil sábháilte do shnáitheanna | 🟠 Ard | ✅ Ceartaithe |
| 5 | Fíorú eisitheoir deimhnithe cliant mTLS gan fhorfheidhmiú | 🟠 Ard | ✅ Ceartaithe |
| 6 | Seiceáil cúlghairm deimhnithe mTLS díchumasaithe de réir réamhshocraithe | 🟠 Ard | ✅ Ceartaithe |
| 7 | OCSP ag filleadh bailí i gcónaí (stub) | 🟠 Ard | ✅ Ceartaithe |
| 8 | Fíordheimhniú/Údarú díchumasaithe de réir réamhshocraithe i mbratacha gnéithe | 🟠 Ard | ✅ Ceartaithe |
| 9 | Ceanntásca slándála curtha leis go déanach sa phíblíne | 🟠 Ard | ✅ Ceartaithe |
| 10 | Fianáin seisiúin ag easnamh `Secure` + `SameSite` | 🟡 Measartha | ✅ Ceartaithe |
| 11 | Ceanntásc `Set-Cookie` domhanda instealladh | 🟡 Measartha | ✅ Ceartaithe |
| 12 | `Content-Type` códaithe go crua mar `text/html` gach áit | 🟡 Measartha | ✅ Ceartaithe |
| 13 | `AllowedHosts` socraithe ar chomhartha wildcard | 🟡 Measartha | ✅ Ceartaithe |
| 14 | Nonce gan cheangal le clibeanna `<script>` sa teimpléad | 🟡 Measartha | ✅ Ceartaithe |
| 15 | Ceanntásc `Referrer-Policy` ar iarraidh | 🟡 Measartha | ✅ Ceartaithe |
| 16 | PII scríofa chuig logaí gnáthtéacs | 🔵 Íseal | ✅ Ceartaithe |
| 17 | Comhchumasc teaghrán i dteachtaireachtaí | 🔵 Íseal | ✅ Ceartaithe |
| 18 | Oibríochtaí Key Vault ina stubs | 🔵 Íseal | ✅ Ceartaithe |
| 19 | `X-XSS-Protection: 1; mode=block` as dáta | 🔵 Íseal | ✅ Ceartaithe |

---

## Buairtí Nua / Iarmhartacha

| # | Suíomh | Tromchúis |
|---|--------|-----------|
| 20 | NonceRefresherService ag coinneáil spleáchas tógóra Key Vault neamhúsáidte | 🟠 Ard |
| 21 | Taisce cuimhne OcspValidationService ag úsáid Dictionary nach bhfuil sábháilte do shnáitheanna | 🟡 Measartha |
| 22 | Stub fíoraithe OCSP fós i láthair — teip-dúnta ach gan cur i bhfeidhm | 🔵 Íseal |
| 23 | mTLS le AllowedIssuers folamh ag diúltú do gach deimhniú cliant (teip-dúnta, gan doiciméadú) | 🔵 Íseal |
| 24 | OcspSettings.ServerUnavailableBehavior réamhshocraithe mar "Warn" (ceadaíonn pasáiste le linn earráidí) | 🔵 Íseal |

---

## Buairtí Mionsonraithe

### ✅ Ceartúcháin Deimhnithe ó 2026-05-05

#### 1. Athúsáid AES-GCM IV — Ceartaithe

**Comhad:** `Models/Main_Objects/Nonce.cs`

Athscríobhadh giniúint nonce go hiomlán ó AES-GCM. Glaodh `Nonce.GenerateSecureNonce()` anois ar `RandomNumberGenerator.Fill(randomBytes)` le 16 bheart randamach agus filleann sé teaghrán Base64. Gan spleáchas Key Vault, gan IV, gan criptiú — is é seo an cur chuige ceart le haghaidh nonce CSP.

---

#### 2. Luach Nonce Gan Scríobh chuig Logaí — Ceartaithe

**Comhaid:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Scríobhann an dá chomhad teachtaireachtaí stádais amháin (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) agus ní scríobhann siad luach nonce féin riamh.

---

#### 3. Noncanna Socraithe Códaithe go Crua Bainte — Ceartaithe

**Comhad:** `Services/OptimizedNonceMiddleware.cs`

Rinneadh na trí theaghrán códaithe go crua (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) a athsholáthar le glaonna ar `Nonce.GenerateSecureNonce()` ar shlite gnátha agus dhá tháscaire earráide socraithe.

---

#### 4. Stór Nonce Sábháilte do Shnáitheanna — Ceartaithe

**Comhad:** `Services/NonceCatalogService.cs`

Athraíodh `Dictionary<string, Nonce>` go `ConcurrentDictionary<string, Nonce>`. Úsáideann `GetANonce` anois glao `TryGetValue` adamhach amháin in ionad seiceáil dhá chéim.

---

#### 5. Fíorú Eisitheoir Deimhnithe Cliant mTLS ag Obair Anois — Ceartaithe

**Comhaid:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Athsholáthraíodh an comhthéacs fíoraithe eisitheora códaithe go crua le glao `mtlsSettings.IsIssuerAllowed(issuer)`, a dhéanann comparáid teaghráin neamhíogair cás in aghaidh `AllowedIssuers`. Má tá an liosta folamh (gan cumrú), filleann an modh `false`, ag diúltú do gach deimhniú (teip-dúnta).

---

#### 6. Seiceáil Cúlghairme Deimhnithe mTLS Cumasaithe de Réir Réamhshocraithe Anois — Ceartaithe

**Comhad:** `Models/Settings/MtlsSettings.cs`

Is é `true` réamhshocrú `CheckCertificateRevocation` anois. Socrófar `appsettings.template.json` freisin ar `"CheckCertificateRevocation": true`.

---

#### 7. Teipeann Stub OCSP go Dúnta Anois — Ceartaithe

**Comhad:** `Services/OcspValidationService.cs`

Filleann `PerformOcspValidationAsync` anois `IsValid = false` le `OcspStatus.Error` agus logálann sé an earráid, in ionad `IsValid = true` a fhilleadh go ciúin. Diúltóidh cumasú OCSP do gach deimhniú go dtí go mbeidh cur i bhfeidhm fíor ar fáil.

---

#### 8. Fíordheimhniú agus Údarú Cumasaithe de Réir Réamhshocraithe Anois — Ceartaithe

**Comhad:** `Models/Settings/FeatureFlags.cs`

Tá `EnableAzureAd` agus `EnableAuthorization` araon ina réamhshocrú `true` sa rang `FeatureFlags` anois. Socrófar `appsettings.json` freisin araon go `true`.

---

#### 9. Ceanntásca Slándála Curtha Leis go Luath sa Phíblíne — Ceartaithe

**Comhad:** `Program.cs`

Glaoitear `UseNonceAndSecurityHeadersAsync` agus `UseStandardSecurityHeaders` anois roimh `UseRouting`, `UseAuthentication`, agus `UseAuthorization`. Gheobhaidh gach freagra, lena n-áirítear freagraí ciorcaid-ghearr 401/403, a gceanntásca slándála.

---

#### 10–15. Fianáin, Content-Type, AllowedHosts, Nonce sa Teimpléad, Referrer-Policy — Ceartaithe

**Comhaid:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Socrófar fianáin seisiúin anois ar `CookieSecurePolicy.Always` agus `SameSiteMode.Strict`.
- Baineadh an ceanntásc `Set-Cookie` domhanda neamhfheidhmiúil instealladh.
- Baineadh an athróg `Content-Type: text/html` domhanda.
- Is é `"localhost;127.0.0.1"` atá anois in `AllowedHosts` in `appsettings.json`; úsáideann an teimpléad `"{{YOUR_HOSTNAME}}"`.
- Tá `nonce="@Context.Items["Nonce"]"` anois ag na trí chlib `<script>` go léir in `_Layout.cshtml`.
- Cuireadh `Referrer-Policy: strict-origin-when-cross-origin` leis ag `UseStandardSecurityHeaders`.

---

#### 16–19. PII i Logaí, Comhchumasc Teaghrán, Key Vault Stubs, X-XSS-Protection — Ceartaithe

**Comhaid:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Déantar haischódú HMAC-SHA256 ar gach PII (OID, ríomhphost, ainm, SID, róil) trí `LoggingHelper.HashPii()` roimh scríobh chuig logaí. Is féidir an eochair HMAC cobhsaí a sholáthar trí `Logging:PiiHmacKey` sa chumraíocht; úsáidtear eochair in aghaidh próisis mura gcumraítear.
- Ní fhíoraíonn teachtaireacht loga Cosmos DB ach go bhfuil an teaghrán comhchumaisc ann (`!string.IsNullOrEmpty`), ní a bhfuil ann.
- Caithfidh `AzureKeyVaultCertificateOperations` `InvalidOperationException` anois le linn tosaithe má tá an deimhniú null, in ionad luachanna meiteashonraí a fhilleadh go ciúin.
- Socrófar `X-XSS-Protection` ar `"0"` anois (díchumasaíonn scagaire XSS oidhreachta), ag teacht le treoirlínte brabhsálaithe nua-aimseartha.

---

## 🟠 Ard

### 20. NonceRefresherService ag Coinneáil Spleáchas Tógóra Key Vault Neamhúsáidte

**Comhad:** `Services/NonceRefresherService.cs`

Insteallann `NonceRefresherService` paraiméadair tógóra do `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, agus `IAzureKeyVaultOperationsService`. Ós rud é gur simplíodh giniúint nonce chun `RandomNumberGenerator` a úsáid go díreach, ní úsáidtear aon cheann de na spleáchais.

**Fadhb:** Nuair is `true` `EnableNonceServices` agus is `false` `EnableKeyVault` (réamhshocrú), ní chlárófar na seirbhísí seo sa choimeádán DI, ag cruthú `InvalidOperationException` le linn tosaithe nuair a réitítear an tseirbhís nonce ar dtús. Is riocht neamhinfhaighteachta seirbhíse é seo a chruthaíonn an réamhchumraíocht. Socrófar an rang `FeatureFlags` de réir réamhshocraithe `EnableNonceServices = true`, mar sin teipfidh ar aon timpeallacht a bhíonn ag brath ar réamhshocranna ranga amháin (gan athruithe `appsettings.json`).

**Moladh:** Bain na ceithre pharaiméadar tógóra neamhúsáidte agus a gcuid réimsí príobháideacha comhfhreagracha as `NonceRefresherService`. Ní theastaíonn ón tseirbhís ach `ILogger<NonceRefresherService>`, `ILoggerFactory`, agus `INonceCatalogService`.

---

## 🟡 Measartha

### 21. Taisce Cuimhne OcspValidationService ag Úsáid Dictionary Nach Bhfuil Sábháilte do Shnáitheanna

**Comhad:** `Services/OcspValidationService.cs` (líne 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

Níl `Dictionary<TKey, TValue>` sábháilte le haghaidh léamh agus scríobh comhthráthach. Má chláraítear `OcspValidationService` mar aontonach (nó má roinntear an sampla céanna thar iarratais), d'fhéadfadh fíoraitheoirí OCSP comhthráthach an taisce a éilliú, ag cur iontrálacha caillte, eisceachtaí gan choinne, nó sonraí as dáta ag filleadh faoi deara.

**Moladh:** Athsholáthair `Dictionary<string, CachedOcspResponse>` le `ConcurrentDictionary<string, CachedOcspResponse>`. Nuashonraigh an glao `_cache.Remove` (líne 103) go `_cache.TryRemove`.

---

## 🔵 Íseal / Faisnéiseach

### 22. Stub Fíoraithe OCSP — Teip-Dúnta ach Gan Cur i bhFeidhm

**Comhad:** `Services/OcspValidationService.cs` (línte 157–173)

Is stub fós é `PerformOcspValidationAsync`. Bhí ceartúchán buairte #7 ceart chun iompar a athrú ó "bailí i gcónaí" go "neamhbhailí i gcónaí (teip-dúnta)". Mar sin féin, ní cur i bhfeidhm OCSP fíor é seo. Le `EnableOcspValidation = false` (réamhshocrú), níl aon tionchar gnó ann. Sula gcumasófar OCSP in aon timpeallacht, ní mór cur i bhfeidhm stádas OCSP fíor a chur i bhfeidhm.

---

### 23. mTLS le AllowedIssuers Folamh ag Diúltú do Gach Deimhniú Cliant

**Comhad:** `Models/Settings/MtlsSettings.cs`

Nuair is `true` `ValidateClientCertificateIssuer` (réamhshocrú) agus atá `AllowedIssuers` folamh (réamhshocrú freisin mura gcumraítear), filleann `IsIssuerAllowed()` `false`, ag diúltú do gach deimhniú cliant. Is iompar teip-dúnta inmhianaithe é seo, ach níl sé doiciméadaithe go soiléir. D'fhéadfadh oibreoirí a chumasaíonn mTLS gan an teimpléad a léamh go cúramach a fháil amach go bhfuil gach deimhniú cliant á dhiúltú gan míniú soiléir.

**Moladh:** Cuir rabhadh logála tosaithe leis nuair is `true` `ValidateClientCertificateIssuer` agus atá `AllowedIssuers` folamh.

---

### 24. OcspSettings.ServerUnavailableBehavior Réamhshocraithe mar "Warn"

**Comhad:** `appsettings.template.json` (líne 134), `Services/OcspValidationService.cs`

Is é `"Warn"` réamhshocrú `ServerUnavailableBehavior` sa teimpléad, a cheadaíonn iarratais dul tríd nuair nach féidir an tseirbhís OCSP a bhaint amach. Le haghaidh timpeallachtaí ardslándála, ba chóir é seo a bheith ina `"Fail"` chun cosc a chur ar chliseadh seirbhíse OCSP ó sheiceáil cúlghairme deimhnithe a sheachaint go ciúin.

**Moladh:** Doiciméadaigh na trí rogha go soiléir (`Fail`, `Allow`, `Warn`) sa teimpléad agus breithniú a dhéanamh ar an réamhshocrú a athrú go `"Fail"` de réir phrionsabal na pribhléide is lú.

---

## Measúnú Ceanntásc Slándála (Stádas Reatha)

Cuirtear na ceanntásca seo leis ag `UseStandardSecurityHeaders`:

| Ceanntásc | Luach | Measúnú |
|-----------|-------|---------|
| `X-Frame-Options` | `DENY` | ✅ Maith |
| `X-XSS-Protection` | `0` | ✅ Maith (díchumasaíonn scagaire XSS oidhreachta) |
| `X-Content-Type-Options` | `nosniff` | ✅ Maith |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Maith |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Maith |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Maith |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Maith |
| `Permissions-Policy` | geolocation, ceamara, micreafón, interest-cohort díchumasaithe | ✅ Maith |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Maith |
| `Content-Security-Policy` | Nonce-bhunaithe, san áireamh nuair a chumasófar CSP | ✅ Maith |
| `Server` | Folaithe mar `"webserver"` | ✅ Maith |
| `X-Powered-By` | Bainte | ✅ Maith |

---

## Measúnú Foriomlán

Cheartaigh an feidhmchlár na leochaileachtaí criticiúla agus ardtromchúisí go léir ón athbhreithniú roimhe. Tá na buairtí reatha teoranta d'fhadhb chumraíochta/DI ardtromchúis amháin (buairt #20) agus míreanna faisnéiseacha íseltromchúis. Tá feabhas suntasach tagtha ar an staid slándála. Moltar gníomh práinneach le haghaidh buairt #20 (spleáchais DI neamhúsáidte in NonceRefresherService) toisc go bhféadfadh sé cosc a chur ar an bhfeidhmchlár tosú faoin réamhchumraíocht.
