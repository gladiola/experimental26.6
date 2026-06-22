# WebAppExperimental266

Een ASP.NET Core 9 Razor Pages-webtoepassing met Azure AD-verificatie, wederzijds TLS (mTLS), Azure Key Vault-certificaatbeheer, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, en een geharde HTTP-beveiligingslaag met nonce-gebaseerd Inhoudsbeveiligingsbeleid.

---

## Inhoudsopgave

- [Functies](#functies)
- [Functievlaggen](#functievlaggen)
- [Vereisten](#vereisten)
- [Installatie – Windows Azure (App Service)](#installatie--windows-azure-app-service)
- [Installatie – OpenBSD-server die communiceert met Azure-services](#installatie--openbsd-server-die-communiceert-met-azure-services)
- [Configuratiereferentie](#configuratiereferentie)
- [Ondersteunende scripts](#ondersteunende-scripts)
- [Beveiligingsnotities](#beveiligingsnotities)

---

## Functies

### Azure AD-verificatie (OpenID Connect)
De toepassing verifieert gebruikers via het **Microsoft-identiteitsplatform** met het OpenID Connect-protocol (via `Microsoft.Identity.Web`). Alle routes onder `/Experimental` vereisen een geverifieerde Azure AD-identiteit. De pagina's `/Privacy`, `/Error` en `/About` zijn openbaar toegankelijk.

### mTLS Clientcertificaatverificatie
Wanneer ingeschakeld, moeten clients een geldig X.509-certificaat presenteren. Instellingen in `MtlsSettings` bepalen of geketende certificaten, zelfondertekende certificaten of beide zijn toegestaan, de certificaatintrekkingscontrole en de toegestane certificaatuitgevers.

### Azure Key Vault-integratie
De toepassing haalt het TLS **servercertificaat** op uit Azure Key Vault bij het opstarten. Het geladen `X509Certificate2` wordt rechtstreeks geïnjecteerd in de HTTPS-standaardinstellingen van Kestrel, zodat er geen PFX-bestand op schijf aanwezig hoeft te zijn.

### Inhoudsbeveiligingsbeleid met Nonce per Verzoek
Wanneer ingeschakeld, bevat elke HTTP-reactie een `Content-Security-Policy`-header waarvan de `script-src`-instructie een **cryptografisch willekeurige nonce** per verzoek bevat. Het CSP ondersteunt ook SHA-256-hash-gebaseerde toestemmingslijsten voor inline scripts.

### Standaard HTTP-beveiligingsheaders
`UseStandardSecurityHeaders` voegt aan elke reactie toe: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, en de verwijdering van `Server`-, `X-Powered-By`- en `X-AspNetMvc-Version`-reactieheaders.

### Azure Blob Storage
Wanneer ingeschakeld, biedt `BlobSettingsService` een Scoped-service die wordt ondersteund door een verbindingsreeks en een configureerbaar maximumaantal bijlagen.

### Azure Cosmos DB
Wanneer ingeschakeld, verifieert de toepassing bij het opstarten de Cosmos DB-verbinding door `database.ReadAsync()` aan te roepen.

### AWS Secrets Manager
Wanneer ingeschakeld, haalt `AwsSecretsManagerOperationsService` geheimen en certificaten op uit AWS Secrets Manager. Configuratie in de sectie `AwsSecretsManager` met: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, en `AccessKeyId`/`SecretAccessKey`-referenties.

### Amazon DynamoDB
Wanneer ingeschakeld, verifieert `AwsDynamoDbService` bij het opstarten de verbinding met de DynamoDB-tabel. Configuratie in de sectie `AwsDynamoDb` met: `Region`, `TableName`, en `AccessKeyId`/`SecretAccessKey`-referenties.

### GCP Secret Manager
Wanneer ingeschakeld, haalt `GcpSecretManagerOperationsService` geheimen op uit Google Cloud Secret Manager. Configuratie in de sectie `GcpSecretManager` met: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, en `CredentialFilePath` (optioneel, gebruikt ADC indien leeg).

### GCP Firestore
Wanneer ingeschakeld, bouwt `GcpFirestoreService` bij het opstarten de Firestore-client. Configuratie in de sectie `GcpFirestore` met: `ProjectId`, `DatabaseId` (standaard: "(default)"), `CollectionName`, en `CredentialFilePath` (optioneel).

### AWS Cognito-identiteitsbeheer
Wanneer ingeschakeld, configureert `AddAwsCognitoAuthentication` OpenID Connect-verificatie tegen een **Amazon Cognito-gebruikerspool** — het AWS-equivalent van Microsoft Entra ID / Azure AD. Het OIDC-detectie-eindpunt:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Configuratie in de sectie `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (opslaan in Gebruikersgeheimen), en `Domain`.

### GCP-identiteitsplatform
Wanneer ingeschakeld, configureert `AddGcpIdentityAuthentication` OpenID Connect-verificatie met **Google OAuth 2.0 / OIDC** — het GCP-equivalent van Microsoft Entra ID / Azure AD. Het OIDC-detectie-eindpunt:
`https://accounts.google.com/.well-known/openid-configuration`
Configuratie in de sectie `GcpIdentity`: `ClientId`, `ClientSecret` (opslaan in Gebruikersgeheimen), en optionele `ProjectId`.

### Veilig sessiebeheer
Sessies gebruiken een in-process gedistribueerde geheugencache met een **time-out van 30 minuten inactiviteit**. Sessiecookies zijn geconfigureerd met `HttpOnly`, `Secure = Always`, en `SameSite = Strict`.

### Lokalisatie
De toepassing ondersteunt **25 talen**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, en ga-IE. Arabisch bevat automatische RTL-indelingsomschakeling.

### PII-veilige logboekregistratie
`LoggingHelper` versleutelt persoonlijk identificeerbare informatie in logboekuitvoer met HMAC-SHA256. Een stabiele 32-byte sleutel kan worden opgegeven via `Logging:PiiHmacKey`.

---

## Functievlaggen

Alle grote subsystemen worden beheerd door booleaanse functievlaggen in `appsettings.json`.

| Vlag | Standaard | Beschrijving |
|---|---|---|
| `EnableSession` | `true` | Sessie aan serverzijde en sessiecookie |
| `EnableLocalization` | `true` | Meertalige ondersteuning (25 talen) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect-verificatie |
| `EnableAuthorization` | `true` | Autorisatiebeleid op routeniveau |
| `EnableKeyVault` | `false` | TLS-servercertificaat laden uit Azure Key Vault |
| `EnableNonceServices` | `false` | CSP-nonce per verzoek genereren |
| `EnableCSP` | `false` | `Content-Security-Policy`-header toevoegen |
| `EnableSecurityHeaders` | `true` | Standaard HTTP-beveiligingsheaders toevoegen |
| `EnableBlobStorage` | `false` | Azure Blob Storage-service |
| `EnableCosmosDb` | `false` | Azure Cosmos DB-service |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (stub) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect-identiteitsbeheer |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (stub) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP-identiteitsplatform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Client-TLS-certificaten vereisen |
| `EnableOcspValidation` | `false` | OCSP-certificaatintrekkingscontrole (stub) |

---

## Vereisten

1. **Azure AD-toepassingsregistratie** – met een omleidings-URI, clientgeheim of certificaatreferentie.
2. **Azure Key Vault** – met het PFX-servercertificaat als geheim.
3. **Azure Cosmos DB-account** (optioneel).
4. **Azure Blob Storage-account** (optioneel).
5. **.NET 9 SDK / Runtime** – versie 9.0 of later.
6. **AWS-referenties** (IAM-gebruiker of -rol met `secretsmanager`- en `dynamodb`-machtigingen) – vereist wanneer `EnableAwsSecretsManager` of `EnableAwsDynamoDb` is ingeschakeld.
7. **GCP-serviceaccount of ADC** (met `secretmanager`- en `datastore`-machtigingen) – vereist wanneer `EnableGcpSecretManager` of `EnableGcpFirestore` is ingeschakeld.

---

## Installatie – Windows Azure (App Service)

### 1. Azure-resources aanmaken

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Een Azure AD-toepassing registreren

In de [Azure Portal](https://portal.azure.com):
1. Navigeer naar **Microsoft Entra ID → App-registraties → Nieuwe registratie**.
2. Stel de omleidings-URI in op `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Maak onder **Certificaten en geheimen** een clientgeheim aan en kopieer de waarde.
4. Noteer de **Tenant-ID** en **Client-ID** op het tabblad Overzicht.

### 3. Azure Key Vault aanmaken en servercertificaat uploaden

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Toepassingsinstellingen configureren

Kopieer `appsettings.template.json` naar `appsettings.json` en vul de tijdelijke aanduidingen in. Geheimen **mogen niet** in bronbeheer worden opgeslagen — stel ze in als App Service-toepassingsinstellingen of via Gebruikersgeheimen lokaal:

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

### 5. De toepassing implementeren

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. HTTPS en aangepast domein inschakelen (aanbevolen)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. mTLS inschakelen op Azure App Service (optioneel)

1. Ga naar **App Service → TLS/SSL-instellingen → Clientcertificaten**.
2. Stel **Inkomende clientcertificaten** in op **Vereist**.

Stel vervolgens `FeatureFlags__EnableMtls=true` in bij Toepassingsinstellingen.

---

## Installatie – OpenBSD-server die communiceert met Azure-services

> **Belangrijk:** .NET 9 heeft **geen** officiële Microsoft-build voor OpenBSD. De onderstaande instructies gebruiken een **Linux-compatibele container** (via [Podman](https://podman.io/)) om de ASP.NET Core 9-toepassing op OpenBSD te draaien terwijl deze via HTTPS communiceert met Azure-services.

### 1. Vereisten installeren op OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. De ASP.NET Core 9 Runtime-image ophalen

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. De toepassing bouwen (op een Linux- of Windows-bouwmachine)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Een configuratiebestand aanmaken

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. De container starten

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

### 6. OpenBSD Packet Filter (pf)-firewall configureren

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Uitgaande verbinding met Azure-services

| Service | Eindpunt |
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

## Configuratiereferentie

Kopieer `appsettings.template.json` naar `appsettings.json` en vervang alle `{{PLACEHOLDER}}`-waarden.

| Sectie | Sleutel | Beschrijving |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Azure AD-toepassingsregistratie |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault en certificaatnaam |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | mTLS-clientcertificaatbeleid |
| `NonceEncryption` | `Key`, `IV` | 32-byte sleutel en 16-byte IV voor nonce-versleuteling (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Blob Storage-verbinding |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Cosmos DB-verbinding |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | OCSP-validering (stub) |
| `Logging` | `PiiHmacKey` | 32-byte base64 HMAC-sleutel voor PII-hashing in logboeken |

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

## Ondersteunende scripts

| Script | Doel |
|---|---|
| `IVandKeySampleGenerator.ps1` | Genereer een willekeurige 32-byte AES-sleutel en 16-byte IV (base64) |
| `HashInlineScriptPowerShell.ps1` | Bereken SHA-256-hashes voor inline scripts (voor CSP-toestemmingslijst) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Hetzelfde als hierboven, produceert hashes in base64-formaat |
| `CertificateUploaderToAzureExample.ps1` | Upload een PFX-certificaat naar Azure Key Vault |
| `CheckRoles.ps1` | Verifieer Azure RBAC-roltoewijzingen voor de toepassing |
| `ExportResourceGroups.ps1` | Exporteer Azure-resourcegroepconfiguraties |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnosticeer Cosmos DB-connectiviteit |
| `SetupFromTemplate.ps1` | Automatiseer de initiële configuratie vanuit `appsettings.template.json` |

---

## Beveiligingsnotities

- **Commit nooit geheimen** naar bronbeheer. Gebruik .NET Gebruikersgeheimen lokaal en Azure App-instellingen / Key Vault-referenties in productie.
- De OCSP-validatie-implementatie is een **stub** die alle certificaten weigert. Vervang `PerformOcspValidationAsync` voordat `EnableOcspValidation` in productie wordt ingeschakeld.
- Nonce-waarden worden **nooit gelogd** — het loggen van een nonce in platte tekst zou een aanvaller met toegang tot logboeken in staat stellen willekeurige inline scripts in te voegen.
- De `Server`-reactieheader is gemaskeerd als `webserver` om blootstelling van platforminformatie te voorkomen.
- AWS `AccessKeyId` en `SecretAccessKey` mogen **nooit** voorkomen in `appsettings.json` — gebruik Gebruikersgeheimen, omgevingsvariabelen of IAM-instantierollen.
- GCP-referenties dienen **Standaard toepassingsreferenties (ADC)** te gebruiken in plaats van serviceaccountsleutelbestanden te committen.
