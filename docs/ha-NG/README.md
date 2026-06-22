# WebAppExperimental266

Aikace-aikacen yanar gizo na ASP.NET Core 9 Razor Pages tare da tabbatarwa ta Azure AD, TLS na juna (mTLS), gudanar da takardar shaidar Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, da kuma Layer na tsaron HTTP mai ƙarfi tare da Manufar Tsaron Abubuwa ta nonce.

---

## Teburin Abun Ciki

- [Fasali](#fasali)
- [Alamomin Fasali](#alamomin-fasali)
- [Buƙatun Da Suka Wajaba](#buƙatun-da-suka-wajaba)
- [Saka – Windows Azure (App Service)](#saka--windows-azure-app-service)
- [Saka – Sabar OpenBSD mai Sadarwa da Azure Services](#saka--sabar-openbsd-mai-sadarwa-da-azure-services)
- [Bayani na Saiti](#bayani-na-saiti)
- [Rubutun Tallafi](#rubutun-tallafi)
- [Bayanin Tsaro](#bayanin-tsaro)

---

## Fasali

### Tabbatarwa ta Azure AD (OpenID Connect)
Aikace-aikacen yana tabbatar da masu amfani ta hanyar **Dandalin Shaida na Microsoft** ta amfani da yarjejeniyar OpenID Connect (ta `Microsoft.Identity.Web`). Duk hanyoyin da ke ƙarƙashin `/Experimental` suna buƙatar shaida ta Azure AD da aka tabbatar. Shafukan `/Privacy`, `/Error`, da `/About` ana iya shiga su a bainar jama'a.

### Tabbatarwa ta Takardar Shaidar Abokin Cinikin mTLS
Da an kunna shi, abokan ciniki dole su gabatar da takardar shaida ta X.509 mai inganci. Saitunan da ke cikin `MtlsSettings` suna sarrafa ko an yarda da takardu masu sarƙoƙi, masu sa hannun kansu, ko duka biyun, duba soke takardar shaida, da masu fitar da takardar shaida da aka yarda da su.

### Haɗin Azure Key Vault
Aikace-aikacen yana dawo da **takardar shaida ta sabar** TLS daga Azure Key Vault a lokacin farawa. An saka `X509Certificate2` da aka loda kai tsaye a cikin tanadar HTTPS na Kestrel, don haka babu buƙatar fayil ɗin PFX ya kasance a kan faifan.

### Manufar Tsaron Abubuwa tare da Nonce Ga Kowane Buƙata
Da an kunna shi, kowace amsar HTTP tana ɗauke da taken `Content-Security-Policy` wanda jagorarsa ta `script-src` ta ƙunshi **nonce mai lissafi bazuwar** ga kowane buƙata. CSP kuma tana tallafawa lissafin izini na SHA-256 don rubutun cikin gida.

### Taken Tsaro na HTTP na Yau da Kullum
`UseStandardSecurityHeaders` yana ƙara zuwa kowane amsa: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, da kuma cire taken `Server`, `X-Powered-By`, da `X-AspNetMvc-Version`.

### Azure Blob Storage
Da an kunna shi, `BlobSettingsService` tana ba da sabis na Scoped da aka goyon baya ta hanyar kirtanin haɗi da yawan da za a iya saita na masu haɗewa.

### Azure Cosmos DB
Da an kunna shi, aikace-aikacen yana tabbatar da haɗin Cosmos DB a lokacin farawa ta kiran `database.ReadAsync()`.

### AWS Secrets Manager
Da an kunna shi, `AwsSecretsManagerOperationsService` tana dawo da sirri da takardu daga AWS Secrets Manager. Saiti a cikin sashi `AwsSecretsManager` tare da: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, da kuma hujjoji `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Da an kunna shi, `AwsDynamoDbService` tana tabbatar da haɗin tebur ɗin DynamoDB a lokacin farawa. Saiti a cikin sashi `AwsDynamoDb` tare da: `Region`, `TableName`, da kuma hujjoji `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Da an kunna shi, `GcpSecretManagerOperationsService` tana dawo da sirri daga Google Cloud Secret Manager. Saiti a cikin sashi `GcpSecretManager` tare da: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, da `CredentialFilePath` (zaɓi, yana amfani da ADC idan fanko).

### GCP Firestore
Da an kunna shi, `GcpFirestoreService` tana gina abokin cinikin Firestore a lokacin farawa. Saiti a cikin sashi `GcpFirestore` tare da: `ProjectId`, `DatabaseId` (tsoho: "(default)"), `CollectionName`, da `CredentialFilePath` (zaɓi).

### Gudanar da Shaida ta AWS Cognito
Da an kunna shi, `AddAwsCognitoAuthentication` tana saita tabbatarwa ta OpenID Connect a kan **Amazon Cognito User Pool** — Madaidaicin AWS na Microsoft Entra ID / Azure AD. Wurin ganowa na OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Saiti a cikin sashi `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (ajiye a cikin Sirrin Mai Amfani), da `Domain`.

### GCP Identity Platform
Da an kunna shi, `AddGcpIdentityAuthentication` tana saita tabbatarwa ta OpenID Connect ta amfani da **Google OAuth 2.0 / OIDC** — Madaidaicin GCP na Microsoft Entra ID / Azure AD. Wurin ganowo na OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Saiti a cikin sashi `GcpIdentity`: `ClientId`, `ClientSecret` (ajiye a cikin Sirrin Mai Amfani), da zaɓin `ProjectId`.

### Gudanar da Zaman Tsaro
Zaman suna amfani da ƙwaƙwalwar ajiyar rarrabawa da ke cikin tsari tare da **kewayen rashin aiki na mintuna 30**. Kukis na zaman an saita su tare da `HttpOnly`, `Secure = Always`, da `SameSite = Strict`.

### Gida-gida
Aikace-aikacen yana goyan bayan **harsuna 25**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, da ga-IE. Larabci yana haɗa da canjin tsarin RTL ta atomatik.

### Yin Rikodin Tsaro na PII
`LoggingHelper` yana hashing bayanan da za a iya gano su a cikin fitattun rikodi ta amfani da HMAC-SHA256. Ana iya bayar da maɓalli mai ɗorewa na 32-byte ta `Logging:PiiHmacKey`.

---

## Alamomin Fasali

Duk manyan ƙananan tsarin suna sarrafa su ta alamomin boolean a `appsettings.json`.

| Alamar | Tsoho | Bayanin |
|---|---|---|
| `EnableSession` | `true` | Zaman gefen sabar da kuki zaman |
| `EnableLocalization` | `true` | Tallafi na harsuna da yawa (harsuna 25) |
| `EnableAzureAd` | `true` | Tabbatarwa ta Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Manufofin izini matakin hanya |
| `EnableKeyVault` | `false` | Loda takardar shaida ta TLS ta sabar daga Azure Key Vault |
| `EnableNonceServices` | `false` | Samar da nonce na CSP ga kowane buƙata |
| `EnableCSP` | `false` | Haɗa taken `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Haɗa taken tsaro na HTTP na yau da kullum |
| `EnableBlobStorage` | `false` | Sabis na Azure Blob Storage |
| `EnableCosmosDb` | `false` | Sabis na Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (farfajiya) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Gudanar da Shaida ta AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (farfajiya) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Buƙatar takardu na TLS na abokin ciniki |
| `EnableOcspValidation` | `false` | Duba soke takardar shaida ta OCSP (farfajiya) |

---

## Buƙatun Da Suka Wajaba

1. **Yin Rijistar Aikace-Aikace ta Azure AD** – tare da URI na koma, sirrin abokin ciniki ko hujja ta takardar shaida.
2. **Azure Key Vault** – mai ɗauke da takardar shaida ta sabar ta PFX a matsayin sirri.
3. **Asusun Azure Cosmos DB** (zaɓi).
4. **Asusun Azure Blob Storage** (zaɓi).
5. **.NET 9 SDK / Runtime** – sigar 9.0 ko bayan haka.
6. **Hujjoji na AWS** (mai amfani ko rawar IAM tare da izinin `secretsmanager` da `dynamodb`) – ana buƙata lokacin da `EnableAwsSecretsManager` ko `EnableAwsDynamoDb` aka kunna.
7. **Asusun sabis na GCP ko ADC** (tare da izinin `secretmanager` da `datastore`) – ana buƙata lokacin da `EnableGcpSecretManager` ko `EnableGcpFirestore` aka kunna.

---

## Saka – Windows Azure (App Service)

### 1. Ƙirƙiri Albarkatun Azure

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Yi Rijistar Aikace-Aikace ta Azure AD

A cikin [Kofar Azure](https://portal.azure.com):
1. Je zuwa **Microsoft Entra ID → Rijistan aikace-aikace → Rajista sabon**.
2. Saita URI na koma zuwa `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Ƙarƙashin **Takardu da sirri**, ƙirƙiri sirrin abokin ciniki kuma kwafi ƙimar.
4. Lura da **ID na Haya** da **ID na Abokin Ciniki** daga shafin Bayanin Yau.

### 3. Ƙirƙiri Azure Key Vault kuma Loda Takardar Shaida ta Sabar

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Saita Saitattun Aikace-Aikace

Kwafi `appsettings.template.json` zuwa `appsettings.json` kuma cika ƙimomi na wurin riƙe. Sirri **bai kamata** a ajiye su a cikin kullin tushe — saita su a matsayin Saitattun Aikace-Aikace na App Service ko ta Sirrin Mai Amfani a gida:

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

### 5. Tura Aikace-Aikacen

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Kunna HTTPS da Sunan Yankin (a ba da shawarar)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Kunna mTLS a kan Azure App Service (zaɓi)

1. Je zuwa **App Service → Saitattun TLS/SSL → Takardu na Abokin Ciniki**.
2. Saita **Takardu na Abokin Ciniki da ke Shigowa** zuwa **Ana Buƙata**.

Sannan saita `FeatureFlags__EnableMtls=true` a cikin Saitattun Aikace-Aikace.

---

## Saka – Sabar OpenBSD mai Sadarwa da Azure Services

> **Muhimmi:** .NET 9 **ba ta da** ginin hukuma na Microsoft don OpenBSD. Umarni a ƙasa suna amfani da **kwantena mai jituwa da Linux** (ta [Podman](https://podman.io/)) don gudanar da aikace-aikacen ASP.NET Core 9 akan OpenBSD yayin sadarwa da Azure services ta HTTPS.

### 1. Saka Buƙatun Da Suka Wajaba akan OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Ja Hoton ASP.NET Core 9 Runtime

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Gina Aikace-Aikacen (a kan injin gini na Linux ko Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Ƙirƙiri Fayil ɗin Saiti

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. Fara Kwantenan

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

### 6. Saita Firewall ɗin OpenBSD Packet Filter (pf)

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Haɗin Waje zuwa Azure Services

| Sabis | Wurin Karshe |
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

## Bayani na Saiti

Kwafi `appsettings.template.json` zuwa `appsettings.json` kuma maye gurbin duk ƙimomi na `{{PLACEHOLDER}}`.

| Sashe | Maɓalli | Bayanin |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Rijistan aikace-aikace ta Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault da sunan takardar shaida |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Manufar takardar shaida ta abokin ciniki mTLS |
| `NonceEncryption` | `Key`, `IV` | Maɓalli na 32-byte da IV na 16-byte don ɓoye nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Haɗin Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Haɗin Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Tabbatarwa ta OCSP (farfajiya) |
| `Logging` | `PiiHmacKey` | Maɓalli na HMAC base64 na 32-byte don hashing na PII a cikin rikodi |

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

---

## Rubutun Tallafi

| Rubutu | Manufa |
|---|---|
| `IVandKeySampleGenerator.ps1` | Samar da maɓalli na AES bazuwar na 32-byte da IV na 16-byte (base64) |
| `HashInlineScriptPowerShell.ps1` | Lissafin hashing SHA-256 don rubutun cikin gida (don lissafin izini na CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Kamar yadda yake sama, yana samar da hashing a cikin tsarin base64 |
| `CertificateUploaderToAzureExample.ps1` | Loda takardar shaida ta PFX zuwa Azure Key Vault |
| `CheckRoles.ps1` | Tabbatar da aikawayen rawar RBAC na Azure don aikace-aikacen |
| `ExportResourceGroups.ps1` | Fitar da saitunan ƙungiyar albarkatun Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Gano haɗin Cosmos DB |
| `SetupFromTemplate.ps1` | Atomatik saiti na farko daga `appsettings.template.json` |

---

## Bayanin Tsaro

- **Kada a taɓa hada sirri** zuwa kullin tushe. Yi amfani da .NET Sirrin Mai Amfani a gida da Saitattun App Service na Azure / nassoshi na Key Vault a samarwa.
- Aiwatar da tabbatarwa ta OCSP wata **farfajiya** ce da ke kin duk takardu. Maye gurbin `PerformOcspValidationAsync` kafin a kunna `EnableOcspValidation` a samarwa.
- Ƙimomi na nonce **ba a taɓa rikodin su ba** — rikodin nonce a cikin rubutu sarari zai ba mai kai hari tare da damar rikodi damar saka rubutun cikin gida.
- Taken amsa `Server` an rufe shi zuwa `webserver` don guje wa bayyana bayanan dandali.
- AWS `AccessKeyId` da `SecretAccessKey` **ba za su taɓa bayyana ba** a cikin `appsettings.json` — yi amfani da Sirrin Mai Amfani, masu canji na yanayi, ko rawoyin misali na IAM.
