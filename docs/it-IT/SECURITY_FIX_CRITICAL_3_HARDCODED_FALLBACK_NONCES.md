# Security Fix: Nonce di fallback hardcoded (Critico #3)

**Corretto in:** `Services/OptimizedNonceMiddleware.cs`  
**Test:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

## Problema

`OptimizedNonceMiddleware` usava stringhe hardcoded come fallback nonce:
- `"bootstrap-nonce-placeholder"`
- `"fallback-nonce"`
- `"error-fallback-nonce"`

Un nonce prevedibile annulla la protezione CSP, soprattutto in condizioni di errore (quando il fallback si attiva).

## Correzione

Tutti i fallback ora usano `Nonce.GenerateSecureNonce()`:

```csharp
existingNonce = Nonce.GenerateSecureNonce();
nonce = Nonce.GenerateSecureNonce();
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

La funzione usa CSPRNG (`RandomNumberGenerator.Fill`) e produce nonce random a 16 byte in Base64, senza dipendenze da Key Vault.

## Regole da mantenere

1. Mai nonce hardcoded nel codice
2. Ogni path che valorizza `context.Items["Nonce"]` deve usare random crittografico
3. Non riusare lo stesso nonce tra richieste
4. In caso di errore generare comunque nonce random

## Test di protezione

| Test | Cosa intercetta |
|------|------------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Reintroduzione placeholder hardcoded |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Reintroduzione fallback hardcoded |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Reintroduzione fallback eccezione hardcoded |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Nonce fallback non unici |
