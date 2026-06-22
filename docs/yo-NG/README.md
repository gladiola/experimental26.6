# WebAppExperimental266

Ohun elo ayelujara ASP.NET Core 9 Razor Pages ti o ni ẹrí Azure AD, TLS ti ẹgbẹ mejeeji (mTLS), iṣakoso iwe-ẹri Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, ati ipele aabo HTTP ti o lagbara pẹlu Eto Aabo Akoonu ti o da lori nonce.

---

## Tabili Awọn Akoonu

- [Awọn Ẹya](#awọn-ẹya)
- [Awọn Asia Ẹya](#awọn-asia-ẹya)
- [Awọn Ibeere Ibẹrẹ](#awọn-ibeere-ibẹrẹ)
- [Fifi Sii – Windows Azure (App Service)](#fifi-sii--windows-azure-app-service)
- [Fifi Sii – Olupin OpenBSD ti n Sọrọ pẹlu Awọn Iṣẹ Azure](#fifi-sii--olupin-openbsd-ti-n-sọrọ-pẹlu-awọn-iṣẹ-azure)
- [Itọkasi Iṣeto](#itọkasi-iṣeto)
- [Awọn Iwe afọwọkọ Atilẹyin](#awọn-iwe-afọwọkọ-atilẹyin)
- [Awọn Akọsilẹ Aabo](#awọn-akọsilẹ-aabo)

---

## Awọn Ẹya

### Ẹrí Azure AD (OpenID Connect)
Ohun elo náà ṣe ẹrí awọn olumulo nipasẹ **Pẹpẹ Idanimọ Microsoft** nipa lilo ilana OpenID Connect (nipasẹ `Microsoft.Identity.Web`). Gbogbo awọn ọna labẹ `/Experimental` nilo idanimọ Azure AD ti o ni ẹrí. Awọn oju-ewe `/Privacy`, `/Error`, ati `/About` wa fun gbogbo eniyan.

### Ẹrí Iwe-ẹri Alabara mTLS
Nigbati o ba ṣiṣẹ, awọn alabara gbọdọ gbekalẹ iwe-ẹri X.509 to wulo. Awọn eto ni `MtlsSettings` ṣakoso boya awọn iwe-ẹri pq, ti ara ẹni fowo si, tabi mejeeji ni a gba laaye, ṣiṣayẹwo iparun iwe-ẹri, ati awọn olupilẹṣẹ iwe-ẹri ti a gba laaye.

### Iṣọpọ Azure Key Vault
Ohun elo náà gba **iwe-ẹri olupin** TLS lati Azure Key Vault lakoko ifilọlẹ. `X509Certificate2` ti a kojọpọ ni a fi sii taara sinu iṣeto HTTPS ti Kestrel, nitorinaa ko si faili PFX ti o nilo lati wa lori disiki.

### Eto Aabo Akoonu pẹlu Nonce fun Gbogbo Ibeere
Nigbati o ba ṣiṣẹ, gbogbo awọn idahun HTTP gbe akọle `Content-Security-Policy` ti itọsọna `script-src` rẹ pẹlu **nonce alaimọ ti cryptography** fun gbogbo ibeere. CSP tun ṣe atilẹyin awọn atokọ igbanilaaye ti o da lori hashing SHA-256 fun awọn iwe afọwọkọ ti inu.

### Awọn Akọle Aabo HTTP Boṣewa
`UseStandardSecurityHeaders` ṣafikun si gbogbo idahun: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, ati yiyọ awọn akọle `Server`, `X-Powered-By`, ati `X-AspNetMvc-Version`.

### Azure Blob Storage
Nigbati o ba ṣiṣẹ, `BlobSettingsService` pese iṣẹ Scoped ti o ni atilẹyin nipasẹ okun asopọ ati nọmba ti o pọju ti a le ṣeto ti awọn asomọ.

### Azure Cosmos DB
Nigbati o ba ṣiṣẹ, ohun elo náà ṣayẹwo asopọ Cosmos DB lakoko ifilọlẹ nipa pipe `database.ReadAsync()`.

### AWS Secrets Manager
Nigbati o ba ṣiṣẹ, `AwsSecretsManagerOperationsService` gba awọn aṣiri ati awọn iwe-ẹri lati AWS Secrets Manager. Iṣeto ni apakan `AwsSecretsManager` pẹlu: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, ati awọn iwe-ẹri `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Nigbati o ba ṣiṣẹ, `AwsDynamoDbService` ṣayẹwo asopọ tabili DynamoDB lakoko ifilọlẹ. Iṣeto ni apakan `AwsDynamoDb` pẹlu: `Region`, `TableName`, ati awọn iwe-ẹri `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Nigbati o ba ṣiṣẹ, `GcpSecretManagerOperationsService` gba awọn aṣiri lati Google Cloud Secret Manager. Iṣeto ni apakan `GcpSecretManager` pẹlu: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, ati `CredentialFilePath` (aṣayan, lo ADC ti o ba ṣofo).

### GCP Firestore
Nigbati o ba ṣiṣẹ, `GcpFirestoreService` kọ alabara Firestore lakoko ifilọlẹ. Iṣeto ni apakan `GcpFirestore` pẹlu: `ProjectId`, `DatabaseId` (aiyipada: "(default)"), `CollectionName`, ati `CredentialFilePath` (aṣayan).

### Iṣakoso Idanimọ AWS Cognito
Nigbati o ba ṣiṣẹ, `AddAwsCognitoAuthentication` ṣeto ẹrí OpenID Connect lodi si **Amazon Cognito User Pool** — deede AWS ti Microsoft Entra ID / Azure AD. Aaye wiwa OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Iṣeto ni apakan `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (tọju ni Awọn Aṣiri Olumulo), ati `Domain`.

### GCP Identity Platform
Nigbati o ba ṣiṣẹ, `AddGcpIdentityAuthentication` ṣeto ẹrí OpenID Connect nipa lilo **Google OAuth 2.0 / OIDC** — deede GCP ti Microsoft Entra ID / Azure AD. Aaye wiwa OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Iṣeto ni apakan `GcpIdentity`: `ClientId`, `ClientSecret` (tọju ni Awọn Aṣiri Olumulo), ati `ProjectId` aṣayan.

### Iṣakoso Igba Ailewu
Awọn igba lo igbohunsafefe iranti ti a pin ti o wa ninu ilana pẹlu **akoko ipari isinmi 30 iṣẹju**. Awọn kuki igba ti wa ni ṣeto pẹlu `HttpOnly`, `Secure = Always`, ati `SameSite = Strict`.

### Agbegbe
Ohun elo náà ṣe atilẹyin **25 awọn ede**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ati ga-IE. Larubawa pẹlu iyipada eto RTL adaṣe.

### Iwe-iṣẹ Ailewu PII
`LoggingHelper` ṣe hashing alaye ti a le ṣe idanimọ ni ẹni kọọkan ni abajade iwe-iṣẹ nipa lilo HMAC-SHA256. Bọtini iduroṣinṣin 32-byte le ṣe ipese nipasẹ `Logging:PiiHmacKey`.

---

## Awọn Asia Ẹya

Gbogbo awọn ọna kekere eto akọkọ ni a ṣakoso nipasẹ awọn asia Boolean ni `appsettings.json`.

| Asia | Aiyipada | Apejuwe |
|---|---|---|
| `EnableSession` | `true` | Igba ẹgbẹ olupin ati kuki igba |
| `EnableLocalization` | `true` | Atilẹyin ede pupọ (25 awọn ede) |
| `EnableAzureAd` | `true` | Ẹrí Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Awọn eto igbanilaaye ipele ọna |
| `EnableKeyVault` | `false` | Kojọ iwe-ẹri olupin TLS lati Azure Key Vault |
| `EnableNonceServices` | `false` | Ṣẹda nonce CSP fun gbogbo ibeere |
| `EnableCSP` | `false` | So akọle `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | So awọn akọle aabo HTTP boṣewa |
| `EnableBlobStorage` | `false` | Iṣẹ Azure Blob Storage |
| `EnableCosmosDb` | `false` | Iṣẹ Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (apẹẹrẹ) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Iṣakoso idanimọ AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (apẹẹrẹ) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Beere awọn iwe-ẹri TLS alabara |
| `EnableOcspValidation` | `false` | Ṣiṣayẹwo iparun iwe-ẹri OCSP (apẹẹrẹ) |

---

## Awọn Ibeere Ibẹrẹ

1. **Iforukọsilẹ App Azure AD** – pẹlu URI atunto, aṣiri alabara tabi iwe-ẹri.
2. **Azure Key Vault** – ti o ni iwe-ẹri olupin PFX bi aṣiri.
3. **Akọọlẹ Azure Cosmos DB** (aṣayan).
4. **Akọọlẹ Azure Blob Storage** (aṣayan).
5. **.NET 9 SDK / Runtime** – ẹya 9.0 tabi nigbamii.
6. **Awọn iwe-ẹri AWS** (olumulo IAM tabi ipa pẹlu awọn igbanilaaye `secretsmanager` ati `dynamodb`) – nilo nigbati `EnableAwsSecretsManager` tabi `EnableAwsDynamoDb` ṣiṣẹ.
7. **Akọọlẹ iṣẹ GCP tabi ADC** (pẹlu awọn igbanilaaye `secretmanager` ati `datastore`) – nilo nigbati `EnableGcpSecretManager` tabi `EnableGcpFirestore` ṣiṣẹ.

---

## Fifi Sii – Windows Azure (App Service)

### 1. Ṣẹda Awọn Orisun Azure

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Forukọ App Azure AD

Ni [Ẹnu-ọna Azure](https://portal.azure.com):
1. Lọ si **Microsoft Entra ID → Awọn forukọsilẹ app → Forukọsilẹ tuntun**.
2. Ṣeto URI atunto si `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Labẹ **Awọn iwe-ẹri ati awọn aṣiri**, ṣẹda aṣiri alabara ki o daakọ iye naa.
4. Ṣe akọsilẹ **ID Oludari** ati **ID Alabara** lati oju-ewe Akopọ.

### 3. Ṣẹda Azure Key Vault ki o Gbe Iwe-ẹri Olupin Soke

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Ṣeto Awọn Eto App

Daakọ `appsettings.template.json` si `appsettings.json` ki o kun awọn iye ipo-didena. Awọn aṣiri **ko gbọdọ** tọju ni iṣakoso orisun — ṣeto wọn bi Awọn Eto App ti App Service tabi nipasẹ Awọn Aṣiri Olumulo ni agbegbe:

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

### 5. Gbe App Naa Jade

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Mu HTTPS ati Orukọ Agbegbe Ṣiṣẹ (a ṣe iṣeduro)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Mu mTLS Ṣiṣẹ lori Azure App Service (aṣayan)

1. Lọ si **App Service → Awọn eto TLS/SSL → Awọn iwe-ẹri alabara**.
2. Ṣeto **Awọn iwe-ẹri alabara ti n bọ sinu** si **Nilo**.

Lẹhinna ṣeto `FeatureFlags__EnableMtls=true` ni Awọn Eto App.

---

## Fifi Sii – Olupin OpenBSD ti n Sọrọ pẹlu Awọn Iṣẹ Azure

> **Pataki:** .NET 9 **ko ni** itumọ Microsoft osise fun OpenBSD. Awọn ilana ni isalẹ lo **apoti ti o ni ibamu pẹlu Linux** (nipasẹ [Podman](https://podman.io/)) lati ṣiṣẹ ohun elo ASP.NET Core 9 lori OpenBSD lakoko sisọrọ pẹlu awọn iṣẹ Azure nipasẹ HTTPS.

### 1. Fifi Awọn Ibeere sii lori OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Fa Aworan ASP.NET Core 9 Runtime

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Kọ App Naa (lori ẹrọ kọ Linux tabi Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Ṣẹda Faili Iṣeto

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. Ṣe Apoti Naa

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

### 6. Ṣeto Odi Ina OpenBSD Packet Filter (pf)

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Asopọ Jade si Awọn Iṣẹ Azure

| Iṣẹ | Aaye |
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

## Itọkasi Iṣeto

Daakọ `appsettings.template.json` si `appsettings.json` ki o rọpo gbogbo awọn iye `{{PLACEHOLDER}}`.

| Abala | Bọtini | Apejuwe |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Iforukọsilẹ app Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault ati orukọ iwe-ẹri |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Eto iwe-ẹri alabara mTLS |
| `NonceEncryption` | `Key`, `IV` | Bọtini 32-byte ati IV 16-byte fun fifi nonce pamọ (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Asopọ Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Asopọ Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Ifọwọsi OCSP (apẹẹrẹ) |
| `Logging` | `PiiHmacKey` | Bọtini HMAC base64 32-byte fun hashing PII ni awọn iwe-iṣẹ |

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

---

## Awọn Iwe afọwọkọ Atilẹyin

| Iwe afọwọkọ | Idi |
|---|---|
| `IVandKeySampleGenerator.ps1` | Ṣẹda bọtini AES alailopin 32-byte ati IV 16-byte (base64) |
| `HashInlineScriptPowerShell.ps1` | Ṣe iṣiro awọn hashing SHA-256 fun awọn iwe afọwọkọ ti inu (fun atokọ igbanilaaye CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Bii loke, ṣe agbekalẹ awọn hashing ni ọna base64 |
| `CertificateUploaderToAzureExample.ps1` | Gbe iwe-ẹri PFX soke si Azure Key Vault |
| `CheckRoles.ps1` | Ṣayẹwo awọn iṣẹ RBAC Azure fun ohun elo naa |
| `ExportResourceGroups.ps1` | Gbe awọn iṣeto ẹgbẹ orisun Azure jade |
| `TroubleshootingCosmosDBInfo.ps1` | Ṣe ayẹwo asopọ Cosmos DB |
| `SetupFromTemplate.ps1` | Ṣe iṣeto akọkọ adaṣe lati `appsettings.template.json` |

---

## Awọn Akọsilẹ Aabo

- **Maṣe fipamọ awọn aṣiri** si iṣakoso orisun. Lo Awọn Aṣiri Olumulo .NET ni agbegbe ati Awọn Eto App Azure / itọkasi Key Vault ni iṣelọpọ.
- Imuse ifọwọsi OCSP jẹ **apẹẹrẹ** ti o kọ gbogbo awọn iwe-ẹri. Rọpo `PerformOcspValidationAsync` ṣaaju ki o to ṣiṣẹ `EnableOcspValidation` ni iṣelọpọ.
- Awọn iye nonce **ko tii tọpa rara** — titọpa nonce ni ọrọ ti o ṣii yoo jẹ ki ikọlu ti o ni iraye si awọn iwe-iṣẹ le fi awọn iwe afọwọkọ ti inu alaimọ sii.
- Akọle idahun `Server` ti ni boju bi `webserver` lati yago fun fifihan alaye pẹpẹ.
- AWS `AccessKeyId` ati `SecretAccessKey` **ko gbọdọ han rara** ni `appsettings.json` — lo Awọn Aṣiri Olumulo, awọn oniyipada ayika, tabi awọn ipa apẹẹrẹ IAM.
