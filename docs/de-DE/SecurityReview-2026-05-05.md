# Sicherheitsprüfung — WebAppExperimental266

**Datum:** 2026-05-05  
**Umfang:** Statische Analyse der gesamten Codebasis

---

## Übersichtstabelle

| # | Bereich | Schweregrad |
|---|---|---|
| 1 | AES-GCM-IV-Wiederverwendung bei Nonce-Erzeugung | 🔴 Kritisch ✅ |
| 2 | Nonce im Klartext protokolliert | 🔴 Kritisch ✅ |
| 3 | Hartcodierte Fallback-Nonces | 🔴 Kritisch ✅ |
| 4 | Nicht thread-sicheres globales Nonce-Dictionary | �� Hoch |
| 5 | mTLS-Issuer-Validierung auskommentiert | 🟠 Hoch |
| 6 | mTLS-Sperrprüfung standardmäßig deaktiviert | 🟠 Hoch |
| 7 | OCSP gibt immer gültig zurück (Stub) | 🟠 Hoch |
| 8 | Authentifizierung/Autorisierung standardmäßig aus | 🟠 Hoch |
| 9 | Security-Header zu spät in der Pipeline | 🟠 Hoch |
| 10 | Session-Cookie ohne Secure + SameSite | 🟡 Mittel |
| 11 | Ungültiger globaler Set-Cookie-Header | 🟡 Mittel |
| 12 | Content-Type überall auf text/html erzwungen | 🟡 Mittel |
| 13 | AllowedHosts ist Wildcard | 🟡 Mittel |
| 14 | Nonce nicht auf `<script>`-Tags im Layout angewendet | 🟡 Mittel |
| 15 | Referrer-Policy-Header fehlt | 🟡 Mittel |
| 16 | PII im Klartext geloggt | 🔵 Niedrig |
| 17 | Teilweise Connection-String-Ausgabe in Logs | 🔵 Niedrig |
| 18 | Key-Vault-Operationen sind Stubs | 🔵 Niedrig |
| 19 | Veralteter X-XSS-Protection-Header | 🔵 Niedrig |

---

## 🔴 Kritisch

### 1. AES-GCM-IV-Wiederverwendung — kryptografisch fehlerhafte Nonce-Erzeugung ✅ Behoben in Commit 45ae31b

**Dateien:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`

Die CSP-Nonce-Erzeugung nutzte AES-GCM mit festem IV aus Key Vault. Bei AES-GCM ist IV-Wiederverwendung mit demselben Schlüssel katastrophal: Vertraulichkeit und Integrität brechen zusammen.

**Korrektur:** CSP-Nonces werden jetzt korrekt als zufällige Werte pro Anfrage mit `RandomNumberGenerator.GetBytes(16)` erzeugt und Base64-kodiert.

---

### 2. Nonce-Werte im Klartext geloggt ✅ Behoben in Commit bb6f27a

**Dateien:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Nonce-Werte wurden direkt protokolliert. Log-Leser konnten dadurch CSP umgehen und Inline-Skripte einschleusen.

---

### 3. Hartcodierte Fallback-Nonces ✅ Behoben in Commit 11cc9f7

**Datei:** `Services/OptimizedNonceMiddleware.cs`

Bei Fehlern wurden feste Strings wie `"fallback-nonce"` genutzt. Diese sind vorhersagbar und damit angreifbar.

---

## 🟠 Hoch

### 4. NonceCatalogService mit nicht thread-sicherem Dictionary ✅ Behoben in Commit ae2b6c9

**Datei:** `Services/NonceCatalogService.cs`

Ein globales `Dictionary<TKey, TValue>` wurde konkurrierend gelesen/geschrieben. Das führte zu Race Conditions und potenzieller Datenkorruption. Lösung: `ConcurrentDictionary` plus per-request Zugriff.

---

### 5. mTLS-Issuer-Validierung stubbed/outkommentiert ✅ Behoben in Commit fd3d4fb

**Datei:** `Extensions/ServiceCollectionExtensions.cs`

`ValidateClientCertificateIssuer` war vorhanden, die eigentliche Prüfung aber deaktiviert. Ergebnis: Zertifikate beliebiger Issuer konnten akzeptiert werden.

---

### 6. mTLS-Sperrprüfung standardmäßig deaktiviert ✅ Behoben in Commit fd3d7b3

**Dateien:** `Models/Settings/MtlsSettings.cs`, `appsettings.template.json`

`CheckCertificateRevocation` stand auf `false`. Widerrufene Zertifikate konnten damit weiterhin genutzt werden.

---

### 7. OCSP-Validierung als Stub immer „gültig“ ✅ Behoben in Commit b4c3807

**Datei:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` war eine Platzhalterimplementierung und ließ Zertifikate trotz fehlender echter Prüfung als gültig durch.

---

### 8. Authentifizierung und Autorisierung standardmäßig aus ✅ Behoben in Commit b392c47

**Datei:** `appsettings.json`

Standardkonfiguration erlaubte eine offene Bereitstellung ohne AuthN/AuthZ.

---

### 9. Security-Header erst nach Routing/Auth angewendet ✅ Behoben in Commit 016e57c

**Datei:** `Program.cs`

Header-Middleware war zu spät registriert; kurzgeschlossene Antworten (401/403) konnten ohne Schutzheader ausgeliefert werden.

---

## 🟡 Mittel

### 10. Session-Cookie ohne `Secure` und `SameSite` ✅ Behoben in Commit 8f2223c

**Datei:** `Extensions/ServiceCollectionExtensions.cs`

Cookie-Flags waren unvollständig und erhöhten das Risiko für unsichere Übertragung bzw. Cross-Site-Sendung.

---

### 11. Fehlerhafter globaler `Set-Cookie`-Header ✅ Behoben in Commit 8f2223c

**Datei:** `Extensions/ApplicationBuilderExtensions.cs`

Es wurde ein namenloser `Set-Cookie`-Header an jede Antwort angehängt. Das ist ungültig und unerwartet.

---

### 12. `Content-Type` global auf `text/html` gesetzt ✅ Behoben in Commit 8f2223c

**Datei:** `Extensions/ApplicationBuilderExtensions.cs`

API/JSON/Downloads wurden fälschlich als HTML deklariert.

---

### 13. `AllowedHosts` als Wildcard ✅ Behoben in Commit 8f2223c

**Dateien:** `appsettings.json`, `appsettings.template.json`

Wildcard deaktivierte wirksame Host-Header-Validierung.

---

### 14. Layout ohne Nonce auf `<script>`-Tags ✅ Behoben in Commit 8f2223c

**Datei:** `Views/Shared/_Layout.cshtml`

Bei aktivierter CSP mit Nonces konnten benötigte Skripte blockiert werden, da `nonce`-Attribute fehlten.

---

### 15. Fehlender Referrer-Policy-Header ✅ Behoben in Commit 8f2223c

**Datei:** `Extensions/ApplicationBuilderExtensions.cs`

Ohne Header konnten zu viele URL-Informationen in `Referer`-Headern weitergegeben werden.

---

## 🔵 Niedrig / Informativ

### 16. PII im Klartext geloggt ✅ Behoben in Commit 93bb4e9

**Datei:** `Services/LoggingHelper.cs`

OID, E-Mail, Name und Session-IDs wurden direkt geloggt. Empfehlung: Hashing/Maskierung.

---

### 17. Teilweiser Connection-String in Logs ✅ Behoben in Commit 93bb4e9

**Datei:** `Extensions/ServiceCollectionExtensions.cs`

Auch Teilausgaben von Secrets sollten vermieden werden.

---

### 18. Key-Vault-Operationen als Stubs ✅ Behoben in Commit 93bb4e9

**Datei:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

Template-Code lieferte Dummy/Null-Werte und war nicht produktionsreif.

---

### 19. `X-XSS-Protection: 1; mode=block` veraltet ✅ Behoben in Commit 93bb4e9

**Datei:** `Extensions/ApplicationBuilderExtensions.cs`

Moderne Browser ignorieren diesen Header; zuverlässiger ist eine starke CSP.
