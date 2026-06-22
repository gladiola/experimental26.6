# WebAppExperimental266

የASP.NET Core 9 Razor Pages ዌብ አፕሊኬሽን ከAzure AD ማረጋገጫ፣ የጋራ TLS (mTLS)፣ የAzure Key Vault ሰርተፊኬት አስተዳደር፣ Azure Cosmos DB፣ Azure Blob Storage፣ **AWS Secrets Manager**፣ **Amazon DynamoDB**፣ **Google Cloud Secret Manager**፣ **Google Cloud Firestore** እና nonce-ላይ የተመሰረተ የይዘት ደህንነት ፖሊሲ ያለው የጠነከረ HTTP ደህንነት ንብርብር ጋር።

---

## የዝርዝር ሰንጠረዥ

- [ባህሪያት](#ባህሪያት)
- [የባህሪ ምልክቶች](#የባህሪ-ምልክቶች)
- [ቅድመ ሁኔታዎች](#ቅድመ-ሁኔታዎች)
- [ጭነት – Windows Azure (App Service)](#ጭነት--windows-azure-app-service)
- [ጭነት – ከAzure አገልግሎቶች ጋር የሚገናኝ OpenBSD አገልጋይ](#ጭነት--ከazure-አገልግሎቶች-ጋር-የሚገናኝ-openbsd-አገልጋይ)
- [የውቅር ማጣቀሻ](#የውቅር-ማጣቀሻ)
- [የድጋፍ ስክሪፕቶች](#የድጋፍ-ስክሪፕቶች)
- [የደህንነት ማስታወሻዎች](#የደህንነት-ማስታወሻዎች)

---

## ባህሪያት

### Azure AD ማረጋገጫ (OpenID Connect)
አፕሊኬሽኑ ተጠቃሚዎችን `Microsoft.Identity.Web` በኩል OpenID Connect ፕሮቶኮልን በመጠቀም **የMicrosoft Identity Platform** በኩል ያረጋግጣል። በ`/Experimental` ስር ያሉ ሁሉም መስመሮች የተረጋገጠ Azure AD ማንነት ያስፈልጋቸዋል። `/Privacy`፣ `/Error`፣ እና `/About` ገፆች ለህዝብ ክፍት ናቸው።

### mTLS የደንበኛ ሰርተፊኬት ማረጋገጫ
ሲቃነስ፣ ደንበኞች ትክክለኛ X.509 ሰርተፊኬት ማቅረብ አለባቸው። `MtlsSettings` ቅንብሮች ሰንሰለት ሰርተፊኬቶች፣ ራሱ የፈረሙ ሰርተፊኬቶች ወይም ሁለቱም ስለሚፈቀዱ ወይም ስለሚከለከሉ፣ ሰርተፊኬት ሰርዛ ማረጋገጫ፣ እና ፈቃድ ያላቸው ሰርተፊኬት አውጪዎችን ይቆጣጠራሉ።

### Azure Key Vault ውህደት
አፕሊኬሽኑ በጅምር ጊዜ TLS **የአገልጋይ ሰርተፊኬቱን** ከAzure Key Vault ያወርዳል። የሚጫነው `X509Certificate2` በቀጥታ ወደ Kestrel's HTTPS ቅንብሮች ይጨምራል፣ ስለዚህ ምንም PFX ፋይል ዲስኩ ላይ ሊኖር አያስፈልግም።

### በጥያቄ ለጥያቄ Nonce ያለው የይዘት ደህንነት ፖሊሲ
ሲቃነስ፣ ሁሉም HTTP ምላሾች `Content-Security-Policy` ጭንቅላት ይሸከማሉ፣ ይህም `script-src` ዳይሬክቲቭ ለእያንዳንዱ ጥያቄ **ምስጢራዊ ዘፈቀደ nonce** ይዟል። CSP ለ inline ስክሪፕቶች SHA-256 ሃሽ-ላይ የተመሰረተ ዝርዝር ፍቃዶችን ይደግፋል።

### መደበኛ HTTP ደህንነት ጭንቅላቶች
`UseStandardSecurityHeaders` ለእያንዳንዱ ምላሽ ይጨምራል፡ `X-Frame-Options`፣ `X-Content-Type-Options`፣ `Strict-Transport-Security`፣ `Referrer-Policy`፣ `Cross-Origin-Opener-Policy`፣ `Cross-Origin-Resource-Policy`፣ `Permissions-Policy`፣ እና `Server`፣ `X-Powered-By`፣ እና `X-AspNetMvc-Version` ጭንቅላቶችን ማስወገድ።

### Azure Blob Storage
ሲቃነስ፣ `BlobSettingsService` የግንኙነት ሕብረቁምፊ እና ሊዋቀር የሚችል ከፍተኛ አባሪ ብዛት ባለው Scoped አገልግሎት ያቀርባል።

### Azure Cosmos DB
ሲቃነስ፣ አፕሊኬሽኑ `database.ReadAsync()` ደውሎ ጅምር ጊዜ ላይ Cosmos DB ግንኙነቱን ያረጋግጣል።

### AWS Secrets Manager
ሲቃነስ፣ `AwsSecretsManagerOperationsService` ሚስጥሮችን እና ሰርተፊኬቶችን ከAWS Secrets Manager ያወርዳል። `AwsSecretsManager` ክፍል ቅንብር፡ `Region`፣ `CertificateSecretName`፣ `IVSecretName`፣ `NonceKeySecretName`፣ እና `AccessKeyId`/`SecretAccessKey` ምስክርነቶች።

### Amazon DynamoDB
ሲቃነስ፣ `AwsDynamoDbService` ጅምር ጊዜ ላይ DynamoDB ሠንጠረዡ ግንኙነቱን ያረጋግጣል። `AwsDynamoDb` ክፍል ቅንብር፡ `Region`፣ `TableName`፣ እና `AccessKeyId`/`SecretAccessKey` ምስክርነቶች።

### GCP Secret Manager
ሲቃነስ፣ `GcpSecretManagerOperationsService` ሚስጥሮችን ከGoogle Cloud Secret Manager ያወርዳል። `GcpSecretManager` ክፍል ቅንብር፡ `ProjectId`፣ `CertificateSecretId`፣ `IVSecretId`፣ `NonceKeySecretId`፣ እና `CredentialFilePath` (አማራጭ፣ ባዶ ከሆነ ADC ይጠቀማል)።

### GCP Firestore
ሲቃነስ፣ `GcpFirestoreService` ጅምር ጊዜ ላይ Firestore ደንበኛ ይሠራል። `GcpFirestore` ክፍል ቅንብር፡ `ProjectId`፣ `DatabaseId` (ነባሪ፡ "(default)")፣ `CollectionName`፣ እና `CredentialFilePath` (አማራጭ)።

### AWS Cognito ማንነት አስተዳደር
ሲቃነስ፣ `AddAwsCognitoAuthentication` **Amazon Cognito User Pool** ላይ OpenID Connect ማረጋገጫ ያዋቅራል — የMicrosoft Entra ID / Azure AD AWS ሚናዊ። OIDC ማወቅ ነጥብ፡
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
`AwsCognito` ክፍል ቅንብር፡ `Region`፣ `UserPoolId`፣ `AppClientId`፣ `AppClientSecret` (ተጠቃሚ ሚስጥሮች ውስጥ ያስቀምጡ)፣ እና `Domain`።

### GCP Identity Platform
ሲቃነስ፣ `AddGcpIdentityAuthentication` **Google OAuth 2.0 / OIDC** ን በመጠቀም OpenID Connect ማረጋገጫ ያዋቅራል — የMicrosoft Entra ID / Azure AD GCP ሚናዊ። OIDC ማወቅ ነጥብ፡
`https://accounts.google.com/.well-known/openid-configuration`
`GcpIdentity` ክፍል ቅንብር፡ `ClientId`፣ `ClientSecret` (ተጠቃሚ ሚስጥሮች ውስጥ ያስቀምጡ)፣ እና አማራጭ `ProjectId`።

### ደህንነቱ የተጠበቀ ክፍለ ጊዜ አስተዳደር
ክፍለ ጊዜዎች ከ**30 ደቂቃ ቅልጥፍና አልባ ማብቂያ** ጋር ሂደት ውስጥ ያለ ተሰራጭ ማህደረ ትውስታ ካሽ ይጠቀማሉ። የክፍለ ጊዜ ኩኪዎች `HttpOnly`፣ `Secure = Always`፣ እና `SameSite = Strict` ሆነው ተዋቅረዋል።

### ቦታዊ ትርጉም
አፕሊኬሽኑ **25 ቋንቋዎችን** ይደግፋል፡ en-US፣ de-DE፣ es-ES፣ fr-FR፣ pt-PT፣ it-IT፣ zh-HK፣ ko-KR፣ hi-IN፣ ru-RU፣ ar-SA፣ sw-KE፣ ja-JP፣ ht-HT፣ haw-US፣ sm-WS፣ mi-NZ፣ af-ZA፣ nl-NL፣ ha-NG፣ am-ET፣ yo-NG፣ bn-BD፣ zh-CN፣ እና ga-IE። አረብኛ ራስ ሰር RTL አቀማመጥ ቀያሪ ያካትታል።

### PII-ደህንነቱ የተጠበቀ ምዝገባ
`LoggingHelper` HMAC-SHA256 ን በመጠቀም ምዝገባ ውፅዓት ውስጥ ያሉ በዕለት ሊታወቅ የሚችል መረጃን ያሃሽ ያደርጋል። ቀጥተኛ 32-byte ቁልፍ `Logging:PiiHmacKey` በኩል ሊቀርብ ይችላል።

---

## የባህሪ ምልክቶች

ሁሉም ዋና ንዑስ ስርዓቶች በ`appsettings.json` ውስጥ ባሉ Boolean ምልክቶች ይቆጣጠራሉ።

| ምልክት | ነባሪ | ዝርዝር |
|---|---|---|
| `EnableSession` | `true` | ወደ አገልጋይ ጎን ክፍለ ጊዜ እና የክፍለ ጊዜ ኩኪ |
| `EnableLocalization` | `true` | ባለ ብዙ ቋንቋ ድጋፍ (25 ቋንቋዎች) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect ማረጋገጫ |
| `EnableAuthorization` | `true` | መስመር-ደረጃ ፍቃደኝነት ፖሊሲዎች |
| `EnableKeyVault` | `false` | TLS አገልጋይ ሰርተፊኬት ከAzure Key Vault ጫን |
| `EnableNonceServices` | `false` | ጥያቄ ለጥያቄ CSP nonce ማመንጨት |
| `EnableCSP` | `false` | `Content-Security-Policy` ጭንቅላት አያይዝ |
| `EnableSecurityHeaders` | `true` | መደበኛ HTTP ደህንነት ጭንቅላቶችን አያይዝ |
| `EnableBlobStorage` | `false` | Azure Blob Storage አገልግሎት |
| `EnableCosmosDb` | `false` | Azure Cosmos DB አገልግሎት |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (ተቋርጦ) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect ማንነት አስተዳደር |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (ተቋርጦ) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | ደንበኛ TLS ሰርተፊኬቶችን ጠይቅ |
| `EnableOcspValidation` | `false` | OCSP ሰርተፊኬት ሰርዛ ምርመራ (ተቋርጦ) |

---

## ቅድመ ሁኔታዎች

1. **Azure AD አፕ ምዝገባ** – ከ redirect URI፣ ደንበኛ ሚስጥር ወይም ሰርተፊኬት ምስክርነት ጋር።
2. **Azure Key Vault** – ሚስጥር ሆኖ PFX አገልጋይ ሰርተፊኬቱን ይዟል።
3. **Azure Cosmos DB አካውንት** (አማራጭ)።
4. **Azure Blob Storage አካውንት** (አማራጭ)።
5. **.NET 9 SDK / Runtime** – ስሪት 9.0 ወይም ከዚያ በኋላ።
6. **AWS ምስክርነቶች** (`secretsmanager` እና `dynamodb` ፈቃዶች ያለው IAM ተጠቃሚ ወይም ሚና) – `EnableAwsSecretsManager` ወይም `EnableAwsDynamoDb` ሲቃነስ ያስፈልጋሉ።
7. **GCP አገልግሎት አካውንት ወይም ADC** (`secretmanager` እና `datastore` ፈቃዶች ያለው) – `EnableGcpSecretManager` ወይም `EnableGcpFirestore` ሲቃነስ ያስፈልጋሉ።

---

## ጭነት – Windows Azure (App Service)

### 1. Azure ሀብቶችን ፍጠር

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Azure AD አፕሊኬሽን ምዝገባ

[Azure Portal](https://portal.azure.com) ውስጥ፡
1. **Microsoft Entra ID → አፕ ምዝገባዎች → አዲስ ምዝገባ** ሂድ።
2. redirect URI ን `https://<your-app>.azurewebsites.net/signin-oidc` አድርግ።
3. **ሰርተፊኬቶች እና ሚስጥሮች** ስር ደንበኛ ሚስጥር ፍጠር እና ዋጋውን ቅዳ።
4. ከ Overview ሰሌዳ **Tenant ID** እና **Client ID** ን ማስታወሻ ያዝ።

### 3. Azure Key Vault ፍጠር እና አገልጋይ ሰርተፊኬቱን ጫን

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. አፕሊኬሽን ቅንብሮችን አዋቅር

`appsettings.template.json`ን `appsettings.json` ወደ ቅዳ እና ቦታ ሞላ ዋጋዎችን ሙላ። ሚስጥሮች ምንጭ ቁጥጥር ውስጥ **መቀመጥ አይገባቸውም** — እነሱን የApp Service አፕሊኬሽን ቅንብሮች ወይም በቦታ ተጠቃሚ ሚስጥሮች አድርጋቸው፡

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

### 5. አፕሊኬሽኑን አሰፍን

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. HTTPS እና ብጁ ጎራ ቃነስ (ይመከራል)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Azure App Service ላይ mTLS ቃነስ (አማራጭ)

1. **App Service → TLS/SSL ቅንብሮች → ደንበኛ ሰርተፊኬቶች** ሂድ።
2. **ወደ ውስጥ የሚመጡ ደንበኛ ሰርተፊኬቶች**ን **ያስፈልጋል** አድርጋቸው።

ከዚያ `FeatureFlags__EnableMtls=true` ን አፕሊኬሽን ቅንብሮች ውስጥ አዘጋጅ።

---

## ጭነት – ከAzure አገልግሎቶች ጋር የሚገናኝ OpenBSD አገልጋይ

> **ጠቃሚ ማስታወሻ:** .NET 9 ለOpenBSD ይፋዊ Microsoft ግንባታ **የለውም**። ከዚህ በታች ያሉ መመሪያዎች HTTPS በኩል ከAzure አገልግሎቶች ጋር ሲገናኝ OpenBSD ላይ ASP.NET Core 9 አፕሊኬሽን ለማሄድ **Linux-ተኳሃኝ ኮንቴይነር** (via [Podman](https://podman.io/)) ይጠቀማሉ።

### 1. OpenBSD ላይ ቅድመ ሁኔታዎችን ጫን

```sh
pkg_add podman
pkg_add curl git
```

### 2. ASP.NET Core 9 Runtime ምስሉን ሳብ

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. አፕሊኬሽኑን ሥራ (ሊኑክስ ወይም ዊንዶውስ ግንባታ ማሽን ላይ)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. ውቅር ፋይል ፍጠር

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. ኮንቴይነሩን አሂድ

```sh
podman run -d --name webappexp26 -p 443:8443 \
  -v /etc/webappexp26:/app/config:ro -v /path/to/publish:/app:ro \
  -e ASPNETCORE_ENVIRONMENT=Production -e ASPNETCORE_URLS="https://+:8443" \
  -e AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
  -e AzureKeyVault__KeyVaultSecret="YOUR_KV_SECRET" \
  -e Logging__PiiHmacKey="YOUR_32_BYTE_BASE64_KEY" \
  mcr.microsoft.com/dotnet/aspnet:9.0 \
  dotnet /app/WebAppExperimental266.dll --contentRoot /app --configDir /app/config
```

### 6. OpenBSD Packet Filter (pf) ፋየርዎልን አዋቅር

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. ወደ Azure አገልግሎቶች ወጪ ግንኙነት

| አገልግሎት | ነጥብ |
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

## የውቅር ማጣቀሻ

`appsettings.template.json`ን `appsettings.json` ወደ ቅዳ እና ሁሉንም `{{PLACEHOLDER}}` ዋጋዎችን ተካ።

| ክፍል | ቁልፍ | ዝርዝር |
|---|---|---|
| `AzureAd` | `TenantId`፣ `ClientId`፣ `ClientSecret` | Azure AD አፕ ምዝገባ |
| `AzureKeyVault` | `KeyVaultURL`፣ `KeyVaultSecret`፣ `KeyVaultPassName` | Key Vault እና ሰርተፊኬት ስም |
| `MtlsSettings` | `RequireClientCertificate`፣ `AllowedIssuers` | mTLS ደንበኛ ሰርተፊኬት ፖሊሲ |
| `NonceEncryption` | `Key`፣ `IV` | nonce ምስጠራ 32-byte ቁልፍ እና 16-byte IV (base64) |
| `BlobSettings` | `BlobConnectionString`፣ `MaxAttachments` | Blob Storage ግንኙነት |
| `CosmosDb` | `CosmosConnectionString`፣ `DatabaseName`፣ `ContainerName` | Cosmos DB ግንኙነት |
| `AwsSecretsManager` | `Region`፣ `CertificateSecretName`፣ `IVSecretName`፣ `NonceKeySecretName`፣ `AccessKeyId`፣ `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`፣ `TableName`፣ `AccessKeyId`፣ `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`፣ `CertificateSecretId`፣ `IVSecretId`፣ `NonceKeySecretId`፣ `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`፣ `DatabaseId`፣ `CollectionName`፣ `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`፣ `CacheDurationMinutes` | OCSP ማረጋገጫ (ተቋርጦ) |
| `Logging` | `PiiHmacKey` | ምዝገባ ውስጥ PII ሃሽ ለማድረግ 32-byte base64 HMAC ቁልፍ |

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

---

## የድጋፍ ስክሪፕቶች

| ስክሪፕት | ዓላማ |
|---|---|
| `IVandKeySampleGenerator.ps1` | ዘፈቀደ 32-byte AES ቁልፍ እና 16-byte IV (base64) አመንጭ |
| `HashInlineScriptPowerShell.ps1` | inline ስክሪፕቶች SHA-256 ሃሾችን ሰላ (ለCSP ዝርዝር ፍቃዶች) |
| `HashInlineScriptPowerShellBase64Output.ps1` | ከላይ ተመሳሳይ፣ ሃሾችን base64 ቅርፀት ያወጣል |
| `CertificateUploaderToAzureExample.ps1` | PFX ሰርተፊኬት ወደ Azure Key Vault ጫን |
| `CheckRoles.ps1` | ለአፕሊኬሽኑ Azure RBAC ሚና ምደቦችን አረጋግጥ |
| `ExportResourceGroups.ps1` | Azure ሀብት ቡድን ውቅሮችን ወደ ውጭ አስተላልፍ |
| `TroubleshootingCosmosDBInfo.ps1` | Cosmos DB ግንኙነትን ምርመራ አድርግ |
| `SetupFromTemplate.ps1` | ከ`appsettings.template.json` የመጀመሪያ ቅንብርን አሰር |

---

## የደህንነት ማስታወሻዎች

- **ሚስጥሮችን ወደ ምንጭ ቁጥጥር ፈፅሞ አቅርብ።** .NET ተጠቃሚ ሚስጥሮችን ቦታ ላይ ቃነስ፣ Azure App ቅንብሮች / Key Vault ማጣቀሻዎች ምርት ላይ።
- OCSP ማረጋገጫ ትግበራ ሁሉንም ሰርተፊኬቶች የሚቃወም **ተቋርጦ** ነው። `EnableOcspValidation` ምርት ላይ ከማቃነስ በፊት `OcspValidationService.cs` ውስጥ `PerformOcspValidationAsync`ን ለውጥ።
- Nonce ዋጋዎች **ፈፅሞ አይዘገቡም** — nonce ን ሳይሸፈን ምዝገባ ምርት ውስጥ ምዝገባ ዳጎናቸውን ያለው ጥቃቀኛ arbitrary inline ስክሪፕቶችን ሊጨምር ይችላል።
- `Server` ምላሽ ጭንቅላት ወደ `webserver` ተሸፍኗል ለአፕ መረጃ ሳይወጣ።
- AWS `AccessKeyId` እና `SecretAccessKey` `appsettings.json` ውስጥ **ፈፅሞ ሊኖሩ አይገባም** — ተጠቃሚ ሚስጥሮች፣ አካባቢ ተለዋዋጮች ወይም IAM ምሳሌ ሚናዎችን ቃነስ።
- GCP ምስክርነቶች የአገልግሎት አካውንት JSON ፋይሎችን ከማቅረብ ይልቅ **ነባሪ አፕሊኬሽን ምስክርነቶችን (ADC)** ሊጠቀሙ ይገባቸዋል።
