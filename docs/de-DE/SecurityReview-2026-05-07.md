# Sicherheitsüberprüfung — WebAppExperimental266

**Datum:** 2026-05-07
**Umfang:** Vollständige statische Codeanalyse (Nachfolge der Überprüfung vom 2026-05-06)
**Prüfer:** Automatisierte Sicherheitsüberprüfung

---

## Zusammenfassung

Diese Nachfolgeüberprüfung bestätigt, dass 3 der 5 Schwachstellen, die in der Sicherheitsüberprüfung vom 2026-05-06 identifiziert wurden, vollständig behoben wurden, wobei 1 teilweise behoben wurde. Die Überprüfung identifiziert auch 4 neue Befunde. Die allgemeine Sicherheitslage der Anwendung verbessert sich weiterhin.

---

## Status früherer Befunde (2026-05-06)

| # | Befund | Schweregrad | Status |
|---|---------|----------|--------|
| 20 | NonceRefresherService behält ungenutzte Key Vault-Konstruktor-Abhängigkeiten | 🟠 Hoch | ✅ Behoben |
| 21 | OcspValidationService interner Cache verwendet nicht-thread-sicheres Dictionary | 🟡 Mittel | ✅ Behoben |
| 22 | OCSP-Validierungs-Stub noch vorhanden — schlägt geschlossen fehl, aber nicht implementiert | 🔵 Niedrig | ⚠️ Akzeptiert (durch Design) |
| 23 | mTLS mit leerem AllowedIssuers lehnt alle Zertifikate ab (fail-closed, undokumentiert) | 🔵 Niedrig | ✅ Behoben |
| 24 | OcspSettings.ServerUnavailableBehavior ist standardmäßig auf "Warn" gesetzt (ermöglicht Durchleitung bei Fehler) | 🔵 Niedrig | ⚠️ Teilweise behoben |

---

## Detaillierter Status früherer Befunde

### ✅ 20. NonceRefresherService Ungenutzte DI-Abhängigkeiten — Behoben

**Datei:** `Services/NonceRefresherService.cs`

Der `NonceRefresherService`-Konstruktor deklariert jetzt nur noch `ILogger<NonceRefresherService>`, `ILoggerFactory` und `INonceCatalogService`. Die vier zuvor ungenutzten Abhängigkeiten (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) wurden entfernt. Damit wird das Denial-of-Service-Risiko behoben, das verhindert hat, dass die Anwendung gestartet werden konnte, wenn `EnableKeyVault = false` (Standard) und `EnableNonceServices = true` (Standard).

---

### ✅ 21. OcspValidationService Nicht-Thread-Sicherer Cache — Behoben

**Datei:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` wurde durch `ConcurrentDictionary<string, CachedOcspResponse>` ersetzt. Der `_cache.Remove`-Aufruf wurde auf `_cache.TryRemove` aktualisiert. Der Cache ist jetzt sicher für den gleichzeitigen Zugriff.

---

### ⚠️ 22. OCSP-Validierungs-Stub — Akzeptiert (durch Design)

**Datei:** `Services/OcspValidationService.cs`

Der Stub ist noch vorhanden, schlägt jedoch korrekt geschlossen fehl. Da `EnableOcspValidation` standardmäßig auf `false` gesetzt ist, hat dies keine Auswirkungen auf die Produktion. Dies wird als informativer Befund akzeptiert, bis eine vollständige OCSP-Implementierung vorliegt.

---

### ✅ 23. mTLS Leere AllowedIssuers — Behoben

**Datei:** `Extensions/ServiceCollectionExtensions.cs`

Eine Startwarnung wird jetzt protokolliert, wenn `ValidateClientCertificateIssuer = true` und `AllowedIssuers` leer ist:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Dies gibt Betreibern, die auf das fail-closed-Verhalten stoßen, eine klare Orientierung.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Teilweise behoben

**Dateien:** `appsettings.template.json` (behoben), `Models/Settings/OcspSettings.cs` (noch nicht behoben)

Die Vorlage gibt jetzt korrekt `"ServerUnavailableBehavior": "Fail"` an. Der C#-Klassen-Standard in `OcspSettings.cs` (Zeile 39) bleibt jedoch `"Warn"`. Wenn ein Betreiber OCSP aktiviert und `ServerUnavailableBehavior` aus seiner Konfigurationsdatei weglässt, wird der Klassen-Standard `"Warn"` stillschweigend angewendet, was eine Durchleitung bei OCSP-Server-Ausfällen erlaubt. Der Klassen-Standard sollte geändert werden, um der Vorlagenempfehlung zu entsprechen.

---

## Neue Befunde

| # | Bereich | Schweregrad |
|---|------|----------|
| 25 | OcspSettings-Klassen-Standard ("Warn") weicht von Vorlage ("Fail") ab | 🔵 Niedrig |
| 26 | NonceCatalogService einzelner gemeinsamer Nonce-Schlüssel ermöglicht anforderungsübergreifende Nonce-Kollision | 🟡 Mittel |
| 27 | OptimizedNonceMiddleware statische Zähler verwenden vorzeichenbehaftete 32-Bit-Ganzzahlen (Überlaufrisiko) | 🔵 Niedrig |
| 28 | Program.cs registriert leeres ILoggerFactory-Singleton und überschattet den Framework-Logger | 🟡 Mittel |

---

## 🟡 Mittel

### 26. NonceCatalogService Gemeinsamer Nonce-Schlüssel Ermöglicht Anforderungsübergreifende Nonce-Kollision

**Dateien:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Der Nonce-Katalog speichert alle Nonces unter einem einzigen gemeinsamen Schlüssel `"CSPNonce"`. Unter gleichzeitiger Last ist die folgende Race-Condition möglich:

1. Anfrage A ruft `RefreshNonceAsync()` auf — Nonce A1 wird als `_nonceCollection["CSPNonce"]` gespeichert.
2. Anfrage B ruft `RefreshNonceAsync()` auf — Nonce B1 überschreibt `_nonceCollection["CSPNonce"]`.
3. Anfrage A ruft `GetANonce("CSPNonce")` auf — erhält B1, nicht A1.
4. CSP-Header und Layout-Nonce von Anfrage A enthalten beide B1.
5. Anfrage B enthält ebenfalls B1.

Zwei gleichzeitige Antworten teilen dieselbe Nonce. Obwohl beide Werte kryptografisch zufällig und unvorhersehbar sind (kein hartcodierter String), erscheint derselbe Nonce-Wert in mehreren gleichzeitigen Antworten und schwächt damit die pro-Anfrage-Eindeutigkeitsgarantie, die die CSP-Spezifikation fordert. Ein Angreifer, der die Nonce einer Antwort beobachten kann, verfügt über eine gültige Nonce für mindestens eine andere gleichzeitige Antwort.

**Empfehlung:** Generieren Sie die Nonce direkt innerhalb der Middleware pro Anfrage (z.B. `Nonce.GenerateSecureNonce()`) und speichern Sie sie nur in `HttpContext.Items["Nonce"]`, wobei der gemeinsame Katalog für anforderungsspezifische Nonces umgangen wird. Der gemeinsame Katalog würde dann nur benötigt, wenn eine Nonce über Middleware-Schichten hinweg innerhalb einer einzelnen Anfrage geteilt werden muss, was `HttpContext.Items` bereits nativ behandelt.

---

### 28. Program.cs Registriert Leeres ILoggerFactory-Singleton

**Datei:** `Program.cs` (Zeile 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core registriert automatisch eine vollständig konfigurierte `ILoggerFactory` (mit allen Logging-Providern aus der `builder.Logging`-Konfiguration) während `WebApplication.CreateBuilder`. Diese explizite `AddSingleton`-Registrierung fügt eine zweite, unkonfigurierte `LoggerFactory`-Instanz ohne Provider hinzu. Da `GetRequiredService<ILoggerFactory>()` die zuletzt registrierte Implementierung zurückgibt, verwenden Dienste, die `ILoggerFactory` über Dependency Injection erhalten (wie `NonceRefresherService`), diese leere Factory und produzieren keine Protokollausgabe über `_loggerFactory.CreateLogger<T>()`.

**Risiko:** Stilles Logging in `NonceRefresherService` — Erfolge und Fehler bei der Nonce-Generierung werden an keine konfigurierte Protokollsenke ausgegeben. Dies reduziert die Beobachtbarkeit der Anwendung während sicherheitsrelevanter Vorgänge, ohne die Funktionalität zu beeinträchtigen.

**Empfehlung:** Entfernen Sie die explizite `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`-Registrierung. Die konfigurierte `ILoggerFactory` des Frameworks (mit Konsole und anderen Providern) wird dann korrekt von Diensten aufgelöst, die davon abhängen.

---

## 🔵 Niedrig / Informativ

### 25. OcspSettings-Klassen-Standard Weicht von Vorlage ab

**Datei:** `Models/Settings/OcspSettings.cs` (Zeile 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Die Vorlage (`appsettings.template.json`) gibt `"ServerUnavailableBehavior": "Fail"` an, aber der C#-Klassen-Standard ist `"Warn"`. Wenn `ServerUnavailableBehavior` in der aktiven Konfigurationsdatei fehlt, wird der Klassen-Standard stillschweigend angewendet, anstatt der Vorlagenempfehlung zu folgen. Dies ist ein Überbleibsel von Befund #24.

**Empfehlung:** Ändern Sie den Klassen-Standard von `"Warn"` zu `"Fail"`, um ihn an die Vorlage und das Prinzip der minimalen Rechte anzupassen.

---

### 27. OptimizedNonceMiddleware Statische Zähler Können Überlaufen

**Datei:** `Services/OptimizedNonceMiddleware.cs` (Zeilen 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Diese vorzeichenbehafteten 32-Bit-Zähler werden atomisch über `Interlocked.Increment` inkrementiert. Nach ungefähr 2,1 Milliarden Inkrementierungen werden sie auf `int.MinValue` (−2.147.483.648) umschlagen, wodurch die Effizienzberechnung `(total - generated) * 100.0 / total` falsche oder bedeutungslose Ergebnisse liefert. Bei 1.000 Anfragen pro Sekunde tritt ein Überlauf nach ungefähr 24,8 Tagen kontinuierlichem Betrieb auf.

**Empfehlung:** Ändern Sie die Zähler-Feldtypen von `int` zu `long` und verwenden Sie die `long`-Überladung von `Interlocked.Increment`, um einen Überlauf zu verhindern.

---

## Bewertung der Sicherheitsheader (Aktueller Zustand)

Die folgenden Header werden über `UseStandardSecurityHeaders` angewendet — unverändert gegenüber der vorherigen Überprüfung:

| Header | Wert | Bewertung |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Gut |
| `X-XSS-Protection` | `0` | ✅ Gut (deaktiviert veralteten Auditor) |
| `X-Content-Type-Options` | `nosniff` | ✅ Gut |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Gut |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Gut |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Gut |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Gut |
| `Permissions-Policy` | Geolokalisierung, Kamera, Mikrofon, Interessengruppe deaktiviert | ✅ Gut |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Gut |
| `Content-Security-Policy` | Nonce-basiert, angewendet wenn CSP aktiviert | ✅ Gut |
| `Server` | Maskiert auf `"webserver"` | ✅ Gut |
| `X-Powered-By` | Entfernt | ✅ Gut |

---

## Gesamtbewertung

Alle hochschweren Befunde aus früheren Überprüfungen wurden behoben. Die aktuellen Befunde beschränken sich auf zwei mittelschwere Probleme (#26 gemeinsamer Nonce-Schlüssel, #28 leeres ILoggerFactory) und zwei informative Punkte mit niedrigem Schweregrad (#25 Klassen-Standard-Abweichung, #27 Ganzzahlüberlauf in Zählern). Sofortige Aufmerksamkeit wird für Befund #28 (leeres ILoggerFactory-Singleton) empfohlen, da er sicherheitsrelevante Diagnose-Protokollierung während Nonce-Operationen stillschweigend unterdrückt. Befund #26 (gemeinsamer Nonce-Schlüssel) sollte behoben werden, um die Garantie der Nonce-Eindeutigkeit pro Anfrage wiederherzustellen, die die CSP-Spezifikation fordert.
