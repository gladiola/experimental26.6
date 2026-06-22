# Guide d'implémentation OCSP (Online Certificate Status Protocol)

## Vue d'ensemble

Ce projet inclut un **support modèle** pour la validation des certificats OCSP. L'OCSP permet de vérifier en temps réel le statut de révocation des certificats avant de traiter les requêtes web.

## Qu'est-ce que l'OCSP ?

L'OCSP offre une alternative aux listes de révocation de certificats (CRL) pour vérifier si un certificat a été révoqué :

- **Validation en temps réel** : Vérifie le statut du certificat immédiatement
- **Efficace** : Interroge uniquement le statut des certificats spécifiques
- **Léger** : Réponses plus petites que les téléchargements complets de CRL
- **À jour** : Toujours en possession des informations de révocation actuelles

## Configuration

### 1. Feature Flag

Activer la validation OCSP dans `appsettings.json` :

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Paramètres OCSP

Configurer le comportement OCSP dans `appsettings.json` :

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.votreentreprise.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

### Options de configuration

| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| `EnableOcspValidation` | bool | `false` | Activer/désactiver la validation OCSP |
| `OcspServerUrl` | string | `null` | URL de votre serveur répondant OCSP |
| `RequestTimeoutSeconds` | int | `30` | Délai d'attente pour les requêtes OCSP |
| `MaxRetryAttempts` | int | `3` | Nombre de nouvelles tentatives en cas d'échec |
| `CacheDurationMinutes` | int | `60` | Durée de mise en cache des réponses OCSP |
| `ServerUnavailableBehavior` | string | `"Warn"` | Comportement quand le serveur est indisponible : `"Fail"`, `"Allow"` ou `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Activer la journalisation détaillée |
| `SkipValidationInDevelopment` | bool | `true` | Ignorer l'OCSP en mode développement |

---

## Implémentation modèle

L'implémentation actuelle est un **modèle** qui démontre la structure et la conception de l'API. Pour utiliser l'OCSP en production, vous devez :

### 1. Implémenter le protocole OCSP

Remplacer la méthode modèle `PerformOcspValidationAsync` dans `OcspValidationService.cs` par une implémentation réelle du protocole OCSP :

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO : Implémenter le protocole OCSP réel
    // 1. Construire la requête OCSP
    // 2. Envoyer au serveur OCSP
    // 3. Analyser la réponse OCSP
    // 4. Valider la signature de la réponse
    // 5. Retourner le statut du certificat
}
```

### 2. Construire un serveur OCSP

Vous avez besoin d'un serveur répondant OCSP séparé qui :
- Accepte les requêtes OCSP (format RFC 6960)
- Vérifie le statut du certificat dans votre base de données AC
- Retourne des réponses OCSP signées

**Options :**
- Utiliser un service OCSP commercial (par ex. DigiCert, Let's Encrypt)
- Construire un répondant OCSP personnalisé avec des bibliothèques :
  - **OpenSSL** — Bibliothèque C/C++ avec support OCSP
  - **BouncyCastle** — Bibliothèque .NET pour OCSP
  - **Python** — Bibliothèque `cryptography` avec support OCSP

---

## Exemple d'utilisation

### Validation de certificat de base

```csharp
public class MonGestionnaireDeCertificat
{
    private readonly IOcspValidationService _ocspService;

    public MonGestionnaireDeCertificat(IOcspValidationService ocspService)
    {
        _ocspService = ocspService;
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
    {
        // Vérification booléenne simple
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### Validation avec détails de statut

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    // Vérifier le statut
    switch (result.Status)
    {
        case OcspStatus.Good:
            logger.LogInformation("Le certificat est valide");
            return result;

        case OcspStatus.Revoked:
            logger.LogError("Le certificat a été révoqué !");
            throw new SecurityException("Certificat révoqué");

        case OcspStatus.Unknown:
            logger.LogWarning("Statut du certificat inconnu");
            // Traiter selon la politique
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("Serveur OCSP indisponible");
            // Comportement de repli selon le paramètre ServerUnavailableBehavior
            break;
    }

    return result;
}
```

---

## Intégration avec mTLS

L'OCSP fonctionne de façon transparente avec l'authentification par certificat mTLS :

```csharp
// Dans ServiceCollectionExtensions.cs
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// Dans l'événement de validation du certificat
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        // Effectuer la validation OCSP
        var ocspService = context.HttpContext.RequestServices
            .GetRequiredService<IOcspValidationService>();

        var isValid = await ocspService.ValidateCertificateAsync(
            context.ClientCertificate);

        if (!isValid)
        {
            context.Fail("Échec de la validation du certificat via OCSP");
        }
    }
};
```

---

## Comportements en cas d'indisponibilité du serveur

### « Fail » — Sécurité stricte

```json
"ServerUnavailableBehavior": "Fail"
```

- Rejette les requêtes quand le serveur OCSP est indisponible
- Option la plus sécurisée
- Peut causer des problèmes de disponibilité

**Utiliser quand :** La sécurité maximale est requise, la validation des certificats est critique

### « Allow » — Haute disponibilité

```json
"ServerUnavailableBehavior": "Allow"
```

- Autorise les requêtes quand le serveur OCSP est indisponible
- Priorise la disponibilité sur la sécurité
- Journalise des avertissements

**Utiliser quand :** La disponibilité du service est plus importante que la validation en temps réel

### « Warn » — Équilibré (défaut)

```json
"ServerUnavailableBehavior": "Warn"
```

- Autorise les requêtes mais journalise des avertissements
- Approche équilibrée
- Permet la surveillance et les alertes

**Utiliser quand :** Vous souhaitez surveiller les problèmes OCSP sans bloquer le trafic

---

## Mise en cache

Les réponses OCSP sont mises en cache pour réduire la charge du serveur :

```json
"CacheDurationMinutes": 60
```

**Avantages :**
- Réduit les requêtes vers le serveur OCSP
- Améliore les performances
- Offre une résilience lors de brèves interruptions

**Invalidation du cache :**
- Automatique après expiration de la durée de cache
- Manuelle : redémarrer l'application

---

## Considérations de sécurité

### ✅ À FAIRE :

- Utiliser HTTPS pour l'URL du serveur OCSP
- Valider les signatures des réponses OCSP
- Définir une durée de cache appropriée (équilibre entre fraîcheur et performance)
- Utiliser le comportement « Fail » dans les environnements hautement sécurisés
- Surveiller la disponibilité du serveur OCSP
- Implémenter une logique de nouvelle tentative pour les défaillances transitoires
- Journaliser tous les échecs de validation OCSP

### ❌ À NE PAS FAIRE :

- Utiliser HTTP pour l'OCSP en production
- Ignorer la validation de la signature des réponses OCSP
- Mettre en cache les réponses trop longtemps (> 24 heures)
- Ignorer silencieusement les défaillances du serveur OCSP
- Désactiver l'OCSP en production sans justification

---

## Implémenter un serveur OCSP

### Option 1 : Répondant OCSP OpenSSL

```bash
# Démarrer le répondant OCSP OpenSSL
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Option 2 : BouncyCastle (.NET)

```csharp
// Exemple avec la bibliothèque BouncyCastle
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. Analyser la requête
        // 2. Vérifier le statut du certificat dans la base de données
        // 3. Construire la réponse
        // 4. Signer la réponse
        // 5. Retourner la réponse signée
    }
}
```

### Option 3 : Service OCSP commercial

- **DigiCert** : Service OCSP géré
- **Let's Encrypt** : OCSP gratuit pour leurs certificats
- **GlobalSign** : Solutions OCSP pour entreprises

---

## Surveillance et journalisation

### Activer la journalisation détaillée

```json
{
  "OcspSettings": {
    "EnableDetailedLogging": true
  },
  "Logging": {
    "LogLevel": {
      "WebAppExperimental266.Services.OcspValidationService": "Debug"
    }
  }
}
```

### Messages de journalisation

```
[Info] La validation OCSP est désactivée
[Info] Validation du certificat CN=Test contre le serveur OCSP https://ocsp.example.com
[Info] Utilisation de la réponse OCSP mise en cache pour le certificat ABC123
[Warning] Serveur OCSP indisponible — Avertissement uniquement : L'URL du serveur OCSP n'est pas configurée
[Error] Serveur OCSP indisponible — Rejet de la requête : Délai de connexion dépassé
```

---

## Tests

### Tests unitaires

Exécuter les tests OCSP :

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Tests manuels

1. **Désactiver l'OCSP** — Vérifier que l'application fonctionne sans OCSP
2. **URL invalide** — Tester les paramètres ServerUnavailableBehavior
3. **Certificat valide** — Doit retourner `OcspStatus.Good`
4. **Réponse mise en cache** — Vérifier que le cache fonctionne

---

## Considérations de performance

### Configuration du cache

```json
"CacheDurationMinutes": 60  // Cache d'une heure
```

**Compromis :**
- **Durée courte (5-15 min)** : Données plus à jour, charge OCSP plus élevée
- **Durée longue (60-120 min)** : Meilleures performances, risque de données obsolètes

### Paramètres de délai d'attente

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Recommandations :**
- Délai d'attente : 10-30 secondes pour la production
- Tentatives : 2-3 tentatives pour les défaillances transitoires

---

## Dépannage

### Problème : Le serveur OCSP est toujours indisponible

**Solutions :**
1. Vérifier que `OcspServerUrl` est correct
2. Vérifier que le pare-feu autorise les connexions HTTPS sortantes
3. Vérifier que le serveur OCSP est en cours d'exécution
4. Consulter les journaux pour les erreurs de délai d'attente

### Problème : Tous les certificats échouent à la validation

**Solutions :**
1. Vérifier que le serveur OCSP dispose des données de statut des certificats
2. Vérifier que la chaîne de certificats est complète
3. S'assurer que la signature de la réponse OCSP est valide
4. Consulter les journaux du serveur OCSP

### Problème : Le cache ne fonctionne pas

**Solutions :**
1. Vérifier que `CacheDurationMinutes > 0`
2. Vérifier que la même empreinte de certificat est utilisée
3. Redémarrer l'application pour vider le cache

---

## Prochaines étapes

Pour rendre l'OCSP pleinement fonctionnel :

1. ✅ **Configuration complète** — Les paramètres sont prêts
2. ✅ **Interface de service complète** — L'API est définie
3. ✅ **Tests complets** — 30+ tests unitaires inclus
4. ⚠️ **Protocole OCSP** — L'implémentation RFC 6960 doit être réalisée
5. ⚠️ **Serveur OCSP** — Le répondant OCSP doit être déployé
6. ⚠️ **Intégration** — Connexion avec l'authentification mTLS

---

## Références

- [RFC 6960](https://tools.ietf.org/html/rfc6960) — Spécification OCSP
- [Documentation BouncyCastle](https://www.bouncycastle.org/csharp/)
- [OCSP OpenSSL](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Authentification par certificat Microsoft](https://learn.microsoft.com/fr-fr/aspnet/core/security/authentication/certauth)

---

**État :** ✅ Modèle prêt  
**Protocole OCSP :** ⚠️ À implémenter  
**Serveur OCSP :** ⚠️ À déployer  
**Tests :** ✅ 30+ tests inclus
