# Guida Implementazione OCSP (Online Certificate Status Protocol)

## Panoramica

Il progetto include il **supporto template** per la validazione OCSP dei certificati client. OCSP consente di verificare in tempo reale lo stato di revoca prima di elaborare le richieste.

## Cos'è OCSP?

OCSP è un'alternativa alle CRL per verificare se un certificato è stato revocato:
- Validazione in tempo reale
- Query mirata al singolo certificato
- Risposte leggere rispetto al download CRL completo
- Informazioni sempre aggiornate

## Configurazione

### 1. Feature flag

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Impostazioni OCSP

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.yourcompany.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

| Impostazione | Tipo | Default | Descrizione |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | Abilita/disabilita OCSP |
| `OcspServerUrl` | string | `null` | URL responder OCSP |
| `RequestTimeoutSeconds` | int | `30` | Timeout richiesta OCSP |
| `MaxRetryAttempts` | int | `3` | Numero retry in caso di errore |
| `CacheDurationMinutes` | int | `60` | Durata cache risposte OCSP |
| `ServerUnavailableBehavior` | string | `"Warn"` | `"Fail"`, `"Allow"`, `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Log verboso |
| `SkipValidationInDevelopment` | bool | `true` | Skip OCSP in dev |

## Stato implementazione

L'implementazione corrente è un **template**. Per la produzione devi:
1. Implementare il protocollo OCSP in `PerformOcspValidationAsync`
2. Configurare/deployare un responder OCSP
3. Gestire firma risposta e status del certificato secondo RFC 6960

## Esempio utilizzo

### Validazione booleana semplice

```csharp
public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
{
    return await _ocspService.ValidateCertificateAsync(clientCert);
}
```

### Validazione con dettagli

```csharp
var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);
switch (result.Status)
{
    case OcspStatus.Good:
        logger.LogInformation("Certificate is valid");
        break;
    case OcspStatus.Revoked:
        throw new SecurityException("Certificate revoked");
    case OcspStatus.Unknown:
    case OcspStatus.ServerUnavailable:
        logger.LogWarning("OCSP issue: {Status}", result.Status);
        break;
}
```

## Integrazione con mTLS

Aggiungi sia mTLS che OCSP nel wiring DI:

```csharp
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);
```

## Comportamenti server non disponibile

### `Fail`
- Rifiuta richieste se OCSP non raggiungibile
- Più sicuro ma riduce disponibilità

### `Allow`
- Consente richieste se OCSP non raggiungibile
- Massima disponibilità, sicurezza ridotta

### `Warn` (default)
- Consente richieste ma logga warning
- Compromesso consigliato in molte situazioni

## Cache

```json
"CacheDurationMinutes": 60
```

Benefici:
- Riduce carico sul responder OCSP
- Migliora performance
- Migliora resilienza durante outage brevi

## Considerazioni sicurezza

### FARE
- Usare HTTPS per `OcspServerUrl`
- Validare la firma delle risposte OCSP
- Definire una cache adeguata
- Monitorare disponibilità server OCSP
- Loggare i failure di validazione

### NON FARE
- Usare HTTP in produzione
- Saltare validazione firma
- Cache troppo lunga (>24h)
- Ignorare silenziosamente failure OCSP

## Test

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

Test manuali raccomandati:
1. OCSP disabilitato
2. URL OCSP non valido
3. Certificato valido
4. Risposta da cache

## Troubleshooting

### Server OCSP sempre non disponibile
1. Verifica `OcspServerUrl`
2. Verifica firewall e uscita HTTPS
3. Verifica stato responder
4. Controlla timeout nei log

### Tutti i certificati falliscono
1. Verifica dati di stato certificati lato OCSP
2. Verifica catena certificato
3. Verifica firma risposta OCSP

### Cache non funziona
1. Verifica `CacheDurationMinutes > 0`
2. Verifica thumbprint coerente
3. Riavvia app per pulire cache

## Riferimenti

- [RFC 6960](https://tools.ietf.org/html/rfc6960)
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Stato:** Template pronto  
**Protocollo OCSP:** Da implementare  
**Server OCSP:** Da distribuire
