# WebAppExperimental266

'n ASP.NET Core 9 Razor Pages-webtoepassing met Azure AD-verifikasie, wedersydse TLS (mTLS), Azure Key Vault-sertifikaatbestuur, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, en 'n geharde HTTP-sekuriteitslaag met nonce-gebaseerde Inhoudsekuriteitsbeleid.

---

## Inhoudsopgawe

- [Funksies](#funksies)
- [Funksievlaggies](#funksievlaggies)
- [Vereistes](#vereistes)
- [Installasie – Windows Azure (App Service)](#installasie--windows-azure-app-service)
- [Installasie – OpenBSD-bediener wat met Azure-dienste kommunikeer](#installasie--openbsd-bediener-wat-met-azure-dienste-kommunikeer)
- [Konfigurasieverwysing](#konfigurasieverwysing)
- [Ondersteuningskrifte](#ondersteuningskrifte)
- [Sekuriteitsnotas](#sekuriteitsnotas)

---

## Funksies

### Azure AD-verifikasie (OpenID Connect)
Die toepassing verifieer gebruikers via die **Microsoft Identiteitsplatform** deur die OpenID Connect-protokol te gebruik (via `Microsoft.Identity.Web`). Alle roetes onder `/Experimental` vereis 'n geverifieerde Azure AD-identiteit. Die bladsye `/Privacy`, `/Error` en `/About` is openbaar toeganklik.

### mTLS Kliëntsertifikaatverifikasie
Wanneer geaktiveer, moet kliënte 'n geldige X.509-sertifikaat aanbied. Instellings in `MtlsSettings` beheer of gekettingde sertifikate, selfondertekende sertifikate of albei toegelaat word, sertifikaatherroepingkontrole, en toegelate sertifikaatuitreikers.

### Azure Key Vault-integrasie
Die toepassing haal die TLS **bedienersertifikaat** by Azure Key Vault tydens opstarten op. Die gelaaide `X509Certificate2` word direk in Kestrel se HTTPS-instellings ingespuit, sodat geen PFX-lêer op skyf hoef te bestaan nie.

### Inhoudsekuriteitsbeleid met Nonce per Versoek
Wanneer geaktiveer, dra elke HTTP-respons 'n `Content-Security-Policy`-kop waarvan die `script-src`-direktief 'n **kriptografies ewekansige nonce** per versoek insluit. Die CSP ondersteun ook SHA-256-hash-gebaseerde toelyslyste vir inlynskrifte.

### Standaard HTTP-veiligheidsopskrifte
`UseStandardSecurityHeaders` voeg by elke respons by: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, en die verwydering van `Server`-, `X-Powered-By`- en `X-AspNetMvc-Version`-opskrifte.

### Azure Blob Storage
Wanneer geaktiveer, bied `BlobSettingsService` 'n Scoped-diens wat deur 'n verbindingstring en 'n konfigureerbare maksimum aantal aanhegsels ondersteun word.

### Azure Cosmos DB
Wanneer geaktiveer, verifieer die toepassing by opstart die Cosmos DB-verbinding deur `database.ReadAsync()` te roep.

### AWS Secrets Manager
Wanneer geaktiveer, haal `AwsSecretsManagerOperationsService` geheime en sertifikate van AWS Secrets Manager op. Konfigurasie in die `AwsSecretsManager`-afdeling met: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, en `AccessKeyId`/`SecretAccessKey`-geloofsbriewe.

### Amazon DynamoDB
Wanneer geaktiveer, verifieer `AwsDynamoDbService` by opstart die verbinding na die DynamoDB-tabel. Konfigurasie in die `AwsDynamoDb`-afdeling met: `Region`, `TableName`, en `AccessKeyId`/`SecretAccessKey`-geloofsbriewe.

### GCP Secret Manager
Wanneer geaktiveer, haal `GcpSecretManagerOperationsService` geheime van Google Cloud Secret Manager op. Konfigurasie in die `GcpSecretManager`-afdeling met: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, en `CredentialFilePath` (opsioneel, gebruik ADC indien leeg).

### GCP Firestore
Wanneer geaktiveer, bou `GcpFirestoreService` by opstart die Firestore-kliënt. Konfigurasie in die `GcpFirestore`-afdeling met: `ProjectId`, `DatabaseId` (standaard: "(default)"), `CollectionName`, en `CredentialFilePath` (opsioneel).

### AWS Cognito-identiteitsbestuur
Wanneer geaktiveer, stel `AddAwsCognitoAuthentication` OpenID Connect-verifikasie teen 'n **Amazon Cognito-gebruikerspool** in — die AWS-ekwivalent van Microsoft Entra ID / Azure AD. Die OIDC-ontdekkingseindpunt:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Konfigurasie in die `AwsCognito`-afdeling: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (stoor in Gebruikersgeheime), en `Domain`.

### GCP Identiteitsplatform
Wanneer geaktiveer, stel `AddGcpIdentityAuthentication` OpenID Connect-verifikasie in deur **Google OAuth 2.0 / OIDC** — die GCP-ekwivalent van Microsoft Entra ID / Azure AD. Die OIDC-ontdekkingseindpunt:
`https://accounts.google.com/.well-known/openid-configuration`
Konfigurasie in die `GcpIdentity`-afdeling: `ClientId`, `ClientSecret` (stoor in Gebruikersgeheime), en opsionele `ProjectId`.

### Veilige Sessiebestuur
Sessies gebruik 'n in-proses verspreide geheue-kas met 'n **30-minuut onaktiwiteitsuitset**. Sessiekoekies is ingestel met `HttpOnly`, `Secure = Always`, en `SameSite = Strict`.

### Lokalisering
Die toepassing ondersteun **25 tale**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, en ga-IE. Arabies sluit outomatiese RTL-uitlegomskakeling in.

### PII-veilige aantekening
`LoggingHelper` hash persoonlik identifiseerbare inligting in aantekenuitvoer deur HMAC-SHA256 te gebruik. 'n Stabiele 32-byte-sleutel kan voorsien word via `Logging:PiiHmacKey`.

---

## Funksievlaggies

Alle hoofsubstelsels word beheer deur Boole-funksievlaggies in `appsettings.json`.

| Vlag | Standaard | Beskrywing |
|---|---|---|
| `EnableSession` | `true` | Bedienerkant-sessie en sessiekoekies |
| `EnableLocalization` | `true` | Meertalige ondersteuning (25 tale) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect-verifikasie |
| `EnableAuthorization` | `true` | Roetevlak-magtigingsbeleide |
| `EnableKeyVault` | `false` | Laai TLS-bedienersertifikaat van Azure Key Vault |
| `EnableNonceServices` | `false` | Per-versoek CSP-nonce-generering |
| `EnableCSP` | `false` | Voeg `Content-Security-Policy`-kop by |
| `EnableSecurityHeaders` | `true` | Voeg standaard HTTP-veiligheidsopskrifte by |
| `EnableBlobStorage` | `false` | Azure Blob Storage-diens |
| `EnableCosmosDb` | `false` | Azure Cosmos DB-diens |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (stuktjie) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect-identiteitsbestuur |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (stuktjie) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP-identiteitsplatform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Vereis kliënt-TLS-sertifikate |
| `EnableOcspValidation` | `false` | OCSP-sertifikaatherroepingkontrole (stuktjie) |

---

## Vereistes

1. **Azure AD-toepassingsregistrasie** – met 'n omleidings-URI, kliëntgeheim of sertifikaatgeloofsbrief.
2. **Azure Key Vault** – wat die PFX-bedienersertifikaat as 'n geheim bevat.
3. **Azure Cosmos DB-rekening** (opsioneel).
4. **Azure Blob Storage-rekening** (opsioneel).
5. **.NET 9 SDK / Runtime** – weergawe 9.0 of later.
6. **AWS-geloofsbriewe** (IAM-gebruiker of -rol met `secretsmanager`- en `dynamodb`-toestemmings) – benodig wanneer `EnableAwsSecretsManager` of `EnableAwsDynamoDb` geaktiveer is.
7. **GCP-diensrekening of ADC** (met `secretmanager`- en `datastore`-toestemmings) – benodig wanneer `EnableGcpSecretManager` of `EnableGcpFirestore` geaktiveer is.

---

## Installasie – Windows Azure (App Service)

### 1. Skep Azure-hulpbronne

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Registreer 'n Azure AD-toepassing

In die [Azure-portaal](https://portal.azure.com):
1. Gaan na **Microsoft Entra ID → Toepassingsregistrasies → Nuwe registrasie**.
2. Stel die omleidings-URI in op `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Onder **Sertifikate en geheime**, skep 'n kliëntgeheim en kopieer die waarde.
4. Let die **Huurder-ID** en **Kliënt-ID** van die Oorsig-blad op.

### 3. Skep Azure Key Vault en Laai die Bedienersertifikaat Op

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Stel Toepassingsinstellings in

Kopieer `appsettings.template.json` na `appsettings.json` en vul die plekhouerwaardes in. Geheime **moet nie** in bronbeheer gestoor word nie — stel dit as App Service-toepassingsinstellings in of via Gebruikersgeheime plaaslik:

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

### 5. Ontplooi die Toepassing

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Aktiveer HTTPS en Pasgemaakte Domein (aanbeveel)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Aktiveer mTLS op Azure App Service (opsioneel)

1. Gaan na **App Service → TLS/SSL-instellings → Kliëntsertifikate**.
2. Stel **Inkomende kliëntsertifikate** in op **Vereis**.

Stel dan `FeatureFlags__EnableMtls=true` in Toepassingsinstellings in.

---

## Installasie – OpenBSD-bediener wat met Azure-dienste kommunikeer

> **Belangrik:** .NET 9 het **nie** 'n amptelike Microsoft-bou vir OpenBSD nie. Die instruksies hieronder gebruik 'n **Linux-versoenbare houer** (via [Podman](https://podman.io/)) om die ASP.NET Core 9-toepassing op OpenBSD te laat loop terwyl dit met Azure-dienste via HTTPS kommunikeer.

### 1. Installeer Vereistes op OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Haal die ASP.NET Core 9 Runtime-beeld op

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Bou die Toepassing (op 'n Linux- of Windows-boumasjien)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Skep 'n Konfigurasielêer

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. Begin die Houer

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

### 6. Stel OpenBSD Packet Filter (pf) Brandmuur in

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Uitgaande Verbinding na Azure-dienste

| Diens | Eindpunt |
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

## Konfigurasieverwysing

Kopieer `appsettings.template.json` na `appsettings.json` en vervang alle `{{PLACEHOLDER}}`-waardes.

| Afdeling | Sleutel | Beskrywing |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Azure AD-toepassingsregistrasie |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault en sertifikaatnaam |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | mTLS-kliëntsertifikaatbeleid |
| `NonceEncryption` | `Key`, `IV` | 32-byte-sleutel en 16-byte-IV vir nonce-enkripsie (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Blob Storage-verbinding |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Cosmos DB-verbinding |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | OCSP-validering (stuktjie) |
| `Logging` | `PiiHmacKey` | 32-byte base64 HMAC-sleutel vir PII-hashing in aantekeninge |

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

## Ondersteuningskrifte

| Skrif | Doel |
|---|---|
| `IVandKeySampleGenerator.ps1` | Genereer 'n ewekansige 32-byte AES-sleutel en 16-byte IV (base64) |
| `HashInlineScriptPowerShell.ps1` | Bereken SHA-256-hashes vir inlynskrifte (vir CSP-toelyslys) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Dieselfde as hierbo, lewer hashes in base64-formaat |
| `CertificateUploaderToAzureExample.ps1` | Laai 'n PFX-sertifikaat op na Azure Key Vault |
| `CheckRoles.ps1` | Verifieer Azure RBAC-roltoekennings vir die toepassing |
| `ExportResourceGroups.ps1` | Voer Azure-hulpbronnegroepe-konfigurasies uit |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnoseer Cosmos DB-verbinding |
| `SetupFromTemplate.ps1` | Outomatiseer aanvanklike konfigurasie van `appsettings.template.json` |

---

## Sekuriteitsnotas

- **Commit nooit geheime** na bronbeheer nie. Gebruik .NET Gebruikersgeheime plaaslik en Azure App-instellings / Key Vault-verwysings in produksie.
- Die OCSP-valideringsimplementasie is 'n **stuktjie** wat alle sertifikate weier. Vervang `PerformOcspValidationAsync` voordat `EnableOcspValidation` in produksie geaktiveer word.
- Nonce-waardes word **nooit aangeteken** nie — die aantekening van 'n nonce in gewone teks sal 'n aanvaller met aantekentoegang in staat stel om willekeurige inlynskrifte in te spuit.
- Die `Server`-responskop word na `webserver` gemaskeer om blootstelling van platforminligting te vermy.
- AWS `AccessKeyId` en `SecretAccessKey` moet **nooit** in `appsettings.json` verskyn nie — gebruik Gebruikersgeheime, omgewingsveranderlikes of IAM-instansierolls.
- GCP-geloofsbriewe behoort **Standaard-toepassingsgeloofsbriewe (ADC)** te gebruik eerder as om diensrekeningsleutellêers te commit.
