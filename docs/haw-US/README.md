# WebAppExperimental266

He polokalamu pūnaewele ASP.NET Core 9 Razor Pages me ka hōʻoia Azure AD, TLS hoʻohui (mTLS), hoʻokele palapala hōʻoia Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, a me kahi papa pale HTTP ikaika me kahi kulekele palekana ʻike e pili ana i ka nonce.

---

## Papa Kuhikuhi

- [Nā Hiʻohiʻona](#nā-hiohiohiʻona)
- [Nā Hae Hiʻohiʻona](#nā-hae-hiohiohiʻona)
- [Nā Koi Mua](#nā-koi-mua)
- [Hoʻokomo – Windows Azure (App Service)](#hoʻokomo--windows-azure-app-service)
- [Hoʻokomo – Kikowaena OpenBSD e kamaʻilio ana me Nā Lawelawe Azure](#hoʻokomo--kikowaena-openbsd-e-kamaʻilio-ana-me-nā-lawelawe-azure)
- [Kuhikuhi Hoʻonohonoho](#kuhikuhi-hoonohonoho)
- [Nā Palapala Kōkua](#nā-palapala-kōkua)
- [Nā Manaʻo Palekana](#nā-manaʻo-palekana)

---

## Nā Hiʻohiʻona

### Hōʻoia Azure AD (OpenID Connect)
Hoʻohana ka polokalamu i ka **Pae Hōʻike Microsoft** ma o ke kaʻina hana OpenID Connect (ma `Microsoft.Identity.Web`) e hōʻoia ai i nā mea hoʻohana. Pono nā ala āpau ma lalo o `/Experimental` i kahi hōʻike Azure AD i hōʻoiaʻiʻo ʻia. Hiki ke komo ʻia nā ʻaoʻao `/Privacy`, `/Error`, a me `/About` e ka lehulehu.

### Hōʻoia Palapala Hōʻoia Mea Komo mTLS
I ka wehe ʻia, pono nā mea komo e waiho i kahi palapala hōʻoia X.509 kūpono. Nā koina o `MtlsSettings` e hoʻomalu ai i ka ʻae ʻana i nā palapala hōʻoia pūʻulu, i ka hoʻopaʻa lima iho ʻia, a i ʻole ʻelua, nā hōʻoia hōʻala palapala hōʻoia, a me nā mea hoʻopuka palapala hōʻoia i ʻae ʻia.

### Hoʻohui Azure Key Vault
Loaʻa i ka polokalamu ka **palapala hōʻoia kikowaena** TLS mai Azure Key Vault i ka wehe ʻana. Hoʻokomo pololei ʻia ka `X509Certificate2` i ukali ʻia i ka hoʻonohonoho HTTPS Kestrel, no laila ʻaʻole pono kahi waihona PFX ma ka paepae.

### Kulekele Palekana ʻIke me Nonce no Kēlā me Kēia Noi
I ka wehe ʻia, lawe kēlā me kēia pane HTTP i kahi poʻo `Content-Security-Policy` nona ke kuhikuhi `script-src` e komo ana i kahi **nonce palekana o ka makemakika** no kēlā me kēia noi. Kākoʻo pū ka CSP i nā papa ʻae e pili ana i ka helu SHA-256 no nā palapala hana komo loko.

### Nā Poʻo Palekana HTTP Maʻamau
Hoʻohui ka `UseStandardSecurityHeaders` i kēlā me kēia pane: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, a me ka wehe ʻana i nā poʻo `Server`, `X-Powered-By`, a me `X-AspNetMvc-Version`.

### Azure Blob Storage
I ka wehe ʻia, hāʻawi ka `BlobSettingsService` i kahi lawelawe Scoped i kākoʻo ʻia e kahi kaʻina hoʻopili a me ka helu paʻa paʻa i hoʻonohonoho ʻia.

### Azure Cosmos DB
I ka wehe ʻia, hōʻoia ka polokalamu i ka hoʻopili Cosmos DB i ka wehe ʻana ma ka hāpai ʻana i `database.ReadAsync()`.

### AWS Secrets Manager
I ka wehe ʻia, lawe ka `AwsSecretsManagerOperationsService` i nā mea hūnā a me nā palapala hōʻoia mai AWS Secrets Manager. Hoʻonohonoho ma ka bahana `AwsSecretsManager` me nā koina: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, a me nā hōʻike `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
I ka wehe ʻia, hōʻoia ka `AwsDynamoDbService` i ka hoʻopili o ka pākaukau DynamoDB i ka wehe ʻana. Hoʻonohonoho ma ka bahana `AwsDynamoDb` me: `Region`, `TableName`, a me nā hōʻike `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
I ka wehe ʻia, lawe ka `GcpSecretManagerOperationsService` i nā mea hūnā mai Google Cloud Secret Manager. Hoʻonohonoho ma ka bahana `GcpSecretManager` me: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, a me `CredentialFilePath` (koho, hoʻohana ADC ke poʻo wale).

### GCP Firestore
I ka wehe ʻia, kūkulu ka `GcpFirestoreService` i ka mea komo Firestore i ka wehe ʻana. Hoʻonohonoho ma ka bahana `GcpFirestore` me: `ProjectId`, `DatabaseId` (kūlou: "(default)"), `CollectionName`, a me `CredentialFilePath` (koho).

### Hoʻokele Hōʻike AWS Cognito
I ka wehe ʻia, hoʻonohonoho ka `AddAwsCognitoAuthentication` i ka hōʻoia OpenID Connect kūlana i kahi **Amazon Cognito User Pool** — ka mea like o AWS no Microsoft Entra ID / Azure AD. Ka wahi ʻimi OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Hoʻonohonoho ma ka bahana `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (mālama ma Nā Mea Hūnā Mea Hoʻohana), a me `Domain`.

### Pae Hōʻike GCP
I ka wehe ʻia, hoʻonohonoho ka `AddGcpIdentityAuthentication` i ka hōʻoia OpenID Connect me **Google OAuth 2.0 / OIDC** — ka mea like o GCP no Microsoft Entra ID / Azure AD. Ka wahi ʻimi OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Hoʻonohonoho ma ka bahana `GcpIdentity`: `ClientId`, `ClientSecret` (mālama ma Nā Mea Hūnā Mea Hoʻohana), a me `ProjectId` koho.

### Hoʻokele Papa Noho Palekana
Hoʻohana nā papa noho i ka hoʻomau meakanu i loko o ka hana me kahi **manawa hōmoe 30 minuke**. Hoʻonohonoho ʻia nā kuke papa noho me `HttpOnly`, `Secure = Always`, a me `SameSite = Strict`.

### Hoʻolako ʻŌlelo
Kākoʻo ka polokalamu i **25 mau ʻōlelo**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, a me ga-IE. Komo pū ka ʻAlapia i ka hoʻololi māmā RTL aunoa.

### Hoʻopaʻa Maʻi Palekana PII
Pelu ka `LoggingHelper` i nā ʻike hōʻike pilikino ma ka wāwae lolouila e hoʻohana ana i HMAC-SHA256. Hiki ke hāʻawi ʻia kahi kī paʻa 32-byte ma `Logging:PiiHmacKey`.

---

## Nā Hae Hiʻohiʻona

Hoʻomalu ʻia nā ʻōnaehana nui āpau e nā hae boolean ma `appsettings.json`.

| Hae | Kūlou | Wehewehe |
|---|---|---|
| `EnableSession` | `true` | Papa noho kikowaena a me ka kuke papa noho |
| `EnableLocalization` | `true` | Kākoʻo ʻōlelo lehulehu (25 mau ʻōlelo) |
| `EnableAzureAd` | `true` | Hōʻoia Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Nā kulekele ʻae ma ka pae ala |
| `EnableKeyVault` | `false` | Hoʻouka palapala hōʻoia kikowaena TLS mai Azure Key Vault |
| `EnableNonceServices` | `false` | Hana nonce CSP no kēlā me kēia noi |
| `EnableCSP` | `false` | Pili poʻo `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Pili nā poʻo palekana HTTP maʻamau |
| `EnableBlobStorage` | `false` | Lawelawe Azure Blob Storage |
| `EnableCosmosDb` | `false` | Lawelawe Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (kumu) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Hoʻokele hōʻike AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (kumu) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | Pae Hōʻike GCP (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Koi nā palapala hōʻoia TLS mea komo |
| `EnableOcspValidation` | `false` | Hōʻoia hōʻala palapala hōʻoia OCSP (kumu) |

---

## Nā Koi Mua

1. **Hoʻopaʻa ʻIa ʻo Azure AD** – me kahi URI hoʻihoʻi, mea hūnā mea komo, a i ʻole hōʻike palapala hōʻoia.
2. **Azure Key Vault** – i loko o ka palapala hōʻoia kikowaena PFX ma ke ʻano he mea hūnā.
3. **Moʻolelo Azure Cosmos DB** (koho).
4. **Moʻolelo Azure Blob Storage** (koho).
5. **.NET 9 SDK / Runtime** – ka mana 9.0 a i ʻole ma hope aku.
6. **Nā Hōʻike AWS** (mea hoʻohana a i ʻole kuleana IAM me nā ʻae `secretsmanager` a me `dynamodb`) – pono i ka wehe ʻana o `EnableAwsSecretsManager` a i ʻole `EnableAwsDynamoDb`.
7. **Moʻolelo lawelawe GCP a i ʻole ADC** (me nā ʻae `secretmanager` a me `datastore`) – pono i ka wehe ʻana o `EnableGcpSecretManager` a i ʻole `EnableGcpFirestore`.

---

## Hoʻokomo – Windows Azure (App Service)

### 1. Hana i Nā Kumuhana Azure

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Hoʻopaʻa ʻIa ʻo Noi Azure AD

Ma ka [Puka Helu Azure](https://portal.azure.com):
1. E hele i **Microsoft Entra ID → Nā hoʻopaʻa noi → Hoʻopaʻa hou**.
2. Hoʻonoho i ka URI hoʻihoʻi i `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Ma lalo o **Nā palapala hōʻoia a me nā mea hūnā**, hana i kahi mea hūnā mea komo a kope i ka waiwai.
4. E hoʻomaopopo i ka **ID Hoʻokipa** a me ka **ID Mea Komo** mai ka ʻaoʻao Nānā.

### 3. Hana i Azure Key Vault a Hoʻouka i ka Palapala Hōʻoia Kikowaena

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Hoʻonohonoho i Nā Koina Noi

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

### 5. Hoʻolele i ka Noi

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Hana i HTTPS a me ka Inoa Kikowaena Maʻamau (i manaʻo ʻia)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Hana i mTLS ma Azure App Service (koho)

1. E hele i **App Service → Nā koina TLS/SSL → Nā palapala hōʻoia mea komo**.
2. Hoʻonoho i **Nā palapala hōʻoia mea komo e komo mai ana** i **Pono**.

```sh
FeatureFlags__EnableMtls=true
```

---

## Hoʻokomo – Kikowaena OpenBSD e kamaʻilio ana me Nā Lawelawe Azure

> **Koʻikoʻi:** ʻAʻohe .NET 9 i kahi kūkulu Microsoft kūpono no OpenBSD. Hoʻohana nā ʻōlelo aʻo ma lalo nei i kahi **kaʻanā ʻaoʻao Linux** (ma [Podman](https://podman.io/)) e hoʻoholo ai i ka noi ASP.NET Core 9 ma OpenBSD.

### 1. Hoʻokomo i Nā Koi Mua ma OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Huki i ka Kiʻi Runtime ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Kūkulu i ka Noi (ma kahi mīkini kūkulu Linux a i ʻole Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Hana i Waihona Hoʻonohonoho

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. Hoʻoholo i ka Kaʻanā

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

### 6. Hoʻonohonoho i ka Pā Ahi OpenBSD Packet Filter (pf)

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Nā Lawelawe Azure e Komo Aku Ana

| Lawelawe | Wahi Hopena |
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

## Kuhikuhi Hoʻonohonoho

| Bahana | Kī | Wehewehe |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Hoʻopaʻa noi Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault a me ka inoa palapala hōʻoia |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Kulekele palapala hōʻoia mea komo mTLS |
| `NonceEncryption` | `Key`, `IV` | Kī 32-byte a me IV 16-byte no ka hoʻopaʻi nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Hoʻopili Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Hoʻopili Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Hōʻoia OCSP (kumu) |
| `Logging` | `PiiHmacKey` | Kī HMAC base64 32-byte no ka helu PII ma nā waihona |

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

---

## Nā Palapala Kōkua

| Palapala | Koʻikoʻi |
|---|---|
| `IVandKeySampleGenerator.ps1` | Hana i kahi kī AES pōkole 32-byte a me IV 16-byte (base64) |
| `HashInlineScriptPowerShell.ps1` | Helu nā helu SHA-256 no nā palapala hana komo loko |
| `HashInlineScriptPowerShellBase64Output.ps1` | Like me luna, hoʻopuka nā helu ma ka hōʻike base64 |
| `CertificateUploaderToAzureExample.ps1` | Hoʻouka i ka palapala hōʻoia PFX i Azure Key Vault |
| `CheckRoles.ps1` | Nānā i nā hāʻawi kuleana RBAC Azure no ka noi |
| `ExportResourceGroups.ps1` | Lawe aku i nā hoʻonohonoho pūʻulu kumuhana Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Māhele i ka hoʻopili Cosmos DB |
| `SetupFromTemplate.ps1` | Hoʻomaka aunoa i ka hoʻonohonoho mai `appsettings.template.json` |

---

## Nā Manaʻo Palekana

- **Mai hoʻopaʻa i nā mea hūnā** i ka mālama koina. Hoʻohana i .NET User Secrets ma ke kūloko a me Nā Koina Noi Azure / nā kuhikuhi Key Vault i ka hana.
- He **kumu** ka hoʻokō hōʻoia OCSP e hōʻole ai i nā palapala hōʻoia āpau. Pani i `PerformOcspValidationAsync` ma mua o ka wehe ʻana i `EnableOcspValidation` i ka hana.
- **ʻAʻole i hoʻopaʻa ʻia** nā waiwai nonce — e ʻae ana i kahi mea hoʻouka i loko o nā ʻōlelo hana i hoʻopaʻa ʻia ka nonce i ke kino pono i kahi mea hoʻouka me ka kōkua i loko o nā waihona.
- Pākuʻi ʻia ka poʻo pane `Server` i `webserver` e pale ai i ka hōʻike ʻana i ka ʻike pae.
- **Mai hoʻopili wale** i `AccessKeyId` a me `SecretAccessKey` AWS i `appsettings.json` — hoʻohana i Nā Mea Hūnā Mea Hoʻohana, nā ʻano kaiapuni, a i ʻole nā kuleana kumu IAM.
