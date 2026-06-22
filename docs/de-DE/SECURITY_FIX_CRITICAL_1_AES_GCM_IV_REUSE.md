# Sicherheitsfix: AES-GCM-IV-Wiederverwendung bei der Nonce-Erzeugung (Kritisch #1)

**Behoben in:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`, `WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Was war falsch?

Die Klasse `Nonce` nutzte AES-GCM mit einem festen IV aus Azure Key Vault. Die Wiederverwendung desselben IV mit demselben Schlüssel ist bei AES-GCM ein schwerer kryptografischer Fehler.

Zusätzlich brachte die Verschlüsselung in diesem Anwendungsfall keinen Sicherheitsgewinn: Eine CSP-Nonce muss nur **nicht vorhersagbar** und **pro Anfrage eindeutig** sein.

---

## Was wurde behoben?

`Nonce.GenerateSecureNonce()` erzeugt jetzt direkt 16 kryptografisch zufällige Bytes und kodiert sie in Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

- Keine IV-/Schlüsselabfrage aus Key Vault mehr für Nonces.
- Kein AES-GCM mehr in der Nonce-Erzeugung.
- Der `Nonce`-Konstruktor benötigt keine `KeyVaultSecret`-Parameter mehr.

Zusätzlich wurde eine Race-Condition in `NonceCatalogService.GetANonce` korrigiert (`TryGetValue` atomar mit `out` statt zweistufigem Zugriff).

---

## Wie bleibt das dauerhaft sicher?

1. Keine IV-/Schlüsselabhängigkeit für Nonce-Erzeugung einführen.
2. Keine Verschlüsselungsschemata mit wiederverwendetem IV/Counter für Nonces nutzen.
3. Nonce-Länge bei mindestens 16 Bytes belassen.
4. `RandomNumberGenerator.Fill` nicht durch nicht-kryptografische Zufallsquellen ersetzen.
5. In `NonceCatalogService.GetANonce` bei atomarem `TryGetValue(out ...)` bleiben.

### Absichernde Tests

| Test | Erkennt |
|---|---|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Regressions bei Konstruktor-Signatur |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Defekte/ungültige Nonce-Erzeugung |
| `GenerateSecureNonce_Returns16ByteBase64` | Zu kurze Nonce-Länge |
| `Nonce_SuccessiveGenerations_AreUnique` | Wiederholte, kollidierende Werte |
| `Nonce_HasSufficientEntropy` | Schwache Entropiequelle |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Rückfall auf nicht thread-sicheren Speicher |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Wiedereinführung von Race Conditions |
