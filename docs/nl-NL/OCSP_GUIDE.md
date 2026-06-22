# OCSP (Online Certificate Status Protocol) implementatiegids

## Overzicht

Dit project bevat **template-ondersteuning** voor OCSP-certificaatvalidatie. Met OCSP kun je realtime controleren of een certificaat is ingetrokken voordat webrequests worden verwerkt.

## Wat is OCSP?

OCSP is een alternatief voor Certificate Revocation Lists (CRL's) om te controleren of een certificaat is ingetrokken:

- **Realtime validatie**: controleert certificaatstatus direct
- **Efficiënt**: vraagt alleen status op van specifieke certificaten
- **Lichtgewicht**: kleinere responses dan volledige CRL-downloads
- **Actueel**: bevat steeds recente intrekkingsinformatie

## Configuratie

### 1. Featureflag

Zet OCSP-validatie aan in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. OCSP-instellingen

Configureer OCSP-gedrag in `appsettings.json`:

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

### Configuratieopties

| Instelling | Type | Standaard | Beschrijving |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | OCSP-validatie aan/uit |
| `OcspServerUrl` | string | `null` | URL van OCSP responder |
| `RequestTimeoutSeconds` | int | `30` | Timeout voor OCSP-requests |
| `MaxRetryAttempts` | int | `3` | Aantal retries bij fouten |
| `CacheDurationMinutes` | int | `60` | Duur voor cachen van OCSP-responses |
| `ServerUnavailableBehavior` | string | `"Warn"` | Gedrag bij serveruitval: `"Fail"`, `"Allow"`, of `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Uitgebreide logging aan/uit |
| `SkipValidationInDevelopment` | bool | `true` | OCSP overslaan in development |

---

## Template-implementatie

De huidige implementatie is een **template** die structuur en API-ontwerp toont. Voor productie moet je:

### 1. Het OCSP-protocol implementeren

Vervang de template-methode `PerformOcspValidationAsync` in `OcspValidationService.cs` door echte protocolimplementatie:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO: implementeer echt OCSP-protocol
    // 1. OCSP-request opbouwen
    // 2. Naar OCSP-server versturen
    // 3. OCSP-response parsen
    // 4. Response-signatuur valideren
    // 5. Certificaatstatus retourneren
}
```

### 2. Een OCSP-server bouwen

Je hebt een aparte OCSP responder nodig die:
- OCSP-requests accepteert (RFC 6960-formaat)
- Certificaatstatus controleert tegen CA-database
- Ondertekende OCSP-responses teruggeeft

**Opties:**
- Commerciële OCSP-service gebruiken (bijv. DigiCert, Let's Encrypt)
- Zelf een responder bouwen met libraries:
  - **OpenSSL** - C/C++ library met OCSP-ondersteuning
  - **BouncyCastle** - .NET library voor OCSP
  - **Python** - `cryptography` library met OCSP-ondersteuning

---

## Gebruiksvoorbeeld

### Basisvalidatie van certificaat

```csharp
public class MyCertificateHandler
{
    private readonly IOcspValidationService _ocspService;

    public MyCertificateHandler(IOcspValidationService ocspService)
    {
        _ocspService = ocspService;
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
    {
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### Gedetailleerde statusvalidatie

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    switch (result.Status)
    {
        case OcspStatus.Good:
            logger.LogInformation("Certificate is valid");
            return result;

        case OcspStatus.Revoked:
            logger.LogError("Certificate has been revoked!");
            throw new SecurityException("Certificate revoked");

        case OcspStatus.Unknown:
            logger.LogWarning("Certificate status unknown");
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP server unavailable");
            break;
    }

    return result;
}
```

---

## Integratie met mTLS

OCSP werkt naadloos samen met mTLS-certificaatauthenticatie:

```csharp
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        var ocspService = context.HttpContext.RequestServices
            .GetRequiredService<IOcspValidationService>();

        var isValid = await ocspService.ValidateCertificateAsync(
            context.ClientCertificate);

        if (!isValid)
        {
            context.Fail("Certificate validation failed via OCSP");
        }
    }
};
```

---

## Gedrag bij serveronbeschikbaarheid

### "Fail" - strikte security

```json
"ServerUnavailableBehavior": "Fail"
```

- Weigert requests wanneer OCSP-server down is
- Meest veilige optie
- Kan beschikbaarheidsproblemen geven

**Gebruik wanneer:** maximale security vereist is

### "Allow" - hoge beschikbaarheid

```json
"ServerUnavailableBehavior": "Allow"
```

- Laat requests door als OCSP-server down is
- Prioriteert beschikbaarheid boven security
- Logt waarschuwingen

**Gebruik wanneer:** beschikbaarheid belangrijker is dan realtime validatie

### "Warn" - gebalanceerd (standaard)

```json
"ServerUnavailableBehavior": "Warn"
```

- Laat requests door maar logt waarschuwingen
- Gebalanceerde aanpak
- Ondersteunt monitoring/alerting

**Gebruik wanneer:** je OCSP-problemen wilt monitoren zonder verkeer te blokkeren

---

## Caching

OCSP-responses worden gecachet om serverbelasting te verminderen:

```json
"CacheDurationMinutes": 60
```

**Voordelen:**
- Minder OCSP-serverqueries
- Betere prestaties
- Meer robuustheid bij korte uitval

**Cache-invalidatie:**
- Automatisch na cacheduur
- Handmatig: applicatie herstarten

---

## Security-overwegingen

### ✅ DO:

- Gebruik HTTPS voor OCSP-server-URL
- Valideer signaturen van OCSP-responses
- Stel geschikte cacheduur in (versheid vs prestatie)
- Gebruik `"Fail"` in high-security omgevingen
- Monitor beschikbaarheid van OCSP-server
- Implementeer retry-logica voor tijdelijke fouten
- Log alle OCSP-validatiefouten

### ❌ DON'T:

- Gebruik geen HTTP voor OCSP in productie
- Sla OCSP-signatuurvalidatie niet over
- Cache responses niet te lang (> 24 uur)
- Negeer OCSP-serverfouten niet stilzwijgend
- Schakel OCSP niet uit in productie zonder onderbouwing

---

## Een OCSP-server implementeren

### Optie 1: OpenSSL OCSP responder

```bash
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Optie 2: BouncyCastle (.NET)

```csharp
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. Request parsen
        // 2. Certificaatstatus in database controleren
        // 3. Response opbouwen
        // 4. Response ondertekenen
        // 5. Ondertekende response retourneren
    }
}
```

### Optie 3: Commerciële OCSP-service

- **DigiCert**: beheerde OCSP-service
- **Let's Encrypt**: gratis OCSP voor hun certificaten
- **GlobalSign**: enterprise OCSP-oplossingen

---

## Monitoring en logging

### Gedetailleerde logging inschakelen

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

### Logberichten

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Testen

### Unit tests

Voer OCSP-tests uit:

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Handmatig testen

1. **OCSP uitzetten** - verifieer dat app werkt zonder OCSP
2. **Ongeldige URL** - test `ServerUnavailableBehavior`
3. **Geldig certificaat** - moet `OcspStatus.Good` geven
4. **Gecachte response** - verifieer dat cache werkt

---

## Prestatieoverwegingen

### Cacheconfiguratie

```json
"CacheDurationMinutes": 60  // 1 uur cache
```

**Afwegingen:**
- **Korte duur (5-15 min)**: actuelere data, hogere OCSP-load
- **Lange duur (60-120 min)**: betere prestatie, risico op verouderde data

### Timeout-instellingen

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Aanbevelingen:**
- Timeout: 10-30 seconden voor productie
- Retries: 2-3 pogingen voor tijdelijke fouten

---

## Troubleshooting

### Probleem: OCSP-server altijd onbereikbaar

**Oplossingen:**
1. Controleer of `OcspServerUrl` correct is
2. Verifieer firewall-uitgaand HTTPS-verkeer
3. Controleer of OCSP-server draait
4. Bekijk logs op timeoutfouten

### Probleem: alle certificaten falen validatie

**Oplossingen:**
1. Verifieer dat OCSP-server statusdata heeft
2. Controleer of certificaatketen compleet is
3. Zorg dat OCSP-responsesignatuur geldig is
4. Bekijk OCSP-serverlogs

### Probleem: cache werkt niet

**Oplossingen:**
1. Verifieer `CacheDurationMinutes > 0`
2. Controleer of dezelfde certificaat-thumbprint wordt gebruikt
3. Herstart applicatie om cache te legen

---

## Volgende stappen

Om OCSP volledig werkend te maken:

1. ✅ **Configuratie compleet** - instellingen gereed
2. ✅ **Service-interface compleet** - API gedefinieerd
3. ✅ **Tests compleet** - 30+ unit tests aanwezig
4. 🚧 **OCSP-protocol** - RFC 6960 nog implementeren
5. 🚧 **OCSP-server** - OCSP responder nog deployen
6. 🚧 **Integratie** - koppelen met mTLS-authenticatie

---

## Referenties

- [RFC 6960](https://tools.ietf.org/html/rfc6960) - OCSP-specificatie
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Status:** ✅ Template gereed  
**OCSP-protocol:** 🚧 Nog te implementeren  
**OCSP-server:** 🚧 Nog te deployen  
**Tests:** ✅ 30+ tests aanwezig
