# Sicherheitsfix: Nonce-Werte im Klartext protokolliert (Kritisch #2)

**Behoben in:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Was war falsch?

Der tatsächliche CSP-Nonce-Wert wurde an zwei Stellen unverändert ins Log geschrieben:

- `Services/NonceMiddleware.cs`
- `Services/NonceRefresherService.cs`

Damit konnte jede Person mit Logzugriff eine gültige Nonce auslesen und CSP für diese Antwort umgehen.

---

## Was wurde behoben?

Die Logmeldungen enthalten jetzt nur noch den Status (erfolgreich erzeugt/abgerufen), aber **nie** den Nonce-Wert selbst.

Beispiel:

```csharp
// Vorher (unsicher)
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// Nachher (sicher)
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

---

## Wie bleibt das dauerhaft sicher?

1. Nonce-Werte niemals loggen (auch nicht als structured field, Telemetrie-Tag oder Trace-Attribut).
2. Änderungen in `NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService` und `NonceCatalogService` gezielt auf Log-Ausgaben prüfen.
3. Nonce als per-request Secret behandeln: nur in CSP-Header/HTML-`nonce` verwenden.

### Absichernde Tests

| Test | Erkennt |
|---|---|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Wiedereinführung von Klartext-Logging in `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Wiedereinführung von Klartext-Logging in `NonceMiddleware` |
