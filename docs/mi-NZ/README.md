# WebAppExperimental266

He tono tukutuku ASP.NET Core 9 Razor Pages me te whakamana Azure AD, TLS tōpū (mTLS), whakahaere tiwhikete Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, **AWS Secrets Manager**, **Amazon DynamoDB**, **Google Cloud Secret Manager**, **Google Cloud Firestore**, me tētahi papa tiakanga HTTP kaha me te Kaupeka Haumaru Ihirangi e whakamahi ana i te nonce.

---

## Rārangi Ihirangi

- [Āhuatanga](#āhuatanga)
- [Haki Āhuatanga](#haki-āhuatanga)
- [Ngā Hiahia Tuatahi](#ngā-hiahia-tuatahi)
- [Tākai – Windows Azure (App Service)](#tākai--windows-azure-app-service)
- [Tākai – Tūmau OpenBSD e whakawhiti ana me ngā Ratonga Azure](#tākai--tūmau-openbsd-e-whakawhiti-ana-me-ngā-ratonga-azure)
- [Tohutohu Whirihoranga](#tohutohu-whirihoranga)
- [Ngā Tuhinga Tautoko](#ngā-tuhinga-tautoko)
- [Ngā Tāpiritanga Haumaru](#ngā-tāpiritanga-haumaru)

---

## Āhuatanga

### Whakamana Azure AD (OpenID Connect)
Ka whakamana te tono i ngā kaiwhakamahi mā te **Pūtake Tuakiri Microsoft** mā te whakamahi i te tikanga OpenID Connect (mā `Microsoft.Identity.Web`). Ko ngā ara katoa i raro i `/Experimental` me whai tuakiri Azure AD kua whakamana. Ka taea e te marea te whakauru ki ngā whārangi `/Privacy`, `/Error`, me `/About`.

### Whakamana Tiwhikete Kiritaki mTLS
Ka whakahohea, me tuku tiwhikete X.509 tika e ngā kiritaki. Ka whakaaweawe ngā tautuhinga i `MtlsSettings` i te whakaaetanga o ngā tiwhikete mekameka, whakaahua i a ia anō, rānei ōrua, te tirohanga hūnukutanga tiwhikete, me ngā kaihoatu tiwhikete whakaaetia.

### Hono Azure Key Vault
Ka tiki te tono i te **tiwhikete tūmau** TLS mai i Azure Key Vault i te tīmata. Ka uru tika te `X509Certificate2` i utaina ki ngā tautuhinga HTTPS Kestrel, nō reira kāore he kōnae PFX e hiahiatia ana ki runga i te whatupae.

### Kaupeka Haumaru Ihirangi me Nonce mō ia Tono
Ka whakahohea, ka mau ngā tauwhāiti HTTP katoa i tētahi upoko `Content-Security-Policy` e kōrero ana mō `script-src` he **nonce tōtika ā-pāngarau** mō ia tono. Ka tautoko anō te CSP i ngā rārangi whakaaetanga ā-hashing SHA-256 mō ngā tuhinga o roto.

### Ngā Upoko Haumaru HTTP Paerewa
Ka āpiti `UseStandardSecurityHeaders` ki ia urupare: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, me te tangohanga o ngā upoko `Server`, `X-Powered-By`, me `X-AspNetMvc-Version`.

### Azure Blob Storage
Ka whakahohea, ka tuku `BlobSettingsService` i tētahi ratonga Scoped e tautokohia ana e tētahi aho hononga me te rahinga tūtohu noa o ngā tāpiritanga.

### Azure Cosmos DB
Ka whakahohea, ka whakaū te tono i te hononga Cosmos DB i te tīmata mā te karanga `database.ReadAsync()`.

### AWS Secrets Manager
Ka whakahohea, ka tiki `AwsSecretsManagerOperationsService` i ngā mea huna me ngā tiwhikete mai i AWS Secrets Manager. Whirihoranga i te wāhanga `AwsSecretsManager` me: `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, me ngā mana `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Ka whakahohea, ka whakaū `AwsDynamoDbService` i te hononga o te ripanga DynamoDB i te tīmata. Whirihoranga i te wāhanga `AwsDynamoDb` me: `Region`, `TableName`, me ngā mana `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Ka whakahohea, ka tiki `GcpSecretManagerOperationsService` i ngā mea huna mai i Google Cloud Secret Manager. Whirihoranga i te wāhanga `GcpSecretManager` me: `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, me `CredentialFilePath` (kōwhiringa, ka whakamahi ADC ki te kore).

### GCP Firestore
Ka whakahohea, ka hanga `GcpFirestoreService` i te kaiwhakamahi Firestore i te tīmata. Whirihoranga i te wāhanga `GcpFirestore` me: `ProjectId`, `DatabaseId` (taunoa: "(default)"), `CollectionName`, me `CredentialFilePath` (kōwhiringa).

### Whakahaere Tuakiri AWS Cognito
Ka whakahohea, ka tautuhia e `AddAwsCognitoAuthentication` te whakamana OpenID Connect ki tētahi **Amazon Cognito User Pool** — te rite AWS o Microsoft Entra ID / Azure AD. Ko te pito kimi OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Whirihoranga i te wāhanga `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (tiakina ki ngā Mea Huna Kaiwhakamahi), me `Domain`.

### GCP Identity Platform
Ka whakahohea, ka tautuhia e `AddGcpIdentityAuthentication` te whakamana OpenID Connect mā te **Google OAuth 2.0 / OIDC** — te rite GCP o Microsoft Entra ID / Azure AD. Ko te pito kimi OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Whirihoranga i te wāhanga `GcpIdentity`: `ClientId`, `ClientSecret` (tiakina ki ngā Mea Huna Kaiwhakamahi), me `ProjectId` kōwhiringa.

### Whakahaere Kūaha Haumaru
Ka whakamahi ngā kūaha i tētahi kete mahara tūaroha-ā-tūāhanga me tētahi **taima noho 30 meneti**. Ka tautuhia ngā pihikete kūaha ki `HttpOnly`, `Secure = Always`, me `SameSite = Strict`.

### Whakaaro ā-Reo
Ka tautoko te tono i **reo 25**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, me ga-IE. Ka huri aunoa te hōtaka RTL mō te reo Arapi.

### Tuhituhinga Haumaru PII
Ka heshi `LoggingHelper` i ngā mōhiohio tohu tangata i ngā tuhinga tāhū mā te whakamahi i HMAC-SHA256. Ka taea te tuku tēpū 32-hurihia mā `Logging:PiiHmacKey`.

---

## Haki Āhuatanga

Ka whakaaweawe ngā punaha nui katoa e ngā haki boolean i `appsettings.json`.

| Haki | Taunoa | Whakamāramatanga |
|---|---|---|
| `EnableSession` | `true` | Kūaha tūmau me te pihikete kūaha |
| `EnableLocalization` | `true` | Tautoko reo maha (reo 25) |
| `EnableAzureAd` | `true` | Whakamana Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Ngā kaupeka whakaaetanga ā-ara |
| `EnableKeyVault` | `false` | Utaina tiwhikete tūmau TLS mai i Azure Key Vault |
| `EnableNonceServices` | `false` | Waihanga nonce CSP mō ia tono |
| `EnableCSP` | `false` | Tūhono upoko `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Tūhono ngā upoko haumaru HTTP paerewa |
| `EnableBlobStorage` | `false` | Ratonga Azure Blob Storage |
| `EnableCosmosDb` | `false` | Ratonga Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (tauira) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Whakahaere tuakiri AWS Cognito OpenID Connect |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (tauira) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Hiahia ngā tiwhikete TLS kiritaki |
| `EnableOcspValidation` | `false` | Tirohanga hūnukutanga tiwhikete OCSP (tauira) |

---

## Ngā Hiahia Tuatahi

1. **Rēhitatanga Tono Azure AD** – me URI huri, mea huna kiritaki rānei tiwhikete mana.
2. **Azure Key Vault** – e mau ana i te tiwhikete tūmau PFX hei mea huna.
3. **Kaute Azure Cosmos DB** (kōwhiringa).
4. **Kaute Azure Blob Storage** (kōwhiringa).
5. **.NET 9 SDK / Runtime** – putanga 9.0 rānei ake.
6. **Ngā Mana AWS** (kaiwhakamahi IAM rānei tūranga me ngā whakaaetanga `secretsmanager` me `dynamodb`) – hiahiatia ina whakahohea `EnableAwsSecretsManager` rānei `EnableAwsDynamoDb`.
7. **Kaute ratonga GCP rānei ADC** (me ngā whakaaetanga `secretmanager` me `datastore`) – hiahiatia ina whakahohea `EnableGcpSecretManager` rānei `EnableGcpFirestore`.

---

## Tākai – Windows Azure (App Service)

### 1. Waihanga Rawa Azure

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Rēhita Tono Azure AD

I te [Tomokanga Azure](https://portal.azure.com):
1. Haere ki **Microsoft Entra ID → Rēhitatanga tono → Rēhitatanga hou**.
2. Tautuhia te URI huri ki `https://<your-app>.azurewebsites.net/signin-oidc`.
3. I raro i **Ngā Tiwhikete me ngā mea huna**, waihanga mea huna kiritaki ka tāruarua i te uara.
4. Kōrero i te **ID Kaiwhakahere** me te **ID Kiritaki** mai i te pane Tirohanga Whakamāpiti.

### 3. Waihanga Azure Key Vault me te Tukuna Tiwhikete Tūmau

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Tautuhia Ngā Tautuhinga Tono

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

### 5. Tukuna te Tono

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Whakahohea HTTPS me te Ingoa Rohe Tikanga (tūtohuatia)

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Whakahohea mTLS i Azure App Service (kōwhiringa)

1. Haere ki **App Service → Ngā Tautuhinga TLS/SSL → Ngā Tiwhikete Kiritaki**.
2. Tautuhia **Ngā Tiwhikete Kiritaki e Uru Mai Ana** ki **Me Whai**.

Ka tautuhia `FeatureFlags__EnableMtls=true` i ngā Tautuhinga Tono.

---

## Tākai – Tūmau OpenBSD e whakawhiti ana me ngā Ratonga Azure

> **Hira:** **Kāore** he hanganga ōkawa Microsoft .NET 9 mō OpenBSD. Ka whakamahi ngā tohutohu i raro nei i tētahi **kaiwhakahaere reo Linux** (mā [Podman](https://podman.io/)) hei whakaheke i te tono ASP.NET Core 9 ki runga i OpenBSD.

### 1. Tākai Ngā Hiahia Tuatahi ki OpenBSD

```sh
pkg_add podman
pkg_add curl git
```

### 2. Tō Ata ASP.NET Core 9 Runtime

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Hangaia te Tono (ki runga i tētahi miihini hanga Linux rānei Windows)

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. Waihanga Kōnae Whirihoranga

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": { "EnableAzureAd": true, "EnableKeyVault": true, "EnableSecurityHeaders": true, "EnableMtls": false },
  "AzureAd": { "Instance": "https://login.microsoftonline.com/", "TenantId": "YOUR_TENANT_ID", "ClientId": "YOUR_CLIENT_ID", "CallbackPath": "/signin-oidc" },
  "AzureKeyVault": { "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/", "KeyVaultPassName": "ServerCert" }
}
```

### 5. Whakahaere Kaiwhakahaere

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

### 6. Whirihoranga Ahi Kūaha OpenBSD Packet Filter (pf)

```
pass in on egress proto tcp to port 443
pass out on egress proto tcp to port { 443 }
```

```sh
pfctl -f /etc/pf.conf
```

### 7. Ngā Ratonga Azure e hiahiatia ana Hononga Waho

| Ratonga | Pito |
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

## Tohutohu Whirihoranga

| Wāhanga | Kī | Whakamāramatanga |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Rēhitatanga tono Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault me te ingoa tiwhikete |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Kaupeka tiwhikete kiritaki mTLS |
| `NonceEncryption` | `Key`, `IV` | Kī 32-hurihia me IV 16-hurihia mō te whakamuna nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Hononga Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Hononga Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Whakaū OCSP (tauira) |
| `Logging` | `PiiHmacKey` | Kī HMAC base64 32-hurihia mō te heshi PII i ngā tāhū |

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
```

---

## Ngā Tuhinga Tautoko

| Tuhinga | Whāinga |
|---|---|
| `IVandKeySampleGenerator.ps1` | Waihanga tēpū AES tōkeke 32-hurihia me IV 16-hurihia (base64) |
| `HashInlineScriptPowerShell.ps1` | Tatau heshi SHA-256 mō ngā tuhinga o roto (mō te rārangi whakaaetanga CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Pērā ake, whakaputa heshi i te hōputu base64 |
| `CertificateUploaderToAzureExample.ps1` | Tukuna tiwhikete PFX ki Azure Key Vault |
| `CheckRoles.ps1` | Whakaū ngā tūtohu tūranga RBAC Azure mō te tono |
| `ExportResourceGroups.ps1` | Kaweake ngā whirihoranga rōpū rawa Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Tātaritia te hononga Cosmos DB |
| `SetupFromTemplate.ps1` | Aunoa te whirihoranga tīmata mai i `appsettings.template.json` |

---

## Ngā Tāpiritanga Haumaru

- **Kaua rawa e hono mea huna** ki te mana puna. Whakamahi .NET User Secrets ā-rohe me ngā Tautuhinga Tono Azure / ngā tohutoro Key Vault i te mahi hua.
- Ko te whakaūtia OCSP he **tauira** e huri ana i ngā tiwhikete katoa. Whakakapia `PerformOcspValidationAsync` i `OcspValidationService.cs` i mua i te whakahohea `EnableOcspValidation` i te mahi hua.
- **Kāore e tāhutia** ngā uara nonce — ko te tāhu i tētahi nonce i roto i te kuputuhi māmā ka taea ai e tētahi kaiwhakaeke me ngā mōhiotanga tāhū te kuru i ngā tuhinga o roto.
- Ka huna te upoko urupare `Server` ki `webserver` hei ārai i te whakaatu ake mōhiohio pūtake.
- Ko `AccessKeyId` me `SecretAccessKey` AWS **kaua rawa** e puta ana i `appsettings.json` — whakamahi Mea Huna Kaiwhakamahi, tāhūnga taiao, rānei ngā tūranga tauira IAM.
