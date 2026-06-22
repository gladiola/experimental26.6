# WebAppExperimental266

Feidhleoireacht ghréasáin ASP.NET Core 9 Razor Pages le fíordheimhniú Azure AD, TLS frithpháirteach (mTLS), bainistíocht deimhnithe Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, agus sraith slándála HTTP crua-neart le Beartas Slándála Inneachair bunaithe ar nonce.

---

## Clár Ábhair

- [Gnéithe](#gnéithe)
- [Bratacha Gnéithe](#bratacha-gnéithe)
- [Réamhriachtanais](#réamhriachtanais)
- [Suiteáil – Windows Azure (App Service)](#suiteáil--windows-azure-app-service)
- [Suiteáil – Freastalaí OpenBSD ag Cumarsáid le Seirbhísí Azure](#suiteáil--freastalaí-openbsd-ag-cumarsáid-le-seirbhísí-azure)
- [Tagairt Cumraíochta](#tagairt-cumraíochta)
- [Scripteanna Tacaíochta](#scripteanna-tacaíochta)
- [Nótaí Slándála](#nótaí-slándála)

---

## Gnéithe

### Fíordheimhniú Azure AD (OpenID Connect)
Déanann an feidhleoireacht fíordheimhniú ar úsáideoirí tríd an **Ardán Céannachta Microsoft** ag baint úsáide as prótacal OpenID Connect (trí `Microsoft.Identity.Web`). Ní mór céannacht Azure AD fíordheimhnithe a bheith ag gach bealach faoi `/Experimental`. Tá rochtain phoiblí ar na leathanaigh `/Privacy`, `/Error`, agus `/About`.

### Fíordheimhniú Deimhnithe Cliaint mTLS
Nuair a chumasaítear é, ní mór do chliaint deimhniú X.509 bailí a chur i láthair. Rialaíonn socruithe i `MtlsSettings` cé acu deimhnithe slabhraithe, deimhnithe féin-shínithe, nó an dá cheann a cheadaítear, fíorú cealaithe deimhnithe, agus eisiúnóirí deimhnithe a cheadaítear.

### Comhtháthú Azure Key Vault
Aimsíonn an feidhleoireacht an **deimhniú freastalaí** TLS ó Azure Key Vault ag am tosaithe. Instealladh an `X509Certificate2` luchtaithe go díreach i gcumraíocht HTTPS Kestrel, mar sin ní gá comhad PFX a bheith ann ar diosca.

### Beartas Slándála Inneachair le Nonce do Gach Iarratas
Nuair a chumasaítear é, iompraíonn gach freagra HTTP ceanntásc `Content-Security-Policy` a bhfuil treoracha `script-src` aige ina bhfuil **nonce randamach cripteagrafach** do gach iarratas. Tacaíonn an CSP freisin le liostaí ceadaithe bunaithe ar hais SHA-256 le haghaidh scripteanna inlíne.

### Ceanntásca Slándála HTTP Caighdeánacha
Cuireann `UseStandardSecurityHeaders` leis gach freagra: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, agus baint na gceanntásc `Server`, `X-Powered-By`, agus `X-AspNetMvc-Version`.

### Azure Blob Storage
Nuair a chumasaítear é, soláthraíonn `BlobSettingsService` seirbhís Scoped arna tacú ag teaghrán ceangail agus uaslíon inathraithe ceangaltáin.

### Azure Cosmos DB
Nuair a chumasaítear é, fíoraíonn an feidhleoireacht nasc Cosmos DB ag tosú trí `database.ReadAsync()` a ghlaoch.

### AWS Secrets Manager
Nuair a chumasaítear é, aimsíonn `AwsSecretsManagerOperationsService` rúin agus deimhnithe ó AWS Secrets Manager. Cumraíocht sa rannán `AwsSecretsManager` le: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, agus dintiúir `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Nuair a chumasaítear é, fíoraíonn `AwsDynamoDbService` nascacht tábla DynamoDB ag tosú. Cumraíocht sa rannán `AwsDynamoDb` le: `Region`, `TableName`, agus dintiúir `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Nuair a chumasaítear é, aimsíonn `GcpSecretManagerOperationsService` rúin ó Google Cloud Secret Manager. Cumraíocht sa rannán `GcpSecretManager` le: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, agus `CredentialFilePath` (roghnach, úsáideann ADC má tá folamh).

### GCP Firestore
Nuair a chumasaítear é, tógann `GcpFirestoreService` cliant Firestore ag tosú. Cumraíocht sa rannán `GcpFirestore` le: `ProjectId`, `DatabaseId` (réamhshocrú: "(default)"), `CollectionName`, agus `CredentialFilePath` (roghnach).

### Bainistíocht Céannachta AWS Cognito
Nuair a chumasaítear é, cumraíonn `AddAwsCognitoAuthentication` fíordheimhniú OpenID Connect in aghaidh **Amazon Cognito User Pool** — coibhéis AWS de Microsoft Entra ID / Azure AD. Pointe deiridh fionnachtana OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Cumraíocht sa rannán `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (stóráil i Rúin Úsáideora), agus `Domain`.

### Ardán Céannachta GCP
Nuair a chumasaítear é, cumraíonn `AddGcpIdentityAuthentication` fíordheimhniú OpenID Connect ag baint úsáide as **Google OAuth 2.0 / OIDC** — coibhéis GCP de Microsoft Entra ID / Azure AD. Pointe deiridh fionnachtana OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Cumraíocht sa rannán `GcpIdentity`: `ClientId`, `ClientSecret` (stóráil i Rúin Úsáideora), agus `ProjectId` roghnach.

### Bainistíocht Seisiúin Slán
Úsáideann seisiúin taisce cuimhne dháilte in-phróisis le **am múchta díomhaointis 30 nóiméad**. Tá fianáin seisiúin cumraithe le `HttpOnly`, `Secure = Always`, agus `SameSite = Strict`.

### Logánú
Tacaíonn an feidhleoireacht le **25 teanga**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, agus ga-IE. Áiríonn Araibis athrú leagan amach RTL uathoibríoch.

### Logáil Sábháilte PII
Déanann `LoggingHelper` haisáil ar fhaisnéis pearsanta inaitheanta in aschur logála ag baint úsáide as HMAC-SHA256. Is féidir eochair cobhsaí 32-bheart a sholáthar trí `Logging:PiiHmacKey`.

---

## Bratacha Gnéithe

Rialaítear gach mórchóras fo-chórais le bratacha Boolean i `appsettings.json`.

| Bratach | Réamhshocrú | Cur síos |
|---|---|---|
| `EnableSession` | `true` | Seisiún freastalaí agus fianán seisiúin |
| `EnableLocalization` | `true` | Tacaíocht ilteangach (25 teanga) |
| `EnableAzureAd` | `true` | Fíordheimhniú Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Beartais údaraithe ar leibhéal bealaigh |
| `EnableKeyVault` | `false` | Luchtaigh deimhniú freastalaí TLS ó Azure Key Vault |
| `EnableNonceServices` | `false` | Giniúint nonce CSP do gach iarratas |
| `EnableCSP` | `false` | Ceangail ceanntásc `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Ceangail ceanntásca slándála HTTP caighdeánacha |
| `EnableBlobStorage` | `false` | Seirbhís Azure Blob Storage |
| `EnableCosmosDb` | `false` | Seirbhís Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (bunscealaí) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Bainistíocht céannachta AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (bunscealaí) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | Ardán Céannachta GCP (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Éiligh deimhnithe TLS cliaint |
| `EnableOcspValidation` | `false` | Seiceáil cealaithe deimhnithe OCSP (bunscealaí) |

---

## Réamhriachtanais

1. **Clárú Feidhleoireachta Azure AD** – le URI atreoraithe, rún cliaint nó dintiúr deimhnithe.
2. **Azure Key Vault** – ina bhfuil deimhniú freastalaí PFX mar rún.
3. **Cuntas Azure Cosmos DB** (roghnach).
4. **Cuntas Azure Blob Storage** (roghnach).
5. **.NET 9 SDK / Runtime** – leagan 9.0 nó níos déanaí.
6. **Dintiúir AWS** (úsáideoir nó ról IAM le ceadanna `secretsmanager` agus `dynamodb`) – riachtanach nuair a chumasaítear `EnableAwsSecretsManager` nó `EnableAwsDynamoDb`.
7. **Cuntas seirbhíse GCP nó ADC** (le ceadanna `secretmanager` agus `datastore`) – riachtanach nuair a chumasaítear `EnableGcpSecretManager` nó `EnableGcpFirestore`.

---

## Suiteáil – Windows Azure (App Service)

### 1. Cruthaigh Acmhainní Azure

```powershell
# Sínigh isteach
az login

# Cruthaigh grúpa acmhainní
az group create --name MyResourceGroup --location eastus

# Cruthaigh plean App Service (Linux nó Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Cruthaigh feidhleoireacht ghréasáin (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Cláraigh Feidhleoireacht Azure AD

In [Tairseach Azure](https://portal.azure.com):
1. Téigh go **Microsoft Entra ID → Cláruithe feidhleoireachta → Clárú nua**.
2. Socraigh URI atreoraithe go `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Faoi **Deimhnithe agus rúin**, cruthaigh rún cliaint agus cóipeáil an luach.
4. Tabhair faoi deara **Aitheantas Tionónta** agus **Aitheantas Cliaint** ón gclár forbhreathnaithe.

### 3. Cruthaigh Azure Key Vault agus Uaslódáil Deimhniú Freastalaí

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# Uaslódáil do PFX mar rún Key Vault (ionchódaithe base64)
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# Deonaigh rochtain chuig Céannacht Bhainistithe App Service
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Cumraigh Socruithe Feidhleoireachta

Cóipeáil `appsettings.template.json` go `appsettings.json` agus líon luachanna áite-sealbhóra. **Níor cheart** rúin a stóráil i rialú foinse — socraigh iad mar Shocruithe Feidhleoireachta App Service nó trí Rúin Úsáideora go háitiúil:

```powershell
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

### 5. Imscaradh na Feidhleoireachta

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Cumasaigh HTTPS agus Fearann Saincheaptha (molta)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Cumasaigh mTLS ar Azure App Service (roghnach)

1. Téigh go **App Service → Socruithe TLS/SSL → Deimhnithe cliaint**.
2. Socraigh **Deimhnithe cliaint isteach** go **Riachtanach**.

Ansin socraigh `FeatureFlags__EnableMtls=true` i Socruithe Feidhleoireachta.

---

## Suiteáil – Freastalaí OpenBSD ag Cumarsáid le Seirbhísí Azure

> **Tábhachtach:** Níl tógáil oifigiúil Microsoft ag .NET 9 **do** OpenBSD. Úsáideann na treoracha thíos **coimeádán comhoiriúnach le Linux** (trí [Podman](https://podman.io/)) chun an feidhleoireacht ASP.NET Core 9 a reáchtáil ar OpenBSD agus í ag cumarsáid le seirbhísí Azure trí HTTPS.

### 1. Suiteáil Réamhriachtanais ar OpenBSD

```sh
# Mar fhréamh
pkg_add podman
pkg_add curl git
```

### 2. Tarraing Íomhá Runtime ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Tóg an Feidhleoireacht (ar mheaisín tógála Linux nó Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Cruthaigh Comhad Cumraíochta

Ar an óstaíocht OpenBSD, cruthaigh `/etc/webappexp26/appsettings.json`:

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

### 5. Tosaigh an Coimeádán

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

### 6. Cumraigh Balla Dóiteáin OpenBSD Packet Filter (pf)

Cuir leis `/etc/pf.conf`:

```
# Ceadaigh HTTPS isteach
pass in on egress proto tcp to port 443

# Ceadaigh amach chuig Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Athlódáil an tacar rialacha:

```sh
pfctl -f /etc/pf.conf
```

### 7. Nascacht Amach chuig Seirbhísí Azure

| Seirbhís | Pointe Deiridh |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

---

## Tagairt Cumraíochta

Cóipeáil `appsettings.template.json` go `appsettings.json` agus athchuir gach luach `{{PLACEHOLDER}}`.

| Rannán | Eochair | Cur síos |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Clárú feidhleoireachta Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault agus ainm deimhnithe |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Beartas deimhnithe cliaint mTLS |
| `NonceEncryption` | `Key`, `IV` | Eochair 32-bheart agus IV 16-bheart le haghaidh criptíocht nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Nasc Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Nasc Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Fíorú OCSP (bunscealaí) |
| `Logging` | `PiiHmacKey` | Eochair HMAC base64 32-bheart le haghaidh haisáil PII i logaí |

Gin eochracha criptíochta agus IVanna ag baint úsáide as an script PowerShell san áireamh:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Stóráil gach rún i **Rúin Úsáideora .NET** le haghaidh forbartha áitiúla:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
```

> Maidir le GCP, socraigh an athróg timpeallachta `GOOGLE_APPLICATION_CREDENTIALS` ar chonair na comhaid JSON eochair cuntas seirbhíse, nó rith `gcloud auth application-default login` le haghaidh forbartha áitiúla.

---

## Scripteanna Tacaíochta

Tá uirlisí PowerShell sa chomhadlann `SupportingScripts/`:

| Script | Cuspóir |
|---|---|
| `IVandKeySampleGenerator.ps1` | Gin eochair AES randamach 32-bheart agus IV 16-bheart (base64) |
| `HashInlineScriptPowerShell.ps1` | Ríomh haiseanna SHA-256 le haghaidh scripteanna inlíne (le haghaidh liosta ceadaithe CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Mar an gcéanna thuas, aschuireann haiseanna i bhformáid base64 |
| `CertificateUploaderToAzureExample.ps1` | Uaslódáil deimhniú PFX go Azure Key Vault |
| `CheckRoles.ps1` | Fíoraigh sannadh ról RBAC Azure don fheidhleoireacht |
| `ExportResourceGroups.ps1` | Easpórtáil cumraíochtaí grúpa acmhainní Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnóisigh nascacht Cosmos DB |
| `SetupFromTemplate.ps1` | Uathoibrigh cumraíocht tosaigh ó `appsettings.template.json` |

---

## Nótaí Slándála

- **Ná déan rúin a ghealladh** d'rialú foinse riamh. Úsáid Rúin Úsáideora .NET go háitiúil agus Socruithe App Service Azure / tagairtí Key Vault i dtáirgeadh.
- Is **bunscealaí** í cur i bhfeidhm fíoraithe OCSP a diúltaíonn gach deimhniú. Athchuir `PerformOcspValidationAsync` in `OcspValidationService.cs` sula gcumasaítear `EnableOcspValidation` i dtáirgeadh.
- **Ní logáiltear** luachanna nonce riamh — logáil nonce i dtéacs soiléir ligfeadh d'ionsaitheoir le rochtain logála scripteanna inlíne treallúsacha a instealladh.
- Cuirtear masc ar cheanntásc freagartha `Server` mar `webserver` chun faisnéis ardáin a nochtadh a sheachaint.
- **Níor cheart** `AccessKeyId` agus `SecretAccessKey` AWS **riamh** a bheith le feiceáil in `appsettings.json` — úsáid Rúin Úsáideora, athróga timpeallachta, nó róil sampla IAM.
- Ba cheart do dhintiúir GCP **Dintiúir Réamhshocraithe Feidhleoireachta (ADC)** a úsáid seachas comhaid JSON eochair cuntas seirbhíse a ghealladh.
