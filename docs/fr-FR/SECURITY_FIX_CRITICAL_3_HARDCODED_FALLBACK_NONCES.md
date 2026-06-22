# Correctif de sécurité : Nonces de repli codés en dur (Critique n°3)

**Corrigé dans :** `Services/OptimizedNonceMiddleware.cs`  
**Tests :** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Ce qui était incorrect

`OptimizedNonceMiddleware` contenait trois chaînes littérales codées en dur utilisées comme valeurs de nonce de repli lorsque la génération normale de nonce échouait ou n'avait pas encore été exécutée :

| Emplacement | Valeur codée en dur |
|-------------|---------------------|
| `InvokeAsync` — première requête, catalogue vide | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — la génération a retourné une chaîne vide | `"fallback-nonce"` |
| `InvokeAsync` — chemin d'exception | `"error-fallback-nonce"` |

### Pourquoi c'est critique

**Un nonce n'est sécurisé que si un attaquant ne peut pas le prédire.** Les littéraux codés en dur sont validés dans le contrôle de source et donc connus de toute personne ayant accès au dépôt (y compris tout attaquant ayant obtenu un accès au source ou décompilé le binaire).

Le danger spécifique est que ces chemins de repli sont activés par des **conditions d'erreur** — exactement les situations qu'un attaquant est le plus susceptible d'organiser (par ex. rendre Key Vault temporairement indisponible via une limitation de débit ou une perturbation réseau). Lorsque l'application se dégrade gracieusement vers un nonce prévisible, l'en-tête CSP devient décoratif : l'attaquant injecte simplement `<script nonce="fallback-nonce">` et le navigateur l'exécute.

### Code à l'origine du problème (avant correction)

```csharp
// Première requête avant qu'un nonce soit généré
existingNonce = "bootstrap-nonce-placeholder";

// La génération de nonce a retourné une valeur vide
nonce = "fallback-nonce";

// Chemin d'exception
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## Ce qui a été corrigé

Les trois chemins de repli appellent maintenant `Nonce.GenerateSecureNonce()` pour produire un nonce aléatoire frais et imprévisible de 16 octets au moment de l'exécution :

```csharp
// AVANT (vulnérable) :
existingNonce = "bootstrap-nonce-placeholder";
// APRÈS (sûr) :
existingNonce = Nonce.GenerateSecureNonce();

// AVANT (vulnérable) :
nonce = "fallback-nonce";
// APRÈS (sûr) :
nonce = Nonce.GenerateSecureNonce();

// AVANT (vulnérable) :
context.Items["Nonce"] = "error-fallback-nonce";
// APRÈS (sûr) :
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` utilise `RandomNumberGenerator.Fill` (un CSPRNG) pour générer 16 octets aléatoires cryptographiquement sûrs encodés en Base64. Comme il s'agit d'une méthode statique sans dépendance envers Key Vault, elle peut être appelée même lorsque Key Vault est indisponible — la condition d'erreur même qui exposait auparavant le repli codé en dur.

---

## Comment maintenir cette correction

1. **N'introduisez jamais de littéral de nonce codé en dur** où que ce soit dans le code source, quelle que soit le contexte (repli, test, espace réservé, exemple de commentaire qui peut être copié-collé, etc.).

2. **Tout chemin de code qui définit `context.Items["Nonce"]` doit utiliser une valeur aléatoire cryptographiquement sûre.** Appelez `Nonce.GenerateSecureNonce()` ou `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **Ne mettez pas en cache un seul nonce entre les requêtes.** Chaque requête doit recevoir son propre nonce frais.

4. **Les chemins d'erreur sont les plus dangereux.** Si la génération de nonce échoue pour quelque raison que ce soit, la réponse doit toujours recevoir un nonce aléatoire, jamais un repli prévisible.

5. **Examinez tout changement futur apporté à `OptimizedNonceMiddleware`** — en particulier les trois branches où le nonce peut être défini : la branche d'ignorance du chemin, la branche de génération vide et le gestionnaire d'exceptions.

### Tests qui appliquent cette correction

| Test | Ce qu'il détecte |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Échoue si `"bootstrap-nonce-placeholder"` est réintroduit dans la branche de première requête |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Échoue si `"fallback-nonce"` est réintroduit dans la branche de génération vide |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Échoue si `"error-fallback-nonce"` est réintroduit dans le gestionnaire d'exceptions |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Échoue si un repli produit le même nonce deux fois en 50 appels consécutifs (ce que ferait toute chaîne codée en dur) |
