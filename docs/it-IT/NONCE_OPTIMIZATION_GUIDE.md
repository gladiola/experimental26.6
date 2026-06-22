# Guida all'ottimizzazione della generazione nonce

## Problema attuale

Il nonce viene generato a ogni richiesta HTTP, incluse richieste statiche e health check, causando:
- Troppe chiamate ad Azure Key Vault
- Operazioni crittografiche non necessarie
- Degrado prestazioni
- Costi Azure maggiori

## Soluzione

Generare un nonce nuovo **solo per risposte HTML** che richiedono header CSP.

## Strategia consigliata

### Opzione 1: filtro per path (semplice)
Evita generazione nonce per path statici/API (`/css`, `/js`, `/lib`, `/images`, `/api`, ecc.).

### Opzione 2: generazione in response pipeline (consigliata)
Usa `context.Response.OnStarting(...)` e genera nonce soltanto quando `Content-Type` è `text/html`.

### Opzione 3: lazy generation (massima efficienza)
Genera nonce solo quando effettivamente serve per costruire la CSP, con caching breve e lock concorrente.

## Esempio middleware ottimizzato

```csharp
if (ShouldIgnoreRequest(context.Request))
{
    var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
    if (string.IsNullOrEmpty(existingNonce))
    {
        existingNonce = Nonce.GenerateSecureNonce();
    }
    context.Items["Nonce"] = existingNonce;
    await _next(context);
    return;
}

await _nonceRefresherService.RefreshNonceAsync();
var nonce = _nonceCatalogService.GetANonce("CSPNonce");
context.Items["Nonce"] = nonce;
await _next(context);
```

## Benefici attesi

### Prima
- 1.000 richieste/minuto
- 1.000 generazioni nonce
- 2.000 chiamate Key Vault (IV + key)

### Dopo
- 1.000 richieste/minuto
- ~100 generazioni nonce (solo pagine)
- ~200 chiamate Key Vault

**Riduzione stimata: ~90%**

## Test di verifica

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"                # deve generare nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"    # non deve generare nonce
Invoke-WebRequest "https://localhost:5001/Privacy"         # deve generare nonce
```

## Migrazione

1. Backup `NonceMiddleware.cs`
2. Crea `OptimizedNonceMiddleware.cs`
3. Aggiorna registrazione middleware in `Program.cs`
4. Testa static file e pagine Razor
5. Monitora metriche Key Vault
6. Rimuovi middleware legacy dopo validazione

## Configurazione suggerita

```json
{
  "NonceGeneration": {
    "GenerateForStaticFiles": false,
    "GenerateForApiCalls": false,
    "NonceLifetimeMinutes": 5,
    "EnableOptimization": true
  }
}
```
