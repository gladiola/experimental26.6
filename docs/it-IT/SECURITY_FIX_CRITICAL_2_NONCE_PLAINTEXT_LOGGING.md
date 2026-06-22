# Security Fix: Valori nonce loggati in chiaro (Critico #2)

**Corretto in:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Test:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

## Problema

Due punti del codice loggavano il valore CSP nonce in chiaro.

Questo è critico perché il nonce è il meccanismo che impedisce l'iniezione di script inline con CSP. Se il valore compare nei log, chi può leggere i log può bypassare CSP durante la finestra della risposta.

## Correzione

I log ora riportano solo lo stato, non il valore del nonce:

- `"Nonce retrieved for request."`
- `"Nonce generated successfully."`

## Regole da mantenere

1. Non loggare mai la stringa nonce
2. Revisionare ogni nuovo log in `NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`
3. Non esporre nonce in telemetry/metriche/tracce
4. Trattare nonce come segreto per richiesta

## Test di protezione

| Test | Cosa intercetta |
|------|------------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Reintroduzione nonce nei log del refresher |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Reintroduzione nonce nei log middleware |
