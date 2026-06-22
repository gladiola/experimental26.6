# WebAppExperimental266

O se faʻaoga web ASP.NET Core 9 Razor Pages e iai ai le faʻamaonia Azure AD, TLS faalua (mTLS), puleaina o tusi faamaonia Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, ma se vaega saogalemu HTTP malosigatasi faʻatasi ai ma le Faʻavae Saogalemu Anotusia e faʻaogaina ai nonce.

---

## Lisi Anotusia

- [Mea Faapitoa](#mea-faapitoa)
- [Faʻailoga Mea Faapitoa](#faailoga-mea-faapitoa)
- [Manaʻoga Muamua](#manaoga-muamua)
- [Faʻatuina – Windows Azure (App Service)](#faʻatuina--windows-azure-app-service)
- [Faʻatuina – Sava OpenBSD e fesoʻotaʻi ma Auaunaga Azure](#faʻatuina--sava-openbsd-e-fesoʻotaʻi-ma-auaunaga-azure)
- [Faʻamatalaga Faʻaogaina](#faʻamatalaga-faʻaogaina)
- [Tusitusiga Lagolago](#tusitusiga-lagolago)
- [Maʻopoopo Saogalemu](#maʻopoopo-saogalemu)

---

## Mea Faapitoa

### Faʻamaonia Azure AD (OpenID Connect)
E faʻamaonia e le faʻaoga tagata faʻaogaina e ala i le **Faalapotopotoga Faʻasinomaga Microsoft** e faʻaogaina ai le faʻasologa OpenID Connect (e ala i `Microsoft.Identity.Web`). O ala uma i lalo o `/Experimental` e manaʻomia ai se faʻasinomaga Azure AD ua faʻamaoniaina. O itulau `/Privacy`, `/Error`, ma `/About` e mafai ona aʻoaʻoina e tagata lautele.

### Faʻamaonia Tusi Faamaonia Tagata Oʻo mTLS
Pe a faʻamalosi, e tatau ona tuʻuina atu e tagata oʻo se tusi faamaonia X.509 aoga. O faʻatonuga i `MtlsSettings` e puleaina ai pe ua faatagaina tusi faamaonia faasologa, faʻamaonia e le tagata lava ia, poʻo mea uma e lua, siaki toe faamalosi tusi faamaonia, ma faʻamaona tusi faamaonia faatagaina.

### Tuʻufaʻatasia Azure Key Vault
E maua e le faʻaoga le **tusi faamaonia sava** TLS mai Azure Key Vault i le amataga. O le `X509Certificate2` ua utaina e tuʻu saʻo i le faʻaogaina o Kestrel HTTPS, o lea la e le manaʻomia ai se faila PFX i le disk.

### Faʻavae Saogalemu Anotusia ma Nonce mo Talosaga Taʻitasi
Pe a faʻamalosi, o tali HTTP uma e aumai ai se ulutala `Content-Security-Policy` o lona faʻatonuga `script-src` o loo i ai se **nonce faitau faʻafuaseʻi o le mea faʻapitoa** mo talosaga taʻitasi. E lagolagoina foi e le CSP lisi faatagaina faʻavae i le hashing SHA-256 mo tusitusiga totonu.

### Ulutala Saogalemu HTTP Masani
E faʻaopoopo e `UseStandardSecurityHeaders` i taʻitasi taʻaloga: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, ma le aveese o ulutala `Server`, `X-Powered-By`, ma `X-AspNetMvc-Version`.

### Azure Blob Storage
Pe a faʻamalosi, e tuʻuina atu e `BlobSettingsService` se auaunaga Scoped e lagolagoina i se filo fesootaʻi ma le aotelega faʻapipiʻi faʻatulagaina.

### Azure Cosmos DB
Pe a faʻamalosi, e faʻamaonia e le faʻaoga le fesootaiga Cosmos DB i le amataga e ala i le valaʻauina o `database.ReadAsync()`.

### AWS Secrets Manager
Pe a faʻamalosi, e maua e `AwsSecretsManagerOperationsService` mea faalilolilo ma tusi faamaonia mai AWS Secrets Manager. Faʻatulagaina i le vaega `AwsSecretsManager` ma: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, ma faʻamaonia `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Pe a faʻamalosi, e faʻamaonia e `AwsDynamoDbService` le fesoʻotaʻiga o le laulau DynamoDB i le amataga. Faʻatulagaina i le vaega `AwsDynamoDb` ma: `Region`, `TableName`, ma faʻamaonia `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Pe a faʻamalosi, e maua e `GcpSecretManagerOperationsService` mea faalilolilo mai Google Cloud Secret Manager. Faʻatulagaina i le vaega `GcpSecretManager` ma: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, ma `CredentialFilePath` (filifiliga, e faʻaogaina le ADC pe tumu).

### GCP Firestore
Pe a faʻamalosi, e fausia e `GcpFirestoreService` le tagata oʻo Firestore i le amataga. Faʻatulagaina i le vaega `GcpFirestore` ma: `ProjectId`, `DatabaseId` (aʻo: "(default)"), `CollectionName`, ma `CredentialFilePath` (filifiliga).

### Puleaina o Faʻasinomaga AWS Cognito
Pe a faʻamalosi, e faʻatulagaina e `AddAwsCognitoAuthentication` le faʻamaonia OpenID Connect faasaga i se **Amazon Cognito User Pool** — le tutusa AWS ma Microsoft Entra ID / Azure AD. Le pito o le suʻesuʻega OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Faʻatulagaina i le vaega `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (teuina i Mea Faalilolilo Tagata Faʻaogaina), ma `Domain`.

### GCP Identity Platform
Pe a faʻamalosi, e faʻatulagaina e `AddGcpIdentityAuthentication` le faʻamaonia OpenID Connect e faʻaogaina ai **Google OAuth 2.0 / OIDC** — le tutusa GCP ma Microsoft Entra ID / Azure AD. Le pito o le suʻesuʻega OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Faʻatulagaina i le vaega `GcpIdentity`: `ClientId`, `ClientSecret` (teuina i Mea Faalilolilo Tagata Faʻaogaina), ma `ProjectId` filifiliga.

### Puleaina Aofiaga Saogalemu
E faʻaaogaina e aofiaga se faʻaogaina meaola faʻasoa i le faiga ma se **taimi malolo o le 30 minute**. E faʻatulagaina kuki aofiaga ma `HttpOnly`, `Secure = Always`, ma `SameSite = Strict`.

### Faʻaliliu ʻOlelo
E lagolagoina e le faʻaoga **gagana e 25**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ma ga-IE. O le gagana Arapi o loʻo i ai le suiga faʻainisinia RTL.

### Faʻamaumauga Saogalemu PII
E hasi e `LoggingHelper` faʻamatalaga faʻasinomaga patino i faʻamaumauga o faʻasologa e faʻaogaina ai HMAC-SHA256. E mafai ona tuʻuina atu se ki mautu 32-byte e ala i `Logging:PiiHmacKey`.

---

## Faʻailoga Mea Faapitoa

E puleaina mea faʻapipiʻi tetele uma e faʻailoga boolean i `appsettings.json`.

| Faʻailoga | Aʻo | Faamatalaga |
|---|---|---|
| `EnableSession` | `true` | Aofiaga faasava ma kuki aofiaga |
| `EnableLocalization` | `true` | Lagolago gagana faateletelega (gagana e 25) |
| `EnableAzureAd` | `true` | Faʻamaonia Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Faʻavae faʻatagaina i le tulaga ala |
| `EnableKeyVault` | `false` | Utaina tusi faamaonia sava TLS mai Azure Key Vault |
| `EnableNonceServices` | `false` | Gaosia nonce CSP mo talosaga taʻitasi |
| `EnableCSP` | `false` | Faʻaopoopo ulutala `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Faʻaopoopo ulutala saogalemu HTTP masani |
| `EnableBlobStorage` | `false` | Auaunaga Azure Blob Storage |
| `EnableCosmosDb` | `false` | Auaunaga Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (faʻataʻitaʻiga) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Puleaina faʻasinomaga AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (faʻataʻitaʻiga) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Manaʻomia tusi faamaonia TLS tagata oʻo |
| `EnableOcspValidation` | `false` | Siaki faatoaga tusi faamaonia OCSP (faʻataʻitaʻiga) |

---

## Manaʻoga Muamua

1. **Resitaraina o Azure AD** – ma se URI faʻafoʻi, mea faalilolilo tagata oʻo poʻo faʻamaona tusi faamaonia.
2. **Azure Key Vault** – e iai ai le tusi faamaonia sava PFX e fai ma mea faalilolilo.
3. **Aʻoaiga Azure Cosmos DB** (filifiliga).
4. **Aʻoaiga Azure Blob Storage** (filifiliga).
5. **.NET 9 SDK / Runtime** – version 9.0 poʻo atili.
6. **Faʻamaona AWS** (tagata faʻaogaina poʻo matafaioi IAM ma faʻatagaina `secretsmanager` ma `dynamodb`) – manaʻomia pe a faʻamalosi `EnableAwsSecretsManager` poʻo `EnableAwsDynamoDb`.
7. **Aʻoaiga faʻatonuga GCP poʻo ADC** (ma faʻatagaina `secretmanager` ma `datastore`) – manaʻomia pe a faʻamalosi `EnableGcpSecretManager` poʻo `EnableGcpFirestore`.

---

## Faʻatuina – Windows Azure (App Service)

### 1. Fausia Puna Faafaigaluega Azure

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Resitara se Faʻaogaina Azure AD

I le [Faitotoa Azure](https://portal.azure.com):
1. Alu i **Microsoft Entra ID → Resitaraina faʻaogaina → Resitaraina fou**.
2. Seti le URI faʻafoʻi i `https://<your-app>.azurewebsites.net/signin-oidc`.
3. I lalo o **Tusi faamaonia ma mea faalilolilo**, fausia se mea faalilolilo tagata oʻo ma kopi le aoga.
4. Manatua le **ID Piʻo** ma le **ID Tagata Oʻo** mai le fola Iloiloga.

### 3. Fausia Azure Key Vault ma Faʻapipiʻi le Tusi Faamaonia Sava

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Faʻatulagaina Faʻatulagaina Faʻaogaina

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

### 5. Faʻatupuina le Faʻaogaina

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Faʻaola HTTPS ma Igoa Tino (fautuaina)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Faʻaola mTLS i Azure App Service (filifiliga)

1. Alu i **App Service → Faʻatulagaina TLS/SSL → Tusi faamaonia tagata oʻo**.
2. Seti **Tusi faamaonia tagata oʻo oʻo mai** i **Manaʻomia**.

Ona seti lea `FeatureFlags__EnableMtls=true` i Faʻatulagaina Faʻaogaina.

---

## Faʻatuina – Sava OpenBSD e fesoʻotaʻi ma Auaunaga Azure

> **Taua:** E **leai** se fausia aloaia Microsoft .NET 9 mo OpenBSD. O faʻatonuga o loʻo i lalo e faʻaogaina ai se **faʻafefe faʻafaigaluega Linux** (e ala i [Podman](https://podman.io/)) e tamoe ai le faʻaogaina ASP.NET Core 9 i luga o OpenBSD.

### 1. Faʻapipiʻi Manaʻoga Muamua i OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Toso le Ata ASP.NET Core 9 Runtime

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Fausia le Faʻaogaina (i luga o masini fausia Linux poʻo Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Fausia se Faila Faʻatulagaina

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. Tamoe le Faʻafefe

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

### 6. Faʻatulagaina le Puipui Afi OpenBSD Packet Filter (pf)

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Auaunaga Azure e manaʻomia Fesootaʻiga Fafo

| Auaunaga | Pito |
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

## Faʻamatalaga Faʻaogaina

| Vaega | Ki | Faamatalaga |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Resitaraina faʻaogaina Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault ma igoa tusi faamaonia |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Faʻavae tusi faamaonia tagata oʻo mTLS |
| `NonceEncryption` | `Key`, `IV` | Ki 32-byte ma IV 16-byte mo le faʻalilolilo nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Fesootaʻiga Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Fesootaʻiga Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Faʻamaonia OCSP (faʻataʻitaʻiga) |
| `Logging` | `PiiHmacKey` | Ki HMAC base64 32-byte mo le hashing PII i faʻamaumauga |

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

---

## Tusitusiga Lagolago

| Tusitusiga | Faʻamoemoe |
|---|---|
| `IVandKeySampleGenerator.ps1` | Gaosia se ki AES faʻafuaseʻi 32-byte ma IV 16-byte (base64) |
| `HashInlineScriptPowerShell.ps1` | Faitau hashing SHA-256 mo tusitusiga totonu |
| `HashInlineScriptPowerShellBase64Output.ps1` | E pei lava o luga, e gaosia hashing i faasologa base64 |
| `CertificateUploaderToAzureExample.ps1` | Faʻapipiʻi tusi faamaonia PFX i Azure Key Vault |
| `CheckRoles.ps1` | Siaki tofitofi matafaioi RBAC Azure mo le faʻaogaina |
| `ExportResourceGroups.ps1` | Auina faʻatulagaina puna faafaigaluega Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Suʻesuʻe fesootaʻiga Cosmos DB |
| `SetupFromTemplate.ps1` | Faʻaotometi faʻatulagaina amata mai `appsettings.template.json` |

---

## Maʻopoopo Saogalemu

- **Aua lava e faʻamaonia mea faalilolilo** i le puleaina o puna. Faʻaogaina .NET User Secrets i le lotoifale ma Azure App Settings / faʻamatalaga Key Vault i le gaosiga.
- O le faʻamalosiina o le OCSP o se **faʻataʻitaʻiga** e talitonuina ai tusi faamaonia uma. Suia `PerformOcspValidationAsync` aʻo tuliina `EnableOcspValidation` i le gaosiga.
- E **lē faʻamauina** aoga nonce — o le faʻamauina o se nonce i le tusitala masani e mafai ai e se osofaʻi e maua aʻoaiga faʻamaumauga ona tuʻi i totonu o tusitusiga totonu feʻumaʻi.
- O le ulutala tali `Server` e faʻamalieina i `webserver` e aloese ai i le faʻaaluina o faʻamatalaga faalapotopotoga.
- O `AccessKeyId` ma `SecretAccessKey` AWS e **lē tatau** ona faʻaalia i `appsettings.json` — faʻaogaina Mea Faalilolilo Tagata Faʻaogaina, suiga siosiomaga, poʻo matafaioi IAM.
