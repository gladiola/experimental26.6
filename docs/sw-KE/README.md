# WebAppExperimental266

Programu ya wavuti ya ASP.NET Core 9 Razor Pages yenye uthibitishaji wa Azure AD, mutual TLS (mTLS), usimamizi wa vyeti kupitia Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, Google Cloud Secret Manager, Google Cloud Firestore, na safu ya usalama ya HTTP iliyoimarishwa kwa kutumia Content Security Policy inayotegemea nonce.

---

## 🌐 Faharasa ya Lugha

Hati hii inapatikana katika lugha zifuatazo:

| Lugha | Faili |
|---|---|
| Kiafrikana (af-ZA) | [docs/README.af-ZA.md](docs/README.af-ZA.md) |
| Kiamhari (am-ET) | [docs/README.am-ET.md](docs/README.am-ET.md) |
| Kiarabu (ar-SA) | [docs/README.ar-SA.md](docs/README.ar-SA.md) |
| Kibengali (bn-BD) | [docs/README.bn-BD.md](docs/README.bn-BD.md) |
| Kijerumani (de-DE) | [docs/README.de-DE.md](docs/README.de-DE.md) |
| Kihispania (es-ES) | [docs/README.es-ES.md](docs/README.es-ES.md) |
| Kifaransa (fr-FR) | [docs/README.fr-FR.md](docs/README.fr-FR.md) |
| Kiayalandi (ga-IE) | [docs/README.ga-IE.md](docs/README.ga-IE.md) |
| Kihausa (ha-NG) | [docs/README.ha-NG.md](docs/README.ha-NG.md) |
| Kihawaii (haw-US) | [docs/README.haw-US.md](docs/README.haw-US.md) |
| Kihindi (hi-IN) | [docs/README.hi-IN.md](docs/README.hi-IN.md) |
| Krioli cha Haiti (ht-HT) | [docs/README.ht-HT.md](docs/README.ht-HT.md) |
| Kiitaliano (it-IT) | [docs/README.it-IT.md](docs/README.it-IT.md) |
| Kijapani (ja-JP) | [docs/README.ja-JP.md](docs/README.ja-JP.md) |
| Kikorea (ko-KR) | [docs/README.ko-KR.md](docs/README.ko-KR.md) |
| Kimāori (mi-NZ) | [docs/README.mi-NZ.md](docs/README.mi-NZ.md) |
| Kiholanzi (nl-NL) | [docs/README.nl-NL.md](docs/README.nl-NL.md) |
| Kireno (pt-PT) | [docs/README.pt-PT.md](docs/README.pt-PT.md) |
| Kirusi (ru-RU) | [docs/README.ru-RU.md](docs/README.ru-RU.md) |
| Kisamoa (sm-WS) | [docs/README.sm-WS.md](docs/README.sm-WS.md) |
| Kiswahili (sw-KE) | [docs/README.sw-KE.md](docs/README.sw-KE.md) |
| Kiyoruba (yo-NG) | [docs/README.yo-NG.md](docs/README.yo-NG.md) |
| Kichina Kilichorahisishwa (zh-CN) | [docs/README.zh-CN.md](docs/README.zh-CN.md) |
| Kichina cha Jadi (zh-HK) | [docs/README.zh-HK.md](docs/README.zh-HK.md) |

---

## Jedwali la Maudhui

- [Vipengele](#vipengele)
- [Alama za Vipengele](#alama-za-vipengele)
- [Mahitaji ya Awali](#mahitaji-ya-awali)
- [Ufungaji – Windows Azure (App Service)](#ufungaji--windows-azure-app-service)
- [Ufungaji – Seva ya OpenBSD inayowasiliana na Huduma za Azure](#ufungaji--seva-ya-openbsd-inayowasiliana-na-huduma-za-azure)
- [Marejeo ya Usanidi](#marejeo-ya-usanidi)
- [Hati za Msaada](#hati-za-msaada)
- [Maelezo ya Usalama](#maelezo-ya-usalama)

---

## Vipengele

### Uthibitishaji wa Azure AD (OpenID Connect)
Programu inawathibitisha watumiaji kupitia **Microsoft Identity Platform** kwa kutumia itifaki ya OpenID Connect (kupitia `Microsoft.Identity.Web`). Njia zote chini ya `/Experimental` zinahitaji utambulisho wa Azure AD uliothibitishwa. Kurasa za `/Privacy`, `/Error`, na `/About` zinapatikana hadharani. Sifa ya `[Authorize]` kwenye `HomeController` inalazimisha uthibitishaji kwenye vitendo vyote vya MVC.

### Uthibitishaji wa Cheti cha Mteja wa Mutual TLS (mTLS)
Inapowezeshwa, programu inahitaji wateja wanaounganisha kuwasilisha cheti halali cha X.509. Mipangilio katika `MtlsSettings` inadhibiti:
- Kama kuruhusu vyeti vya mnyororo, vyeti vilivyojisaini, au vyote viwili
- Ukaguzi wa kufutwa kwa vyeti (X.509 CRL / online mode)
- Watoa vyeti wanaoruhusiwa (hukaguliwa kama ulinganifu wa sehemu ya maandishi bila kuzingatia herufi kubwa/ndogo dhidi ya `Issuer` DN ya cheti)

Seva ya wavuti ya Kestrel husanidiwa kwa `ClientCertificateMode.RequireCertificate` wakati mTLS imewashwa, na `ClientCertificateMode.AllowCertificate` katika mazingira ya maendeleo au wakati mTLS imezimwa.

### Ushirikiano wa Azure Key Vault
Programu hupata **cheti cha seva** cha TLS kutoka Azure Key Vault wakati wa kuanza. Mteja wa Key Vault hutumia vitambulisho vya mteja wa Azure AD (client ID + client secret) kutoka kwenye usanidi. `X509Certificate2` iliyopakiwa huingizwa moja kwa moja katika mipangilio chaguo-msingi ya HTTPS ya Kestrel ili faili ya PFX isihitaji kuwepo kwenye diski.

### Uthibitishaji wa Kufutwa kwa Cheti kwa kutumia OCSP
`OcspValidationService` ya mfano imejumuishwa kwa ajili ya kuthibitisha vyeti vya mteja dhidi ya seva ya OCSP (Online Certificate Status Protocol). Huduma hii inaunga mkono usanidi wa:
- Kuwasha/kuzima kulingana na mazingira
- Muda wa mwisho wa ombi na idadi ya majaribio upya
- Kache ya ndani ya kumbukumbu kwa majibu ya OCSP (kwa muda unaosanidiwa)
- Tabia ya fail-closed, fail-open, au warn-only wakati seva ya OCSP haipatikani

> **Kumbuka:** Utekelezaji halisi wa wire protocol wa OCSP (`PerformOcspValidationAsync`) ni stub inayokataa vyeti vyote hadi utekelezaji wa uzalishaji utolewe.

### Content Security Policy yenye Nonce kwa Kila Ombi
Inapowezeshwa, kila jibu la HTTP hubeba kichwa cha `Content-Security-Policy` ambacho maelekezo ya `script-src` yake yanajumuisha **nonce ya nasibu ya kriptografia** inayozalishwa kwa kila ombi. Nonce hiyo:
1. Huzalishwa/kuhuishwa na `NonceRefresherService` kwa kutumia usimbaji wa AES-CBC wenye ufunguo wa byte 32 na IV ya byte 16 inayoweza kusanidiwa, iliyohifadhiwa kwenye usanidi (au User Secrets).
2. Huwekwa kwenye katalogi na `NonceCatalogService` inayotumia `ConcurrentDictionary` salama kwa nyuzi nyingi.
3. Huingizwa katika kila jibu na `NonceMiddleware` na kuwekwa katika `HttpContext.Items["Nonce"]` ili Razor views ziweze kuiweka kwenye lebo za `<script>`.

CSP pia inasaidia orodha ya kuruhusu ya inline scripts kulingana na heshi ya SHA-256 kupitia faili ya maandishi bapa (`wwwroot/csp-hashes.txt`) na heshi ya ziada inayoweza kubainishwa kwa mkono katika usanidi.

### Vichwa vya Kawaida vya Usalama vya HTTP
`UseStandardSecurityHeaders` huongeza vichwa vifuatavyo kwenye kila jibu:
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Cross-Origin-Opener-Policy: same-origin`
- `Cross-Origin-Resource-Policy: same-site`
- `Permissions-Policy` inayozima geolocation, camera, microphone, na FLoC (`interest-cohort`)
- Kuondolewa kwa vichwa vya majibu vya `Server`, `X-Powered-By`, na `X-AspNetMvc-Version`
- `Cache-Control: no-cache, no-store, must-revalidate`

### Azure Blob Storage
Inapowezeshwa, `BlobSettingsService` hutoa huduma ya scoped inayotegemea connection string na idadi ya juu ya viambatisho inayoweza kusanidiwa. Connection string inatarajiwa kuhifadhiwa katika User Secrets au Azure Key Vault, si kwenye source control.

### Azure Cosmos DB
Inapowezeshwa, programu inathibitisha muunganisho wa Cosmos DB wakati wa kuanza kwa kuita `database.ReadAsync()`. `CosmosDbService` huzungusha `CosmosClient` singleton na kuifunga kwa database na container vinavyoweza kusanidiwa. Connection string na account key ni siri zinazopaswa kuhifadhiwa nje ya source control.

### AWS Secrets Manager
Inapowezeshwa, `AwsSecretsManagerOperationsService` hutoa stub ya kiolezo kwa ajili ya kuchukua siri na vyeti vya TLS kutoka **AWS Secrets Manager** (sawa na Azure Key Vault kwa AWS). Inafuata kiolesura cha `AzureKeyVaultOperationsService`:
- `FetchSecret(secretName)` — pata siri yoyote iliyopewa jina kwa ARN au jina.
- `FetchCertificate()` — pata cheti cha seva cha PFX.
- `FetchSecretIVSecret()` / `FetchSecretNonceKeySecret()` — pata nyenzo za usimbaji za nonce.

Darasa la msingi la `AwsSecretManagerOperations` huandika onyo na kurudisha thamani tupu hadi utekelezaji wa uzalishaji utolewe. Vitambulisho vya AWS (`AccessKeyId`, `SecretAccessKey`) lazima vihifadhiwe katika User Secrets au environment variables — si katika source control.

### Amazon DynamoDB
Inapowezeshwa, `AwsDynamoDbService` huzungusha singleton ya mteja wa `IAmazonDynamoDB` (sawa na Azure Cosmos DB kwa AWS). Wakati wa kuanza, huduma huthibitisha muunganisho kwa kuita `DescribeTable`. Huduma pia hutoa `GetTableAsync()` na `GetTableName()` kwa matumizi ya huduma nyingine. Vitambulisho vya AWS lazima vihifadhiwe nje ya source control.

### Google Cloud Secret Manager
Inapowezeshwa, `GcpSecretManagerOperationsService` hutoa stub ya kiolezo kwa ajili ya kuchukua siri na vyeti vya TLS kutoka **Google Cloud Secret Manager** (sawa na Azure Key Vault kwa GCP). Inafuata kiolesura cha `AzureKeyVaultOperationsService`:
- `FetchSecret(secretId)` — pata siri yoyote iliyopewa jina kwa ID.
- `FetchCertificate()` — pata cheti cha seva cha PFX.
- `FetchSecretIVSecret()` / `FetchSecretNonceKeySecret()` — pata nyenzo za usimbaji za nonce.

Darasa la msingi la `GcpSecretManagerOperations` huandika onyo na kurudisha thamani tupu hadi utekelezaji wa uzalishaji utolewe. Uthibitishaji hutumia **Application Default Credentials (ADC)** kwa chaguo-msingi; njia ya faili ya JSON ya service account inaweza kutolewa kupitia `GcpSecretManager:CredentialFilePath`.

### Google Cloud Firestore
Inapowezeshwa, `GcpFirestoreService` huzungusha singleton ya `FirestoreDb` (sawa na Azure Cosmos DB kwa GCP). Wakati wa kuanza, programu huunda mteja wa Firestore uliounganishwa na project ID na collection name iliyosanidiwa. Huduma hutoa `GetCollection()`, `GetCollectionName()`, na `GetDatabase()` kwa matumizi ya sehemu nyingine. Uthibitishaji hutumia ADC au faili ya ufunguo wa service account JSON.

### Usimamizi wa Utambulisho wa AWS Cognito
Inapowezeshwa, `AddAwsCognitoAuthentication` husanidi uthibitishaji wa OpenID Connect dhidi ya **AWS Cognito User Pool** — sawa na Microsoft Entra ID / Azure AD kwa AWS. Middleware hutumia endpoint ya ugunduzi ya OIDC inayofuata viwango vya Cognito:

```
https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration
```

Usanidi uko chini ya sehemu ya `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (hifadhi katika User Secrets), na `Domain` (kikoa cha hosted UI cha Cognito). Callback path ya chaguo-msingi ni `/signin-aws-cognito`.

### Jukwaa la Utambulisho la GCP
Inapowezeshwa, `AddGcpIdentityAuthentication` husanidi uthibitishaji wa OpenID Connect kwa kutumia endpoint ya **Google OAuth 2.0 / OpenID Connect** — sawa na Microsoft Entra ID / Azure AD kwa GCP. Middleware hutumia endpoint ya kawaida ya ugunduzi ya OIDC ya Google:

```
https://accounts.google.com/.well-known/openid-configuration
```

Usanidi uko chini ya sehemu ya `GcpIdentity`: `ClientId`, `ClientSecret` (hifadhi katika User Secrets), na `ProjectId` ya hiari kwa logging. Callback path ya chaguo-msingi ni `/signin-gcp`. Pata client ID na secret kutoka **Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client IDs**.

### Usimamizi Salama wa Session
Sessions hutumia in-process distributed memory cache na **idle timeout ya dakika 30**. Vidakuzi vya session husanidiwa kama:
- `HttpOnly = true`
- `Secure = Always` (HTTPS pekee)
- `SameSite = Strict`

### Ujanibishaji
Programu inasaidia **lugha 25** kwa kutumia faili za rasilimali za `.resx` kwa kila view. Culture inayotumika huamuliwa katika request pipeline kupitia `RequestLocalizationOptions` (kichwa cha Accept-Language, query string, au cookie). Watumiaji wanaweza kubadilisha lugha wakati wowote kwa kutumia kichagua lugha kwenye navigation bar.

| Culture Tag | Lugha |
|---|---|
| `en-US` | Kiingereza (Marekani) — chaguo-msingi |
| `de-DE` | Deutsch (Kijerumani) |
| `es-ES` | Español (Kihispania) |
| `fr-FR` | Français (Kifaransa) |
| `pt-PT` | Português (Kireno) |
| `it-IT` | Italiano (Kiitaliano) |
| `zh-HK` | 廣東話 (Kikantoni — Kichina cha Jadi cha Hong Kong) |
| `ko-KR` | 한국어 (Kikorea) |
| `hi-IN` | हिन्दी (Kihindi) |
| `ru-RU` | Русский (Kirusi) |
| `ar-SA` | العربية (Kiarabu — mpangilio wa kulia kwenda kushoto) |
| `sw-KE` | Kiswahili (Kiswahili) |
| `ja-JP` | 日本語 (Kijapani) |
| `ht-HT` | Kreyòl ayisyen (Krioli cha Haiti) |
| `haw-US` | ʻŌlelo Hawaiʻi (Kihawaii) |
| `sm-WS` | Gagana Samoa (Kisamoa) |
| `mi-NZ` | Te Reo Māori (Kimāori) |
| `af-ZA` | Afrikaans (Kiafrikana) |
| `nl-NL` | Nederlands (Kiholanzi) |
| `ha-NG` | Hausa (Kihausa) |
| `am-ET` | አማርኛ (Kiamhari) |
| `yo-NG` | Yorùbá (Kiyoruba) |
| `bn-BD` | বাংলা (Kibengali) |
| `zh-CN` | 普通话 (Kichina cha Mandarin — Kilichorahisishwa) |
| `ga-IE` | Gaeilge (Kiayalandi) |

Mpangilio wa kulia kwenda kushoto (RTL) huwashwa kiotomatiki wakati Kiarabu kimechaguliwa: kipengele cha `<html>` hupokea `dir="rtl"`, na sifa ya `lang` hubeba culture tag kamili ya BCP-47 (kwa mfano `ar-SA`) ili kivinjari kitende ipasavyo.

### Logging Salama kwa PII
`LoggingHelper` hufanya hashing ya taarifa zinazomtambulisha mtu binafsi katika matokeo ya kumbukumbu kwa kutumia HMAC-SHA256. Ufunguo thabiti wa byte 32 unaweza kutolewa kupitia `Logging:PiiHmacKey` (uliohifadhiwa katika User Secrets). Ikiwa ufunguo haupo au si halali, ufunguo wa nasibu huzalishwa wakati wa kuanza ili PII isiwahi kuandikwa kama plaintext.

---

## Alama za Vipengele

Mifumo mikubwa yote inadhibitiwa na alama za boolean katika `appsettings.json`. Kila alama ina chaguo-msingi salama.

| Alama | Chaguo-msingi | Maelezo |
|---|---|---|
| `EnableSession` | `true` | Session ya upande wa seva na kidakuzi cha session |
| `EnableLocalization` | `true` | Usaidizi wa lugha nyingi (en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ga-IE) |
| `EnableAzureAd` | `true` | Uthibitishaji wa Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Sera za uidhinishaji katika kiwango cha njia |
| `EnableKeyVault` | `false` | Pakia cheti cha TLS cha seva kutoka Azure Key Vault |
| `EnableNonceServices` | `false` | Uzalishaji wa nonce ya CSP kwa kila ombi |
| `EnableCSP` | `false` | Ambatanisha kichwa cha `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Ambatanisha vichwa vya kawaida vya usalama vya HTTP |
| `EnableBlobStorage` | `false` | Huduma ya Azure Blob Storage |
| `EnableCosmosDb` | `false` | Huduma ya Azure Cosmos DB |
| `EnableMtls` | `false` | Hitaji vyeti vya TLS vya mteja |
| `EnableOcspValidation` | `false` | Ukaguzi wa kufutwa kwa cheti wa OCSP (stub) |
| `EnableAwsSecretsManager` | `false` | Huduma ya AWS Secrets Manager (stub) |
| `EnableAwsDynamoDb` | `false` | Huduma ya Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Usimamizi wa utambulisho wa AWS Cognito kwa OpenID Connect |
| `EnableGcpSecretManager` | `false` | Huduma ya GCP Secret Manager (stub) |
| `EnableGcpFirestore` | `false` | Huduma ya Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | Jukwaa la Utambulisho la GCP (Google OAuth 2.0 / OIDC) |

---

## Mahitaji ya Awali

Yafuatayo yanapaswa kuwepo kabla ya kusambaza kwenye jukwaa lolote:

1. **Usajili wa programu ya Azure AD** — wenye redirect URI inayoelekeza kwenye hostname yako, client secret au certificate credential, na (kwa hiari) API permissions.
2. **Azure Key Vault** — yenye cheti cha seva cha PFX kama siri. Usajili wa programu lazima uwe na ruhusa ya `Get` kwenye siri.
3. **Akaunti ya Azure Cosmos DB** (si lazima) — yenye database na container vinavyolingana na usanidi wako.
4. **Akaunti ya Azure Blob Storage** (si lazima) — yenye connection string.
5. **Vitambulisho vya AWS** (si lazima) — IAM user au role yenye ruhusa za `secretsmanager:GetSecretValue` na/au `dynamodb:DescribeTable` kwa vipengele vya AWS. Kwa AWS Cognito, unda User Pool, App Client, na uwashe hosted UI pamoja na OAuth 2.0 scopes zinazohitajika.
6. **Service account ya GCP au ADC** (si lazima) — service account yenye majukumu ya IAM ya `secretmanager.versions.access` na/au `datastore.databases.get` kwa vipengele vya GCP. Kwa GCP Identity, unda OAuth 2.0 credentials kwenye Google Cloud Console na uwashe Google Identity API.
7. **.NET 9 SDK / Runtime** — toleo la 9.0 au baadaye.

---

## Ufungaji – Windows Azure (App Service)

### 1. Unda Rasilimali za Azure

```powershell
# Ingia
az login

# Unda resource group
az group create --name MyResourceGroup --location eastus

# Unda App Service plan (Linux au Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Unda web app (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Sajili Programu ya Azure AD

Katika [Azure Portal](https://portal.azure.com):
1. Nenda kwenye **Microsoft Entra ID → App registrations → New registration**.
2. Weka redirect URI kuwa `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Chini ya **Certificates & secrets**, unda client secret na unakili thamani yake.
4. Andika **Tenant ID** na **Client ID** kutoka kwenye Overview blade.

### 3. Unda Azure Key Vault na Pakia Cheti cha Seva

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# Pakia PFX yako kama siri ya Key Vault (ikiwa katika base64)
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# Toa ufikiaji kwa Managed Identity ya App Service
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Sanidi Mipangilio ya Programu

Nakili `appsettings.template.json` kwenda `appsettings.json` na ujaze thamani za placeholder. Siri **hazipaswi** kuhifadhiwa kwenye source control — ziweke kama App Service Application Settings au kupitia User Secrets ndani ya mazingira ya local:

```powershell
# Katika Azure App Service, weka siri kama app settings:
az webapp config appsettings set --name MyWebApp26 --resource-group MyResourceGroup --settings \
  "AzureAd__TenantId=<TENANT_ID>" \
  "AzureAd__ClientId=<CLIENT_ID>" \
  "AzureAd__ClientSecret=<CLIENT_SECRET>" \
  "AzureKeyVault__KeyVaultURL=https://MyKeyVault26.vault.azure.net/" \
  "AzureKeyVault__KeyVaultSecret=<KV_SECRET>" \
  "AzureKeyVault__KeyVaultPassName=ServerCert" \
  "FeatureFlags__EnableKeyVault=true" \
  "FeatureFlags__EnableAzureAd=true"
```

### 5. Sambaza Programu

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Wezesha HTTPS na Kikoa Maalum (inapendekezwa)

```powershell
# Lazimisha HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Funga custom domain na managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Wezesha mTLS kwenye Azure App Service (si lazima)

Azure App Service inaunga mkono client certificates kupitia portal:
1. Nenda kwenye **App Service → TLS/SSL settings → Client certificates**.
2. Weka **Incoming client certificates** kuwa **Require**.

Kisha weka `FeatureFlags__EnableMtls=true` katika Application Settings.

---

## Ufungaji – Seva ya OpenBSD inayowasiliana na Huduma za Azure

> **Muhimu:** .NET 9 **haina** build rasmi ya Microsoft kwa OpenBSD. Maelekezo yaliyo hapa chini hutumia **container inayooana na Linux** (kupitia [Podman](https://podman.io/), ambayo inapatikana kwenye package tree ya OpenBSD) kuendesha programu ya ASP.NET Core 9 kwenye OpenBSD huku ikiwasiliana na huduma za Azure kupitia HTTPS.

### 1. Sakinisha Mahitaji ya Awali kwenye OpenBSD

```sh
# Kama root
pkg_add podman
pkg_add curl git
```

Ikiwa Podman wala Docker hazipatikani kwa toleo lako la OpenBSD, fikiria kuendesha programu kwenye **Linux VM** (kwa mfano `vmm(4)` yenye mgeni wa Debian/Ubuntu) na ufuate njia ya kawaida ya usambazaji wa Linux kutoka ndani ya VM hiyo.

### 2. Pakua Picha ya ASP.NET Core 9 Runtime

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Jenga Programu (kwenye mashine ya kujenga ya Linux au Windows)

Kwenye mashine yenye .NET 9 SDK iliyosakinishwa, chapisha build ya self-contained inayolenga Linux x64:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

Hamisha saraka ya `publish/` kwenda kwa mwenyeji wa OpenBSD (kwa mfano kupitia `scp` au shared volume).

### 4. Unda Faili ya Usanidi

Kwenye mwenyeji wa OpenBSD, unda `/etc/webappexp26/appsettings.json` yenye thamani zako za uzalishaji (bila siri kwenye faili; tumia environment variables badala yake):

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": {
    "EnableAzureAd": true,
    "EnableKeyVault": true,
    "EnableSecurityHeaders": true,
    "EnableMtls": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/",
    "KeyVaultPassName": "ServerCert"
  }
}
```

Siri huingizwa kama environment variables katika hatua inayofuata.

### 5. Endesha Kontena

```sh
podman run -d \
  --name webappexp26 \
  -p 443:8443 \
  -v /etc/webappexp26:/app/config:ro \
  -v /path/to/publish:/app:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="https://+:8443" \
  -e AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
  -e AzureKeyVault__KeyVaultSecret="YOUR_KV_SECRET" \
  -e Logging__PiiHmacKey="YOUR_32_BYTE_BASE64_KEY" \
  mcr.microsoft.com/dotnet/aspnet:9.0 \
  dotnet /app/WebAppExperimental266.dll \
    --contentRoot /app \
    --configDir /app/config
```

### 6. Sanidi Firewall ya OpenBSD Packet Filter (pf)

Ongeza kwenye `/etc/pf.conf` ili kuruhusu HTTPS inayoingia na kuruhusu miunganisho ya kutoka kwenda endpoint za Azure:

```
# Ruhusu HTTPS inayoingia
pass in on egress proto tcp to port 443

# Ruhusu kutoka kwenda Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Pakia upya seti ya sheria:

```sh
pfctl -f /etc/pf.conf
```

### 7. Sanidi DNS na Vyeti vya TLS

Hakikisha hostname katika `AllowedHosts` inatatua kwenda kwenye IP ya umma ya seva ya OpenBSD. Azure AD inahitaji redirect URI (`/signin-oidc`) ipatikane kupitia HTTPS, kwa hivyo cheti cha seva lazima kiaminiwe. Tumia cheti kutoka CA ya umma (kwa mfano Let's Encrypt kupitia `acme-client(1)`) au pakia cheti kilichosainiwa na CA kwenye Azure Key Vault na uwashe `EnableKeyVault`.

### 8. Muunganisho wa Kutoka kuelekea Huduma za Azure

Endpoint zifuatazo za huduma za Azure lazima zipatikane kutoka kwa mwenyeji wa OpenBSD kupitia TCP 443:

| Huduma | Endpoint |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |

Wakati huduma za AWS zimewashwa, ongeza endpoint za kikanda husika (kwa mfano `secretsmanager.us-east-1.amazonaws.com`, `dynamodb.us-east-1.amazonaws.com`). Wakati huduma za GCP zimewashwa, ongeza `secretmanager.googleapis.com` na `firestore.googleapis.com`.

Jaribu muunganisho kabla ya kuanzisha kontena:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## Marejeo ya Usanidi

Nakili `appsettings.template.json` kwenda `appsettings.json` na ubadilishe thamani zote za `{{PLACEHOLDER}}`.

| Sehemu | Ufunguo | Maelezo |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Usajili wa programu ya Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault na jina la cheti |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Sera ya cheti cha mteja ya mTLS |
| `NonceEncryption` | `Key`, `IV` | Ufunguo wa byte 32 na IV ya byte 16 kwa usimbaji wa nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Muunganisho wa Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Muunganisho wa Cosmos DB |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Uthibitishaji wa OCSP (stub) |
| `Logging` | `PiiHmacKey` | Ufunguo wa HMAC wa base64 wa byte 32 kwa hashing ya PII kwenye logs |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager (stub) |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `AwsCognito` | `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret`, `Domain`, `CallbackPath` | Utambulisho wa AWS Cognito OIDC |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager (stub) |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `GcpIdentity` | `ClientId`, `ClientSecret`, `ProjectId`, `CallbackPath` | Jukwaa la Utambulisho la GCP (Google OAuth 2.0 / OIDC) |

Zalisha funguo za usimbaji na IV kwa kutumia script ya PowerShell iliyojumuishwa:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Hifadhi siri zote kwenye **.NET User Secrets** kwa maendeleo ya local:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

Kwa huduma za AWS:

```bash
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsCognito:AppClientSecret" "YOUR_COGNITO_APP_CLIENT_SECRET"
```

Kwa huduma za GCP, weka environment variable ya `GOOGLE_APPLICATION_CREDENTIALS` au tumia mpangilio wa `CredentialFilePath` kuelekeza kwenye faili ya ufunguo wa service account JSON.

```bash
dotnet user-secrets set "GcpIdentity:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

---

## Hati za Msaada

Saraka ya `SupportingScripts/` ina huduma za PowerShell:

| Script | Kusudi |
|---|---|
| `IVandKeySampleGenerator.ps1` | Zalisha ufunguo wa AES wa nasibu wa byte 32 na IV ya byte 16 (base64) |
| `HashInlineScriptPowerShell.ps1` | Kokotoa heshi za SHA-256 kwa inline scripts (kwa CSP allow-listing) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Sawa na hapo juu, lakini hutokeza heshi katika muundo wa base64 |
| `CertificateUploaderToAzureExample.ps1` | Pakia cheti cha PFX kwenye Azure Key Vault |
| `CheckRoles.ps1` | Thibitisha ugawaji wa majukumu ya Azure RBAC kwa programu |
| `ExportResourceGroups.ps1` | Hamisha usanidi wa Azure resource groups |
| `TroubleshootingCosmosDBInfo.ps1` | Chunguza muunganisho wa Cosmos DB |
| `SetupFromTemplate.ps1` | Otoomatisha usanidi wa awali kutoka `appsettings.template.json` |

---

## Maelezo ya Usalama

- **Usiwahi kufanya commit ya siri** (`ClientSecret`, `KeyVaultSecret`, connection strings, funguo za usimbaji, vitambulisho vya AWS/GCP) kwenye source control. Tumia .NET User Secrets katika local na Azure App Settings / Key Vault references katika uzalishaji.
- Utekelezaji wa uthibitishaji wa OCSP ni **stub** inayokataa vyeti vyote. Badilisha `PerformOcspValidationAsync` ndani ya `OcspValidationService.cs` kabla ya kuwasha `EnableOcspValidation` katika uzalishaji.
- Utekelezaji wa AWS Secrets Manager na GCP Secret Manager ni **stubs** zinazotoa onyo kwenye logs na kurudisha thamani tupu. Badilisha bodies za mbinu katika `AwsSecretManagerOperations` na `GcpSecretManagerOperations` kabla ya kuwasha vipengele hivyo katika uzalishaji.
- Thamani za nonce **haziandikwi kamwe** — kuandika nonce kama plaintext kungemwezesha mshambuliaji mwenye ufikiaji wa logs kuingiza inline scripts kiholela.
- Kichwa cha jibu cha `Server` hufichwa kama `webserver` ili kuepuka kufichua taarifa za jukwaa.
- Kagua `AllowSelfSignedCertificates = false` (chaguo-msingi) kabla ya kusambaza mTLS; vyeti vilivyojisaini vinapaswa kutumika katika maendeleo tu.
- `AccessKeyId` na `SecretAccessKey` za AWS **hazipaswi kamwe** kuonekana katika `appsettings.json` — tumia User Secrets, environment variables, au IAM instance roles.
- Kwa **AWS Cognito**, pendelea IAM roles au Cognito Identity Pools badala ya static credentials; usiwahi kufanya commit ya `AppClientSecret` kwenye source control.
- Vitambulisho vya GCP vinapaswa kutumia **Application Default Credentials (ADC)** (kwa mfano Workload Identity kwenye GKE, au `gcloud auth application-default login` katika local) badala ya kufanya commit ya faili za service-account JSON.
- Kwa **GCP Identity**, `ClientSecret` lazima ihifadhiwe katika User Secrets au environment variables — si ndani ya `appsettings.json`.
