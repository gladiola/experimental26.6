# WebAppExperimental266

An ASP.NET Core 9 Razor Pages web application with Azure AD authentication, mutual TLS (mTLS), Azure Key Vault certificate management, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, Google Cloud Secret Manager, Google Cloud Firestore, and a hardened HTTP security layer with nonce-based Content Security Policy.

---

## 🌐 Language Index

This documentation is available in the following languages. Each language folder contains a README, Security Reviews, and translated guides.

| Language | Folder |
|---|---|
| English (en-US) | [docs/en-US/](docs/en-US/) |
| Afrikaans (af-ZA) | [docs/af-ZA/](docs/af-ZA/) |
| Amharic (am-ET) | [docs/am-ET/](docs/am-ET/) |
| Arabic (ar-SA) | [docs/ar-SA/](docs/ar-SA/) |
| Bengali (bn-BD) | [docs/bn-BD/](docs/bn-BD/) |
| German (de-DE) | [docs/de-DE/](docs/de-DE/) |
| Spanish (es-ES) | [docs/es-ES/](docs/es-ES/) |
| French (fr-FR) | [docs/fr-FR/](docs/fr-FR/) |
| Irish (ga-IE) | [docs/ga-IE/](docs/ga-IE/) |
| Hausa (ha-NG) | [docs/ha-NG/](docs/ha-NG/) |
| Hawaiian (haw-US) | [docs/haw-US/](docs/haw-US/) |
| Hindi (hi-IN) | [docs/hi-IN/](docs/hi-IN/) |
| Haitian Creole (ht-HT) | [docs/ht-HT/](docs/ht-HT/) |
| Italian (it-IT) | [docs/it-IT/](docs/it-IT/) |
| Japanese (ja-JP) | [docs/ja-JP/](docs/ja-JP/) |
| Korean (ko-KR) | [docs/ko-KR/](docs/ko-KR/) |
| Māori (mi-NZ) | [docs/mi-NZ/](docs/mi-NZ/) |
| Dutch (nl-NL) | [docs/nl-NL/](docs/nl-NL/) |
| Portuguese (pt-PT) | [docs/pt-PT/](docs/pt-PT/) |
| Russian (ru-RU) | [docs/ru-RU/](docs/ru-RU/) |
| Samoan (sm-WS) | [docs/sm-WS/](docs/sm-WS/) |
| Swahili (sw-KE) | [docs/sw-KE/](docs/sw-KE/) |
| Yoruba (yo-NG) | [docs/yo-NG/](docs/yo-NG/) |
| Chinese Simplified (zh-CN) | [docs/zh-CN/](docs/zh-CN/) |
| Chinese Traditional (zh-HK) | [docs/zh-HK/](docs/zh-HK/) |

---

## Table of Contents

- [Features](#features)
- [Feature Flags](#feature-flags)
- [Prerequisites](#prerequisites)
- [Installation – Windows Azure (App Service)](#installation--windows-azure-app-service)
- [Installation – OpenBSD Server communicating with Azure Services](#installation--openbsd-server-communicating-with-azure-services)
- [Configuration Reference](#configuration-reference)
- [Supporting Scripts](#supporting-scripts)
- [Security Notes](#security-notes)

---

## Features

### Azure AD Authentication (OpenID Connect)
The application authenticates users through **Microsoft Identity Platform** using the OpenID Connect protocol (via `Microsoft.Identity.Web`). All routes under `/Experimental` require an authenticated Azure AD identity. The `/Privacy`, `/Error`, and `/About` pages are publicly accessible. The `[Authorize]` attribute on `HomeController` enforces authentication across all MVC actions.

### Mutual TLS (mTLS) Client Certificate Authentication
When enabled, the application requires connecting clients to present a valid X.509 certificate. Settings in `MtlsSettings` control:
- Whether to allow chained certificates, self-signed certificates, or both
- Certificate revocation checking (X.509 CRL / online mode)
- Allowed certificate issuers (checked as a case-insensitive substring match against the certificate's `Issuer` DN)

The Kestrel web server is configured with `ClientCertificateMode.RequireCertificate` when mTLS is on, and `ClientCertificateMode.AllowCertificate` in development or when mTLS is off.

### Azure Key Vault Integration
The application retrieves the TLS **server certificate** from Azure Key Vault at startup. The Key Vault client uses Azure AD client credentials (client ID + client secret) from configuration. The loaded `X509Certificate2` is injected directly into Kestrel's HTTPS defaults so no PFX file needs to exist on disk.

### OCSP Certificate Revocation Validation
An `OcspValidationService` stub is included for validating client certificates against an OCSP (Online Certificate Status Protocol) server. The service supports configurable:
- Enable/disable per environment
- Request timeout and retry count
- In-memory caching of OCSP responses (configurable duration)
- Fail-closed, fail-open, or warn-only behavior when the OCSP server is unavailable

> **Note:** The actual OCSP wire-protocol implementation (`PerformOcspValidationAsync`) is a stub that rejects all certificates until a production implementation is supplied.

### Content Security Policy with Per-Request Nonces
When enabled, every HTTP response carries a `Content-Security-Policy` header whose `script-src` directive includes a **cryptographically random nonce** generated per request. The nonce is:
1. Generated/refreshed by `NonceRefresherService` using AES-CBC encryption with a configurable 32-byte key and 16-byte IV stored in configuration (or User Secrets).
2. Catalogued in a thread-safe `ConcurrentDictionary` by `NonceCatalogService`.
3. Injected into every response by `NonceMiddleware` and placed in `HttpContext.Items["Nonce"]` so Razor views can embed it in `<script>` tags.

The CSP also supports SHA-256 hash-based allow-listing of inline scripts via a flat text file (`wwwroot/csp-hashes.txt`) and an optional manually specified hash in configuration.

### Standard HTTP Security Headers
`UseStandardSecurityHeaders` appends the following headers to every response:
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Cross-Origin-Opener-Policy: same-origin`
- `Cross-Origin-Resource-Policy: same-site`
- `Permissions-Policy` disabling geolocation, camera, microphone, and FLoC (`interest-cohort`)
- Removal of `Server`, `X-Powered-By`, and `X-AspNetMvc-Version` response headers
- `Cache-Control: no-cache, no-store, must-revalidate`

### Azure Blob Storage
When enabled, `BlobSettingsService` provides a scoped service backed by a connection string and a configurable maximum attachment count. The connection string is expected to be stored in User Secrets or Azure Key Vault, never in source control.

### Azure Cosmos DB
When enabled, the application verifies the Cosmos DB connection at startup by calling `database.ReadAsync()`. `CosmosDbService` wraps a `CosmosClient` singleton and is bound to a configurable database and container. The connection string and account key are secrets stored outside source control.

### AWS Secrets Manager
When enabled, `AwsSecretsManagerOperationsService` provides a template stub for fetching secrets and TLS certificates from **AWS Secrets Manager** (the AWS equivalent of Azure Key Vault). It mirrors the interface of `AzureKeyVaultOperationsService`:
- `FetchSecret(secretName)` — retrieve any named secret by ARN or name.
- `FetchCertificate()` — retrieve the PFX server certificate.
- `FetchSecretIVSecret()` / `FetchSecretNonceKeySecret()` — retrieve nonce encryption material.

The underlying `AwsSecretManagerOperations` class logs a warning and returns empty values until a production implementation is supplied. AWS credentials (`AccessKeyId`, `SecretAccessKey`) must be stored in User Secrets or environment variables — never in source control.

### Amazon DynamoDB
When enabled, `AwsDynamoDbService` wraps an `IAmazonDynamoDB` client singleton (the AWS equivalent of Azure Cosmos DB). At startup the service verifies connectivity by calling `DescribeTable`. The service exposes `GetTableAsync()` and `GetTableName()` for downstream use. AWS credentials must be stored outside source control.

### Google Cloud Secret Manager
When enabled, `GcpSecretManagerOperationsService` provides a template stub for fetching secrets and TLS certificates from **Google Cloud Secret Manager** (the GCP equivalent of Azure Key Vault). It mirrors the interface of `AzureKeyVaultOperationsService`:
- `FetchSecret(secretId)` — retrieve any named secret by ID.
- `FetchCertificate()` — retrieve the PFX server certificate.
- `FetchSecretIVSecret()` / `FetchSecretNonceKeySecret()` — retrieve nonce encryption material.

The underlying `GcpSecretManagerOperations` class logs a warning and returns empty values until a production implementation is supplied. Authentication uses **Application Default Credentials (ADC)** by default; a service-account JSON key file path can optionally be supplied via `GcpSecretManager:CredentialFilePath`.

### Google Cloud Firestore
When enabled, `GcpFirestoreService` wraps a `FirestoreDb` singleton (the GCP equivalent of Azure Cosmos DB). At startup the application builds a Firestore client bound to the configured project ID and collection name. The service exposes `GetCollection()`, `GetCollectionName()`, and `GetDatabase()` for downstream use. Authentication uses ADC or a service-account JSON key file.

### AWS Cognito Identity Management
When enabled, `AddAwsCognitoAuthentication` configures OpenID Connect authentication against an **AWS Cognito User Pool** — the AWS equivalent of Microsoft Entra ID / Azure AD. The middleware consumes Cognito's standards-compliant OIDC discovery endpoint:

```
https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration
```

Configuration is under the `AwsCognito` section: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (store in User Secrets), and `Domain` (the Cognito hosted-UI domain). The callback path defaults to `/signin-aws-cognito`.

### GCP Identity Platform
When enabled, `AddGcpIdentityAuthentication` configures OpenID Connect authentication using **Google's OAuth 2.0 / OpenID Connect** endpoint — the GCP equivalent of Microsoft Entra ID / Azure AD. The middleware consumes Google's standard OIDC discovery endpoint:

```
https://accounts.google.com/.well-known/openid-configuration
```

Configuration is under the `GcpIdentity` section: `ClientId`, `ClientSecret` (store in User Secrets), and optional `ProjectId` for logging. The callback path defaults to `/signin-gcp`. Obtain a client ID and secret from the **Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client IDs**.

### Secure Session Management
Sessions use in-process distributed memory cache with a **30-minute idle timeout**. Session cookies are configured as:
- `HttpOnly = true`
- `Secure = Always` (HTTPS-only)
- `SameSite = Strict`

### Localization
The application supports **25 languages** with per-view `.resx` resource files. The active culture is determined at the request pipeline level via `RequestLocalizationOptions` (Accept-Language header, query string, or cookie). Users can switch language at any time using the language picker in the navigation bar.

| Culture Tag | Language |
|---|---|
| `en-US` | English (United States) — default |
| `de-DE` | Deutsch (German) |
| `es-ES` | Español (Spanish) |
| `fr-FR` | Français (French) |
| `pt-PT` | Português (Portuguese) |
| `it-IT` | Italiano (Italian) |
| `zh-HK` | 廣東話 (Cantonese — Hong Kong Traditional Chinese) |
| `ko-KR` | 한국어 (Korean) |
| `hi-IN` | हिन्दी (Hindi) |
| `ru-RU` | Русский (Russian) |
| `ar-SA` | العربية (Arabic — right-to-left layout) |
| `sw-KE` | Kiswahili (Swahili) |
| `ja-JP` | 日本語 (Japanese) |
| `ht-HT` | Kreyòl ayisyen (Haitian Creole) |
| `haw-US` | ʻŌlelo Hawaiʻi (Hawaiian) |
| `sm-WS` | Gagana Samoa (Samoan) |
| `mi-NZ` | Te Reo Māori (Māori) |
| `af-ZA` | Afrikaans |
| `nl-NL` | Nederlands (Dutch) |
| `ha-NG` | Hausa |
| `am-ET` | አማርኛ (Amharic) |
| `yo-NG` | Yorùbá (Yoruba) |
| `bn-BD` | বাংলা (Bengali) |
| `zh-CN` | 普通话 (Mandarin Chinese — Simplified) |
| `ga-IE` | Gaeilge (Irish) |

Right-to-left (RTL) layout is activated automatically when Arabic is selected: the `<html>` element receives `dir="rtl"`, and the `lang` attribute carries the full BCP-47 culture tag (e.g. `ar-SA`) for correct browser behaviour.

### PII-Safe Logging
`LoggingHelper` hashes personally identifiable information in log output using HMAC-SHA256. A stable 32-byte key can be supplied via `Logging:PiiHmacKey` (stored in User Secrets). If the key is absent or invalid, a random key is generated at startup so PII is never logged in plaintext.

---

## Feature Flags

All major subsystems are controlled by boolean feature flags in `appsettings.json`. Each flag defaults to a safe state.

| Flag | Default | Description |
|---|---|---|
| `EnableSession` | `true` | Server-side session and session cookie |
| `EnableLocalization` | `true` | Multi-language support (en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ga-IE) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect authentication |
| `EnableAuthorization` | `true` | Route-level authorization policies |
| `EnableKeyVault` | `false` | Load TLS server cert from Azure Key Vault |
| `EnableNonceServices` | `false` | Per-request CSP nonce generation |
| `EnableCSP` | `false` | Attach `Content-Security-Policy` header |
| `EnableSecurityHeaders` | `true` | Attach standard HTTP security headers |
| `EnableBlobStorage` | `false` | Azure Blob Storage service |
| `EnableCosmosDb` | `false` | Azure Cosmos DB service |
| `EnableMtls` | `false` | Require client TLS certificates |
| `EnableOcspValidation` | `false` | OCSP certificate revocation check (stub) |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager service (stub) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB service |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect identity management |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager service (stub) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore service |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |

---

## Prerequisites

The following must be in place before deploying on either platform:

1. **Azure AD App Registration** – with a redirect URI pointing to your hostname, a client secret or certificate credential, and (optionally) API permissions.
2. **Azure Key Vault** – containing the PFX server certificate as a secret. The app registration must have `Get` permission on secrets.
3. **Azure Cosmos DB account** (optional) – with a database and container matching your configuration.
4. **Azure Blob Storage account** (optional) – with a connection string.
5. **AWS credentials** (optional) – an IAM user or role with `secretsmanager:GetSecretValue` and/or `dynamodb:DescribeTable` permissions for the AWS features. For AWS Cognito, create a User Pool, an App Client, and enable the hosted UI with the required OAuth 2.0 scopes.
6. **GCP service account or ADC** (optional) – a service account with `secretmanager.versions.access` and/or `datastore.databases.get` IAM roles for the GCP features. For GCP Identity, create OAuth 2.0 credentials in the Google Cloud Console and enable the Google Identity API.
7. **.NET 9 SDK / Runtime** – version 9.0 or later.

---

## Installation – Windows Azure (App Service)

### 1. Create Azure Resources

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

### 2. Register an Azure AD Application

In the [Azure Portal](https://portal.azure.com):
1. Navigate to **Microsoft Entra ID → App registrations → New registration**.
2. Set the redirect URI to `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Under **Certificates & secrets**, create a client secret and copy the value.
4. Note the **Tenant ID** and **Client ID** from the Overview blade.

### 3. Create Azure Key Vault and Upload the Server Certificate

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

### 4. Configure Application Settings

Copy `appsettings.template.json` to `appsettings.json` and fill in the placeholder values. Secrets must **not** be stored in source control — set them as App Service Application Settings or via User Secrets locally:

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

### 5. Deploy the Application

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Enable HTTPS and Custom Domain (recommended)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Enable mTLS on Azure App Service (optional)

Azure App Service supports client certificates via the portal:
1. Go to **App Service → TLS/SSL settings → Client certificates**.
2. Set **Incoming client certificates** to **Require**.

Then set `FeatureFlags__EnableMtls=true` in Application Settings.

---

## Installation – OpenBSD Server communicating with Azure Services

> **Important:** .NET 9 does **not** have an official Microsoft build for OpenBSD. The instructions below use a **Linux-compatible container** (via [Podman](https://podman.io/), which is available in OpenBSD's package tree) to run the ASP.NET Core 9 application on OpenBSD while communicating with Azure services over HTTPS.

### 1. Install Prerequisites on OpenBSD

```sh
# As root
pkg_add podman
pkg_add curl git
```

If neither Podman nor Docker is available for your OpenBSD version, consider running the app in a **Linux VM** (e.g., vmm(4) with a Debian/Ubuntu guest) and following the standard Linux deployment path from within that guest.

## MVC CRUD pages and admin certificates

The application now includes MVC CRUD pages for `CrudRecord` data:

- `/Records` is a login-protected CRUD area for the signed-in user's own records.
- `/AdminRecords` is a globally scoped admin area that requires both the normal application login and a dedicated admin client certificate.

Configure the backing store in `CrudData`:

- `Provider = "Sqlite"` uses a local relational SQLite database at `CrudData:SqliteDatabasePath`.
- `Provider = "Cosmos"` uses the existing Azure Cosmos DB settings and stores records in `CrudData:CosmosContainerName`.

Configure `AdminCertificateSettings` with:

- `AllowedIssuers`: the issuer DN fragment for the admin-only CA
- `AdminUsers[*].UserIdentifiers`: the admin user's identity values (OID, email, UPN, etc.)
- `AdminUsers[*].AllowedThumbprints`: the certificate thumbprints allowed for that admin user

Admin certificate authentication attempts and admin page access are logged with the certificate thumbprint that was presented.

### 2. Pull the ASP.NET Core 9 Runtime Image

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Build the Application (on a Linux or Windows build machine)

On a machine with the .NET 9 SDK installed, publish a self-contained build targeting Linux x64:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

Transfer the `publish/` directory to the OpenBSD host (e.g., via `scp` or a shared volume).

### 4. Create a Configuration File

On the OpenBSD host, create `/etc/webappexp26/appsettings.json` with your production values (no secrets in the file; use environment variables instead):

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

Secrets are injected as environment variables in the next step.

### 5. Run the Container

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

### 6. Configure OpenBSD Packet Filter (pf) Firewall

Add to `/etc/pf.conf` to allow inbound HTTPS and permit outbound connections to Azure endpoints:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Reload the ruleset:

```sh
pfctl -f /etc/pf.conf
```

### 7. Configure DNS and TLS Certificates

Ensure the hostname in `AllowedHosts` resolves to the OpenBSD server's public IP. Azure AD requires the redirect URI (`/signin-oidc`) to be reachable over HTTPS, so the server certificate must be trusted. Use a certificate from a public CA (e.g., Let's Encrypt via `acme-client(1)`) or upload a CA-signed certificate to Azure Key Vault and enable `EnableKeyVault`.

### 8. Outbound Connectivity to Azure Services

The following Azure service endpoints must be reachable from the OpenBSD host over TCP 443:

| Service | Endpoint |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |

When AWS services are enabled, add the relevant regional endpoints (e.g. `secretsmanager.us-east-1.amazonaws.com`, `dynamodb.us-east-1.amazonaws.com`). When GCP services are enabled, add `secretmanager.googleapis.com` and `firestore.googleapis.com`.

Test connectivity before starting the container:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## Configuration Reference

Copy `appsettings.template.json` to `appsettings.json` and replace all `{{PLACEHOLDER}}` values.

| Section | Key | Description |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Azure AD app registration |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault and certificate name |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | mTLS client cert policy |
| `NonceEncryption` | `Key`, `IV` | 32-byte key and 16-byte IV for nonce encryption (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Blob Storage connection |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Cosmos DB connection |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | OCSP validation (stub) |
| `Logging` | `PiiHmacKey` | 32-byte base64 HMAC key for PII hashing in logs |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager (stub) |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `AwsCognito` | `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret`, `Domain`, `CallbackPath` | AWS Cognito OIDC identity |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager (stub) |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `GcpIdentity` | `ClientId`, `ClientSecret`, `ProjectId`, `CallbackPath` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |

Generate encryption keys and IVs using the included PowerShell script:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Store all secrets in **.NET User Secrets** for local development:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

For AWS services:

```bash
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsCognito:AppClientSecret" "YOUR_COGNITO_APP_CLIENT_SECRET"
```

For GCP services, set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable or use the `CredentialFilePath` setting to point to a service-account JSON key file.

```bash
dotnet user-secrets set "GcpIdentity:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

---

## Supporting Scripts

The `SupportingScripts/` directory contains PowerShell utilities:

| Script | Purpose |
|---|---|
| `IVandKeySampleGenerator.ps1` | Generate a random 32-byte AES key and 16-byte IV (base64) |
| `HashInlineScriptPowerShell.ps1` | Compute SHA-256 hashes for inline scripts (for CSP allow-listing) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Same as above, outputs hashes in base64 format |
| `CertificateUploaderToAzureExample.ps1` | Upload a PFX certificate to Azure Key Vault |
| `CheckRoles.ps1` | Verify Azure RBAC role assignments for the app |
| `ExportResourceGroups.ps1` | Export Azure resource group configurations |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnose Cosmos DB connectivity |
| `SetupFromTemplate.ps1` | Automate initial configuration from `appsettings.template.json` |

---

## Security Notes

- **Never commit secrets** (`ClientSecret`, `KeyVaultSecret`, connection strings, encryption keys, AWS/GCP credentials) to source control. Use .NET User Secrets locally and Azure App Settings / Key Vault references in production.
- The OCSP validation implementation is a **stub** that rejects all certificates. Replace `PerformOcspValidationAsync` in `OcspValidationService.cs` before enabling `EnableOcspValidation` in production.
- The AWS Secrets Manager and GCP Secret Manager implementations are **stubs** that log a warning and return empty values. Replace the method bodies in `AwsSecretManagerOperations` and `GcpSecretManagerOperations` before enabling those features in production.
- Nonce values are **never logged** — logging a nonce in plaintext would allow an attacker with log access to inject arbitrary inline scripts.
- The `Server` response header is masked to `webserver` to avoid exposing platform information.
- Review `AllowSelfSignedCertificates = false` (default) before deploying mTLS; self-signed certificates should only be used in development.
- AWS `AccessKeyId` and `SecretAccessKey` must **never** appear in `appsettings.json` — use User Secrets, environment variables, or IAM instance roles.
- For **AWS Cognito**, prefer IAM roles or Cognito Identity Pools over static credentials; never commit `AppClientSecret` to source control.
- GCP credentials should use **Application Default Credentials (ADC)** (e.g., Workload Identity on GKE, or `gcloud auth application-default login` locally) rather than committing service-account JSON files.
- For **GCP Identity**, the `ClientSecret` must be stored in User Secrets or environment variables — never in `appsettings.json`.
