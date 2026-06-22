# WebAppExperimental266

Azure AD प्रमाणीकरण, म्यूचुअल TLS (mTLS), Azure Key Vault प्रमाण-पत्र प्रबंधन, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, GCP Secret Manager, GCP Firestore, और nonce-आधारित Content Security Policy के साथ सुदृढ़ HTTP सुरक्षा परत वाला ASP.NET Core 9 Razor Pages वेब अनुप्रयोग।

---

## विषय-सूची

- [विशेषताएँ](#विशेषताएँ)
- [फ़ीचर फ़्लैग](#फ़ीचर-फ़्लैग)
- [पूर्वापेक्षाएँ](#पूर्वापेक्षाएँ)
- [इंस्टॉलेशन – Windows Azure (App Service)](#इंस्टॉलेशन--windows-azure-app-service)
- [इंस्टॉलेशन – Azure सेवाओं से संचार करने वाला OpenBSD सर्वर](#इंस्टॉलेशन--azure-सेवाओं-से-संचार-करने-वाला-openbsd-सर्वर)
- [कॉन्फ़िगरेशन संदर्भ](#कॉन्फ़िगरेशन-संदर्भ)
- [सहायक स्क्रिप्ट](#सहायक-स्क्रिप्ट)
- [सुरक्षा नोट्स](#सुरक्षा-नोट्स)

---

## विशेषताएँ

### Azure AD प्रमाणीकरण (OpenID Connect)
अनुप्रयोग OpenID Connect प्रोटोकॉल का उपयोग करके **Microsoft Identity Platform** के माध्यम से उपयोगकर्ताओं को प्रमाणित करता है (`Microsoft.Identity.Web` के ज़रिए)। `/Experimental` के अंतर्गत सभी रूट्स के लिए प्रमाणित Azure AD पहचान आवश्यक है। `/Privacy`, `/Error`, और `/About` पृष्ठ सार्वजनिक रूप से सुलभ हैं।

### mTLS क्लाइंट सर्टिफिकेट प्रमाणीकरण
सक्षम होने पर, क्लाइंट को एक वैध X.509 सर्टिफिकेट प्रस्तुत करना होगा। `MtlsSettings` में सेटिंग्स यह नियंत्रित करती हैं कि चेन किए गए, स्व-हस्ताक्षरित या दोनों प्रकार के सर्टिफिकेट, सर्टिफिकेट निरसन जाँच और अनुमत सर्टिफिकेट जारीकर्ता की अनुमति है या नहीं।

### Azure Key Vault एकीकरण
अनुप्रयोग स्टार्टअप पर Azure Key Vault से TLS **सर्वर सर्टिफिकेट** प्राप्त करता है। लोड किया गया `X509Certificate2` सीधे Kestrel के HTTPS डिफ़ॉल्ट में इंजेक्ट किया जाता है, इसलिए डिस्क पर कोई PFX फ़ाइल आवश्यक नहीं है।

### प्रति-अनुरोध Nonce के साथ Content Security Policy
सक्षम होने पर, प्रत्येक HTTP प्रतिक्रिया में एक `Content-Security-Policy` हेडर होता है जिसकी `script-src` निर्देशिका में प्रति-अनुरोध **क्रिप्टोग्राफिक रूप से यादृच्छिक nonce** होती है। CSP इनलाइन स्क्रिप्ट के लिए SHA-256 हैश-आधारित अनुमति सूचियों का भी समर्थन करती है।

### मानक HTTP सुरक्षा हेडर
`UseStandardSecurityHeaders` प्रत्येक प्रतिक्रिया में निम्न जोड़ता है: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, और `Server`, `X-Powered-By`, `X-AspNetMvc-Version` हेडर हटाता है।

### Azure Blob Storage
सक्षम होने पर, `BlobSettingsService` एक कनेक्शन स्ट्रिंग और कॉन्फ़िगर करने योग्य अधिकतम अनुलग्नक संख्या द्वारा समर्थित Scoped सेवा प्रदान करता है।

### Azure Cosmos DB
सक्षम होने पर, अनुप्रयोग `database.ReadAsync()` कॉल करके स्टार्टअप पर Cosmos DB कनेक्शन सत्यापित करता है।

### AWS Secrets Manager
सक्षम होने पर, `AwsSecretsManagerOperationsService` AWS Secrets Manager से सीक्रेट और सर्टिफिकेट प्राप्त करता है। `AwsSecretsManager` अनुभाग में `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName` और `AccessKeyId`/`SecretAccessKey` क्रेडेंशियल के साथ कॉन्फ़िगरेशन।

### Amazon DynamoDB
सक्षम होने पर, `AwsDynamoDbService` स्टार्टअप पर DynamoDB तालिका कनेक्टिविटी सत्यापित करता है। `AwsDynamoDb` अनुभाग में `Region`, `TableName` और `AccessKeyId`/`SecretAccessKey` क्रेडेंशियल के साथ कॉन्फ़िगरेशन।

### GCP Secret Manager
सक्षम होने पर, `GcpSecretManagerOperationsService` Google Cloud Secret Manager से सीक्रेट प्राप्त करता है। `GcpSecretManager` अनुभाग में `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId` और `CredentialFilePath` (वैकल्पिक, खाली होने पर ADC उपयोग करता है) के साथ कॉन्फ़िगरेशन।

### GCP Firestore
सक्षम होने पर, `GcpFirestoreService` स्टार्टअप पर Firestore क्लाइंट बनाता है। `GcpFirestore` अनुभाग में `ProjectId`, `DatabaseId` (डिफ़ॉल्ट: "(default)"), `CollectionName` और `CredentialFilePath` (वैकल्पिक) के साथ कॉन्फ़िगरेशन।

### AWS Cognito पहचान प्रबंधन
सक्षम होने पर, `AddAwsCognitoAuthentication` एक **Amazon Cognito User Pool** के विरुद्ध OpenID Connect प्रमाणीकरण कॉन्फ़िगर करता है — Microsoft Entra ID / Azure AD का AWS समकक्ष। OIDC डिस्कवरी एंडपॉइंट:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
`AwsCognito` अनुभाग में कॉन्फ़िगर करें: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (User Secrets में संग्रहीत करें) और `Domain` (Cognito होस्टेड UI डोमेन)।

### GCP Identity Platform
सक्षम होने पर, `AddGcpIdentityAuthentication` **Google OAuth 2.0 / OIDC** का उपयोग करके OpenID Connect प्रमाणीकरण कॉन्फ़िगर करता है — Microsoft Entra ID / Azure AD का GCP समकक्ष। OIDC डिस्कवरी एंडपॉइंट:
`https://accounts.google.com/.well-known/openid-configuration`
`GcpIdentity` अनुभाग में कॉन्फ़िगर करें: `ClientId`, `ClientSecret` (User Secrets में संग्रहीत करें) और वैकल्पिक `ProjectId`। **Google Cloud Console → APIs और Services → Credentials** में क्रेडेंशियल प्राप्त करें।

### सुरक्षित सत्र प्रबंधन
सत्र **30 मिनट के निष्क्रियता टाइमआउट** के साथ इन-प्रोसेस वितरित मेमोरी कैश का उपयोग करते हैं। सत्र कुकीज़ `HttpOnly`, `Secure = Always` और `SameSite = Strict` के रूप में कॉन्फ़िगर की जाती हैं।

### स्थानीयकरण
अनुप्रयोग **11 भाषाओं** का समर्थन करता है: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU और ar-SA। अरबी में स्वचालित RTL लेआउट स्विचिंग शामिल है।

### PII-सुरक्षित लॉगिंग
`LoggingHelper` HMAC-SHA256 का उपयोग करके लॉग आउटपुट में व्यक्तिगत पहचान योग्य जानकारी को हैश करता है। `Logging:PiiHmacKey` के माध्यम से एक स्थिर 32-बाइट कुंजी प्रदान की जा सकती है।

---

## फ़ीचर फ़्लैग

सभी प्रमुख सबसिस्टम `appsettings.json` में बूलियन फ़ीचर फ़्लैग द्वारा नियंत्रित होते हैं।

| फ़्लैग | डिफ़ॉल्ट | विवरण |
|---|---|---|
| `EnableSession` | `true` | सर्वर-साइड सत्र और सत्र कुकी |
| `EnableLocalization` | `true` | बहुभाषी समर्थन (11 भाषाएँ) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect प्रमाणीकरण |
| `EnableAuthorization` | `true` | रूट-स्तरीय प्राधिकरण नीतियाँ |
| `EnableKeyVault` | `false` | Azure Key Vault से TLS सर्वर सर्टिफिकेट लोड करें |
| `EnableNonceServices` | `false` | प्रति-अनुरोध CSP nonce जनरेशन |
| `EnableCSP` | `false` | `Content-Security-Policy` हेडर संलग्न करें |
| `EnableSecurityHeaders` | `true` | मानक HTTP सुरक्षा हेडर संलग्न करें |
| `EnableBlobStorage` | `false` | Azure Blob Storage सेवा |
| `EnableCosmosDb` | `false` | Azure Cosmos DB सेवा |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager स्टब |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito पहचान प्रबंधन (OpenID Connect) |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager स्टब |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | क्लाइंट TLS सर्टिफिकेट की आवश्यकता |
| `EnableOcspValidation` | `false` | OCSP सर्टिफिकेट निरसन जाँच (स्टब) |

---

## पूर्वापेक्षाएँ

1. **Azure AD ऐप पंजीकरण** – पुनर्निर्देशन URI, क्लाइंट सीक्रेट या सर्टिफिकेट क्रेडेंशियल के साथ।
2. **Azure Key Vault** – PFX सर्वर सर्टिफिकेट को सीक्रेट के रूप में।
3. **Azure Cosmos DB खाता** (वैकल्पिक)।
4. **Azure Blob Storage खाता** (वैकल्पिक)।
5. **.NET 9 SDK / रनटाइम** – संस्करण 9.0 या उसके बाद का।
6. **AWS क्रेडेंशियल** (IAM उपयोगकर्ता/भूमिका के साथ `secretsmanager` और `dynamodb` अनुमतियाँ) – `EnableAwsSecretsManager` या `EnableAwsDynamoDb` सक्षम होने पर आवश्यक।
7. **GCP सेवा खाता या ADC** (`secretmanager` और `datastore` अनुमतियों के साथ) – `EnableGcpSecretManager` या `EnableGcpFirestore` सक्षम होने पर आवश्यक।

---

## इंस्टॉलेशन – Windows Azure (App Service)

### 1. Azure संसाधन बनाएं

```powershell
# Log in
az login

# Create a resource group
az group create --name MyResourceGroup --location eastus

# Create an App Service plan (Linux or Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Create the web app (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Azure AD एप्लिकेशन रजिस्टर करें

[Azure Portal](https://portal.azure.com) में:
1. **Microsoft Entra ID → App registrations → New registration** पर नेविगेट करें।
2. रीडायरेक्ट URI को `https://<your-app>.azurewebsites.net/signin-oidc` पर सेट करें।
3. **Certificates & secrets** के अंतर्गत, एक क्लाइंट सीक्रेट बनाएं और मान कॉपी करें।
4. Overview ब्लेड से **Tenant ID** और **Client ID** नोट करें।

### 3. Azure Key Vault बनाएं और सर्वर सर्टिफिकेट अपलोड करें

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# Upload your PFX as a Key Vault secret (base64-encoded)
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# Grant the App Service Managed Identity access
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. एप्लिकेशन सेटिंग कॉन्फ़िगर करें

`appsettings.template.json` को `appsettings.json` में कॉपी करें और प्लेसहोल्डर मान भरें। सीक्रेट को सोर्स कंट्रोल में **संग्रहीत नहीं किया जाना चाहिए** — उन्हें App Service Application Settings के रूप में या स्थानीय रूप से User Secrets के माध्यम से सेट करें:

```powershell
# In Azure App Service, set secrets as app settings:
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

### 5. एप्लिकेशन डिप्लॉय करें

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. HTTPS और कस्टम डोमेन सक्षम करें (अनुशंसित)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Azure App Service पर mTLS सक्षम करें (वैकल्पिक)

Azure App Service पोर्टल के माध्यम से क्लाइंट सर्टिफिकेट का समर्थन करता है:
1. **App Service → TLS/SSL settings → Client certificates** पर जाएं।
2. **Incoming client certificates** को **Require** पर सेट करें।

फिर Application Settings में `FeatureFlags__EnableMtls=true` सेट करें।

---

## इंस्टॉलेशन – Azure सेवाओं से संचार करने वाला OpenBSD सर्वर

> **महत्वपूर्ण:** .NET 9 के पास OpenBSD के लिए कोई आधिकारिक Microsoft बिल्ड **नहीं** है। नीचे दिए गए निर्देश HTTPS के माध्यम से Azure सेवाओं के साथ संचार करते हुए OpenBSD पर ASP.NET Core 9 एप्लिकेशन चलाने के लिए **Linux-संगत कंटेनर** ([Podman](https://podman.io/) के माध्यम से, जो OpenBSD के पैकेज ट्री में उपलब्ध है) का उपयोग करते हैं।

### 1. OpenBSD पर पूर्वापेक्षाएं इंस्टॉल करें

```sh
# As root
pkg_add podman
pkg_add curl git
```

यदि आपके OpenBSD संस्करण के लिए न Podman और न Docker उपलब्ध है, तो **Linux VM** (जैसे Debian/Ubuntu गेस्ट के साथ vmm(4)) में ऐप चलाने और उस गेस्ट के भीतर से मानक Linux डिप्लॉयमेंट पथ का पालन करने पर विचार करें।

### 2. ASP.NET Core 9 Runtime Image प्राप्त करें

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. एप्लिकेशन बनाएं (Linux या Windows बिल्ड मशीन पर)

.NET 9 SDK इंस्टॉल की गई मशीन पर, Linux x64 को लक्षित करते हुए एक self-contained बिल्ड प्रकाशित करें:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

`publish/` डायरेक्टरी को OpenBSD होस्ट पर ट्रांसफर करें (जैसे `scp` या shared volume के माध्यम से)।

### 4. कॉन्फ़िगरेशन फ़ाइल बनाएं

OpenBSD होस्ट पर, अपने प्रोडक्शन मानों के साथ `/etc/webappexp26/appsettings.json` बनाएं (फ़ाइल में कोई सीक्रेट नहीं; इसके बजाय environment variables का उपयोग करें):

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

सीक्रेट को अगले चरण में environment variables के रूप में inject किया जाता है।

### 5. कंटेनर चलाएं

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

### 6. OpenBSD Packet Filter (pf) Firewall कॉन्फ़िगर करें

Inbound HTTPS की अनुमति देने और Azure endpoints के लिए outbound connections की अनुमति देने के लिए `/etc/pf.conf` में जोड़ें:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Ruleset पुनः लोड करें:

```sh
pfctl -f /etc/pf.conf
```

### 7. DNS और TLS सर्टिफिकेट कॉन्फ़िगर करें

सुनिश्चित करें कि `AllowedHosts` में hostname OpenBSD सर्वर के public IP पर resolve होता है। Azure AD के लिए आवश्यक है कि redirect URI (`/signin-oidc`) HTTPS के माध्यम से पहुंच योग्य हो, इसलिए server certificate trusted होना चाहिए। किसी public CA से certificate उपयोग करें (जैसे `acme-client(1)` के माध्यम से Let's Encrypt) या CA-signed certificate को Azure Key Vault पर अपलोड करें और `EnableKeyVault` सक्षम करें।

### 8. Azure सेवाओं के लिए Outbound Connectivity

निम्नलिखित Azure service endpoints OpenBSD होस्ट से TCP 443 के माध्यम से पहुंच योग्य होने चाहिए:

| Service | Endpoint |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

कंटेनर शुरू करने से पहले connectivity परीक्षण करें:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## कॉन्फ़िगरेशन संदर्भ

`appsettings.template.json` को `appsettings.json` में कॉपी करें और सभी `{{PLACEHOLDER}}` मान बदलें।

| अनुभाग | कुंजी | विवरण |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Azure AD ऐप पंजीकरण |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault और सर्टिफिकेट नाम |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | mTLS क्लाइंट सर्टिफिकेट नीति |
| `NonceEncryption` | `Key`, `IV` | Nonce एन्क्रिप्शन के लिए 32-बाइट कुंजी और 16-बाइट IV (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Blob Storage कनेक्शन |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Cosmos DB कनेक्शन |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | OCSP सत्यापन (स्टब) |
| `Logging` | `PiiHmacKey` | लॉग में PII हैशिंग के लिए 32-बाइट base64 HMAC कुंजी |

शामिल PowerShell script का उपयोग करके encryption keys और IVs उत्पन्न करें:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

स्थानीय विकास के लिए सभी सीक्रेट को **.NET User Secrets** में संग्रहीत करें:

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

> GCP के लिए, सेवा खाता JSON फ़ाइल के पथ पर `GOOGLE_APPLICATION_CREDENTIALS` पर्यावरण चर सेट करें या स्थानीय विकास के लिए `gcloud auth application-default login` चलाएं।

---

## सहायक स्क्रिप्ट

`SupportingScripts/` डायरेक्टरी में PowerShell utilities शामिल हैं:

| स्क्रिप्ट | उद्देश्य |
|---|---|
| `IVandKeySampleGenerator.ps1` | एक यादृच्छिक 32-बाइट AES कुंजी और 16-बाइट IV (base64) उत्पन्न करें |
| `HashInlineScriptPowerShell.ps1` | इनलाइन स्क्रिप्ट के लिए SHA-256 हैश की गणना करें (CSP अनुमति सूची के लिए) |
| `HashInlineScriptPowerShellBase64Output.ps1` | उपरोक्त के समान, base64 प्रारूप में हैश आउटपुट करता है |
| `CertificateUploaderToAzureExample.ps1` | Azure Key Vault में PFX सर्टिफिकेट अपलोड करें |
| `CheckRoles.ps1` | ऐप के लिए Azure RBAC भूमिका असाइनमेंट सत्यापित करें |
| `ExportResourceGroups.ps1` | Azure संसाधन समूह कॉन्फ़िगरेशन निर्यात करें |
| `TroubleshootingCosmosDBInfo.ps1` | Cosmos DB कनेक्टिविटी डायग्नोज़ करें |
| `SetupFromTemplate.ps1` | `appsettings.template.json` से प्रारंभिक कॉन्फ़िगरेशन स्वचालित करें |

---

## सुरक्षा नोट्स

- **कभी भी सोर्स कंट्रोल में सीक्रेट कमिट न करें।**
- OCSP सत्यापन कार्यान्वयन एक **स्टब** है जो सभी सर्टिफिकेट अस्वीकार करता है। प्रोडक्शन में `EnableOcspValidation` सक्षम करने से पहले `PerformOcspValidationAsync` को बदलें।
- Nonce मान **कभी भी लॉग नहीं किए जाते**।
- `Server` प्रतिक्रिया हेडर को `webserver` पर मास्क किया जाता है।
- **AWS या GCP क्रेडेंशियल्स को कभी भी सोर्स कंट्रोल में स्टोर न करें।** Environment variables या secrets manager का उपयोग करें।
- AWS और GCP कार्यान्वयन **स्टब** हैं जिन्हें प्रोडक्शन उपयोग से पहले पूर्ण कार्यान्वयन की आवश्यकता है।
- AWS के लिए, जहाँ संभव हो हार्डकोड access keys के बजाय IAM roles को प्राथमिकता दें।
- GCP के लिए, स्पष्ट service account files के बजाय Application Default Credentials (ADC) को प्राथमिकता दें।
