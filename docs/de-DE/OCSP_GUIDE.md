# OCSP (Online Certificate Status Protocol) — Implementierungsleitfaden

## Überblick

Dieses Projekt enthält **Template-Unterstützung** für OCSP-Zertifikatsvalidierung. OCSP ermöglicht die Echtzeitprüfung, ob ein Zertifikat widerrufen wurde.

## Was ist OCSP?

OCSP ist eine Alternative zu CRLs und bietet:
- Echtzeitvalidierung
- Zielgerichtete Abfragen einzelner Zertifikate
- Geringere Last als vollständige CRL-Downloads
- Aktuelle Widerrufsinformationen

## Konfiguration

### 1. Feature-Flag

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. OCSP-Einstellungen

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

| Einstellung | Typ | Standard | Beschreibung |
|---|---|---|---|
| `EnableOcspValidation` | bool | `false` | OCSP ein/aus |
| `OcspServerUrl` | string | `null` | URL des OCSP-Responders |
| `RequestTimeoutSeconds` | int | `30` | Timeout |
| `MaxRetryAttempts` | int | `3` | Wiederholungen bei Fehlern |
| `CacheDurationMinutes` | int | `60` | Cache-Dauer |
| `ServerUnavailableBehavior` | string | `"Warn"` | Verhalten bei Ausfall: `Fail`, `Allow`, `Warn` |
| `EnableDetailedLogging` | bool | `false` | Verbose-Logging |
| `SkipValidationInDevelopment` | bool | `true` | OCSP in Development überspringen |

---

## Template-Status

Die aktuelle Implementierung zeigt Struktur und API, enthält aber keinen vollständigen RFC-6960-Protocol-Stack.

### Erforderlich für Produktion

1. `PerformOcspValidationAsync` vollständig implementieren:
   - OCSP-Request erstellen
   - an Responder senden
   - Response parsen
   - Signatur prüfen
   - Zertifikatsstatus zurückgeben
2. OCSP-Responder bereitstellen (eigen oder gemanagt)

---

## Nutzung

### Einfache Prüfung

```csharp
public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
{
    return await _ocspService.ValidateCertificateAsync(clientCert);
}
```

### Detailstatus

```csharp
var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);
switch (result.Status)
{
    case OcspStatus.Good:
        logger.LogInformation("Certificate is valid");
        break;
    case OcspStatus.Revoked:
        logger.LogError("Certificate has been revoked!");
        throw new SecurityException("Certificate revoked");
}
```

---

## Integration mit mTLS

```csharp
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);
```

Im Zertifikats-Event kann dann OCSP-Prüfung vorgeschaltet werden, bevor ein Client-Zertifikat akzeptiert wird.

---

## Verhalten bei nicht verfügbarem Server

### `Fail`
- Anfrage wird abgelehnt
- Höchste Sicherheit, geringere Verfügbarkeit

### `Allow`
- Anfrage wird zugelassen
- Höhere Verfügbarkeit, geringere Sicherheit

### `Warn` (Standard)
- Anfrage zulassen + Warnung loggen
- Ausgewogenes Standardverhalten

---

## Caching

```json
"CacheDurationMinutes": 60
```

Vorteile:
- Weniger Last auf OCSP-Server
- Bessere Performance
- Robustheit bei kurzen Ausfällen

---

## Sicherheitsregeln

### Do
- HTTPS für OCSP-URL verwenden
- Signaturen von OCSP-Responses validieren
- Angemessene Cache-Zeiten wählen
- OCSP-Ausfälle überwachen
- Validierungsfehler vollständig protokollieren

### Don’t
- Kein HTTP in Produktion
- Signaturprüfung nie auslassen
- Cache nicht übermäßig lange halten (>24h)

---

## OCSP-Server-Optionen

1. OpenSSL-Responder
2. Eigene .NET-Lösung (z. B. BouncyCastle)
3. Gemanagte Anbieter (DigiCert, Let’s Encrypt, GlobalSign)

---

## Monitoring und Logging

```json
{
  "OcspSettings": {
    "EnableDetailedLogging": true
  },
  "Logging": {
    "LogLevel": {
      "WebAppExperimental266.Services.OcspValidationService": "Debug"
    }
  }
}
```

Typische Logs:

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Tests

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

Manuell testen:
1. OCSP deaktivieren
2. Ungültige URL testen
3. Gültiges Zertifikat prüfen
4. Cache-Verhalten prüfen

---

## Nächste Schritte

1. ✅ Konfiguration vorhanden
2. ✅ Service-Schnittstelle vorhanden
3. ✅ Unit-Tests vorhanden
4. ⏳ RFC-6960-Protokoll implementieren
5. ⏳ OCSP-Responder bereitstellen
6. ⏳ Vollständige mTLS-Integration aktivieren

---

## Referenzen

- [RFC 6960](https://tools.ietf.org/html/rfc6960)
- [BouncyCastle](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [ASP.NET Core Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
