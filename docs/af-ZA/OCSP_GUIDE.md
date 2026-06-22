# OCSP (Online Sertifikaatstatusdiens) Implementasiegids

## Oorsig

Hierdie projek sluit **sjabloonondersteuning** in vir OCSP-sertifikaat-validasie. OCSP laat intydse kontrolering van sertifikaat-herroepingstatus toe voor die verwerking van webversoeke.

## Wat is OCSP?

OCSP bied 'n alternatief vir Sertifikaat-Herroepingslyste (CRL's) vir die kontrolering of 'n sertifikaat herroep is:

- **Intydse validasie**: Kontroleer sertifikaatstatustelling onmiddellik
- **Doeltreffend**: Bevra slegs die status van spesifieke sertifikate
- **Ligggewig**: Kleiner responsse as volledige CRL-aflaaie
- **Opgedateer**: Het altyd huidige herroepingsinligting

## Konfigurasie

### 1. Funksievlag

Aktiveer OCSP-validasie in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. OCSP-Instellings

Stel OCSP-gedrag in `appsettings.json` in:

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

### Konfigurasie-Opsies

| Instelling | Tipe | Standaard | Beskrywing |
|-----------|------|-----------|-----------|
| `EnableOcspValidation` | bool | `false` | Aktiveer/deaktiveer OCSP-validasie |
| `OcspServerUrl` | string | `null` | URL van jou OCSP-responderbediener |
| `RequestTimeoutSeconds` | int | `30` | Uitstel vir OCSP-versoeke |
| `MaxRetryAttempts` | int | `3` | Aantal hertoetse by mislukking |
| `CacheDurationMinutes` | int | `60` | Hoe lank om OCSP-responsse te berg |
| `ServerUnavailableBehavior` | string | `"Warn"` | Gedrag wanneer bediener af is: `"Fail"`, `"Allow"`, of `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Aktiveer uitgebreide aantekening |
| `SkipValidationInDevelopment` | bool | `true` | Slaan OCSP in ontwikkelingsmodus oor |

---

## Sjabloon-Implementasie

Die huidige implementasie is 'n **sjabloon** wat die struktuur en API-ontwerp demonstreer. Om OCSP in produksie te gebruik, moet jy:

### 1. Implementeer die OCSP-Protokol

Vervang die sjabloon `PerformOcspValidationAsync`-metode in `OcspValidationService.cs` met werklike OCSP-protokol-implementasie:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO: Implementeer werklike OCSP-protokol
    // 1. Bou OCSP-versoek
    // 2. Stuur na OCSP-bediener
    // 3. Parseer OCSP-respons
    // 4. Valideer respons-handtekening
    // 5. Stuur sertifikaatstatustelling terug
}
```

### 2. Bou 'n OCSP-Bediener

Jy benodig 'n aparte OCSP-responderbediener wat:
- OCSP-versoeke aanvaar (RFC 6960-formaat)
- Sertifikaatstatustelling teen jou CA-databasis kontroleer
- Ondertekende OCSP-responsse terugstuur

**Opsies:**
- Gebruik kommersiële OCSP-diens (bv. DigiCert, Let's Encrypt)
- Bou 'n pasgemaakte OCSP-responder deur biblioteke te gebruik:
  - **OpenSSL** — C/C++-biblioteek met OCSP-ondersteuning
  - **BouncyCastle** — .NET-biblioteek vir OCSP
  - **Python** — `cryptography`-biblioteek met OCSP-ondersteuning

---

## Gebruiksvoorbeeld

### Basiese Sertifikaat-Validasie

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
        // Eenvoudige boolese kontrolering
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### Gedetailleerde Statusvalidasie

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    // Kontroleer status
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
            // Hanteer gebaseer op beleid
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP server unavailable");
            // Terugvalgedrag gebaseer op ServerUnavailableBehavior-instelling
            break;
    }

    return result;
}
```

---

## Integrasie met mTLS

OCSP werk naatloos saam met mTLS-sertifikaat-verifikasie:

```csharp
// In ServiceCollectionExtensions.cs
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// In sertifikaat-valideringsgeleentheid
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        // Voer OCSP-validasie uit
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

## Bediener Onbeskikbaar-Gedrag

### "Fail" — Streng Sekuriteit

```json
"ServerUnavailableBehavior": "Fail"
```

- Weier versoeke wanneer OCSP-bediener af is
- Mees veilige opsie
- Kan beskikbaarheidsprobleme veroorsaak

**Gebruik wanneer:** Maksimum sekuriteit vereis word, sertifikaat-validasie is krities

### "Allow" — Hoë Beskikbaarheid

```json
"ServerUnavailableBehavior": "Allow"
```

- Laat versoeke toe wanneer OCSP-bediener af is
- Prioritiseer beskikbaarheid bo sekuriteit
- Teken waarskuwings aan

**Gebruik wanneer:** Diensbestikbaarheid belangriker is as intydse validasie

### "Warn" — Gebalanseer (Standaard)

```json
"ServerUnavailableBehavior": "Warn"
```

- Laat versoeke toe maar teken waarskuwings aan
- Gebalanseerde benadering
- Laat monitering/waarskuwing toe

**Gebruik wanneer:** Jy OCSP-probleme wil monitor sonder om verkeer te blokkeer

---

## Berging

OCSP-responsse word gebêre om bedienerlas te verminder:

```json
"CacheDurationMinutes": 60
```

**Voordele:**
- Verminder OCSP-bediener-navrae
- Verbeter prestasie
- Bied veerkragtigheid tydens kort uitvalle

**Kasberging-ongeldigverklaring:**
- Outomaties na kasberging-duur verval
- Handmatige verwydering: herbegin toepassing

---

## Sekuriteitsoorwegings

### ✅ DOEN:

- Gebruik HTTPS vir OCSP-bediener-URL
- Valideer OCSP-respons-handtekeninge
- Stel toepaslike kasberging-duur in (balanseer varsheid teenoor prestasie)
- Gebruik "Fail"-gedrag in hoë-sekuriteitsomgewings
- Monitor OCSP-bediener-beskikbaarheid
- Implementeer hertoets-logika vir tydelike mislukkings
- Teken alle OCSP-validasiemislukkings aan

### ❌ MOENIE:

- HTTP vir OCSP in produksie gebruik
- OCSP-respons-handtekening-validasie oorslaan
- Responsse te lank berg (> 24 uur)
- OCSP-bedienermislukkings stilswyend ignoreer
- OCSP in produksie deaktiveer sonder regverdiging

---

## Implementering van 'n OCSP-Bediener

### Opsie 1: OpenSSL OCSP-Responder

```bash
# Begin OpenSSL OCSP-responder
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Opsie 2: BouncyCastle (.NET)

```csharp
// Voorbeeld met BouncyCastle-biblioteek
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. Parseer versoek
        // 2. Kontroleer sertifikaatstatustelling in databasis
        // 3. Bou respons
        // 4. Teken respons
        // 5. Stuur getekende respons terug
    }
}
```

### Opsie 3: Kommersiële OCSP-Diens

- **DigiCert**: Bestuurde OCSP-diens
- **Let's Encrypt**: Gratis OCSP vir hul sertifikate
- **GlobalSign**: Onderneming-OCSP-oplossings

---

## Monitering en Aantekening

### Aktiveer Gedetailleerde Aantekening

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

### Logboodskappe

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Toetsing

### Eenheidstoetse

Voer OCSP-toetse uit:

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Handmatige Toetsing

1. **Deaktiveer OCSP** — Verifieer dat toepassing sonder OCSP werk
2. **Ongeldige URL** — Toets ServerUnavailableBehavior-instellings
3. **Geldige Sertifikaat** — Moet `OcspStatus.Good` terugstuur
4. **Gebêrde Respons** — Verifieer dat kasberging werk

---

## Prestasieoórwegings

### Kasberging-Konfigurasie

```json
"CacheDurationMinutes": 60  // 1 uur kasberging
```

**Kompromisse:**
- **Kort duur (5–15 min)**: Meer huidige data, hoër OCSP-las
- **Lang duur (60–120 min)**: Beter prestasie, risiko van verouderde data

### Uitstel-Instellings

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Aanbevelings:**
- Uitstel: 10–30 sekondes vir produksie
- Hertoetse: 2–3 pogings vir tydelike mislukkings

---

## Probleemoplossing

### Probleem: OCSP-bediener altyd onbeskikbaar

**Oplossings:**
1. Kontroleer of `OcspServerUrl` korrek is
2. Verifieer dat firewall uitgaande HTTPS toelaat
3. Kontroleer of OCSP-bediener loop
4. Hersien logs vir uitstelfoute

### Probleem: Alle sertifikate misluk validasie

**Oplossings:**
1. Verifieer dat OCSP-bediener sertifikaatstatus-data het
2. Kontroleer of sertifikaat-ketting volledig is
3. Verseker dat OCSP-respons-handtekening geldig is
4. Hersien OCSP-bedienerlogs

### Probleem: Kasberging werk nie

**Oplossings:**
1. Verifieer `CacheDurationMinutes > 0`
2. Kontroleer of dieselfde sertifikaat-vingerafdruk gebruik word
3. Herbegin toepassing om kasberging te verwyder

---

## Volgende Stappe

Om OCSP volledig funksioneel te maak:

1. ✅ **Konfigurasie Voltooi** — Instellings is gereed
2. ✅ **Dienskoppelvlak Voltooi** — API is gedefinieer
3. ✅ **Toetse Voltooi** — 30+ eenheidstoetse ingesluit
4. 📋 **OCSP-Protokol** — Moet RFC 6960 implementeer
5. 📋 **OCSP-Bediener** — Moet OCSP-responder ontplooi
6. 📋 **Integrasie** — Verbind met mTLS-verifikasie

---

## Verwysings

- [RFC 6960](https://tools.ietf.org/html/rfc6960) — OCSP-Spesifikasie
- [BouncyCastle Dokumentasie](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Sertifikaat-Verifikasie](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Status:** ✅ Sjabloon Gereed  
**OCSP-Protokol:** 📋 Moet nog Geïmplementeer Word  
**OCSP-Bediener:** 📋 Moet nog Ontplooi Word  
**Toetse:** ✅ 30+ Toetse Ingesluit
