# WebAppExperimental266

Une application web ASP.NET Core 9 Razor Pages avec authentification Azure AD, TLS mutuel (mTLS), gestion des certificats via Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, GCP Secret Manager, GCP Firestore et une couche de sécurité HTTP renforcée avec une politique de sécurité du contenu basée sur des nonces.

---

## Table des matières

- [Fonctionnalités](#fonctionnalités)
- [Feature Flags](#feature-flags)
- [Prérequis](#prérequis)
- [Installation – Windows Azure (App Service)](#installation--windows-azure-app-service)
- [Installation – Serveur OpenBSD avec services Azure](#installation--serveur-openbsd-avec-services-azure)
- [Référence de configuration](#référence-de-configuration)
- [Scripts utilitaires](#scripts-utilitaires)
- [Notes de sécurité](#notes-de-sécurité)

---

## Fonctionnalités

### Authentification Azure AD (OpenID Connect)
L'application authentifie les utilisateurs via la **Plateforme d'identité Microsoft** en utilisant le protocole OpenID Connect (via `Microsoft.Identity.Web`). Toutes les routes sous `/Experimental` nécessitent une identité Azure AD authentifiée. Les pages `/Privacy`, `/Error` et `/About` sont accessibles publiquement.

### Authentification mTLS par certificat client
Lorsqu'il est activé, les clients doivent présenter un certificat X.509 valide. Les paramètres dans `MtlsSettings` contrôlent si les certificats chaînés, auto-signés ou les deux sont autorisés, la vérification de révocation des certificats et les émetteurs de certificats autorisés.

### Intégration Azure Key Vault
L'application récupère le **certificat serveur** TLS depuis Azure Key Vault au démarrage. Le `X509Certificate2` chargé est injecté directement dans la configuration HTTPS de Kestrel, sans nécessiter de fichier PFX sur le disque.

### Politique de sécurité du contenu avec nonces par requête
Lorsqu'il est activé, chaque réponse HTTP porte un en-tête `Content-Security-Policy` dont la directive `script-src` inclut un **nonce aléatoire cryptographiquement sécurisé** par requête. La CSP prend également en charge les listes d'autorisation basées sur des hachages SHA-256 pour les scripts en ligne.

### En-têtes de sécurité HTTP standard
`UseStandardSecurityHeaders` ajoute à chaque réponse : `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, ainsi que la suppression des en-têtes `Server`, `X-Powered-By` et `X-AspNetMvc-Version`.

### Azure Blob Storage
Lorsqu'il est activé, `BlobSettingsService` fournit un service Scoped alimenté par une chaîne de connexion et un nombre maximum configurable de pièces jointes.

### Azure Cosmos DB
Lorsqu'il est activé, l'application vérifie la connexion à Cosmos DB au démarrage en appelant `database.ReadAsync()`.

### AWS Secrets Manager
Lorsqu'il est activé, `AwsSecretsManagerOperationsService` récupère les secrets et certificats depuis AWS Secrets Manager. Configuration dans la section `AwsSecretsManager` avec les paramètres `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName` et les identifiants `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Lorsqu'il est activé, `AwsDynamoDbService` vérifie la connectivité à la table DynamoDB au démarrage. Configuration dans la section `AwsDynamoDb` avec les paramètres `Region`, `TableName` et les identifiants `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Lorsqu'il est activé, `GcpSecretManagerOperationsService` récupère les secrets depuis Google Cloud Secret Manager. Configuration dans la section `GcpSecretManager` avec les paramètres `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId` et `CredentialFilePath` (optionnel, utilise ADC si vide).

### GCP Firestore
Lorsqu'il est activé, `GcpFirestoreService` construit le client Firestore au démarrage. Configuration dans la section `GcpFirestore` avec les paramètres `ProjectId`, `DatabaseId` (défaut : "(default)"), `CollectionName` et `CredentialFilePath` (optionnel).

### Gestion d'identité AWS Cognito
Lorsqu'il est activé, `AddAwsCognitoAuthentication` configure l'authentification OpenID Connect contre un **Amazon Cognito User Pool** — l'équivalent AWS de Microsoft Entra ID / Azure AD. Le point de découverte OIDC est :
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Configuration dans la section `AwsCognito` : `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (stocker dans les Secrets Utilisateur) et `Domain` (domaine de l'interface hébergée Cognito).

### GCP Identity Platform
Lorsqu'il est activé, `AddGcpIdentityAuthentication` configure l'authentification OpenID Connect en utilisant **Google OAuth 2.0 / OIDC** — l'équivalent GCP de Microsoft Entra ID / Azure AD. Le point de découverte OIDC est :
`https://accounts.google.com/.well-known/openid-configuration`
Configuration dans la section `GcpIdentity` : `ClientId`, `ClientSecret` (stocker dans les Secrets Utilisateur) et `ProjectId` optionnel. Obtenir les identifiants dans la **Google Cloud Console → APIs et services → Identifiants**.

### Gestion de session sécurisée
Les sessions utilisent un cache mémoire distribué en processus avec un **délai d'inactivité de 30 minutes**. Les cookies de session sont configurés avec `HttpOnly`, `Secure = Always` et `SameSite = Strict`.

### Localisation
L'application prend en charge **11 langues** : en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU et ar-SA. L'arabe inclut un basculement automatique de la mise en page RTL.

### Journalisation sécurisée pour les données PII
`LoggingHelper` hache les informations personnelles identifiables dans la sortie de journalisation à l'aide de HMAC-SHA256. Une clé stable de 32 octets peut être fournie via `Logging:PiiHmacKey`.

---

## Feature Flags

Tous les sous-systèmes majeurs sont contrôlés par des flags booléens dans `appsettings.json`.

| Flag | Valeur par défaut | Description |
|---|---|---|
| `EnableSession` | `true` | Session côté serveur et cookie de session |
| `EnableLocalization` | `true` | Support multilingue (11 langues) |
| `EnableAzureAd` | `true` | Authentification Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Politiques d'autorisation au niveau des routes |
| `EnableKeyVault` | `false` | Charger le certificat TLS du serveur depuis Azure Key Vault |
| `EnableNonceServices` | `false` | Génération de nonce CSP par requête |
| `EnableCSP` | `false` | Ajouter l'en-tête `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Ajouter les en-têtes de sécurité HTTP standard |
| `EnableBlobStorage` | `false` | Service Azure Blob Storage |
| `EnableCosmosDb` | `false` | Service Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (stub) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Gestion d'identité AWS Cognito (OpenID Connect) |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (stub) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Exiger des certificats TLS clients |
| `EnableOcspValidation` | `false` | Vérification de révocation OCSP (stub) |

---

## Prérequis

1. **Enregistrement d'application Azure AD** – avec URI de redirection, secret client ou identifiant de certificat.
2. **Azure Key Vault** – contenant le certificat serveur PFX en tant que secret.
3. **Compte Azure Cosmos DB** (optionnel).
4. **Compte Azure Blob Storage** (optionnel).
5. **.NET 9 SDK / Runtime** – version 9.0 ou ultérieure.
6. **Identifiants AWS** (utilisateur/rôle IAM avec les permissions `secretsmanager` et `dynamodb`) – requis lorsque `EnableAwsSecretsManager` ou `EnableAwsDynamoDb` sont activés.
7. **Compte de service GCP ou ADC** (avec les permissions `secretmanager` et `datastore`) – requis lorsque `EnableGcpSecretManager` ou `EnableGcpFirestore` sont activés.

---

## Installation – Windows Azure (App Service)

### 1. Créer les ressources Azure

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

### 2. Enregistrer une application Azure AD

Dans le [Portail Azure](https://portal.azure.com) :
1. Accédez à **Microsoft Entra ID → Inscriptions d'applications → Nouvelle inscription**.
2. Définissez l'URI de redirection sur `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Sous **Certificats et secrets**, créez un secret client et copiez la valeur.
4. Notez l'**ID de locataire** et l'**ID client** depuis le panneau Vue d'ensemble.

### 3. Créer Azure Key Vault et charger le certificat serveur

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

### 4. Configurer les paramètres de l'application

Copiez `appsettings.template.json` vers `appsettings.json` et renseignez les valeurs des espaces réservés. Les secrets **ne doivent pas** être stockés dans le contrôle de code source — définissez-les comme paramètres d'application App Service ou via User Secrets en local :

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

### 5. Déployer l'application

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Activer HTTPS et le domaine personnalisé (recommandé)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Activer mTLS sur Azure App Service (optionnel)

Azure App Service prend en charge les certificats clients via le portail :
1. Accédez à **App Service → Paramètres TLS/SSL → Certificats clients**.
2. Définissez **Certificats clients entrants** sur **Obligatoire**.

Définissez ensuite `FeatureFlags__EnableMtls=true` dans les paramètres de l'application.

---

## Installation – Serveur OpenBSD avec services Azure

> **Important :** .NET 9 ne dispose **pas** de version officielle Microsoft pour OpenBSD. Les instructions ci-dessous utilisent un **conteneur compatible Linux** (via [Podman](https://podman.io/), disponible dans l'arborescence des paquets OpenBSD) pour exécuter l'application ASP.NET Core 9 sur OpenBSD tout en communiquant avec les services Azure via HTTPS.

### 1. Installer les prérequis sur OpenBSD

```sh
# As root
pkg_add podman
pkg_add curl git
```

Si ni Podman ni Docker n'est disponible pour votre version d'OpenBSD, envisagez d'exécuter l'application dans une **VM Linux** (p. ex., vmm(4) avec un hôte Debian/Ubuntu) et suivez le chemin de déploiement Linux standard depuis cet hôte.

### 2. Télécharger l'image du runtime ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Compiler l'application (sur une machine de build Linux ou Windows)

Sur une machine disposant du SDK .NET 9, publiez une build autonome ciblant Linux x64 :

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

Transférez le répertoire `publish/` vers l'hôte OpenBSD (p. ex., via `scp` ou un volume partagé).

### 4. Créer un fichier de configuration

Sur l'hôte OpenBSD, créez `/etc/webappexp26/appsettings.json` avec vos valeurs de production (pas de secrets dans le fichier ; utilisez des variables d'environnement à la place) :

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

Les secrets sont injectés en tant que variables d'environnement à l'étape suivante.

### 5. Démarrer le conteneur

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

### 6. Configurer le pare-feu OpenBSD Packet Filter (pf)

Ajoutez à `/etc/pf.conf` pour autoriser le HTTPS entrant et les connexions sortantes vers les endpoints Azure :

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Rechargez le jeu de règles :

```sh
pfctl -f /etc/pf.conf
```

### 7. Configurer le DNS et les certificats TLS

Assurez-vous que le nom d'hôte dans `AllowedHosts` résout vers l'IP publique du serveur OpenBSD. Azure AD exige que l'URI de redirection (`/signin-oidc`) soit accessible via HTTPS, donc le certificat serveur doit être approuvé. Utilisez un certificat d'une CA publique (p. ex., Let's Encrypt via `acme-client(1)`) ou téléchargez un certificat signé par CA dans Azure Key Vault et activez `EnableKeyVault`.

### 8. Connectivité sortante vers les services Azure

Les endpoints de service Azure suivants doivent être accessibles depuis l'hôte OpenBSD via TCP 443 :

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

Testez la connectivité avant de démarrer le conteneur :

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## Référence de configuration

Copiez `appsettings.template.json` vers `appsettings.json` et remplacez toutes les valeurs `{{PLACEHOLDER}}`.

| Section | Clé | Description |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Enregistrement d'application Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault et nom du certificat |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Politique de certificat client mTLS |
| `NonceEncryption` | `Key`, `IV` | Clé de 32 octets et IV de 16 octets pour le chiffrement des nonces (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Connexion Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Connexion Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Validation OCSP (stub) |
| `Logging` | `PiiHmacKey` | Clé HMAC base64 de 32 octets pour le hachage des données PII dans les journaux |

Générez les clés de chiffrement et les IVs à l'aide du script PowerShell inclus :

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Stockez tous les secrets dans **.NET User Secrets** pour le développement local :

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

> Pour GCP, définissez la variable d'environnement `GOOGLE_APPLICATION_CREDENTIALS` sur le chemin du fichier JSON du compte de service ou exécutez `gcloud auth application-default login` pour le développement local.

---

## Scripts utilitaires

Le répertoire `SupportingScripts/` contient des utilitaires PowerShell :

| Script | Objectif |
|---|---|
| `IVandKeySampleGenerator.ps1` | Générer une clé AES aléatoire de 32 octets et un IV de 16 octets (base64) |
| `HashInlineScriptPowerShell.ps1` | Calculer les hachages SHA-256 des scripts en ligne (pour la liste d'autorisation CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Comme ci-dessus, génère les hachages au format base64 |
| `CertificateUploaderToAzureExample.ps1` | Télécharger un certificat PFX vers Azure Key Vault |
| `CheckRoles.ps1` | Vérifier les attributions de rôles RBAC Azure pour l'application |
| `ExportResourceGroups.ps1` | Exporter les configurations des groupes de ressources Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnostiquer la connectivité Cosmos DB |
| `SetupFromTemplate.ps1` | Automatiser la configuration initiale depuis `appsettings.template.json` |

---

## Notes de sécurité

- **Ne jamais valider des secrets dans le contrôle de code source.**
- L'implémentation de validation OCSP est un **stub** qui rejette tous les certificats. Remplacez `PerformOcspValidationAsync` avant d'activer `EnableOcspValidation` en production.
- Les valeurs nonce ne sont **jamais journalisées**.
- L'en-tête de réponse `Server` est masqué par `webserver`.
- **Ne stockez jamais les identifiants AWS ou GCP dans le contrôle de version.** Utilisez des variables d'environnement ou un gestionnaire de secrets.
- Les implémentations AWS et GCP sont des **stubs** qui nécessitent une implémentation complète avant utilisation en production.
- Pour AWS, préférez les rôles IAM aux clés d'accès codées en dur lorsque c'est possible.
- Pour GCP, préférez Application Default Credentials (ADC) aux fichiers de compte de service explicites.
