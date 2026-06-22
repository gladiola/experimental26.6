# Securityfix: Nonce-waarden gelogd in plaintext (Kritiek #2)

**Gefixt in:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Wat was er mis

Op twee plekken werd de daadwerkelijke CSP-nonce letterlijk in de applicatielogs geschreven:

**`Services/NonceMiddleware.cs` (regel 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (regel 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Waarom dit kritiek is

Een CSP-nonce is het *enige* mechanisme dat inline-scriptinjectie verhindert zodra CSP is afgedwongen.
De veiligheid hangt volledig af van het feit dat de nonce **geheim blijft gedurende de levensduur van één response**.

Applicatielogs in cloud/enterprise-omgevingen zijn doorgaans leesbaar voor:
* Operations-teams
* Logaggregatieplatformen (bijv. Azure Monitor, Splunk, ELK)
* Elk account met leestoegang tot de logsink

Iedereen die een logregel met `Nonce: <waarde>` kan lezen, kan een inline `<script>` met die nonce injecteren en door de browser laten uitvoeren, waarmee CSP volledig wordt omzeild. Ook bij rotatie per request kan een aanvaller met live logtoegang binnen hetzelfde requestvenster handelen.

---

## Wat is gefixt

Beide logstatements zijn vervangen door berichten die de *status* van nonce-generatie bevestigen zonder de waarde prijs te geven:

**`NonceMiddleware.cs`:**
```csharp
// BEFORE (kwetsbaar):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// AFTER (veilig):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// BEFORE (kwetsbaar):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// AFTER (veilig):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## Hoe je dit gefixt houdt

1. **Log nooit de nonce-waarde.** Logregels mogen aangeven dat een nonce is gegenereerd of opgehaald (success/fail-status), maar de nonce-string zelf mag nooit in logparameters, structured logging-velden of stringinterpolatie verschijnen.

2. **Controleer elke nieuwe logregel in nonce-gerelateerde code** (`NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) om zeker te zijn dat de nonce-waarde niet wordt opgenomen.

3. **Exporteer de nonce niet naar telemetry, metrics of traces** om dezelfde reden.
Trace-attributen en span-tags worden vaak doorgestuurd naar logbackends.

4. **Behandel de nonce als een geheim per request.** De nonce mag in `HttpContext.Items` staan voor gebruik binnen één render-pipeline, maar mag het proces niet verlaten via waarneembare kanalen behalve de HTTP-responseheader en het `nonce="..."`-attribuut in de HTML die ermee wordt beveiligd.

### Tests die deze fix afdwingen

| Test | Wat het opvangt |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Faal als nonce-string opnieuw in een logboodschap van `NonceRefresherService` terechtkomt |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Faal als nonce-string opnieuw in een logboodschap van `NonceMiddleware` terechtkomt |
