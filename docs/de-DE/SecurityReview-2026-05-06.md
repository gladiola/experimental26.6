# Sicherheitsüberprüfung — WebAppExperimental266

**Datum:** 2026-05-06
**Umfang:** Statische Analyse des gesamten Quellcodes (Nachfolgeprüfung zur Überprüfung vom 2026-05-05)
**Prüfer:** Automatisierte Sicherheitsüberprüfung

---

## Zusammenfassung

Diese Nachfolgeprüfung bestätigt, dass alle 19 in der Sicherheitsüberprüfung vom 2026-05-05 identifizierten Schwachstellen behoben wurden. Die Überprüfung identifiziert außerdem 5 neue oder verbleibende Befunde, die während dieser Sitzung entdeckt wurden. Die allgemeine Sicherheitslage der Anwendung hat sich seit der vorherigen Überprüfung erheblich verbessert.

---

## Status der früheren Befunde (2026-05-05)

Alle 19 früheren Befunde sind **bestätigt behoben**:

| # | Befund | Schweregrad | Status |
|---|--------|-------------|--------|
| 1 | AES-GCM IV-Wiederverwendung bei der Nonce-Generierung | 🔴 Kritisch | ✅ Behoben |
| 2 | Nonce im Klartext protokolliert | 🔴 Kritisch | ✅ Behoben |
| 3 | Fest kodierte Fallback-Nonce-Zeichenfolgen | 🔴 Kritisch | ✅ Behoben |
| 4 | Nicht thread-sicheres globales Nonce-Dictionary | 🟠 Hoch | ✅ Behoben |
| 5 | mTLS-Ausstellervalidierung auskommentiert | 🟠 Hoch | ✅ Behoben |
| 6 | mTLS-Sperrprüfung standardmäßig deaktiviert | 🟠 Hoch | ✅ Behoben |
| 7 | OCSP gibt immer gültig zurück (Stub) | 🟠 Hoch | ✅ Behoben |
| 8 | Authentifizierung/Autorisierung standardmäßig deaktiviert | 🟠 Hoch | ✅ Behoben |
| 9 | Sicherheits-Header zu spät in der Pipeline angewendet | 🟠 Hoch | ✅ Behoben |
| 10 | Sitzungs-Cookie fehlt `Secure` + `SameSite` | 🟡 Mittel | ✅ Behoben |
| 11 | Fehlerhafter globaler `Set-Cookie`-Header | 🟡 Mittel | ✅ Behoben |
| 12 | `Content-Type` überall auf `text/html` erzwungen | 🟡 Mittel | ✅ Behoben |
| 13 | `AllowedHosts` auf Platzhalter gesetzt | 🟡 Mittel | ✅ Behoben |
| 14 | Nonce nicht auf `<script>`-Tags im Layout angewendet | 🟡 Mittel | ✅ Behoben |
| 15 | `Referrer-Policy`-Header fehlt | 🟡 Mittel | ✅ Behoben |
| 16 | PII im Klartext protokolliert | 🔵 Niedrig | ✅ Behoben |
| 17 | Teilweise Verbindungszeichenfolge in Protokollen | 🔵 Niedrig | ✅ Behoben |
| 18 | Key Vault-Operationen sind Stubs | 🔵 Niedrig | ✅ Behoben |
| 19 | Veraltetes `X-XSS-Protection: 1; mode=block` | 🔵 Niedrig | ✅ Behoben |

---

## Neue / Verbleibende Befunde

| # | Bereich | Schweregrad |
|---|---------|-------------|
| 20 | NonceRefresherService behält ungenutzte Key Vault-Konstruktorabhängigkeiten | 🟠 Hoch |
| 21 | Interner Cache von OcspValidationService verwendet nicht thread-sicheres Dictionary | 🟡 Mittel |
| 22 | OCSP-Validierungs-Stub noch vorhanden — schlägt geschlossen fehl, aber nicht implementiert | 🔵 Niedrig |
| 23 | mTLS mit leerem AllowedIssuers weist alle Zertifikate zurück (fail-closed, undokumentiert) | 🔵 Niedrig |
| 24 | OcspSettings.ServerUnavailableBehavior standardmäßig auf „Warn" (erlaubt Durchleitung bei Fehler) | 🔵 Niedrig |

---

## Detaillierte Befunde

### ✅ Bestätigte Korrekturen aus 2026-05-05

#### 1. AES-GCM IV-Wiederverwendung — Behoben

**Datei:** `Models/Main_Objects/Nonce.cs`

Die AES-GCM-basierte Nonce-Generierung wurde vollständig ersetzt. `Nonce.GenerateSecureNonce()` ruft nun `RandomNumberGenerator.Fill(randomBytes)` auf 16 zufälligen Bytes auf und gibt einen Base64-String zurück. Keine Key Vault-Abhängigkeit, kein IV, keine Verschlüsselung — genau der richtige Ansatz für einen CSP-Nonce.

---

#### 2. Nonce-Werte werden nicht mehr protokolliert — Behoben

**Dateien:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Beide Dateien protokollieren nun nur noch Statusmeldungen (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) und niemals den Nonce-Wert selbst.

---

#### 3. Fest kodierte Fallback-Nonces entfernt — Behoben

**Datei:** `Services/OptimizedNonceMiddleware.cs`

Alle drei fest kodierten Literalzeichenfolgen (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) wurden durch Aufrufe von `Nonce.GenerateSecureNonce()` sowohl im normalen als auch im Ausnahme-Fallback-Pfad ersetzt.

---

#### 4. Thread-sicheres Nonce-Dictionary — Behoben

**Datei:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` wurde durch `ConcurrentDictionary<string, Nonce>` ersetzt. `GetANonce` verwendet nun einen einzigen atomaren `TryGetValue`-Aufruf anstelle eines zweistufigen Prüfen-dann-Nachschlagen-Verfahrens.

---

#### 5. mTLS-Ausstellervalidierung jetzt funktionsfähig — Behoben

**Datei:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

Der auskommentierte Ausstellervalidierungsblock wurde durch einen Aufruf von `mtlsSettings.IsIssuerAllowed(issuer)` ersetzt, der einen Groß-/Kleinschreibung-unabhängigen Teilzeichenfolgenvergleich mit `AllowedIssuers` durchführt. Wenn die Liste leer ist (nicht konfiguriert), gibt die Methode `false` zurück und weist alle Zertifikate zurück (fail-closed).

---

#### 6. mTLS-Sperrprüfung standardmäßig aktiviert — Behoben

**Datei:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` ist nun standardmäßig `true`. Die `appsettings.template.json` gibt ebenfalls `"CheckCertificateRevocation": true` an.

---

#### 7. OCSP-Stub schlägt jetzt geschlossen fehl — Behoben

**Datei:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` gibt nun `IsValid = false` mit `OcspStatus.Error` zurück und protokolliert einen Fehler, anstatt stillschweigend `IsValid = true` zurückzugeben. Das Aktivieren von OCSP in der Konfiguration wird nun alle Zertifikate ablehnen, bis eine echte Implementierung bereitgestellt wird, anstatt sie stillschweigend zu akzeptieren.

---

#### 8. Authentifizierung und Autorisierung standardmäßig aktiviert — Behoben

**Datei:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` und `EnableAuthorization` sind nun beide standardmäßig `true` in der `FeatureFlags`-Klasse. `appsettings.json` setzt beide ebenfalls auf `true`.

---

#### 9. Sicherheits-Header vor dem Routing angewendet — Behoben

**Datei:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` und `UseStandardSecurityHeaders` werden nun vor `UseRouting`, `UseAuthentication` und `UseAuthorization` aufgerufen. Alle Antworten, einschließlich 401/403-Kurzschlüsse, erhalten die Sicherheits-Header.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce im Layout, Referrer-Policy — Behoben

**Dateien:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- Sitzungs-Cookie setzt nun `CookieSecurePolicy.Always` und `SameSiteMode.Strict`.
- Der fehlerhafte namenlose `Set-Cookie`-Header wurde entfernt.
- Das globale `Content-Type: text/html`-Override wurde entfernt.
- `AllowedHosts` in `appsettings.json` ist nun `"localhost;127.0.0.1"`; die Vorlage verwendet `"{{YOUR_HOSTNAME}}"`.
- Alle drei `<script>`-Tags in `_Layout.cshtml` enthalten nun `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` wird nun durch `UseStandardSecurityHeaders` hinzugefügt.

---

#### 16–19. PII-Protokollierung, Verbindungszeichenfolge in Protokollen, Key Vault-Stubs, X-XSS-Protection — Behoben

**Dateien:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Alle PII (OID, E-Mail, Name, SID, Rollen) werden nun per HMAC-SHA256 über `LoggingHelper.HashPii()` gehasht, bevor sie in Protokolle geschrieben werden. Ein stabiler HMAC-Schlüssel kann über `Logging:PiiHmacKey` in der Konfiguration bereitgestellt werden; ein zufälliger prozessbezogener Schlüssel wird verwendet, wenn keiner konfiguriert ist.
- Die Cosmos DB-Protokollanweisung bestätigt nun nur, ob eine Verbindungszeichenfolge vorhanden ist (`!string.IsNullOrEmpty`), nicht ihren Inhalt.
- `AzureKeyVaultCertificateOperations` wirft nun beim Start `InvalidOperationException`, wenn das Zertifikat null ist, anstatt stillschweigend Dummy-Werte zurückzugeben.
- `X-XSS-Protection` ist nun auf `"0"` gesetzt (deaktiviert den veralteten XSS-Prüfer), konsistent mit modernen Browser-Empfehlungen.

---

## 🟠 Hoch

### 20. NonceRefresherService behält ungenutzte Key Vault-Konstruktorabhängigkeiten

**Datei:** `Services/NonceRefresherService.cs`

`NonceRefresherService` deklariert weiterhin Konstruktorparameter für `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService` und `IAzureKeyVaultOperationsService`. Da die Nonce-Generierung vereinfacht wurde, um `RandomNumberGenerator` direkt zu verwenden, werden keine dieser Abhängigkeiten genutzt.

**Risiko:** Wenn `EnableNonceServices = true` und `EnableKeyVault = false` (Standard), sind diese Dienste nicht im DI-Container registriert, was zu einer `InvalidOperationException` zur Laufzeit führt, wenn der Nonce-Dienst zum ersten Mal aufgelöst wird. Dies ist effektiv eine Denial-of-Service-Bedingung, die durch die Standardkonfiguration ausgelöst wird. Die `FeatureFlags`-Klasse setzt `EnableNonceServices = true` standardmäßig, sodass jede Umgebung, die ausschließlich auf Klassenstandards angewiesen ist (ohne `appsettings.json`-Überschreibungen), nicht starten würde.

**Empfehlung:** Entfernen Sie die vier ungenutzten Konstruktorparameter und ihre entsprechenden privaten Felder aus `NonceRefresherService`. Der Dienst benötigt nur `ILogger<NonceRefresherService>`, `ILoggerFactory` und `INonceCatalogService`.

---

## 🟡 Mittel

### 21. Interner Cache von OcspValidationService verwendet nicht thread-sicheres Dictionary

**Datei:** `Services/OcspValidationService.cs` (Zeile 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` ist für gleichzeitige Lese- und Schreibvorgänge nicht thread-sicher. Wenn `OcspValidationService` als Singleton registriert ist (oder wenn dieselbe Instanz durch einen anderen Mechanismus anfragenübergreifend geteilt wird), könnten gleichzeitige OCSP-Validierungen den Cache beschädigen, was zu verlorenen Einträgen, ausgelösten Ausnahmen oder der Rückgabe veralteter Daten führt.

**Empfehlung:** Ersetzen Sie `Dictionary<string, CachedOcspResponse>` durch `ConcurrentDictionary<string, CachedOcspResponse>`. Aktualisieren Sie den `_cache.Remove`-Aufruf (Zeile 103) zu `_cache.TryRemove`.

---

## 🔵 Niedrig / Informativ

### 22. OCSP-Validierungs-Stub — Schlägt geschlossen fehl, aber nicht implementiert

**Datei:** `Services/OcspValidationService.cs` (Zeilen 157–173)

`PerformOcspValidationAsync` ist noch ein Stub. Die Korrektur aus Befund #7 hat das Verhalten korrekt von „immer gültig" auf „immer ungültig (fail-closed)" geändert. Die Methode ist jedoch noch keine echte OCSP-Implementierung. Solange `EnableOcspValidation = false` (Standard), hat dies keine Produktionsauswirkung. Bevor OCSP in einer Umgebung aktiviert wird, muss ein OCSP-Client in Produktionsqualität implementiert werden.

---

### 23. mTLS mit leerem AllowedIssuers weist alle Client-Zertifikate zurück

**Datei:** `Models/Settings/MtlsSettings.cs`

Wenn `ValidateClientCertificateIssuer = true` (Standard) und `AllowedIssuers` leer ist (ebenfalls Standard, wenn nicht konfiguriert), gibt `IsIssuerAllowed()` `false` zurück, was dazu führt, dass alle Client-Zertifikate abgelehnt werden. Dies ist korrektes Fail-Closed-Verhalten, aber es ist nicht prominent dokumentiert. Betreiber, die mTLS aktivieren, ohne die Vorlage sorgfältig zu lesen, könnten feststellen, dass alle Client-Verbindungen ohne offensichtliche Erklärung abgelehnt werden.

**Empfehlung:** Fügen Sie beim Start eine Warnungsprotokollmeldung hinzu, wenn `ValidateClientCertificateIssuer = true` und `AllowedIssuers` leer ist.

---

### 24. OcspSettings.ServerUnavailableBehavior standardmäßig auf „Warn"

**Datei:** `appsettings.template.json` (Zeile 134), `Services/OcspValidationService.cs`

Die Einstellung `ServerUnavailableBehavior` ist in der Vorlage standardmäßig auf `"Warn"` gesetzt, was es Anfragen erlaubt, durchzugehen, wenn der OCSP-Server nicht erreichbar ist. Für hochsichere Umgebungen sollte dies `"Fail"` sein, damit OCSP-Serverausfälle die Zertifikatssperrprüfung nicht stillschweigend beeinträchtigen.

**Empfehlung:** Dokumentieren Sie die drei Optionen (`Fail`, `Allow`, `Warn`) klar in der Vorlage und erwägen Sie, den Standard auf `"Fail"` zu ändern, um dem Prinzip der geringsten Berechtigung zu entsprechen.

---

## Bewertung der Sicherheits-Header (Aktueller Zustand)

Die folgenden Header werden nun über `UseStandardSecurityHeaders` angewendet:

| Header | Wert | Bewertung |
|--------|------|-----------|
| `X-Frame-Options` | `DENY` | ✅ Gut |
| `X-XSS-Protection` | `0` | ✅ Gut (deaktiviert veralteten Prüfer) |
| `X-Content-Type-Options` | `nosniff` | ✅ Gut |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Gut |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Gut |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Gut |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Gut |
| `Permissions-Policy` | Geolocation, Kamera, Mikrofon, Interest-Cohort deaktiviert | ✅ Gut |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Gut |
| `Content-Security-Policy` | Nonce-basiert, angewendet wenn CSP aktiviert | ✅ Gut |
| `Server` | Maskiert zu `"webserver"` | ✅ Gut |
| `X-Powered-By` | Entfernt | ✅ Gut |

---

## Gesamtbewertung

Die Anwendung hat alle kritischen und hochgradigen Schwachstellen der vorherigen Überprüfung behoben. Die aktuellen Befunde beschränken sich auf ein hochgradiges Konfigurations-/DI-Problem (Befund #20) und Informationselemente mit niedrigerem Schweregrad. Die Sicherheitslage hat sich erheblich verbessert. Sofortiges Handeln wird für Befund #20 (ungenutzte DI-Abhängigkeiten in NonceRefresherService) empfohlen, da dies verhindern kann, dass die Anwendung bei der Standardkonfiguration startet.
