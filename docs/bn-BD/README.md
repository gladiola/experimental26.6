# WebAppExperimental266

Azure AD প্রমাণীকরণ, মিউচুয়াল TLS (mTLS), Azure Key Vault সার্টিফিকেট ব্যবস্থাপনা, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, এবং nonce-ভিত্তিক কন্টেন্ট সিকিউরিটি পলিসি সহ একটি শক্তিশালী HTTP নিরাপত্তা স্তর সম্বলিত একটি ASP.NET Core 9 Razor Pages ওয়েব অ্যাপ্লিকেশন।

---

## বিষয়বস্তুর সারণী

- [বৈশিষ্ট্যসমূহ](#বৈশিষ্ট্যসমূহ)
- [ফিচার ফ্ল্যাগ](#ফিচার-ফ্ল্যাগ)
- [পূর্বশর্তসমূহ](#পূর্বশর্তসমূহ)
- [ইনস্টলেশন – Windows Azure (App Service)](#ইনস্টলেশন--windows-azure-app-service)
- [ইনস্টলেশন – Azure সার্ভিসের সাথে যোগাযোগকারী OpenBSD সার্ভার](#ইনস্টলেশন--azure-সার্ভিসের-সাথে-যোগাযোগকারী-openbsd-সার্ভার)
- [কনফিগারেশন রেফারেন্স](#কনফিগারেশন-রেফারেন্স)
- [সাপোর্ট স্ক্রিপ্ট](#সাপোর্ট-স্ক্রিপ্ট)
- [নিরাপত্তা নোট](#নিরাপত্তা-নোট)

---

## বৈশিষ্ট্যসমূহ

### Azure AD প্রমাণীকরণ (OpenID Connect)
অ্যাপ্লিকেশনটি `Microsoft.Identity.Web` এর মাধ্যমে OpenID Connect প্রোটোকল ব্যবহার করে **Microsoft Identity Platform** এর মাধ্যমে ব্যবহারকারীদের প্রমাণীকরণ করে। `/Experimental` এর অধীনে সমস্ত রুটের জন্য একটি প্রমাণীকৃত Azure AD পরিচয় প্রয়োজন। `/Privacy`, `/Error`, এবং `/About` পৃষ্ঠাগুলি সর্বজনীনভাবে অ্যাক্সেসযোগ্য।

### mTLS ক্লায়েন্ট সার্টিফিকেট প্রমাণীকরণ
সক্রিয় করা হলে, ক্লায়েন্টদের একটি বৈধ X.509 সার্টিফিকেট উপস্থাপন করতে হবে। `MtlsSettings` এর সেটিংস নিয়ন্ত্রণ করে যে চেইনড সার্টিফিকেট, স্ব-স্বাক্ষরিত সার্টিফিকেট, বা উভয়ই অনুমোদিত কিনা, সার্টিফিকেট প্রত্যাহার যাচাইকরণ, এবং অনুমোদিত সার্টিফিকেট ইস্যুকারী।

### Azure Key Vault ইন্টিগ্রেশন
অ্যাপ্লিকেশনটি স্টার্টআপে Azure Key Vault থেকে TLS **সার্ভার সার্টিফিকেট** পুনরুদ্ধার করে। লোড করা `X509Certificate2` সরাসরি Kestrel এর HTTPS কনফিগারেশনে ইনজেক্ট করা হয়, তাই ডিস্কে কোনো PFX ফাইল থাকার প্রয়োজন নেই।

### প্রতি-অনুরোধ Nonce সহ কন্টেন্ট সিকিউরিটি পলিসি
সক্রিয় করা হলে, প্রতিটি HTTP প্রতিক্রিয়া একটি `Content-Security-Policy` হেডার বহন করে যার `script-src` নির্দেশিকায় প্রতিটি অনুরোধের জন্য একটি **ক্রিপ্টোগ্রাফিক্যালি র‍্যান্ডম nonce** অন্তর্ভুক্ত থাকে। CSP ইনলাইন স্ক্রিপ্টের জন্য SHA-256 হ্যাশ-ভিত্তিক অনুমতি তালিকাও সমর্থন করে।

### স্ট্যান্ডার্ড HTTP নিরাপত্তা হেডার
`UseStandardSecurityHeaders` প্রতিটি প্রতিক্রিয়ায় যোগ করে: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, এবং `Server`, `X-Powered-By`, এবং `X-AspNetMvc-Version` হেডার অপসারণ।

### Azure Blob Storage
সক্রিয় করা হলে, `BlobSettingsService` একটি কানেকশন স্ট্রিং এবং কনফিগারযোগ্য সর্বাধিক সংযুক্তির সংখ্যা দ্বারা সমর্থিত একটি Scoped সার্ভিস প্রদান করে।

### Azure Cosmos DB
সক্রিয় করা হলে, অ্যাপ্লিকেশনটি `database.ReadAsync()` কল করে স্টার্টআপে Cosmos DB সংযোগ যাচাই করে।

### AWS Secrets Manager
সক্রিয় করা হলে, `AwsSecretsManagerOperationsService` AWS Secrets Manager থেকে সিক্রেট এবং সার্টিফিকেট পুনরুদ্ধার করে। `AwsSecretsManager` বিভাগে কনফিগারেশন: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, এবং `AccessKeyId`/`SecretAccessKey` ক্রেডেনশিয়াল।

### Amazon DynamoDB
সক্রিয় করা হলে, `AwsDynamoDbService` স্টার্টআপে DynamoDB টেবিলের কানেক্টিভিটি যাচাই করে। `AwsDynamoDb` বিভাগে কনফিগারেশন: `Region`, `TableName`, এবং `AccessKeyId`/`SecretAccessKey` ক্রেডেনশিয়াল।

### GCP Secret Manager
সক্রিয় করা হলে, `GcpSecretManagerOperationsService` Google Cloud Secret Manager থেকে সিক্রেট পুনরুদ্ধার করে। `GcpSecretManager` বিভাগে কনফিগারেশন: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, এবং `CredentialFilePath` (ঐচ্ছিক, খালি হলে ADC ব্যবহার করে)।

### GCP Firestore
সক্রিয় করা হলে, `GcpFirestoreService` স্টার্টআপে Firestore ক্লায়েন্ট তৈরি করে। `GcpFirestore` বিভাগে কনফিগারেশন: `ProjectId`, `DatabaseId` (ডিফল্ট: "(default)"), `CollectionName`, এবং `CredentialFilePath` (ঐচ্ছিক)।

### AWS Cognito পরিচয় ব্যবস্থাপনা
সক্রিয় করা হলে, `AddAwsCognitoAuthentication` একটি **Amazon Cognito User Pool** এর বিপরীতে OpenID Connect প্রমাণীকরণ কনফিগার করে — Microsoft Entra ID / Azure AD এর AWS সমতুল্য। OIDC আবিষ্কার এন্ডপয়েন্ট:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
`AwsCognito` বিভাগে কনফিগারেশন: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (User Secrets এ সংরক্ষণ করুন), এবং `Domain`।

### GCP Identity Platform
সক্রিয় করা হলে, `AddGcpIdentityAuthentication` **Google OAuth 2.0 / OIDC** ব্যবহার করে OpenID Connect প্রমাণীকরণ কনফিগার করে — Microsoft Entra ID / Azure AD এর GCP সমতুল্য। OIDC আবিষ্কার এন্ডপয়েন্ট:
`https://accounts.google.com/.well-known/openid-configuration`
`GcpIdentity` বিভাগে কনফিগারেশন: `ClientId`, `ClientSecret` (User Secrets এ সংরক্ষণ করুন), এবং ঐচ্ছিক `ProjectId`।

### নিরাপদ সেশন ব্যবস্থাপনা
সেশনগুলি **30 মিনিটের নিষ্ক্রিয়তা টাইমআউট** সহ ইন-প্রসেস ডিস্ট্রিবিউটেড মেমরি ক্যাশ ব্যবহার করে। সেশন কুকিগুলি `HttpOnly`, `Secure = Always`, এবং `SameSite = Strict` দিয়ে কনফিগার করা হয়েছে।

### স্থানীয়করণ
অ্যাপ্লিকেশনটি **২৫টি ভাষা** সমর্থন করে: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, এবং ga-IE। আরবিতে স্বয়ংক্রিয় RTL লেআউট স্যুইচিং অন্তর্ভুক্ত।

### PII-নিরাপদ লগিং
`LoggingHelper` HMAC-SHA256 ব্যবহার করে লগ আউটপুটে ব্যক্তিগতভাবে সনাক্তযোগ্য তথ্য হ্যাশ করে। একটি স্থিতিশীল 32-বাইট কী `Logging:PiiHmacKey` এর মাধ্যমে সরবরাহ করা যেতে পারে।

---

## ফিচার ফ্ল্যাগ

সমস্ত প্রধান সাবসিস্টেম `appsettings.json` এ Boolean ফিচার ফ্ল্যাগ দ্বারা নিয়ন্ত্রিত।

| ফ্ল্যাগ | ডিফল্ট | বিবরণ |
|---|---|---|
| `EnableSession` | `true` | সার্ভার-সাইড সেশন এবং সেশন কুকি |
| `EnableLocalization` | `true` | বহুভাষিক সমর্থন (২৫টি ভাষা) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect প্রমাণীকরণ |
| `EnableAuthorization` | `true` | রুট-স্তর অনুমোদন নীতি |
| `EnableKeyVault` | `false` | Azure Key Vault থেকে TLS সার্ভার সার্টিফিকেট লোড |
| `EnableNonceServices` | `false` | প্রতি-অনুরোধ CSP nonce জেনারেশন |
| `EnableCSP` | `false` | `Content-Security-Policy` হেডার সংযুক্ত করুন |
| `EnableSecurityHeaders` | `true` | স্ট্যান্ডার্ড HTTP নিরাপত্তা হেডার সংযুক্ত করুন |
| `EnableBlobStorage` | `false` | Azure Blob Storage সার্ভিস |
| `EnableCosmosDb` | `false` | Azure Cosmos DB সার্ভিস |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (স্টাব) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect পরিচয় ব্যবস্থাপনা |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (স্টাব) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | ক্লায়েন্ট TLS সার্টিফিকেট প্রয়োজন |
| `EnableOcspValidation` | `false` | OCSP সার্টিফিকেট প্রত্যাহার চেক (স্টাব) |

---

## পূর্বশর্তসমূহ

1. **Azure AD অ্যাপ রেজিস্ট্রেশন** – রিডাইরেক্ট URI, ক্লায়েন্ট সিক্রেট বা সার্টিফিকেট ক্রেডেনশিয়াল সহ।
2. **Azure Key Vault** – সিক্রেট হিসেবে PFX সার্ভার সার্টিফিকেট ধারণ করে।
3. **Azure Cosmos DB অ্যাকাউন্ট** (ঐচ্ছিক)।
4. **Azure Blob Storage অ্যাকাউন্ট** (ঐচ্ছিক)।
5. **.NET 9 SDK / Runtime** – সংস্করণ 9.0 বা পরবর্তী।
6. **AWS ক্রেডেনশিয়াল** (`secretsmanager` এবং `dynamodb` পারমিশন সহ IAM ব্যবহারকারী বা রোল) – `EnableAwsSecretsManager` বা `EnableAwsDynamoDb` সক্রিয় হলে প্রয়োজন।
7. **GCP সার্ভিস অ্যাকাউন্ট বা ADC** (`secretmanager` এবং `datastore` পারমিশন সহ) – `EnableGcpSecretManager` বা `EnableGcpFirestore` সক্রিয় হলে প্রয়োজন।

---

## ইনস্টলেশন – Windows Azure (App Service)

### ১. Azure রিসোর্স তৈরি করুন

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### ২. Azure AD অ্যাপ্লিকেশন রেজিস্টার করুন

[Azure Portal](https://portal.azure.com) এ:
1. **Microsoft Entra ID → অ্যাপ রেজিস্ট্রেশন → নতুন রেজিস্ট্রেশন** এ যান।
2. রিডাইরেক্ট URI `https://<your-app>.azurewebsites.net/signin-oidc` সেট করুন।
3. **সার্টিফিকেট ও সিক্রেট** এর অধীনে একটি ক্লায়েন্ট সিক্রেট তৈরি করুন এবং মান কপি করুন।
4. Overview ব্লেড থেকে **Tenant ID** এবং **Client ID** নোট করুন।

### ৩. Azure Key Vault তৈরি করুন এবং সার্ভার সার্টিফিকেট আপলোড করুন

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### ৪. অ্যাপ্লিকেশন সেটিংস কনফিগার করুন

`appsettings.template.json` কপি করে `appsettings.json` এ রাখুন এবং প্লেসহোল্ডার মানগুলি পূরণ করুন। সিক্রেটগুলি সোর্স কন্ট্রোলে **সংরক্ষণ করা উচিত নয়** — এগুলিকে App Service Application Settings হিসেবে বা স্থানীয়ভাবে User Secrets এর মাধ্যমে সেট করুন:

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

### ৫. অ্যাপ্লিকেশন ডিপ্লয় করুন

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### ৬. HTTPS এবং কাস্টম ডোমেইন সক্রিয় করুন (প্রস্তাবিত)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### ৭. Azure App Service এ mTLS সক্রিয় করুন (ঐচ্ছিক)

1. **App Service → TLS/SSL সেটিংস → ক্লায়েন্ট সার্টিফিকেট** এ যান।
2. **ইনকামিং ক্লায়েন্ট সার্টিফিকেট** **প্রয়োজনীয়** সেট করুন।

তারপর Application Settings এ `FeatureFlags__EnableMtls=true` সেট করুন।

---

## ইনস্টলেশন – Azure সার্ভিসের সাথে যোগাযোগকারী OpenBSD সার্ভার

> **গুরুত্বপূর্ণ:** .NET 9 এর জন্য OpenBSD এর জন্য কোনো সরকারী Microsoft বিল্ড **নেই**। নীচের নির্দেশাবলী HTTPS এর মাধ্যমে Azure সার্ভিসের সাথে যোগাযোগ করার সময় OpenBSD এ ASP.NET Core 9 অ্যাপ্লিকেশন চালানোর জন্য একটি **Linux-সামঞ্জস্যপূর্ণ কন্টেইনার** (via [Podman](https://podman.io/)) ব্যবহার করে।

### ১. OpenBSD এ পূর্বশর্ত ইনস্টল করুন

```sh
pkg_add podman
pkg_add curl git
```

### ২. ASP.NET Core 9 Runtime ইমেজ পুল করুন

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### ৩. অ্যাপ্লিকেশন বিল্ড করুন (Linux বা Windows বিল্ড মেশিনে)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### ৪. কনফিগারেশন ফাইল তৈরি করুন

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### ৫. কন্টেইনার চালান

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

### ৬. OpenBSD Packet Filter (pf) ফায়ারওয়াল কনফিগার করুন

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### ৭. Azure সার্ভিসে আউটবাউন্ড কানেক্টিভিটি

| সার্ভিস | এন্ডপয়েন্ট |
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

## কনফিগারেশন রেফারেন্স

`appsettings.template.json` কপি করে `appsettings.json` এ রাখুন এবং সমস্ত `{{PLACEHOLDER}}` মান প্রতিস্থাপন করুন।

| বিভাগ | কী | বিবরণ |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Azure AD অ্যাপ রেজিস্ট্রেশন |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault এবং সার্টিফিকেটের নাম |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | mTLS ক্লায়েন্ট সার্টিফিকেট নীতি |
| `NonceEncryption` | `Key`, `IV` | nonce এনক্রিপশনের জন্য 32-বাইট কী এবং 16-বাইট IV (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Blob Storage কানেকশন |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Cosmos DB কানেকশন |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | OCSP যাচাইকরণ (স্টাব) |
| `Logging` | `PiiHmacKey` | লগে PII হ্যাশিংয়ের জন্য 32-বাইট base64 HMAC কী |

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

---

## সাপোর্ট স্ক্রিপ্ট

| স্ক্রিপ্ট | উদ্দেশ্য |
|---|---|
| `IVandKeySampleGenerator.ps1` | একটি র‍্যান্ডম 32-বাইট AES কী এবং 16-বাইট IV (base64) তৈরি করুন |
| `HashInlineScriptPowerShell.ps1` | ইনলাইন স্ক্রিপ্টের SHA-256 হ্যাশ গণনা করুন (CSP অনুমতি তালিকার জন্য) |
| `HashInlineScriptPowerShellBase64Output.ps1` | উপরের মতো, base64 ফরম্যাটে হ্যাশ আউটপুট করে |
| `CertificateUploaderToAzureExample.ps1` | Azure Key Vault এ একটি PFX সার্টিফিকেট আপলোড করুন |
| `CheckRoles.ps1` | অ্যাপ্লিকেশনের জন্য Azure RBAC রোল অ্যাসাইনমেন্ট যাচাই করুন |
| `ExportResourceGroups.ps1` | Azure রিসোর্স গ্রুপ কনফিগারেশন এক্সপোর্ট করুন |
| `TroubleshootingCosmosDBInfo.ps1` | Cosmos DB কানেক্টিভিটি নির্ণয় করুন |
| `SetupFromTemplate.ps1` | `appsettings.template.json` থেকে প্রাথমিক কনফিগারেশন স্বয়ংক্রিয় করুন |

---

## নিরাপত্তা নোট

- **সিক্রেটগুলি সোর্স কন্ট্রোলে কমিট করবেন না।** স্থানীয়ভাবে .NET User Secrets এবং প্রোডাকশনে Azure App Settings / Key Vault রেফারেন্স ব্যবহার করুন।
- OCSP যাচাইকরণ বাস্তবায়ন একটি **স্টাব** যা সমস্ত সার্টিফিকেট প্রত্যাখ্যান করে। প্রোডাকশনে `EnableOcspValidation` সক্রিয় করার আগে `OcspValidationService.cs` এ `PerformOcspValidationAsync` প্রতিস্থাপন করুন।
- Nonce মানগুলি **কখনই লগ করা হয় না** — প্লেইন টেক্সটে nonce লগ করা একজন আক্রমণকারীকে লগ অ্যাক্সেস দিয়ে ইচ্ছামতো ইনলাইন স্ক্রিপ্ট ইনজেক্ট করতে দেবে।
- `Server` রেসপন্স হেডার প্ল্যাটফর্ম তথ্য প্রকাশ এড়াতে `webserver` এ মাস্ক করা হয়েছে।
- AWS `AccessKeyId` এবং `SecretAccessKey` `appsettings.json` এ **কখনই উপস্থিত থাকা উচিত নয়** — User Secrets, পরিবেশ পরিবর্তনশীল, বা IAM ইনস্ট্যান্স রোল ব্যবহার করুন।
- GCP ক্রেডেনশিয়ালগুলির সার্ভিস অ্যাকাউন্ট JSON ফাইল কমিট করার পরিবর্তে **Application Default Credentials (ADC)** ব্যবহার করা উচিত।
