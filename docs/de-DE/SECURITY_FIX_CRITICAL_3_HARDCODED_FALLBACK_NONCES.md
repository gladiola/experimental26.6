# Sicherheitsfix: Hartcodierte Fallback-Nonces (Kritisch #3)

**Behoben in:** `Services/OptimizedNonceMiddleware.cs`  
**Tests:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Was war falsch?

`OptimizedNonceMiddleware` verwendete feste String-Literale als Fallback-Nonces:

- `"bootstrap-nonce-placeholder"`
- `"fallback-nonce"`
- `"error-fallback-nonce"`

Vorhersagbare Nonces machen CSP praktisch wirkungslos.

---

## Was wurde behoben?

Alle Fallback-Pfade erzeugen jetzt eine frische, kryptografisch zufällige Nonce:

```csharp
existingNonce = Nonce.GenerateSecureNonce();
nonce = Nonce.GenerateSecureNonce();
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` verwendet `RandomNumberGenerator.Fill` für 16 zufällige Bytes (Base64-kodiert).

---

## Wie bleibt das dauerhaft sicher?

1. Niemals hartcodierte Nonce-Literale einführen.
2. Jeder Pfad, der `context.Items["Nonce"]` setzt, muss einen kryptografisch zufälligen Wert verwenden.
3. Keine Nonce über mehrere Requests wiederverwenden.
4. Fehlerpfade besonders prüfen: auch dort nur zufällige Nonces.

### Absichernde Tests

| Test | Erkennt |
|---|---|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Wiedereinführung von `bootstrap-nonce-placeholder` |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Wiedereinführung von `fallback-nonce` |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Wiedereinführung von `error-fallback-nonce` |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Nicht-zufällige/identische Fallback-Nonces |
