# Guide d'authentification par certificat client mTLS (TLS mutuel)

## Vue d'ensemble

Ce projet prend désormais en charge l'authentification **mTLS (TLS mutuel)**, qui exige que le serveur et le client présentent tous deux des certificats valides. Cela offre une sécurité renforcée grâce à une authentification bidirectionnelle.

## Qu'est-ce que mTLS ?

mTLS étend le TLS standard en exigeant :
1. **Certificat serveur** : Le serveur présente un certificat pour prouver son identité (HTTPS standard)
2. **Certificat client** : Le client présente également un certificat pour prouver son identité (ajout mTLS)

## Configuration

### 1. Feature Flag

Activer mTLS dans `appsettings.json` :

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Paramètres mTLS

Configurer le comportement mTLS dans `appsettings.json` :

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "mon-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

#### Options de configuration

| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| `RequireClientCertificate` | bool | `true` | Si vrai, le certificat client est obligatoire |
| `AllowCertificateChains` | bool | `true` | Autoriser les certificats enchaînés (signés par une AC) |
| `AllowSelfSignedCertificates` | bool | `false` | Autoriser les certificats auto-signés (développement uniquement) |
| `CheckCertificateRevocation` | bool | `false` | Effectuer une vérification de révocation en ligne |
| `ClientCertificateName` | string | null | Nom du certificat dans Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Valider l'émetteur du certificat |

### 3. Certificat serveur (Azure Key Vault)

Le certificat serveur est récupéré depuis Azure Key Vault :

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://votre-keyvault.vault.azure.net/",
    "KeyVaultSecret": "nom-cert-serveur",
    "KeyVaultPassName": "mot-de-passe-cert-serveur"
  }
}
```

## Instructions de configuration

### Prérequis

1. Azure Key Vault avec les autorisations appropriées
2. Certificat serveur stocké dans Azure Key Vault en tant que secret (format PFX)
3. Certificats clients (peuvent être générés ou obtenus auprès d'une AC)

### Étape 1 : Télécharger le certificat serveur dans Key Vault

```bash
# Convertir le certificat en PFX si nécessaire
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Télécharger dans Key Vault avec Azure CLI
az keyvault secret set --vault-name "votre-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Stocker le mot de passe en tant que secret séparé
az keyvault secret set --vault-name "votre-keyvault" --name "server-cert-password" --value "votre-mot-de-passe"
```

### Étape 2 : Générer des certificats clients

#### Option A : Auto-signé (développement uniquement)

```powershell
# Générer un certificat client
$cert = New-SelfSignedCertificate `
    -Subject "CN=MonClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Exporter en PFX
$password = ConvertTo-SecureString -String "MotDePasseCertClient" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Option B : Signé par une AC (production)

Travaillez avec votre autorité de certification pour obtenir des certificats clients.

### Étape 3 : Configurer l'application

Mettre à jour `appsettings.json` :

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://votre-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert",
    "KeyVaultPassName": "server-cert-password"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false
  }
}
```

### Étape 4 : Tester avec le certificat client

#### Avec cURL :

```bash
curl --cert client.pem --key client.key https://votre-app.azurewebsites.net
```

#### Avec PowerShell :

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://votre-app.azurewebsites.net" -Certificate $cert
```

#### Avec un navigateur :

1. Importer le certificat client dans le magasin de certificats du navigateur
2. Accéder à votre application
3. Le navigateur demandera de sélectionner un certificat client

## Comportement selon l'environnement

### Développement
- Le certificat serveur est chargé depuis Key Vault (si disponible)
- Les certificats clients sont **facultatifs** (mode `AllowCertificate`)
- Les certificats auto-signés peuvent être autorisés

### Production
- Le certificat serveur est chargé depuis Key Vault
- Les certificats clients sont **obligatoires** si `EnableMtls = true`
- Seuls les certificats enchaînés sont recommandés

## Bonnes pratiques de sécurité

### ✅ À FAIRE :
- Utiliser des certificats signés par une AC en production
- Stocker les certificats dans Azure Key Vault
- Activer la vérification de révocation des certificats en production
- Valider l'émetteur du certificat
- Utiliser des mots de passe forts pour les fichiers PFX
- Effectuer une rotation régulière des certificats

### ❌ À NE PAS FAIRE :
- Utiliser des certificats auto-signés en production
- Valider les certificats dans le contrôle de source
- Partager des certificats clients entre utilisateurs
- Désactiver la validation des certificats en production

## Dépannage

### Erreur : « Aucun certificat client fourni »

**Cause** : Le client n'a pas envoyé de certificat  
**Solution** :
- Vérifier que le certificat client est installé
- Vérifier le paramètre `RequireClientCertificate`
- S'assurer que le certificat est approuvé par le système

### Erreur : « La validation de la chaîne de certificat a échoué »

**Cause** : Certificat non approuvé  
**Solution** :
- Installer le certificat racine de l'AC
- Définir `AllowSelfSignedCertificates = true` pour les tests
- Vérifier que le certificat n'a pas expiré

### Erreur : « Le certificat serveur n'a pas été récupéré depuis Key Vault »

**Cause** : Problème d'accès à Azure Key Vault  
**Solution** :
- Vérifier les autorisations Key Vault
- Vérifier les informations d'identification Azure AD
- S'assurer que l'identité managée est configurée

## Journalisation

Les événements d'authentification mTLS sont journalisés :

```
[Information] mTLS activé — Certificats clients OBLIGATOIRES
[Information] Authentification mTLS RÉUSSIE pour le certificat : CN=MonClient
[Error] Authentification mTLS ÉCHOUÉE : Échec de la validation du certificat
```

## Intégration avec l'authentification existante

mTLS fonctionne conjointement avec l'authentification Azure AD :

1. **Validation du certificat client** se produit en premier (couche transport)
2. **Authentification Azure AD** se produit ensuite (couche application)

Les deux peuvent être activés simultanément pour une sécurité en profondeur.

## Références

- [Microsoft Docs : Authentification par certificat](https://learn.microsoft.com/fr-fr/aspnet/core/security/authentication/certauth)
- [Intégration Azure Key Vault](https://learn.microsoft.com/fr-fr/azure/app-service/configure-ssl-certificate-in-code)

## Exemple de code

L'implémentation se trouve dans :
- `Models/Settings/MtlsSettings.cs` — Modèle de configuration
- `Models/Settings/FeatureFlags.cs` — Feature flag
- `Extensions/ServiceCollectionExtensions.cs` — Enregistrement du service
- `Program.cs` — Démarrage de l'application

## Ressources supplémentaires

Consultez `SupportingScripts/CertificateUploaderToAzureExample.ps1` pour des exemples de téléchargement de certificats.
