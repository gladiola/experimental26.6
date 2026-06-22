# WebAppExperimental266

Yon aplikasyon wèb ASP.NET Core 9 Razor Pages ak otantifikasyon Azure AD, TLS mityèl (mTLS), jesyon sètifika Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, ak yon kouch sekirite HTTP ranfòse avèk yon Politik Sekirite Kontni ki baze sou nonce.

---

## Tablo Matyè

- [Fonksyonalite](#fonksyonalite)
- [Drapo Fonksyonalite](#drapo-fonksyonalite)
- [Prereki](#prereki)
- [Enstalasyon – Windows Azure (App Service)](#enstalasyon--windows-azure-app-service)
- [Enstalasyon – Sèvè OpenBSD ki kominike avèk Sèvis Azure](#enstalasyon--sèvè-openbsd-ki-kominike-avèk-sèvis-azure)
- [Referans Konfigirasyon](#referans-konfigirasyon)
- [Skript Sipò](#skript-sipò)
- [Nòt Sekirite](#nòt-sekirite)

---

## Fonksyonalite

### Otantifikasyon Azure AD (OpenID Connect)
Aplikasyon an otantifye itilizatè yo atravè **Platfòm Idantite Microsoft** lè li itilize pwotokòl OpenID Connect (via `Microsoft.Identity.Web`). Tout wout anba `/Experimental` mande yon idantite Azure AD ki otantifye. Paj `/Privacy`, `/Error`, ak `/About` yo aksesib piblikman.

### Otantifikasyon mTLS Sètifika Kliyan
Lè li aktive, kliyan yo dwe prezante yon sètifika X.509 valid. Paramèt nan `MtlsSettings` kontwole si sètifika chèn, sètifika siyen pou kont yo, oswa toulede yo otorize, verifikasyon revokasyon sètifika, ak emèt sètifika otorize yo.

### Entegrasyon Azure Key Vault
Aplikasyon an rekipere **sètifika sèvè** TLS la nan Azure Key Vault lè li lanse. `X509Certificate2` chaje a enjekte dirèkteman nan konfigirasyon HTTPS Kestrel la, kidonk pa gen fichye PFX ki bezwen egziste sou disk.

### Politik Sekirite Kontni avèk Nonce Pa Demann
Lè li aktive, chak repons HTTP pote yon antèt `Content-Security-Policy` ki gen direktiv `script-src` li ki gen yon **nonce kriptografik aleatwa** pa demann. CSP a sipòte tou lis otorizasyon baze sou hachaj SHA-256 pou skript entèn yo.

### Antèt Sekirite HTTP Estanda
`UseStandardSecurityHeaders` ajoute nan chak repons: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, ak sipresyon antèt `Server`, `X-Powered-By`, ak `X-AspNetMvc-Version`.

### Azure Blob Storage
Lè li aktive, `BlobSettingsService` bay yon sèvis Scoped sipòte pa yon chèn koneksyon ak yon kantite maksimòm atachman ki ka konfigire.

### Azure Cosmos DB
Lè li aktive, aplikasyon an verifye koneksyon Cosmos DB nan lanse lè li rele `database.ReadAsync()`.

### AWS Secrets Manager
Lè li aktive, `AwsSecretsManagerOperationsService` rekipere sekrè ak sètifika nan AWS Secrets Manager. Konfigirasyon nan seksyon `AwsSecretsManager` avèk paramèt: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, ak kalifikasyon `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Lè li aktive, `AwsDynamoDbService` verifye konektivite tab DynamoDB nan lanse. Konfigirasyon nan seksyon `AwsDynamoDb` avèk paramèt: `Region`, `TableName`, ak kalifikasyon `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Lè li aktive, `GcpSecretManagerOperationsService` rekipere sekrè nan Google Cloud Secret Manager. Konfigirasyon nan seksyon `GcpSecretManager` avèk paramèt: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, ak `CredentialFilePath` (opsyonèl, itilize ADC si vid).

### GCP Firestore
Lè li aktive, `GcpFirestoreService` konstri kliyan Firestore nan lanse. Konfigirasyon nan seksyon `GcpFirestore` avèk paramèt: `ProjectId`, `DatabaseId` (defo: "(default)"), `CollectionName`, ak `CredentialFilePath` (opsyonèl).

### Jesyon Idantite AWS Cognito
Lè li aktive, `AddAwsCognitoAuthentication` konfigire otantifikasyon OpenID Connect kont yon **Amazon Cognito User Pool** — ekivalan AWS Microsoft Entra ID / Azure AD. Pwen dekouvèt OIDC a:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Konfigirasyon nan seksyon `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (estoke nan Sekrè Itilizatè), ak `Domain`.

### Platfòm Idantite GCP
Lè li aktive, `AddGcpIdentityAuthentication` konfigire otantifikasyon OpenID Connect lè li itilize **Google OAuth 2.0 / OIDC** — ekivalan GCP Microsoft Entra ID / Azure AD. Pwen dekouvèt OIDC a:
`https://accounts.google.com/.well-known/openid-configuration`
Konfigirasyon nan seksyon `GcpIdentity`: `ClientId`, `ClientSecret` (estoke nan Sekrè Itilizatè), ak `ProjectId` opsyonèl.

### Jesyon Sesyon Sekirize
Sesyon yo itilize kachè memwa distribye nan pwosesis avèk yon **depo tan enaktivite 30 minit**. Bonbon sesyon yo konfigire avèk `HttpOnly`, `Secure = Always`, ak `SameSite = Strict`.

### Lokalizasyon
Aplikasyon an sipòte **25 lang**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ak ga-IE. Arab la gen yon baskil dispozisyon RTL otomatik.

### Koneksyon Sekirize PII
`LoggingHelper` hache enfòmasyon pèsonèlman idantifyab nan sòti jounal lè li itilize HMAC-SHA256. Yon kle estab 32-byte kapab founi via `Logging:PiiHmacKey`.

---

## Drapo Fonksyonalite

Tout sous-sistèm prensipal yo kontwole pa drapo boolean nan `appsettings.json`.

| Drapo | Defo | Deskripsyon |
|---|---|---|
| `EnableSession` | `true` | Sesyon sèvè ak bonbon sesyon |
| `EnableLocalization` | `true` | Sipò milti-lang (25 lang) |
| `EnableAzureAd` | `true` | Otantifikasyon Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Politik otorizasyon nivo wout |
| `EnableKeyVault` | `false` | Chaje sètifika TLS sèvè nan Azure Key Vault |
| `EnableNonceServices` | `false` | Jenere nonce CSP pa demann |
| `EnableCSP` | `false` | Ajoute antèt `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Ajoute antèt sekirite HTTP estanda |
| `EnableBlobStorage` | `false` | Sèvis Azure Blob Storage |
| `EnableCosmosDb` | `false` | Sèvis Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (estab) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Jesyon idantite AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (estab) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | Platfòm Idantite GCP (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Mande sètifika TLS kliyan |
| `EnableOcspValidation` | `false` | Verifikasyon revokasyon sètifika OCSP (estab) |

---

## Prereki

1. **Anrejistreman Aplikasyon Azure AD** – avèk yon URI redireksyon, sekrè kliyan oswa kalifikasyon sètifika.
2. **Azure Key Vault** – ki gen sètifika sèvè PFX kòm sekrè.
3. **Kont Azure Cosmos DB** (opsyonèl).
4. **Kont Azure Blob Storage** (opsyonèl).
5. **.NET 9 SDK / Runtime** – vèsyon 9.0 oswa pita.
6. **Kalifikasyon AWS** (itilizatè ou wòl IAM avèk pèmisyon `secretsmanager` ak `dynamodb`) – nesesè lè `EnableAwsSecretsManager` oswa `EnableAwsDynamoDb` yo aktive.
7. **Kont sèvis GCP oswa ADC** (avèk pèmisyon `secretmanager` ak `datastore`) – nesesè lè `EnableGcpSecretManager` oswa `EnableGcpFirestore` yo aktive.

---

## Enstalasyon – Windows Azure (App Service)

### 1. Kreye Resous Azure

```powershell
# Konekte
az login

# Kreye yon gwoup resous
az group create --name MyResourceGroup --location eastus

# Kreye yon plan App Service (Linux oswa Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Kreye aplikasyon wèb la (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Anrejistre yon Aplikasyon Azure AD

Nan [Pòtay Azure](https://portal.azure.com):
1. Ale nan **Microsoft Entra ID → Anrejistreman aplikasyon → Nouvo anrejistreman**.
2. Mete URI redireksyon an sou `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Anba **Sètifika ak sekrè**, kreye yon sekrè kliyan epi kopye valè a.
4. Note **ID Lokatè** ak **ID Kliyan** nan lam Apèsi a.

### 3. Kreye Azure Key Vault epi Telechaje Sètifika Sèvè a

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Konfigire Paramèt Aplikasyon

Kopye `appsettings.template.json` nan `appsettings.json` epi ranpli valè espas-plase yo. Sekrè yo **pa dwe** estoke nan kontwòl sous — mete yo kòm Paramèt Aplikasyon App Service oswa via Sekrè Itilizatè lokalman:

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

### 5. Deplwaye Aplikasyon an

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Aktive HTTPS ak Domèn Pèsonalize (rekòmande)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Aktive mTLS sou Azure App Service (opsyonèl)

1. Ale nan **App Service → Paramèt TLS/SSL → Sètifika kliyan**.
2. Mete **Sètifika kliyan ki antre** sou **Obligatwa**.

Epi mete `FeatureFlags__EnableMtls=true` nan Paramèt Aplikasyon.

---

## Enstalasyon – Sèvè OpenBSD ki kominike avèk Sèvis Azure

> **Enpòtan:** .NET 9 **pa** gen yon bati ofisyèl Microsoft pou OpenBSD. Enstriksyon ki anba yo itilize yon **konteyè konpatib Linux** (via [Podman](https://podman.io/)) pou kouri aplikasyon ASP.NET Core 9 sou OpenBSD pandan y ap kominike avèk sèvis Azure via HTTPS.

### 1. Enstale Prereki sou OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Tire Imaj Runtime ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Bati Aplikasyon an (sou yon machin bati Linux oswa Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Kreye yon Fichye Konfigirasyon

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

### 5. Kouri Konteyè a

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

### 6. Konfigire Firewall OpenBSD Packet Filter (pf)

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. DNS ak Sètifika TLS

Asire ou ke non òdinatè nan `AllowedHosts` rezoud nan IP piblik sèvè OpenBSD a.

### 8. Konektivite Sòti nan Sèvis Azure

| Sèvis | Pwen Fen |
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

## Referans Konfigirasyon

Kopye `appsettings.template.json` nan `appsettings.json` epi ranplase tout valè `{{PLACEHOLDER}}`.

| Seksyon | Kle | Deskripsyon |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Anrejistreman aplikasyon Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault ak non sètifika |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Politik sètifika kliyan mTLS |
| `NonceEncryption` | `Key`, `IV` | Kle 32-byte ak IV 16-byte pou chifre nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Koneksyon Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Koneksyon Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Validasyon OCSP (estab) |
| `Logging` | `PiiHmacKey` | Kle HMAC base64 32-byte pou hachaj PII nan jounal |

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

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

## Skript Sipò

| Skript | Bi |
|---|---|
| `IVandKeySampleGenerator.ps1` | Jenere yon kle AES aleatwa 32-byte ak IV 16-byte (base64) |
| `HashInlineScriptPowerShell.ps1` | Kalkile hachaj SHA-256 pou skript entèn (pou lis otorizasyon CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Menm jan an, pwodui hachaj nan fòma base64 |
| `CertificateUploaderToAzureExample.ps1` | Telechaje yon sètifika PFX nan Azure Key Vault |
| `CheckRoles.ps1` | Verifye atribisyon wòl RBAC Azure pou aplikasyon an |
| `ExportResourceGroups.ps1` | Eksporte konfigirasyon gwoup resous Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Dyagnostike konektivite Cosmos DB |
| `SetupFromTemplate.ps1` | Otomatize konfigirasyon inisyal nan `appsettings.template.json` |

---

## Nòt Sekirite

- **Pa janm valide sekrè yo** nan kontwòl sous. Itilize .NET User Secrets lokalman ak Paramèt Aplikasyon Azure / referans Key Vault nan pwodiksyon.
- Aplikasyon validasyon OCSP a se yon **estab** ki rejte tout sètifika. Ranplase `PerformOcspValidationAsync` anvan ou aktive `EnableOcspValidation` nan pwodiksyon.
- Valè nonce yo **pa janm jounal** — jounal yon nonce nan tèks klè ta pèmèt yon atakè ki gen aksè jounal pou enjekte skript entèn abitrè.
- Antèt repons `Server` la maske sou `webserver` pou evite revele enfòmasyon platfòm.
- `AccessKeyId` ak `SecretAccessKey` AWS yo **pa dwe parèt** nan `appsettings.json` — itilize Sekrè Itilizatè, varyab anviwònman, oswa wòl enstans IAM.
- Kalifikasyon GCP yo ta dwe itilize **Kalifikasyon Defo Aplikasyon (ADC)** olye pou valide fichye JSON kont sèvis.
