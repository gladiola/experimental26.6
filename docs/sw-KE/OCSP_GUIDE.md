# Mwongozo wa Utekelezaji wa OCSP (Online Certificate Status Protocol)

## Muhtasari

Mradi huu unajumuisha **msaada wa template** kwa uthibitishaji wa cheti wa OCSP. OCSP huruhusu ukaguzi wa wakati halisi wa hali ya kufutwa kwa cheti kabla ya kuchakata maombi ya wavuti.

## OCSP ni nini?

OCSP hutoa mbadala wa Certificate Revocation Lists (CRLs) kwa kuangalia kama cheti kimefutwa:

- **Uthibitishaji wa wakati halisi**: Hukagua hali ya cheti mara moja
- **Yenye ufanisi**: Huuliza hali ya vyeti maalum pekee
- **Nyepesi**: Majibu ni madogo kuliko upakuaji kamili wa CRL
- **Ya kisasa**: Daima huwa na taarifa za sasa za kufutwa

## Usanidi

### 1. Alama ya Kipengele

Wezesha uthibitishaji wa OCSP katika `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Mipangilio ya OCSP

Sanidi tabia ya OCSP katika `appsettings.json`:

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

### Chaguo za Usanidi

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | Wezesha/zima uthibitishaji wa OCSP |
| `OcspServerUrl` | string | `null` | URL ya seva yako ya OCSP responder |
| `RequestTimeoutSeconds` | int | `30` | Timeout ya maombi ya OCSP |
| `MaxRetryAttempts` | int | `3` | Idadi ya majaribio ya kurudia baada ya kushindwa |
| `CacheDurationMinutes` | int | `60` | Muda wa kuhifadhi majibu ya OCSP kwenye cache |
| `ServerUnavailableBehavior` | string | `"Warn"` | Tabia seva ikiwa chini: `"Fail"`, `"Allow"`, au `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Wezesha uandishi wa kumbukumbu wenye maelezo mengi |
| `SkipValidationInDevelopment` | bool | `true` | Ruka OCSP katika development mode |

---

## Utekelezaji wa Template

Utekelezaji wa sasa ni **template** unaoonyesha muundo na API design. Ili kutumia OCSP katika production, lazima:

### 1. Tekeleza Itifaki ya OCSP

Badilisha method ya template ya `PerformOcspValidationAsync` katika `OcspValidationService.cs` kwa utekelezaji halisi wa itifaki ya OCSP:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO: Implement actual OCSP protocol
    // 1. Build OCSP request
    // 2. Send to OCSP server
    // 3. Parse OCSP response
    // 4. Validate response signature
    // 5. Return certificate status
}
```

### 2. Jenga Seva ya OCSP

Unahitaji seva tofauti ya OCSP responder ambayo:
- Inakubali maombi ya OCSP (muundo wa RFC 6960)
- Hukagua hali ya cheti dhidi ya database ya CA yako
- Hurudisha majibu ya OCSP yaliyosainiwa

**Chaguo:**
- Tumia huduma ya kibiashara ya OCSP (kwa mfano, DigiCert, Let's Encrypt)
- Jenga OCSP responder ya kawaida kwa kutumia libraries:
  - **OpenSSL** - C/C++ library yenye msaada wa OCSP
  - **BouncyCastle** - .NET library kwa OCSP
  - **Python** - library ya `cryptography` yenye msaada wa OCSP

---

## Mfano wa Matumizi

### Uthibitishaji wa Msingi wa Cheti

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
        // Simple boolean check
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### Uthibitishaji wa Kina wa Hali

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    // Check status
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
            // Handle based on policy
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP server unavailable");
            // Fallback behavior based on ServerUnavailableBehavior setting
            break;
    }

    return result;
}
```

---

## Ushirikiano na mTLS

OCSP hufanya kazi vizuri pamoja na uthibitishaji wa cheti wa mTLS:

```csharp
// In ServiceCollectionExtensions.cs
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// In certificate validation event
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        // Perform OCSP validation
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

## Tabia za Seva Isiyopatikana

### "Fail" - Usalama Mkali

```json
"ServerUnavailableBehavior": "Fail"
```

- Hukataa maombi wakati seva ya OCSP iko chini
- Chaguo salama zaidi
- Inaweza kusababisha matatizo ya upatikanaji

**Tumia wakati:** Usalama wa juu kabisa unahitajika, uthibitishaji wa cheti ni muhimu

### "Allow" - Upatikanaji wa Juu

```json
"ServerUnavailableBehavior": "Allow"
```

- Huruhusu maombi wakati seva ya OCSP iko chini
- Huweka kipaumbele kwa upatikanaji kuliko usalama
- Huandika maonyo kwenye kumbukumbu

**Tumia wakati:** Upatikanaji wa huduma ni muhimu zaidi kuliko uthibitishaji wa wakati halisi

### "Warn" - Ulinganifu (Chaguo-msingi)

```json
"ServerUnavailableBehavior": "Warn"
```

- Huruhusu maombi lakini huandika maonyo kwenye kumbukumbu
- Mbinu ya uwiano
- Huwezesha ufuatiliaji/alerting

**Tumia wakati:** Unataka kufuatilia matatizo ya OCSP bila kuzuia traffic

---

## Uhifadhi kwenye Cache

Majibu ya OCSP huhifadhiwa kwenye cache ili kupunguza mzigo wa seva:

```json
"CacheDurationMinutes": 60
```

**Faida:**
- Hupunguza maswali kwa seva ya OCSP
- Huboresha utendaji
- Hutoa uimara wakati wa hitilafu fupi

**Kubatilisha cache:**
- Kiotomatiki baada ya muda wa cache kuisha
- Kufuta kwa manual: anzisha upya programu

---

## Mambo ya Kuzingatia ya Usalama

### ? FANYA:

- Tumia HTTPS kwa URL ya seva ya OCSP
- Thibitisha signatures za majibu ya OCSP
- Weka muda unaofaa wa cache (sawazisha freshness dhidi ya utendaji)
- Tumia tabia ya "Fail" katika mazingira yenye usalama wa juu
- Fuatilia upatikanaji wa seva ya OCSP
- Tekeleza retry logic kwa transient failures
- Andika kushindwa kote kwa uthibitishaji wa OCSP

### ? USIFANYE:

- Kutumia HTTP kwa OCSP katika production
- Kuruka uthibitishaji wa signature ya jibu la OCSP
- Kuhifadhi majibu kwenye cache kwa muda mrefu sana (> masaa 24)
- Kupuuza kushindwa kwa seva ya OCSP kimya kimya
- Kuzima OCSP katika production bila uhalali

---

## Kutekeleza Seva ya OCSP

### Chaguo la 1: OpenSSL OCSP Responder

```bash
# Start OpenSSL OCSP responder
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Chaguo la 2: BouncyCastle (.NET)

```csharp
// Example using BouncyCastle library
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. Parse request
        // 2. Check certificate status in database
        // 3. Build response
        // 4. Sign response
        // 5. Return signed response
    }
}
```

### Chaguo la 3: Huduma ya Kibiashara ya OCSP

- **DigiCert**: Huduma inayosimamiwa ya OCSP
- **Let's Encrypt**: OCSP ya bure kwa vyeti vyao
- **GlobalSign**: Suluhisho za enterprise za OCSP

---

## Ufuatiliaji na Uandishi wa Kumbukumbu

### Wezesha Uandishi wa Kumbukumbu wa Kina

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

### Ujumbe wa Kumbukumbu

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Upimaji

### Unit Tests

Endesha majaribio ya OCSP:

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Upimaji wa Manual

1. **Zima OCSP** - Thibitisha programu inafanya kazi bila OCSP
2. **URL Isiyo Sahihi** - Jaribu mipangilio ya ServerUnavailableBehavior
3. **Cheti Halali** - Inapaswa kurudisha `OcspStatus.Good`
4. **Jibu Lililohifadhiwa Kwenye Cache** - Thibitisha cache inafanya kazi

---

## Mambo ya Kuzingatia ya Utendaji

### Usanidi wa Cache

```json
"CacheDurationMinutes": 60  // 1 hour cache
```

**Mabadilishano:**
- **Muda mfupi (dakika 5-15)**: Data ya sasa zaidi, mzigo mkubwa wa OCSP
- **Muda mrefu (dakika 60-120)**: Utendaji bora, hatari ya data iliyopitwa na wakati

### Mipangilio ya Timeout

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Mapendekezo:**
- Timeout: sekunde 10-30 kwa production
- Retries: majaribio 2-3 kwa transient failures

---

## Utatuzi wa Matatizo

### Tatizo: seva ya OCSP haipatikani kila wakati

**Suluhisho:**
1. Angalia `OcspServerUrl` ni sahihi
2. Thibitisha firewall inaruhusu outbound HTTPS
3. Angalia seva ya OCSP inaendelea kufanya kazi
4. Kagua kumbukumbu kwa makosa ya timeout

### Tatizo: vyeti vyote vinashindwa uthibitishaji

**Suluhisho:**
1. Thibitisha seva ya OCSP ina data ya hali ya cheti
2. Angalia certificate chain imekamilika
3. Hakikisha signature ya jibu la OCSP ni halali
4. Kagua kumbukumbu za seva ya OCSP

### Tatizo: cache haifanyi kazi

**Suluhisho:**
1. Thibitisha `CacheDurationMinutes > 0`
2. Angalia thumbprint ileile ya certificate inatumika
3. Anzisha upya programu ili kufuta cache

---

## Hatua Zinazofuata

Ili kufanya OCSP ifanye kazi kikamilifu:

1. ? **Usanidi Umekamilika** - Mipangilio iko tayari
2. ? **Service Interface Imekamilika** - API imefafanuliwa
3. ? **Majaribio Yamekamilika** - Unit tests 30+ zimejumuishwa
4. ?? **Itifaki ya OCSP** - Inahitaji kutekeleza RFC 6960
5. ?? **Seva ya OCSP** - Inahitaji kusambaza OCSP responder
6. ?? **Ushirikiano** - Unganisha na uthibitishaji wa mTLS

---

## Marejeo

- [RFC 6960](https://tools.ietf.org/html/rfc6960) - Ufafanuzi wa OCSP
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Hali:** ? Template Iko Tayari  
**Itifaki ya OCSP:** ?? Inapaswa Kutekelezwa  
**Seva ya OCSP:** ?? Inapaswa Kusambazwa  
**Majaribio:** ? Majaribio 30+ Yamejumuishwa

