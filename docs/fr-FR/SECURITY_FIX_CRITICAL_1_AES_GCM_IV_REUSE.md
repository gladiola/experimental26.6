# Correctif de sécurité : Réutilisation de l'IV AES-GCM dans la génération de nonces (Critique n°1)

**Corrigé dans :** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Tests :** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Ce qui était incorrect

La classe `Nonce` utilisait un **chiffrement AES-GCM avec un IV fixe** récupéré depuis Azure Key Vault à chaque appel. Réutiliser le même IV avec la même clé AES-GCM est une erreur cryptographique catastrophique :

* Un attaquant qui observe deux textes chiffrés avec le même IV et la même clé peut les XORer pour récupérer le XOR des deux textes en clair.
* Plus grave encore pour les étiquettes d'authentification basées sur les nonces, la réutilisation de l'IV permet la falsification des étiquettes d'authentification, compromettant totalement la garantie d'intégrité d'AES-GCM.

Au-delà de l'échec cryptographique, le chiffrement n'apportait **aucun avantage de sécurité** pour ce cas d'utilisation. Un nonce CSP ne nécessite que deux propriétés : il doit être **imprévisible** et **unique par requête**. Ces propriétés sont déjà fournies directement par un générateur de nombres aléatoires cryptographiquement sûr (`RandomNumberGenerator`). Le chiffrement ajoutait de la complexité sans apporter de sécurité.

### Code à l'origine du problème (avant correction)

```csharp
// Nonce.cs — même IV récupéré depuis Key Vault à chaque appel
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — récupéré une fois et réutilisé pour toutes les requêtes
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## Ce qui a été corrigé

`Nonce.GenerateSecureNonce()` appelle maintenant directement `RandomNumberGenerator.Fill(byte[])` pour produire 16 octets de données aléatoires cryptographiquement sûres, puis encode le résultat en Base64 :

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* Aucun appel à Key Vault pour l'IV ou la clé de chiffrement n'est nécessaire ou effectué.
* Aucun AES-GCM ni aucun autre chiffre n'est impliqué.
* Le constructeur `Nonce` n'accepte plus de paramètres `KeyVaultSecret`.

Un bogue secondaire a également été corrigé dans `NonceCatalogService.GetANonce` : la méthode utilisait auparavant une vérification en deux étapes (vérifier puis rechercher — `TryGetValue` suivi de l'indexeur `[]`), qui n'est pas atomique et pouvait lever une `KeyNotFoundException` lorsqu'un autre fil d'exécution supprimait la clé entre les deux appels. La correction utilise `TryGetValue` avec le paramètre `out` pour récupérer la valeur en une seule opération atomique.

---

## Comment maintenir cette correction

1. **N'introduisez jamais d'IV ou de clé Key Vault pour la génération de nonces.** Si Key Vault est utilisé pour d'autres secrets, c'est acceptable — mais la génération de nonces ne doit jamais dépendre d'un IV fixe.

2. **Ne remplacez jamais `GenerateSecureNonce` par un schéma AES-GCM ou CBC/CTR** qui réutilise un IV ou un compteur entre les requêtes.

3. **Maintenez le nonce à au moins 16 octets (128 bits).** Réduire la longueur en octets augmente la probabilité de collision et réduit l'entropie disponible pour la CSP.

4. **Ne remplacez pas `RandomNumberGenerator.Fill` par `new Random()`** ou tout autre générateur non cryptographiquement sûr.

5. **Maintenez `NonceCatalogService.GetANonce` en utilisant `TryGetValue` avec le paramètre `out`.** Le schéma de vérification en deux étapes (`TryGetValue` + indexeur) n'est pas thread-safe même avec `ConcurrentDictionary`.

### Tests qui appliquent cette correction

| Test | Ce qu'il détecte |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Échoue à la compilation si le constructeur est rétabli pour accepter les paramètres IV + clé `KeyVaultSecret` |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Échoue si la génération de nonce est défaillante ou retourne une valeur non Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | Échoue si la longueur en octets est réduite en dessous de 16 |
| `Nonce_SuccessiveGenerations_AreUnique` | Échoue si la réutilisation de l'IV produit le même nonce de façon répétée |
| `Nonce_HasSufficientEntropy` | Échoue si la source d'entropie n'est pas aléatoire |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Échoue si `ConcurrentDictionary` est rétabli en `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Échoue si la race TOCTOU dans `GetANonce` est réintroduite |
