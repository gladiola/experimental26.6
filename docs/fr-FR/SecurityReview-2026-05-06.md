# Revue de Sécurité — WebAppExperimental266

**Date :** 2026-05-06
**Périmètre :** Analyse statique complète du code source (suite à la revue du 2026-05-05)
**Réviseur :** Revue de Sécurité Automatisée

---

## Résumé Exécutif

Cette revue de suivi confirme que les 19 vulnérabilités identifiées lors de la revue de sécurité du 2026-05-05 ont toutes été corrigées. La revue identifie également 5 nouveaux résultats ou résiduels découverts au cours de cette session. La posture de sécurité générale de l'application s'est considérablement améliorée depuis la revue précédente.

---

## Statut des Résultats Précédents (2026-05-05)

Les 19 résultats précédents sont **confirmés comme corrigés** :

| # | Résultat | Sévérité | Statut |
|---|----------|----------|--------|
| 1 | Réutilisation de l'IV AES-GCM lors de la génération de nonce | 🔴 Critique | ✅ Corrigé |
| 2 | Nonce enregistré en texte clair | 🔴 Critique | ✅ Corrigé |
| 3 | Chaînes de nonce de secours codées en dur | 🔴 Critique | ✅ Corrigé |
| 4 | Dictionnaire de nonce global non thread-safe | 🟠 Élevé | ✅ Corrigé |
| 5 | Validation de l'émetteur mTLS commentée | 🟠 Élevé | ✅ Corrigé |
| 6 | Vérification de révocation mTLS désactivée par défaut | 🟠 Élevé | ✅ Corrigé |
| 7 | OCSP retourne toujours valide (stub) | 🟠 Élevé | ✅ Corrigé |
| 8 | Authentification/autorisation désactivée par défaut dans la configuration | 🟠 Élevé | ✅ Corrigé |
| 9 | En-têtes de sécurité appliqués trop tard dans le pipeline | 🟠 Élevé | ✅ Corrigé |
| 10 | Cookie de session manquant `Secure` + `SameSite` | 🟡 Moyen | ✅ Corrigé |
| 11 | En-tête `Set-Cookie` global malformé | 🟡 Moyen | ✅ Corrigé |
| 12 | `Content-Type` forcé à `text/html` partout | 🟡 Moyen | ✅ Corrigé |
| 13 | `AllowedHosts` défini sur joker | 🟡 Moyen | ✅ Corrigé |
| 14 | Nonce non appliqué aux balises `<script>` dans la mise en page | 🟡 Moyen | ✅ Corrigé |
| 15 | En-tête `Referrer-Policy` manquant | 🟡 Moyen | ✅ Corrigé |
| 16 | PII enregistrée en texte clair | 🔵 Faible | ✅ Corrigé |
| 17 | Chaîne de connexion partielle dans les journaux | 🔵 Faible | ✅ Corrigé |
| 18 | Les opérations Key Vault sont des stubs | 🔵 Faible | ✅ Corrigé |
| 19 | `X-XSS-Protection: 1; mode=block` obsolète | 🔵 Faible | ✅ Corrigé |

---

## Nouveaux Résultats / Résiduels

| # | Domaine | Sévérité |
|---|---------|----------|
| 20 | NonceRefresherService conserve des dépendances de constructeur Key Vault inutilisées | 🟠 Élevé |
| 21 | Le cache interne d'OcspValidationService utilise un Dictionary non thread-safe | 🟡 Moyen |
| 22 | Le stub de validation OCSP est toujours présent — échoue fermé mais non implémenté | 🔵 Faible |
| 23 | mTLS avec AllowedIssuers vide rejette tous les certificats (fail-closed, non documenté) | 🔵 Faible |
| 24 | OcspSettings.ServerUnavailableBehavior par défaut à "Warn" (permet le passage en cas d'erreur) | 🔵 Faible |

---

## Résultats Détaillés

### ✅ Corrections Confirmées de 2026-05-05

#### 1. Réutilisation de l'IV AES-GCM — Corrigé

**Fichier :** `Models/Main_Objects/Nonce.cs`

La génération de nonce basée sur AES-GCM a été complètement remplacée. `Nonce.GenerateSecureNonce()` appelle désormais `RandomNumberGenerator.Fill(randomBytes)` sur 16 octets aléatoires et retourne une chaîne Base64. Aucune dépendance Key Vault, aucun IV, aucun chiffrement — exactement la bonne approche pour un nonce CSP.

---

#### 2. Les Valeurs de Nonce Ne Sont Plus Enregistrées — Corrigé

**Fichiers :** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Les deux fichiers n'enregistrent désormais que des messages de statut (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) et jamais la valeur du nonce elle-même.

---

#### 3. Nonces de Secours Codés en Dur Supprimés — Corrigé

**Fichier :** `Services/OptimizedNonceMiddleware.cs`

Les trois chaînes littérales codées en dur (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) ont été remplacées par des appels à `Nonce.GenerateSecureNonce()` dans les chemins normaux et de secours d'exception.

---

#### 4. Dictionnaire de Nonce Thread-Safe — Corrigé

**Fichier :** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` a été remplacé par `ConcurrentDictionary<string, Nonce>`. `GetANonce` utilise désormais un seul appel atomique `TryGetValue` plutôt qu'une vérification en deux étapes.

---

#### 5. Validation de l'Émetteur mTLS Maintenant Fonctionnelle — Corrigé

**Fichier :** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Le bloc de validation de l'émetteur commenté a été remplacé par un appel à `mtlsSettings.IsIssuerAllowed(issuer)`, qui effectue une correspondance de sous-chaîne insensible à la casse contre `AllowedIssuers`. Lorsque la liste est vide (non configurée), la méthode retourne `false`, rejetant tous les certificats (fail-closed).

---

#### 6. La Vérification de Révocation mTLS Est Activée par Défaut — Corrigé

**Fichier :** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` est maintenant par défaut à `true`. Le fichier `appsettings.template.json` spécifie également `"CheckCertificateRevocation": true`.

---

#### 7. Le Stub OCSP Échoue Maintenant Fermé — Corrigé

**Fichier :** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` retourne maintenant `IsValid = false` avec `OcspStatus.Error` et enregistre une erreur, plutôt que de retourner silencieusement `IsValid = true`. L'activation d'OCSP dans la configuration rejettera désormais tous les certificats jusqu'à ce qu'une implémentation réelle soit fournie, au lieu de les accepter silencieusement.

---

#### 8. Authentification et Autorisation Activées par Défaut — Corrigé

**Fichier :** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` et `EnableAuthorization` sont maintenant par défaut à `true` dans la classe `FeatureFlags`. `appsettings.json` définit également les deux à `true`.

---

#### 9. En-têtes de Sécurité Appliqués Avant le Routage — Corrigé

**Fichier :** `Program.cs`

`UseNonceAndSecurityHeadersAsync` et `UseStandardSecurityHeaders` sont maintenant appelés avant `UseRouting`, `UseAuthentication` et `UseAuthorization`. Toutes les réponses, y compris les courts-circuits 401/403, reçoivent les en-têtes de sécurité.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce dans la Mise en Page, Referrer-Policy — Corrigé

**Fichiers :** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Le cookie de session définit maintenant `CookieSecurePolicy.Always` et `SameSiteMode.Strict`.
- L'en-tête `Set-Cookie` sans nom malformé a été supprimé.
- La substitution globale `Content-Type: text/html` a été supprimée.
- `AllowedHosts` dans `appsettings.json` est maintenant `"localhost;127.0.0.1"` ; le modèle utilise `"{{YOUR_HOSTNAME}}"`.
- Les trois balises `<script>` dans `_Layout.cshtml` incluent maintenant `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` est maintenant ajouté par `UseStandardSecurityHeaders`.

---

#### 16–19. Journalisation PII, Journal de Chaîne de Connexion, Stubs Key Vault, X-XSS-Protection — Corrigé

**Fichiers :** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Toutes les PII (OID, e-mail, nom, SID, rôles) sont maintenant hachées via HMAC-SHA256 via `LoggingHelper.HashPii()` avant d'être écrites dans les journaux. Une clé HMAC stable peut être fournie via `Logging:PiiHmacKey` dans la configuration ; une clé aléatoire par processus est utilisée si non configurée.
- L'instruction de journal Cosmos DB confirme désormais uniquement si une chaîne de connexion est présente (`!string.IsNullOrEmpty`), pas son contenu.
- `AzureKeyVaultCertificateOperations` lève maintenant `InvalidOperationException` au démarrage lorsque le certificat est nul, plutôt que de retourner silencieusement des valeurs factices.
- `X-XSS-Protection` est maintenant défini à `"0"` (désactivant l'auditeur XSS obsolète), conformément aux recommandations modernes des navigateurs.

---

## 🟠 Élevé

### 20. NonceRefresherService Conserve des Dépendances de Constructeur Key Vault Inutilisées

**Fichier :** `Services/NonceRefresherService.cs`

`NonceRefresherService` déclare toujours des paramètres de constructeur pour `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService` et `IAzureKeyVaultOperationsService`. Étant donné que la génération de nonce a été simplifiée pour utiliser directement `RandomNumberGenerator`, aucune de ces dépendances n'est utilisée.

**Risque :** Lorsque `EnableNonceServices = true` et `EnableKeyVault = false` (la valeur par défaut), ces services ne sont pas enregistrés dans le conteneur DI, provoquant une `InvalidOperationException` au moment de l'exécution lorsque le service de nonce est résolu pour la première fois. C'est effectivement une condition de déni de service déclenchée par la configuration par défaut. La classe `FeatureFlags` par défaut définit `EnableNonceServices = true`, donc tout environnement s'appuyant uniquement sur les valeurs par défaut de classe (sans remplacements `appsettings.json`) ne démarrerait pas.

**Recommandation :** Supprimez les quatre paramètres de constructeur inutilisés et leurs champs privés correspondants de `NonceRefresherService`. Le service ne nécessite que `ILogger<NonceRefresherService>`, `ILoggerFactory` et `INonceCatalogService`.

---

## 🟡 Moyen

### 21. Le Cache Interne d'OcspValidationService Utilise un Dictionary Non Thread-Safe

**Fichier :** `Services/OcspValidationService.cs` (ligne 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` n'est pas thread-safe pour les lectures et écritures concurrentes. Si `OcspValidationService` est enregistré comme singleton (ou si la même instance est partagée entre les requêtes par tout autre mécanisme), les validations OCSP simultanées pourraient corrompre le cache, entraînant des entrées perdues, des exceptions levées ou le retour de données périmées.

**Recommandation :** Remplacez `Dictionary<string, CachedOcspResponse>` par `ConcurrentDictionary<string, CachedOcspResponse>`. Mettez à jour l'appel `_cache.Remove` (ligne 103) en `_cache.TryRemove`.

---

## 🔵 Faible / Informatif

### 22. Stub de Validation OCSP — Échoue Fermé mais Non Implémenté

**Fichier :** `Services/OcspValidationService.cs` (lignes 157–173)

`PerformOcspValidationAsync` est toujours un stub. La correction du résultat #7 a correctement modifié le comportement de « toujours valide » à « toujours invalide (fail-closed) ». Cependant, la méthode n'est toujours pas une vraie implémentation OCSP. Tant que `EnableOcspValidation = false` (la valeur par défaut), cela n'a aucun impact en production. Avant d'activer OCSP dans un environnement, un client OCSP de qualité production doit être implémenté.

---

### 23. mTLS avec AllowedIssuers Vide Rejette Tous les Certificats Client

**Fichier :** `Models/Settings/MtlsSettings.cs`

Lorsque `ValidateClientCertificateIssuer = true` (la valeur par défaut) et que `AllowedIssuers` est vide (également la valeur par défaut lorsque non configuré), `IsIssuerAllowed()` retourne `false`, ce qui entraîne le rejet de tous les certificats client. C'est un comportement fail-closed correct, mais il n'est pas documenté de manière prominente. Les opérateurs qui activent mTLS sans lire attentivement le modèle peuvent constater que toutes les connexions client sont rejetées sans explication évidente.

**Recommandation :** Ajoutez un message de journal d'avertissement au démarrage lorsque `ValidateClientCertificateIssuer = true` et que `AllowedIssuers` est vide.

---

### 24. OcspSettings.ServerUnavailableBehavior Par Défaut à "Warn"

**Fichier :** `appsettings.template.json` (ligne 134), `Services/OcspValidationService.cs`

Le paramètre `ServerUnavailableBehavior` est par défaut à `"Warn"` dans le modèle, ce qui permet aux requêtes de passer lorsque le serveur OCSP ne peut pas être atteint. Pour les environnements à haute sécurité, cela devrait être `"Fail"` afin que les pannes du serveur OCSP ne dégradent pas silencieusement la vérification de révocation des certificats.

**Recommandation :** Documentez clairement les trois options (`Fail`, `Allow`, `Warn`) dans le modèle et envisagez de changer la valeur par défaut à `"Fail"` pour respecter le principe du moindre privilège.

---

## Évaluation des En-têtes de Sécurité (État Actuel)

Les en-têtes suivants sont maintenant appliqués via `UseStandardSecurityHeaders` :

| En-tête | Valeur | Évaluation |
|---------|--------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bon |
| `X-XSS-Protection` | `0` | ✅ Bon (désactive l'auditeur obsolète) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bon |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bon |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bon |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bon |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bon |
| `Permissions-Policy` | géolocalisation, caméra, microphone, interest-cohort désactivés | ✅ Bon |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bon |
| `Content-Security-Policy` | Basé sur nonce, appliqué quand CSP est activé | ✅ Bon |
| `Server` | Masqué en `"webserver"` | ✅ Bon |
| `X-Powered-By` | Supprimé | ✅ Bon |

---

## Évaluation Globale

L'application a corrigé toutes les vulnérabilités de sévérité critique et élevée de la revue précédente. Les résultats actuels se limitent à un problème de configuration/DI de haute sévérité (résultat #20) et à des éléments informatifs de sévérité inférieure. La posture de sécurité s'est considérablement améliorée. Une action immédiate est recommandée pour le résultat #20 (dépendances DI inutilisées dans NonceRefresherService), car cela peut empêcher l'application de démarrer avec la configuration par défaut.
