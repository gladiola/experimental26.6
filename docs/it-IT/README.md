# WebAppExperimental266

Un'applicazione web ASP.NET Core 9 Razor Pages con autenticazione Azure AD, TLS mutuale (mTLS), gestione dei certificati tramite Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, GCP Secret Manager, GCP Firestore e un livello di sicurezza HTTP rafforzato con Content Security Policy basata su nonce.

---

## Indice

- [Funzionalità](#funzionalità)
- [Feature Flag](#feature-flag)
- [Prerequisiti](#prerequisiti)
- [Installazione – Windows Azure (App Service)](#installazione--windows-azure-app-service)
- [Installazione – Server OpenBSD con servizi Azure](#installazione--server-openbsd-con-servizi-azure)
- [Riferimento alla configurazione](#riferimento-alla-configurazione)
- [Script di supporto](#script-di-supporto)
- [Note sulla sicurezza](#note-sulla-sicurezza)

---

## Funzionalità

### Autenticazione Azure AD (OpenID Connect)
L'applicazione autentica gli utenti tramite la **Microsoft Identity Platform** usando il protocollo OpenID Connect (tramite `Microsoft.Identity.Web`). Tutte le route sotto `/Experimental` richiedono un'identità Azure AD autenticata. Le pagine `/Privacy`, `/Error` e `/About` sono accessibili pubblicamente.

### Autenticazione mTLS con certificato client
Quando abilitato, i client devono presentare un certificato X.509 valido. Le impostazioni in `MtlsSettings` controllano se sono consentiti certificati concatenati, auto-firmati o entrambi, la verifica della revoca dei certificati e gli emittenti di certificati consentiti.

### Integrazione con Azure Key Vault
L'applicazione recupera il **certificato del server** TLS da Azure Key Vault all'avvio. Il `X509Certificate2` caricato viene iniettato direttamente nella configurazione HTTPS di Kestrel, senza necessità di file PFX sul disco.

### Content Security Policy con nonce per richiesta
Quando abilitato, ogni risposta HTTP include un'intestazione `Content-Security-Policy` la cui direttiva `script-src` contiene un **nonce casuale crittograficamente sicuro** per ogni richiesta. La CSP supporta anche elenchi di autorizzazioni basati su hash SHA-256 per script inline.

### Intestazioni di sicurezza HTTP standard
`UseStandardSecurityHeaders` aggiunge a ogni risposta: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy` e rimuove le intestazioni `Server`, `X-Powered-By` e `X-AspNetMvc-Version`.

### Azure Blob Storage
Quando abilitato, `BlobSettingsService` fornisce un servizio Scoped supportato da una stringa di connessione e un numero massimo configurabile di allegati.

### Azure Cosmos DB
Quando abilitato, l'applicazione verifica la connessione a Cosmos DB all'avvio chiamando `database.ReadAsync()`.

### AWS Secrets Manager
Quando abilitato, `AwsSecretsManagerOperationsService` recupera segreti e certificati da AWS Secrets Manager. Configurazione nella sezione `AwsSecretsManager` con i parametri `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName` e le credenziali `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Quando abilitato, `AwsDynamoDbService` verifica la connettività alla tabella DynamoDB all'avvio. Configurazione nella sezione `AwsDynamoDb` con i parametri `Region`, `TableName` e le credenziali `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Quando abilitato, `GcpSecretManagerOperationsService` recupera segreti da Google Cloud Secret Manager. Configurazione nella sezione `GcpSecretManager` con i parametri `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId` e `CredentialFilePath` (opzionale, usa ADC se vuoto).

### GCP Firestore
Quando abilitato, `GcpFirestoreService` costruisce il client Firestore all'avvio. Configurazione nella sezione `GcpFirestore` con i parametri `ProjectId`, `DatabaseId` (predefinito: "(default)"), `CollectionName` e `CredentialFilePath` (opzionale).

### Gestione identità AWS Cognito
Quando abilitato, `AddAwsCognitoAuthentication` configura l'autenticazione OpenID Connect contro un **Amazon Cognito User Pool** — l'equivalente AWS di Microsoft Entra ID / Azure AD. L'endpoint di discovery OIDC è:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Configurazione nella sezione `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (salvare nei Segreti Utente) e `Domain` (dominio dell'interfaccia ospitata Cognito).

### GCP Identity Platform
Quando abilitato, `AddGcpIdentityAuthentication` configura l'autenticazione OpenID Connect utilizzando **Google OAuth 2.0 / OIDC** — l'equivalente GCP di Microsoft Entra ID / Azure AD. L'endpoint di discovery OIDC è:
`https://accounts.google.com/.well-known/openid-configuration`
Configurazione nella sezione `GcpIdentity`: `ClientId`, `ClientSecret` (salvare nei Segreti Utente) e `ProjectId` opzionale. Ottenere le credenziali nella **Google Cloud Console → API e servizi → Credenziali**.

### Gestione sicura delle sessioni
Le sessioni utilizzano una cache di memoria distribuita in-process con un **timeout di inattività di 30 minuti**. I cookie di sessione sono configurati come `HttpOnly`, `Secure = Always` e `SameSite = Strict`.

### Localizzazione
L'applicazione supporta **11 lingue**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU e ar-SA. L'arabo include il cambio automatico del layout RTL.

### Logging sicuro per i dati PII
`LoggingHelper` esegue l'hash delle informazioni di identificazione personale nell'output di log usando HMAC-SHA256. Una chiave stabile di 32 byte può essere fornita tramite `Logging:PiiHmacKey`.

---

## Feature Flag

Tutti i principali sottosistemi sono controllati da flag booleani in `appsettings.json`.

| Flag | Predefinito | Descrizione |
|---|---|---|
| `EnableSession` | `true` | Sessione lato server e cookie di sessione |
| `EnableLocalization` | `true` | Supporto multilingue (11 lingue) |
| `EnableAzureAd` | `true` | Autenticazione Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Criteri di autorizzazione a livello di route |
| `EnableKeyVault` | `false` | Carica il certificato TLS del server da Azure Key Vault |
| `EnableNonceServices` | `false` | Generazione di nonce CSP per richiesta |
| `EnableCSP` | `false` | Aggiungere l'intestazione `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Aggiungere le intestazioni di sicurezza HTTP standard |
| `EnableBlobStorage` | `false` | Servizio Azure Blob Storage |
| `EnableCosmosDb` | `false` | Servizio Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | Stub AWS Secrets Manager |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Gestione identità AWS Cognito (OpenID Connect) |
| `EnableGcpSecretManager` | `false` | Stub GCP Secret Manager |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Richiedere certificati TLS client |
| `EnableOcspValidation` | `false` | Verifica revoca OCSP (stub) |

---

## Prerequisiti

1. **Registrazione applicazione Azure AD** – con URI di reindirizzamento, segreto client o credenziale certificato.
2. **Azure Key Vault** – con il certificato PFX del server come segreto.
3. **Account Azure Cosmos DB** (opzionale).
4. **Account Azure Blob Storage** (opzionale).
5. **.NET 9 SDK / Runtime** – versione 9.0 o successiva.
6. **Credenziali AWS** (utente/ruolo IAM con permessi `secretsmanager` e `dynamodb`) – necessarie quando `EnableAwsSecretsManager` o `EnableAwsDynamoDb` sono abilitati.
7. **Account di servizio GCP o ADC** (con permessi `secretmanager` e `datastore`) – necessari quando `EnableGcpSecretManager` o `EnableGcpFirestore` sono abilitati.

---

## Installazione – Windows Azure (App Service)

### 1. Creare le risorse Azure

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

### 2. Registrare un'applicazione Azure AD

Nel [Portale di Azure](https://portal.azure.com):
1. Passare a **Microsoft Entra ID → Registrazioni app → Nuova registrazione**.
2. Impostare l'URI di reindirizzamento su `https://<your-app>.azurewebsites.net/signin-oidc`.
3. In **Certificati e segreti**, creare un segreto client e copiare il valore.
4. Annotare l'**ID tenant** e l'**ID client** dal pannello Panoramica.

### 3. Creare Azure Key Vault e caricare il certificato del server

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

### 4. Configurare le impostazioni dell'applicazione

Copiare `appsettings.template.json` in `appsettings.json` e compilare i valori segnaposto. I segreti **non devono** essere memorizzati nel controllo del codice sorgente — impostarli come Impostazioni applicazione di App Service o tramite User Secrets in locale:

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

### 5. Distribuire l'applicazione

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Abilitare HTTPS e dominio personalizzato (consigliato)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Abilitare mTLS su Azure App Service (facoltativo)

Azure App Service supporta i certificati client tramite il portale:
1. Andare a **App Service → Impostazioni TLS/SSL → Certificati client**.
2. Impostare **Certificati client in ingresso** su **Richiedi**.

Quindi impostare `FeatureFlags__EnableMtls=true` nelle Impostazioni applicazione.

---

## Installazione – Server OpenBSD con servizi Azure

> **Importante:** .NET 9 **non** ha una build ufficiale Microsoft per OpenBSD. Le istruzioni seguenti utilizzano un **contenitore compatibile con Linux** (tramite [Podman](https://podman.io/), disponibile nell'albero dei pacchetti di OpenBSD) per eseguire l'applicazione ASP.NET Core 9 su OpenBSD comunicando con i servizi Azure tramite HTTPS.

### 1. Installare i prerequisiti su OpenBSD

```sh
# As root
pkg_add podman
pkg_add curl git
```

Se né Podman né Docker è disponibile per la versione di OpenBSD in uso, considerare di eseguire l'app in una **VM Linux** (ad es., vmm(4) con un guest Debian/Ubuntu) e seguire il percorso di distribuzione Linux standard dall'interno di quel guest.

### 2. Scaricare l'immagine del runtime ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Compilare l'applicazione (su una macchina di build Linux o Windows)

Su una macchina con .NET 9 SDK installato, pubblicare una build self-contained per Linux x64:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

Trasferire la directory `publish/` all'host OpenBSD (ad es., tramite `scp` o un volume condiviso).

### 4. Creare un file di configurazione

Sull'host OpenBSD, creare `/etc/webappexp26/appsettings.json` con i valori di produzione (nessun segreto nel file; usare variabili d'ambiente):

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

I segreti vengono iniettati come variabili d'ambiente nel passaggio successivo.

### 5. Avviare il contenitore

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

### 6. Configurare il firewall OpenBSD Packet Filter (pf)

Aggiungere a `/etc/pf.conf` per consentire HTTPS in entrata e connessioni in uscita verso gli endpoint Azure:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Ricaricare il set di regole:

```sh
pfctl -f /etc/pf.conf
```

### 7. Configurare DNS e certificati TLS

Assicurarsi che il nome host in `AllowedHosts` si risolva nell'IP pubblico del server OpenBSD. Azure AD richiede che l'URI di reindirizzamento (`/signin-oidc`) sia raggiungibile tramite HTTPS, quindi il certificato del server deve essere attendibile. Usare un certificato di una CA pubblica (ad es., Let's Encrypt tramite `acme-client(1)`) o caricare un certificato firmato da CA in Azure Key Vault e abilitare `EnableKeyVault`.

### 8. Connettività in uscita verso i servizi Azure

I seguenti endpoint del servizio Azure devono essere raggiungibili dall'host OpenBSD tramite TCP 443:

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

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## Riferimento alla configurazione

Copiare `appsettings.template.json` in `appsettings.json` e sostituire tutti i valori `{{PLACEHOLDER}}`.

| Sezione | Chiave | Descrizione |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Registrazione app Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault e nome del certificato |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Criteri per il certificato client mTLS |
| `NonceEncryption` | `Key`, `IV` | Chiave a 32 byte e IV a 16 byte per la crittografia dei nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Connessione Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Connessione Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Validazione OCSP (stub) |
| `Logging` | `PiiHmacKey` | Chiave HMAC base64 a 32 byte per l'hashing dei dati PII nei log |

Generare chiavi di crittografia e IV utilizzando lo script PowerShell incluso:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Archiviare tutti i segreti in **.NET User Secrets** per lo sviluppo locale:

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

> Per GCP, impostare la variabile d'ambiente `GOOGLE_APPLICATION_CREDENTIALS` sul percorso del file JSON dell'account di servizio, oppure eseguire `gcloud auth application-default login` per lo sviluppo locale.

---

## Script di supporto

La directory `SupportingScripts/` contiene utilità PowerShell:

| Script | Scopo |
|---|---|
| `IVandKeySampleGenerator.ps1` | Genera una chiave AES casuale a 32 byte e un IV a 16 byte (base64) |
| `HashInlineScriptPowerShell.ps1` | Calcola gli hash SHA-256 per gli script inline (per le liste di autorizzazione CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Come sopra, genera gli hash in formato base64 |
| `CertificateUploaderToAzureExample.ps1` | Carica un certificato PFX in Azure Key Vault |
| `CheckRoles.ps1` | Verifica le assegnazioni di ruolo RBAC di Azure per l'app |
| `ExportResourceGroups.ps1` | Esporta le configurazioni dei gruppi di risorse di Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnostica la connettività di Cosmos DB |
| `SetupFromTemplate.ps1` | Automatizza la configurazione iniziale da `appsettings.template.json` |

---

## Note sulla sicurezza

- **Non memorizzare mai i segreti nel controllo del codice sorgente.**
- L'implementazione della validazione OCSP è uno **stub** che rifiuta tutti i certificati. Sostituire `PerformOcspValidationAsync` prima di abilitare `EnableOcspValidation` in produzione.
- I valori nonce non vengono **mai registrati** nei log.
- L'intestazione di risposta `Server` è mascherata con `webserver`.
- **Non salvare mai le credenziali AWS o GCP nel controllo del codice sorgente.** Usare le variabili d'ambiente o un gestore di segreti.
- Le implementazioni AWS e GCP sono **stub** che richiedono un'implementazione completa prima dell'uso in produzione.
- Per AWS, preferire i ruoli IAM alle chiavi di accesso hardcoded ove possibile.
- Per GCP, preferire l'Application Default Credentials (ADC) ai file di account di servizio espliciti.
