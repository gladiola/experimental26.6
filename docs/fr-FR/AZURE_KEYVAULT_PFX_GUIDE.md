# Guide des certificats PFX Azure Key Vault

## Date : 2024-12-20

## Vue d'ensemble

Ce guide documente l'**approche correcte** pour stocker et récupérer des certificats PFX complets (avec clés privées) dans Azure Key Vault, en s'appuyant sur les leçons tirées d'une implémentation en production.

---

## ⚠️ **Erreurs courantes à éviter**

### ❌ **INCORRECT : Stocker le PFX en tant que secret Base64**

```powershell
# NE PAS FAIRE — Cela ne fonctionne pas !
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Pourquoi cela échoue :**
1. **Limite de taille** : Les secrets Key Vault ont une limite de 25 Ko — les fichiers PFX la dépassent souvent
2. **Problèmes d'encodage** : L'encodage Base64 peut introduire des sauts de ligne et des corruptions
3. **Type incompatible** : Les secrets sont pour des chaînes simples, pas des données binaires de certificat
4. **Pas de métadonnées de certificat** : Les dates d'expiration, informations de sujet, etc. sont perdues

---

## ✅ **CORRECT : Utiliser les API spécifiques aux certificats**

### **Méthode 1 : Importer le certificat directement (recommandé)**

C'est la **meilleure approche** et celle qui fonctionne actuellement dans le code source.

#### Télécharger le certificat (PowerShell)

```powershell
# Définir les variables
$vaultName = "nom-de-votre-keyvault"
$certificateName = "server-cert"
$pfxFilePath = "C:\chemin\vers\votre\certificat.pfx"
$plainPassword = "votre-mot-de-passe-pfx"

# Convertir le mot de passe en SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Importer le certificat dans Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Avantages :**
- ✅ Gère les certificats de toute taille
- ✅ Préserve toutes les métadonnées du certificat
- ✅ Crée automatiquement une version secrète avec la clé privée
- ✅ Prend en charge la rotation des certificats
- ✅ S'intègre avec Azure RBAC et les stratégies d'accès

#### Récupérer le certificat (C#)

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public async Task<X509Certificate2?> GetCertificateFromKeyVaultAsync(
    string tenantId,
    string clientId,
    string clientSecret,
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // Créer les informations d'identification
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // Initialiser le client de certificat
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        
        // Obtenir le certificat (clé publique et métadonnées)
        KeyVaultCertificateWithPolicy certificate = 
            await certificateClient.GetCertificateAsync(certificateName);
        
        // Obtenir le secret contenant la clé privée
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        // La valeur du secret est le PKCS12 (PFX) encodé en Base64 avec la clé privée
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        // Créer X509Certificate2 avec la clé privée
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // Pas de mot de passe nécessaire — Key Vault gère le déchiffrement
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Erreur lors du chargement du certificat PFX depuis Key Vault");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erreur inattendue lors de la récupération du certificat");
        return null;
    }
}
```

---

### **Méthode 2 : Utiliser l'identité managée (production)**

Pour les environnements de production, utilisez l'**identité managée** plutôt que les secrets client.

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // Utiliser DefaultAzureCredential — utilise automatiquement l'identité managée dans Azure
        var credential = new DefaultAzureCredential();
        
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        var certificate = await certificateClient.GetCertificateAsync(certificateName);
        
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        var secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erreur lors de la récupération du certificat avec l'identité managée");
        return null;
    }
}
```

---

## 🔧 **Implémentation dans WebAppExperimental266**

### État actuel de l'implémentation

**Emplacement :** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**État :** ⚠️ Implémentation modèle — nécessite du code de production

**Code actuel (modèle) :**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // Implémentation modèle — les utilisateurs doivent implémenter selon leur configuration Key Vault
    _logger.LogWarning("GetCertificateFromKeyVault appelé — implémenter cette méthode pour la production");
    
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### Mise à jour recommandée

Remplacer par l'implémentation prête pour la production :

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public class AzureKeyVaultCertificateOperations : IAzureKeyVaultCertificateOperations
{
    private readonly ILogger<AzureKeyVaultCertificateOperations> _logger;

    public AzureKeyVaultCertificateOperations(ILogger<AzureKeyVaultCertificateOperations> logger)
    {
        _logger = logger;
    }

    public async Task<X509Certificate2?> GetCertificateFromKeyVault(
        string tenantId,
        string clientId,
        string keyVaultURL,
        string certificateName,
        string certPasswordName)
    {
        try
        {
            _logger.LogInformation("Récupération du certificat '{CertName}' depuis Key Vault", certificateName);
            
            // Option 1 : Utiliser DefaultAzureCredential (recommandé pour la production)
            var credential = new DefaultAzureCredential();
            
            // Option 2 : Utiliser ClientSecretCredential (si vous avez un secret client)
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // Obtenir les métadonnées du certificat
            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate = 
                await certificateClient.GetCertificateAsync(certificateName);
            
            _logger.LogDebug("Certificat trouvé. Empreinte : {Thumbprint}, Expiration : {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);
            
            // Obtenir le secret contenant la clé privée
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
            
            // Convertir le PKCS12 Base64 en X509Certificate2
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            
            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null, // Key Vault gère le déchiffrement
                X509KeyStorageFlags.MachineKeySet | 
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.PersistKeySet);
            
            _logger.LogInformation("Certificat avec clé privée chargé avec succès");
            
            return x509Certificate;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Erreur cryptographique lors du chargement du certificat '{CertName}'", certificateName);
            return null;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Erreur Azure Key Vault : {StatusCode} - {Message}", 
                ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de la récupération du certificat");
            return null;
        }
    }

    public async Task<KeyVaultSecret> GetSecretFromKeyVault(
        string tenantId,
        string clientId,
        string clientSecret,
        string keyVaultURL,
        string secretName)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            
            return await secretClient.GetSecretAsync(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du secret '{SecretName}'", secretName);
            throw;
        }
    }
}
```

---

## 📦 **Paquets NuGet requis**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Remarque :** Déjà installés dans le projet WebAppExperimental266.

---

## ⚙️ **Configuration**

### appsettings.json

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://votre-keyvault.vault.azure.net/",
    "KeyVaultSecret": "{{USE_USER_SECRETS}}",
    "KeyVaultPassName": "server-cert"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "ClientCertificateName": "client-cert"
  }
}
```

### Secrets utilisateur

```powershell
# Pour l'authentification par secret client
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "votre-secret-client"

# Pour l'identité managée (production)
# Aucun secret nécessaire — l'identité est gérée par Azure
```

---

## 🔐 **Stratégies d'accès Azure Key Vault**

### Autorisations requises

Pour l'identité de l'application (principal de service ou identité managée) :

**Autorisations sur les certificats :**
- ✅ Obtenir
- ✅ Lister

**Autorisations sur les secrets :**
- ✅ Obtenir
- ✅ Lister

**Pourquoi des autorisations sur les certificats ET les secrets ?**
- Les autorisations sur les certificats permettent d'obtenir les métadonnées
- Les autorisations sur les secrets permettent d'obtenir la clé privée

### Configuration via le portail Azure

1. Accéder à Key Vault → Stratégies d'accès
2. Cliquer sur « Ajouter une stratégie d'accès »
3. Sélectionner les autorisations de certificat : Obtenir, Lister
4. Sélectionner les autorisations de secret : Obtenir, Lister
5. Sélectionner le principal (votre application ou identité managée)
6. Enregistrer

### Configuration via Azure CLI

```bash
# Obtenir l'ID d'objet de votre application ou identité managée
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# Accorder les autorisations
az keyvault set-policy \
  --name votre-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 **Tester l'implémentation**

### Exemple de test unitaire

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    // Arrange
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);
    
    // Act
    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "votre-tenant-id",
        clientId: "votre-client-id",
        keyVaultURL: "https://votre-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "non-utilisé");
    
    // Assert
    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### Test d'intégration

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    // Nécessite des ressources Azure réelles
    var keyVaultUrl = TestConfiguration["AzureKeyVault:KeyVaultURL"];
    var certName = TestConfiguration["AzureKeyVault:CertificateName"];
    
    var operations = new AzureKeyVaultCertificateOperations(_logger);
    
    var cert = await operations.GetCertificateFromKeyVault(
        tenantId: TestConfiguration["AzureAd:TenantId"],
        clientId: TestConfiguration["AzureAd:ClientId"],
        keyVaultURL: keyVaultUrl,
        certificateName: certName,
        certPasswordName: "");
    
    Assert.NotNull(cert);
    Assert.True(cert.HasPrivateKey, "Le certificat doit avoir une clé privée");
}
```

---

## 🔗 **Utilisation avec mTLS**

### Intégration avec l'authentification par certificat

```csharp
// Dans Program.cs
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    // Récupérer le certificat serveur depuis Key Vault
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();
    
    if (serverCertificate != null)
    {
        // Configurer Kestrel pour utiliser le certificat
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = serverCertificate;
            });
        });
        
        logger.LogInformation("mTLS activé avec le certificat Key Vault");
    }
}
```

---

## 📊 **Comparaison : Secret vs stockage de certificat**

| Fonctionnalité | Stocker comme secret | Stocker comme certificat |
|----------------|---------------------|--------------------------|
| **Limite de taille** | 25 Ko | Illimitée |
| **Clé privée** | ❌ Gestion manuelle | ✅ Automatique |
| **Métadonnées** | ❌ Aucune | ✅ Informations complètes |
| **Rotation** | ❌ Manuelle | ✅ Intégrée |
| **Expiration** | ❌ Suivi manuel | ✅ Suivi automatique |
| **RBAC** | Basique | Spécifique aux certificats |
| **Complexité** | Élevée | Faible |
| **Recommandation** | ❌ Ne pas utiliser | ✅ **Utiliser ceci** |

---

## 🔄 **Rotation des certificats**

### Rotation automatique

Les certificats Key Vault prennent en charge la rotation automatique :

```powershell
# Configurer la stratégie de rotation automatique
az keyvault certificate set-policy `
    --vault-name votre-keyvault `
    --name server-cert `
    --policy @policy.json
```

policy.json :
```json
{
  "lifetimeActions": [
    {
      "trigger": {
        "daysBeforeExpiry": 30
      },
      "action": {
        "actionType": "AutoRenew"
      }
    }
  ]
}
```

### Code applicatif

Votre application obtient automatiquement la dernière version :

```csharp
// Obtient toujours la version actuelle
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Pour obtenir une version spécifique :
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "id-de-version-spécifique");
```

---

## 🛠️ **Dépannage**

### Erreur : « Certificat introuvable »

**Vérifier :**
1. Le nom du certificat est correct
2. Le certificat existe dans Key Vault
3. Les stratégies d'accès sont configurées

```bash
# Lister les certificats
az keyvault certificate list --vault-name votre-keyvault
```

### Erreur : « Accès refusé »

**Vérifier :**
1. Le principal de service a les autorisations correctes
2. Les autorisations sur les certificats ET les secrets sont accordées
3. L'identité managée est activée (si utilisée)

```bash
# Vérifier les stratégies d'accès
az keyvault show --name votre-keyvault --query properties.accessPolicies
```

### Erreur : « Le certificat n'a pas de clé privée »

**Vérifier :**
1. Utiliser `.GetSecretAsync()` et non seulement `.GetCertificateAsync()`
2. Le certificat a été importé avec la clé privée
3. La version correcte du secret est utilisée

```csharp
// INCORRECT — Pas de clé privée
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // Clé publique uniquement

// CORRECT — Avec clé privée
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // Avec clé privée
```

### Erreur : « CryptographicException »

**Causes courantes :**
1. Données PFX corrompues
2. Format de certificat incorrect
3. Mot de passe incorrect (ne devrait pas être nécessaire pour KV)

---

## ✅ **Liste de contrôle de migration**

- [ ] Installer les paquets NuGet requis
- [ ] Mettre à jour `AzureKeyVaultCertificateOperations.cs` avec le code de production
- [ ] Importer le certificat dans Key Vault avec `Import-AzKeyVaultCertificate`
- [ ] Configurer les stratégies d'accès (Certificat : Get/List, Secret : Get/List)
- [ ] Mettre à jour la configuration dans `appsettings.json`
- [ ] Configurer l'identité managée (production) ou le secret client (développement)
- [ ] Tester la récupération du certificat
- [ ] Vérifier que la clé privée est présente
- [ ] Tester mTLS avec le certificat récupéré
- [ ] Configurer la stratégie de rotation des certificats
- [ ] Documenter les procédures de gestion des certificats

---

## 📋 **Résumé**

### ✅ **À FAIRE :**
- Utiliser `Import-AzKeyVaultCertificate` pour télécharger le PFX
- Utiliser `CertificateClient` + `SecretClient` pour récupérer
- Utiliser l'identité managée en production
- Accorder les autorisations sur les certificats ET les secrets
- Vérifier que le certificat a une clé privée

### ❌ **À NE PAS FAIRE :**
- Stocker le PFX en tant que secret Base64
- Essayer de gérer manuellement les données de certificat
- Utiliser des secrets client en production
- Oublier d'accorder les autorisations sur les secrets
- Ignorer les dates d'expiration des certificats

---

## 📚 **Références**

- [Vue d'ensemble des certificats Azure Key Vault](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Paquet Azure.Security.KeyVault.Certificates](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Documentation sur l'identité managée](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Classe X509Certificate2](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**État :** ✅ Guide complet  
**Dernière mise à jour :** 2024-12-20  
**Version :** 1.0  
**Projet :** WebAppExperimental266
