# Correctif de sécurité : Valeurs de nonces journalisées en clair (Critique n°2)

**Corrigé dans :** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Tests :** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Ce qui était incorrect

Deux emplacements journalisaient la valeur réelle du nonce CSP en clair dans le flux de journaux de l'application :

**`Services/NonceMiddleware.cs` (ligne 31) :**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (ligne 82) :**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Pourquoi c'est critique

Un nonce CSP est le *seul* mécanisme empêchant l'injection de scripts en ligne une fois que la CSP est appliquée. Sa sécurité dépend entièrement du fait qu'il soit **secret pendant la durée d'une seule réponse**.

Les journaux d'application dans un environnement cloud/entreprise sont généralement lisibles par :
* Les équipes opérationnelles
* Les services d'agrégation de journaux (par ex. Azure Monitor, Splunk, ELK)
* Tout compte ayant un accès en lecture au récepteur de journaux

Quiconque peut lire une ligne de journal contenant `Nonce: <valeur>` peut injecter une balise `<script>` en ligne avec cette valeur de nonce et faire exécuter le script par le navigateur, contournant complètement la CSP. Même si le nonce change à chaque requête, un attaquant disposant d'un accès en direct aux journaux peut agir dans la fenêtre de la même requête.

---

## Ce qui a été corrigé

Les deux instructions de journalisation ont été remplacées par des messages qui confirment l'*état* de la génération de nonce sans révéler la valeur :

**`NonceMiddleware.cs` :**
```csharp
// AVANT (vulnérable) :
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// APRÈS (sûr) :
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce récupéré pour la requête.");
```

**`NonceRefresherService.cs` :**
```csharp
// AVANT (vulnérable) :
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// APRÈS (sûr) :
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce généré avec succès.");
```

---

## Comment maintenir cette correction

1. **Ne journalisez jamais la valeur du nonce.** Les messages de journalisation peuvent confirmer qu'un nonce a été généré ou récupéré (statut de succès/échec), mais la chaîne de nonce elle-même ne doit jamais apparaître dans un paramètre de journalisation, un champ de journalisation structurée ou une interpolation de chaîne.

2. **Examinez tout nouveau message de journalisation dans le code lié aux nonces** (`NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) pour s'assurer que la valeur du nonce n'y figure pas.

3. **N'exposez pas le nonce dans la télémétrie, les métriques ou les traces distribuées** pour les mêmes raisons. Les attributs de trace et les étiquettes de span sont souvent transmis aux backends d'agrégation de journaux.

4. **Le nonce doit être traité comme un secret par requête.** Il peut être stocké dans `HttpContext.Items` pour une utilisation dans le pipeline de rendu d'une seule requête, mais il ne doit pas quitter le processus via un canal observable autre que l'en-tête de réponse HTTP et l'attribut `nonce="..."` dans le HTML qu'il protège.

### Tests qui appliquent cette correction

| Test | Ce qu'il détecte |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Échoue si la chaîne de nonce est réintroduite dans un message de journalisation de `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Échoue si la chaîne de nonce est réintroduite dans un message de journalisation de `NonceMiddleware` |
