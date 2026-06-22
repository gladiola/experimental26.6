# Revue de Sécurité — WebAppExperimental266

**Date :** 2026-05-07
**Périmètre :** Analyse statique complète du code source (suite à la revue du 2026-05-06)
**Réviseur :** Revue de Sécurité Automatisée

---

## Résumé Exécutif

Cette revue de suivi confirme que 3 des 5 vulnérabilités identifiées lors de la revue de sécurité du 2026-05-06 ont été entièrement corrigées, 1 restant partiellement corrigée. La revue identifie également 4 nouveaux constats. La posture de sécurité globale de l'application continue de s'améliorer.

---

## État des Constats Antérieurs (2026-05-06)

| # | Constat | Gravité | État |
|---|---------|----------|--------|
| 20 | NonceRefresherService conserve des dépendances de constructeur Key Vault inutilisées | 🟠 Élevée | ✅ Corrigé |
| 21 | Le cache interne d'OcspValidationService utilise un Dictionary non thread-safe | 🟡 Moyenne | ✅ Corrigé |
| 22 | Le stub de validation OCSP est toujours présent — échoue en mode fermé mais non implémenté | 🔵 Faible | ⚠️ Accepté (par conception) |
| 23 | mTLS avec AllowedIssuers vide rejette tous les certificats (fail-closed, non documenté) | 🔵 Faible | ✅ Corrigé |
| 24 | OcspSettings.ServerUnavailableBehavior est par défaut à "Warn" (autorise le passage en cas d'erreur) | 🔵 Faible | ⚠️ Partiellement corrigé |

---

## État Détaillé des Constats Antérieurs

### ✅ 20. NonceRefresherService Dépendances DI Inutilisées — Corrigé

**Fichier :** `Services/NonceRefresherService.cs`

Le constructeur de `NonceRefresherService` ne déclare désormais que `ILogger<NonceRefresherService>`, `ILoggerFactory` et `INonceCatalogService`. Les quatre dépendances précédemment inutilisées (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) ont été supprimées. Cela résout le risque de déni de service qui empêchait l'application de démarrer lorsque `EnableKeyVault = false` (la valeur par défaut) et `EnableNonceServices = true` (la valeur par défaut).

---

### ✅ 21. Cache Non Thread-Safe d'OcspValidationService — Corrigé

**Fichier :** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` a été remplacé par `ConcurrentDictionary<string, CachedOcspResponse>`. L'appel `_cache.Remove` a été mis à jour vers `_cache.TryRemove`. Le cache est désormais sûr pour les accès concurrents.

---

### ⚠️ 22. Stub de Validation OCSP — Accepté (Par Conception)

**Fichier :** `Services/OcspValidationService.cs`

Le stub est toujours présent mais échoue correctement en mode fermé. Comme `EnableOcspValidation` est par défaut à `false`, cela n'a aucun impact en production. Ceci est accepté comme un constat informatif en attendant une implémentation complète d'OCSP.

---

### ✅ 23. mTLS AllowedIssuers Vide — Corrigé

**Fichier :** `Extensions/ServiceCollectionExtensions.cs`

Un avertissement au démarrage est désormais journalisé lorsque `ValidateClientCertificateIssuer = true` et `AllowedIssuers` est vide :

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Cela fournit des indications claires aux opérateurs qui rencontrent le comportement fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Partiellement Corrigé

**Fichiers :** `appsettings.template.json` (corrigé), `Models/Settings/OcspSettings.cs` (pas encore corrigé)

Le modèle spécifie désormais correctement `"ServerUnavailableBehavior": "Fail"`. Cependant, la valeur par défaut de la classe C# dans `OcspSettings.cs` (ligne 39) reste à `"Warn"`. Si un opérateur active OCSP et omet `ServerUnavailableBehavior` de son fichier de configuration, la valeur par défaut de la classe `"Warn"` s'applique silencieusement, autorisant le passage lors des pannes du serveur OCSP. La valeur par défaut de la classe doit être modifiée pour correspondre à la recommandation du modèle.

---

## Nouveaux Constats

| # | Domaine | Gravité |
|---|------|----------|
| 25 | La valeur par défaut de la classe OcspSettings ("Warn") diverge du modèle ("Fail") | 🔵 Faible |
| 26 | La clé nonce partagée unique de NonceCatalogService permet une collision de nonce entre requêtes | 🟡 Moyenne |
| 27 | Les compteurs statiques d'OptimizedNonceMiddleware utilisent des entiers signés 32 bits (risque de débordement) | 🔵 Faible |
| 28 | Program.cs enregistre un singleton ILoggerFactory vide, occultant le logger du framework | 🟡 Moyenne |

---

## 🟡 Moyenne

### 26. La Clé Nonce Partagée de NonceCatalogService Permet une Collision de Nonce Entre Requêtes

**Fichiers :** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Le catalogue de nonces stocke tous les nonces sous une seule clé partagée `"CSPNonce"`. Sous charge concurrente, la condition de course suivante est possible :

1. La requête A appelle `RefreshNonceAsync()` — le nonce A1 est stocké sous `_nonceCollection["CSPNonce"]`.
2. La requête B appelle `RefreshNonceAsync()` — le nonce B1 écrase `_nonceCollection["CSPNonce"]`.
3. La requête A appelle `GetANonce("CSPNonce")` — reçoit B1, pas A1.
4. L'en-tête CSP et le nonce de mise en page de la requête A contiennent tous deux B1.
5. La requête B contient également B1.

Deux réponses concurrentes partagent le même nonce. Bien que les deux valeurs soient toujours cryptographiquement aléatoires et imprévisibles (pas de chaîne codée en dur), la même valeur de nonce apparaît dans plusieurs réponses simultanées, affaiblissant la garantie d'unicité par requête requise par la spécification CSP. Un attaquant qui peut observer le nonce d'une réponse dispose d'un nonce valide pour au moins une autre réponse concurrente.

**Recommandation :** Générez le nonce directement à l'intérieur du middleware par requête (par exemple, `Nonce.GenerateSecureNonce()`) et stockez-le uniquement dans `HttpContext.Items["Nonce"]`, contournant le catalogue partagé pour les nonces par requête. Le catalogue partagé ne serait alors nécessaire que si un nonce doit être partagé entre les couches de middleware au sein d'une seule requête, ce que `HttpContext.Items` gère déjà nativement.

---

### 28. Program.cs Enregistre un Singleton ILoggerFactory Vide

**Fichier :** `Program.cs` (ligne 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core enregistre automatiquement un `ILoggerFactory` entièrement configuré (avec tous les fournisseurs de journalisation de la configuration `builder.Logging`) lors de `WebApplication.CreateBuilder`. Cet enregistrement explicite `AddSingleton` ajoute une deuxième instance `LoggerFactory` non configurée sans fournisseurs. Étant donné que `GetRequiredService<ILoggerFactory>()` retourne l'implémentation la plus récemment enregistrée, les services qui reçoivent `ILoggerFactory` via l'injection de dépendances (comme `NonceRefresherService`) utiliseront cette fabrique vide et ne produiront aucune sortie de journal via `_loggerFactory.CreateLogger<T>()`.

**Risque :** Journalisation silencieuse dans `NonceRefresherService` — les succès et échecs de génération de nonces ne sont émis vers aucun récepteur de journalisation configuré. Cela réduit l'observabilité de l'application lors des opérations sensibles à la sécurité sans affecter la fonctionnalité.

**Recommandation :** Supprimez l'enregistrement explicite `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. Le `ILoggerFactory` configuré du framework (avec la console et tout autre fournisseur) sera alors résolu correctement par les services qui en dépendent.

---

## 🔵 Faible / Informationnel

### 25. La Valeur par Défaut de la Classe OcspSettings Diverge du Modèle

**Fichier :** `Models/Settings/OcspSettings.cs` (ligne 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Le modèle (`appsettings.template.json`) spécifie `"ServerUnavailableBehavior": "Fail"`, mais la valeur par défaut de la classe C# est `"Warn"`. Si `ServerUnavailableBehavior` est absent du fichier de configuration actif, la valeur par défaut de la classe s'applique silencieusement plutôt que la recommandation du modèle. Il s'agit d'un résidu du constat #24.

**Recommandation :** Changez la valeur par défaut de la classe de `"Warn"` à `"Fail"` pour s'aligner avec le modèle et le principe du moindre privilège.

---

### 27. Les Compteurs Statiques d'OptimizedNonceMiddleware Peuvent Déborder

**Fichier :** `Services/OptimizedNonceMiddleware.cs` (lignes 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Ces compteurs signés de 32 bits sont incrémentés de manière atomique via `Interlocked.Increment`. Après environ 2,1 milliards d'incréments, ils se replieront à `int.MinValue` (−2 147 483 648), entraînant le calcul d'efficacité `(total - generated) * 100.0 / total` à produire des résultats incorrects ou sans signification. À 1 000 requêtes par seconde, le débordement se produit après environ 24,8 jours de fonctionnement continu.

**Recommandation :** Modifiez les types de champ des compteurs de `int` à `long` et utilisez la surcharge `long` d'`Interlocked.Increment` pour éviter le débordement.

---

## Évaluation des En-têtes de Sécurité (État Actuel)

Les en-têtes suivants sont appliqués via `UseStandardSecurityHeaders` — inchangés par rapport à la revue précédente :

| En-tête | Valeur | Évaluation |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bien |
| `X-XSS-Protection` | `0` | ✅ Bien (désactive l'auditeur obsolète) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bien |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bien |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bien |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bien |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bien |
| `Permissions-Policy` | géolocalisation, caméra, microphone, interest-cohort désactivés | ✅ Bien |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bien |
| `Content-Security-Policy` | Basé sur nonce, appliqué lorsque CSP est activé | ✅ Bien |
| `Server` | Masqué à `"webserver"` | ✅ Bien |
| `X-Powered-By` | Supprimé | ✅ Bien |

---

## Évaluation Globale

Tous les constats de haute gravité des revues précédentes ont été corrigés. Les constats actuels se limitent à deux problèmes de gravité moyenne (#26 clé nonce partagée, #28 ILoggerFactory vide) et deux éléments informationnels de faible gravité (#25 divergence de valeur par défaut de classe, #27 débordement d'entier dans les compteurs). Une attention immédiate est recommandée pour le constat #28 (singleton ILoggerFactory vide) car il supprime silencieusement la journalisation de diagnostic liée à la sécurité lors des opérations de nonce. Le constat #26 (clé nonce partagée) doit être traité pour restaurer la garantie d'unicité de nonce par requête requise par la spécification CSP.
