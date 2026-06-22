# Security Fix: Riutilizzo IV AES-GCM nella generazione nonce (Critico #1)

**Corretto in:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`  
**Test:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`, `WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

## Problema

La classe `Nonce` usava AES-GCM con IV fisso recuperato da Key Vault a ogni chiamata. Riutilizzare lo stesso IV con la stessa chiave in AES-GCM è un errore crittografico grave:

- espone relazioni tra plaintext
- consente forgery del tag di autenticazione
- compromette integrità e confidenzialità

Inoltre la cifratura non era necessaria per CSP nonce: servono solo **imprevedibilità** e **unicità per richiesta**.

## Correzione

`Nonce.GenerateSecureNonce()` ora usa direttamente `RandomNumberGenerator.Fill(byte[])` per creare 16 byte casuali e codificarli in Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

Effetti:
- nessuna chiamata Key Vault per IV/chiave nonce
- nessun uso AES-GCM per nonce
- costruttore `Nonce` semplificato (niente `KeyVaultSecret`)

È stato anche corretto un problema TOCTOU in `NonceCatalogService.GetANonce`: ora usa `TryGetValue(out ...)` in modo atomico.

## Come mantenere la correzione

1. Non reintrodurre IV/chiavi Key Vault per generazione nonce
2. Non sostituire con schemi AES che possono riusare IV/counter
3. Mantenere nonce di almeno 16 byte
4. Non sostituire CSPRNG con `new Random()`
5. Mantenere uso thread-safe di `TryGetValue(out ...)`

## Test di protezione

| Test | Cosa intercetta |
|------|------------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Regressioni firma costruttore |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Nonce non valido/non Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | Riduzione lunghezza nonce |
| `Nonce_SuccessiveGenerations_AreUnique` | Collisioni/riuso nonce |
| `Nonce_HasSufficientEntropy` | Fonte casuale debole |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Regressione struttura concorrente |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Regressione race condition |
