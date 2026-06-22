# Revue de sécurité — WebAppExperimental266

**Date :** 2026-05-05  
**Périmètre :** Analyse statique complète du code source  

---

## Tableau récapitulatif

| # | Domaine | Sévérité |
|---|---------|----------|
| 1 | Réutilisation de l'IV AES-GCM dans la génération des nonces | 🔴 Critique ✅ |
| 2 | Nonce journalisé en clair | 🔴 Critique ✅ |
| 3 | Chaînes de nonces de repli codées en dur | 🔴 Critique ✅ |
| 4 | Dictionnaire global de nonces non thread-safe | 🟠 Élevé |
| 5 | Validation de l'émetteur du certificat mTLS commentée | 🟠 Élevé |
| 6 | Vérification de révocation mTLS désactivée par défaut | 🟠 Élevé |
| 7 | OCSP retourne toujours valide (stub) | 🟠 Élevé |
| 8 | Authentification/autorisation désactivées par défaut dans la configuration | 🟠 Élevé |
| 9 | En-têtes de sécurité appliqués trop tard dans le pipeline | 🟠 Élevé |
| 10 | Cookie de session sans attributs Secure + SameSite | 🟡 Moyen |
| 11 | En-tête Set-Cookie global malformé | 🟡 Moyen |
| 12 | Content-Type forcé à text/html pour toutes les réponses | 🟡 Moyen |
| 13 | AllowedHosts est un caractère générique | 🟡 Moyen |
| 14 | Nonce non appliqué aux balises `<script>` dans la mise en page | 🟡 Moyen |
| 15 | En-tête Referrer-Policy manquant | 🟡 Moyen |
| 16 | Données personnelles journalisées en clair | 🔵 Faible |
| 17 | Chaîne de connexion partielle dans les journaux | 🔵 Faible |
| 18 | Opérations Key Vault sont des stubs | 🔵 Faible |
| 19 | En-tête X-XSS-Protection obsolète | 🔵 Faible |

---

## 🔴 Critique

### 1. Réutilisation de l'IV AES-GCM — La génération de nonces est cryptographiquement compromise ✅ Corrigé dans le commit 45ae31b

**Fichiers :** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

Le chiffrement AES-GCM utilisé pour générer les nonces CSP utilise un **IV fixe récupéré depuis Key Vault à chaque appel**. AES-GCM est compromis lorsque l'IV est réutilisé avec la même clé : un attaquant qui observe deux textes chiffrés peut les XORer pour récupérer le XOR des textes en clair, et les étiquettes d'authentification peuvent être falsifiées.

La correction est simple — les nonces CSP n'ont pas besoin de chiffrement. Un nonce CSP n'a besoin que d'être **imprévisible et unique par requête** ; un appel à `RandomNumberGenerator.GetBytes(16)` converti en Base64 est suffisant et correct.

---

### 2. Valeurs de nonces journalisées en clair ✅ Corrigé dans le commit bb6f27a

**Fichiers :** `Services/NonceMiddleware.cs` (ligne 31), `Services/NonceRefresherService.cs` (ligne 82)

Le nonce CSP généré est journalisé en clair dans les journaux d'application :

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

Toute personne ayant accès aux journaux obtient un nonce valide et peut facilement contourner la CSP pour injecter des scripts en ligne.

---

### 3. Nonces de repli codés en dur ✅ Corrigé dans le commit 11cc9f7

**Fichier :** `Services/OptimizedNonceMiddleware.cs` (lignes 53, 78, 92)

Si la génération de nonce échoue ou si le catalogue de nonces est vide, le middleware utilise les chaînes littérales `"bootstrap-nonce-placeholder"`, `"fallback-nonce"` et `"error-fallback-nonce"`. Ces chaînes sont validées dans le code source et connues des attaquants. Une condition d'erreur (par ex. Key Vault indisponible) placerait un nonce prévisible et exploitable dans l'en-tête CSP.

---

## 🟠 Élevé

### 4. NonceCatalogService utilise un dictionnaire statique non thread-safe ✅ Corrigé dans le commit ae2b6c9

**Fichier :** `Services/NonceCatalogService.cs` (ligne 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` n'est pas thread-safe pour les lectures et écritures simultanées. Sous charge, deux requêtes concurrentes peuvent provoquer une corruption de données ou des exceptions. Utiliser `ConcurrentDictionary` et stocker les nonces par requête dans `HttpContext.Items` plutôt que dans un singleton global.

---

### 5. Validation de l'émetteur du certificat client mTLS désactivée ✅ Corrigé dans le commit fd3d4fb

**Fichier :** `Extensions/ServiceCollectionExtensions.cs` (lignes 305–313)

Le paramètre `ValidateClientCertificateIssuer` existe et vaut `true` par défaut, mais le code de validation réel est commenté :

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

Avec mTLS activé, n'importe quel certificat client de n'importe quel émetteur (qui s'enchaîne à une racine de confiance) peut s'authentifier — aucune restriction d'émetteur ne s'applique.

---

### 6. Vérification de révocation du certificat mTLS désactivée par défaut ✅ Corrigé dans le commit fd3d7b3

**Fichiers :** `Models/Settings/MtlsSettings.cs` (ligne 26), `appsettings.template.json`

`CheckCertificateRevocation` vaut `false` par défaut dans le modèle et le template. Les certificats clients révoqués peuvent être utilisés pour s'authentifier indéfiniment. Pour mTLS en production, la vérification de révocation doit être activée par défaut.

---

### 7. La validation OCSP est un stub qui retourne toujours valide ✅ Corrigé dans le commit b4c3807

**Fichier :** `Services/OcspValidationService.cs` (lignes 149–163)

La méthode `PerformOcspValidationAsync` est explicitement une « implémentation modèle » qui retourne toujours `IsValid = true` après un `Task.Delay(100)`. Si la validation OCSP est activée dans la configuration, elle approuvera silencieusement tous les certificats — y compris les révoqués — comme valides.

---

### 8. Authentification et autorisation désactivées par défaut ✅ Corrigé dans le commit b392c47

**Fichier :** `appsettings.json` (lignes 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

La configuration par défaut est livrée sans authentification ni autorisation. Un développeur qui copie `appsettings.template.json` sans lire attentivement la documentation déploiera une application ouverte. Les valeurs par défaut du template doivent nécessiter un désactivation délibérée, pas une activation volontaire.

---

### 9. En-têtes de sécurité appliqués après le routage/l'authentification ✅ Corrigé dans le commit 016e57c

**Fichier :** `Program.cs` (lignes 130–152)

`UseNonceAndSecurityHeadersAsync` et `UseStandardSecurityHeaders` sont appelés après `UseRouting`, `UseAuthentication` et `UseAuthorization`. Les réponses qui court-circuitent le pipeline avant ces middlewares (par ex. redirections 401, refus 403) peuvent ne pas recevoir d'en-têtes de sécurité. Les en-têtes de sécurité doivent être placés aussi tôt que possible dans le pipeline.

---

## 🟡 Moyen

### 10. Cookie de session sans attributs `Secure` et `SameSite` ✅ Corrigé dans le commit 8f2223c

**Fichier :** `Extensions/ServiceCollectionExtensions.cs` (lignes 41–46)

Le cookie de session définit `HttpOnly = true` et `IsEssential = true`, mais omet `Cookie.SecurePolicy = CookieSecurePolicy.Always` et `Cookie.SameSite = SameSiteMode.Strict`. Le cookie pourrait être transmis en HTTP simple ou envoyé cross-site.

---

### 11. En-tête global `Set-Cookie` malformé ✅ Corrigé dans le commit 8f2223c

**Fichier :** `Extensions/ApplicationBuilderExtensions.cs` (ligne 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

Cela ajoute un en-tête `Set-Cookie` sans nom ni valeur à chaque réponse. Il est invalide et sera ignoré (ou rejeté) par les navigateurs, mais produit des artefacts dans toutes les réponses. La sécurité des cookies doit être définie dans les options du cookie spécifique, pas injectée globalement.

---

### 12. `Content-Type` forcé à `text/html` pour toutes les réponses ✅ Corrigé dans le commit 8f2223c

**Fichier :** `Extensions/ApplicationBuilderExtensions.cs` (ligne 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

Cela écrase le Content-Type pour toutes les réponses — points d'API, JSON, téléchargements binaires et fichiers statiques se déclarent tous `text/html`. Cela entre en conflit avec `X-Content-Type-Options: nosniff`.

---

### 13. `AllowedHosts` défini sur le caractère générique ✅ Corrigé dans le commit 8f2223c

**Fichiers :** `appsettings.json` (ligne 11), `appsettings.template.json` (ligne 36)

```json
"AllowedHosts": "*"
```

Cela désactive la validation de l'en-tête d'hôte intégrée d'ASP.NET Core. Les attaques par injection d'en-tête d'hôte permettent l'empoisonnement du cache, l'empoisonnement des liens de réinitialisation de mot de passe et les redirections ouvertes. Ce paramètre doit être défini sur le ou les domaines spécifiques.

---

### 14. La mise en page n'applique pas le nonce aux balises `<script>` ✅ Corrigé dans le commit 8f2223c

**Fichier :** `Views/Shared/_Layout.cshtml`

La mise en page charge plusieurs fichiers JavaScript mais aucune balise `<script>` n'inclut `nonce="@Context.Items["Nonce"]"`. Si CSP avec nonces est activé, ces scripts seraient bloqués par le navigateur.

---

### 15. En-tête Referrer-Policy manquant ✅ Corrigé dans le commit 8f2223c

**Fichier :** `Extensions/ApplicationBuilderExtensions.cs`

Les en-têtes de sécurité standard n'incluent pas `Referrer-Policy`. Sans cela, le navigateur envoie l'URL complète dans l'en-tête `Referer` aux ressources tierces, ce qui pourrait divulguer des chemins de session authentifiés.

---

## 🔵 Faible / Informatif

### 16. Données personnelles journalisées en clair ✅ Corrigé dans le commit 93bb4e9

**Fichier :** `Services/LoggingHelper.cs` (lignes 85, 105)

L'OID utilisateur, l'e-mail, le nom, l'ID de session et les rôles sont journalisés en clair à chaque requête authentifiée. Selon les réglementations de confidentialité applicables (RGPD, CCPA, HIPAA), cela pourrait constituer un problème de conformité. Il convient de masquer ou hacher les identifiants dans les journaux.

---

### 17. Chaîne de connexion partielle dans les journaux ✅ Corrigé dans le commit 93bb4e9

**Fichier :** `Extensions/ServiceCollectionExtensions.cs` (ligne 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

Même une partie d'un secret dans les journaux n'est pas une bonne pratique. L'instruction de journal doit confirmer qu'une chaîne de connexion est présente (non vide) plutôt que de journaliser une portion quelconque.

---

### 18. Les opérations Key Vault sont des stubs ✅ Corrigé dans le commit 93bb4e9

**Fichier :** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

`GetCertificateFromKeyVault` et `GetSecretFromKeyVault` sont des stubs modèles retournant `null`/des valeurs factices. Avec Key Vault activé, `GetCertificateFromKeyVault` retourne `null`, ce qui cause une `InvalidOperationException` au démarrage.

---

### 19. L'en-tête `X-XSS-Protection: 1; mode=block` est obsolète ✅ Corrigé dans le commit 93bb4e9

**Fichier :** `Extensions/ApplicationBuilderExtensions.cs` (ligne 70)

Les navigateurs modernes ont supprimé le support de `X-XSS-Protection`. L'en-tête n'est pas nuisible, mais il donne un faux sentiment de sécurité. L'approche recommandée est de s'appuyer sur une CSP stricte.
