# Carte de référence rapide — Modèle Razor Pages

## 🚀 Démarrage rapide (5 minutes)

```powershell
# 1. Exécuter le script de configuration
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Compiler et exécuter
dotnet build
dotnet run
```

## 📁 Fichiers de configuration

| Fichier | Rôle | Validé ? |
|---------|------|----------|
| `appsettings.template.json` | Modèle avec espaces réservés | ✅ Oui |
| `appsettings.json` | Votre configuration réelle | ❌ Non (ignoré par git) |
| Secrets utilisateur | Valeurs sensibles | ❌ Non (local uniquement) |

## 🚩 Feature Flags (activation/désactivation rapide)

Modifiez `appsettings.json` → section `FeatureFlags` :

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // 🔐 Activer pour l'authentification
  "EnableNonceServices": false,  // 🛡️ Activer pour la CSP
  "EnableCosmosDb": false,       // 🗄️ Activer pour la base de données
  "EnableBlobStorage": false     // 📦 Activer pour les fichiers
}
```

## 🔑 Commandes pour les secrets utilisateur

```powershell
# Initialiser
dotnet user-secrets init

# Définir un secret
dotnet user-secrets set "AzureAd:ClientSecret" "votre-secret"

# Lister tous les secrets
dotnet user-secrets list

# Supprimer un secret
dotnet user-secrets remove "AzureAd:ClientSecret"

# Effacer tous les secrets
dotnet user-secrets clear
```

## 🔐 Secrets requis par fonctionnalité

### Authentification Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "votre-secret-client"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "votre-secret-client"
```

### Nonce/CSP
```powershell
# Générer d'abord : .\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "votre-clé-base64-32-octets"
dotnet user-secrets set "NonceEncryption:IV" "votre-iv-base64-16-octets"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "votre-chaîne-de-connexion"
dotnet user-secrets set "CosmosDb:AccountKey" "votre-clé-de-compte"
```

### Stockage Blob
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "votre-chaîne-de-connexion"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "votre-secret"
```

## 🛠️ Scripts utiles

| Script | Rôle | Utilisation |
|--------|------|-------------|
| `SetupFromTemplate.ps1` | Configuration initiale | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Changer l'espace de noms | `.\RenameNamespace.ps1 -NewNamespace "MonApp"` |
| `IVandKeySampleGenerator.ps1` | Générer des clés | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Calculer les hachages CSP | `.\HashInlineScriptPowerShell.ps1` |

## 📋 Phases de développement

### Phase 1 : De base (5 min de configuration)
- ✅ Session
- ✅ Localisation
- ✅ En-têtes de sécurité
- ❌ Pas d'authentification
- ❌ Pas de base de données

**Config** : Tous les flags à `false` sauf `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Phase 2 : + Authentification (30 min de configuration)
- ✅ Fonctionnalités Phase 1
- ✅ Azure AD
- ✅ Autorisation
- ✅ CSP + Nonce
- ❌ Pas de base de données

**Config** : Activer `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

**Prérequis** :
- Enregistrement d'application Azure AD
- Clés de chiffrement générées

### Phase 3 : + Services Azure (1-2 h de configuration)
- ✅ Fonctionnalités Phase 2
- ✅ Cosmos DB
- ✅ Stockage Blob
- ✅ Key Vault

**Config** : Activer `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

**Prérequis** :
- Ressources Azure créées
- Chaînes de connexion dans les secrets utilisateur

## 🔧 Dépannage rapide

### Erreurs de compilation
```powershell
# Nettoyer et recompiler
dotnet clean
dotnet build

# Vérifier les paquets manquants
dotnet restore
```

### « Configuration introuvable »
```powershell
# Vérifier que le fichier existe
Test-Path appsettings.json

# Si manquant, copier depuis le modèle
Copy-Item appsettings.template.json appsettings.json
```

### « Secret introuvable »
```powershell
# Lister les secrets
dotnet user-secrets list

# Relancer la configuration
.\SupportingScripts\SetupFromTemplate.ps1
```

### Boucle d'authentification / erreurs 401
1. Vérifier que l'URI de redirection Azure AD correspond
2. Vérifier `EnableAzureAd: true` dans appsettings.json
3. Vérifier le secret client dans les secrets utilisateur
4. Effacer les cookies du navigateur

### Violations CSP
1. Vérifier `EnableNonceServices: true`
2. Vérifier que les clés de chiffrement sont définies
3. Consulter la console du navigateur pour les erreurs CSP
4. Désactiver temporairement la CSP pour tester : `EnableCSP: false`

## 📚 Documentation

- **Documentation complète** : `TEMPLATE_README.md`
- **Configuration** : `appsettings.template.json`
- **Espace de noms** : Exécuter `.\RenameNamespace.ps1 -NewNamespace "VotreEspaceDeNoms"`

## ✅ Liste de contrôle de sécurité

Avant le déploiement en production :

- [ ] Tous les secrets dans Azure Key Vault ou les secrets utilisateur
- [ ] `appsettings.json` est ignoré par git
- [ ] `.gitignore` inclut les exclusions spécifiques au modèle
- [ ] En-têtes de sécurité activés
- [ ] CSP configurée avec des nonces
- [ ] HTTPS imposé
- [ ] Authentification activée pour les pages protégées
- [ ] Secrets changés par rapport aux valeurs par défaut

## 💡 Conseils

- **Commencez simplement** : Démarrez avec la Phase 1, ajoutez les fonctionnalités progressivement
- **Utilisez WhatIf** : Testez les scripts avec `-WhatIf` avant d'appliquer
- **Consultez les journaux** : Activez `"Default": "Debug"` dans `Logging:LogLevel` pour le débogage
- **Vérifiez les secrets** : Exécutez `dotnet user-secrets list` pour voir ce qui est configuré
- **Nettoyez les compilations** : En cas d'erreurs bizarres, essayez `dotnet clean && dotnet build`

## ❓ Aide

1. Lisez `TEMPLATE_README.md`
2. Consultez les commentaires dans `appsettings.template.json`
3. Exécutez `dotnet user-secrets list`
4. Activez la journalisation de débogage
5. Vérifiez l'état des ressources dans le portail Azure

---

**Version du modèle** : 1.0  
**ASP.NET Core** : 9.0  
**Dernière mise à jour** : 2024-12-20
