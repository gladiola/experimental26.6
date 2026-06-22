# Aratohu Whakatinanatanga OCSP (Online Certificate Status Protocol)

## Tirohanga Whānui

Kei roto i tēnei kaupapa te **tautoko tātauira** mō te manatoko tiwhikete OCSP. Mā OCSP e taea ai te
tirotiro ora o te tūnga whakakorenga tiwhikete i mua i te tukatuka tono tukutuku.

## He aha te OCSP?

He kē OCSP mō ngā Certificate Revocation Lists (CRLs):

- **Manatoko wā-tūturu**
- **Pai ake te tōtika** (tono mō te tiwhikete motuhake)
- **Mama ake** i ngā CRL nui
- **Kōrero hou tonu** mō te whakakorenga

## Whirihoranga

### 1. Haki Āhuatanga

Whakahohea i `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Tautuhinga OCSP

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

### Kōwhiringa whirihoranga

| Tautuhinga | Momo | Taunoa | Whakamārama |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | Whakahohe/whakaweto OCSP |
| `OcspServerUrl` | string | `null` | URL o te OCSP responder |
| `RequestTimeoutSeconds` | int | `30` | Wā tatari tono OCSP |
| `MaxRetryAttempts` | int | `3` | Maha o ngā whakamātau anō |
| `CacheDurationMinutes` | int | `60` | Roa penapena whakautu OCSP |
| `ServerUnavailableBehavior` | string | `"Warn"` | Ina hinga te tūmau: `Fail`, `Allow`, `Warn` |
| `EnableDetailedLogging` | bool | `false` | Whakahohe logs taipitopito |
| `SkipValidationInDevelopment` | bool | `true` | Peke OCSP i dev mode |

---

## Whakatinanatanga Tātauira

Ko te implementation o nāianei he **tātauira** anake mō te hanganga/API. Hei whakamahi i production me:

### 1. Whakatinana i te kawa OCSP

Whakakapia `PerformOcspValidationAsync` i `OcspValidationService.cs`:

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

### 2. Hanga OCSP server

Me whai OCSP responder motuhake e:
- tango tono OCSP (RFC 6960)
- tirotiro tūnga tiwhikete i te pātengi raraunga CA
- whakahoki urupare OCSP kua hainatia

**Kōwhiringa:**
- Ratonga OCSP arumoni (DigiCert, Let's Encrypt)
- Hanga ā-ringa mā OpenSSL/BouncyCastle/Python cryptography

---

## Tauira Whakamahi

### Manatoko taketake

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

### Manatoko taipitopito

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

## Hononga ki te mTLS

Ka mahi pai te OCSP me te mTLS:

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

## Ngā whanonga ina hinga te OCSP server

### "Fail" - Haumaru tino kaha
```json
"ServerUnavailableBehavior": "Fail"
```
- Ka whakakore i ngā tono ina hinga te OCSP server
- Tino haumaru
- Ka pā ki te wātea ratonga

### "Allow" - Wātea ratonga te aronga
```json
"ServerUnavailableBehavior": "Allow"
```
- Ka tuku tono ahakoa hinga te OCSP server
- Wātea > haumaru

### "Warn" - Taurite (taunoa)
```json
"ServerUnavailableBehavior": "Warn"
```
- Ka tuku tono, ka tuhi whakatūpato
- Pai mō te aroturuki me te kore aukati waka

---

## Caching

```json
"CacheDurationMinutes": 60
```

**Painga:**
- Iti ake ngā tono ki te OCSP server
- Pai ake te mahi
- Pakari ake i ngā outage poto

**Cache invalidation:**
- Ka pau aunoa i muri i te wā kua tautuhia
- Ka taea te muku ā-ringa mā te tīmata anō i te taupānga

---

## Whakaaro Haumaru

### ✅ ME MAHI:
- Whakamahia HTTPS mō te URL OCSP
- Manatoko signature o ngā urupare OCSP
- Tautuhia he cache roa tōtika
- Whakamahia "Fail" i ngā taiao tino haumaru
- Aroturuki i te wātea o te OCSP server
- Whakamahia te retry mō ngā hapa rangitahi
- Tuhi i ngā rahunga manatoko OCSP katoa

### ❌ KAUA E MAHI:
- Kaua e whakamahi HTTP i production
- Kaua e peke i te manatoko signature urupare
- Kaua e cache roa rawa (> 24 hāora)
- Kaua e noho puku mō ngā rahunga OCSP
- Kaua e whakaweto OCSP i production me te kore take

---

## Whakatinana OCSP Server

### Kōwhiringa 1: OpenSSL responder

```bash
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Kōwhiringa 2: BouncyCastle (.NET)

```csharp
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

### Kōwhiringa 3: Ratonga OCSP arumoni

- **DigiCert**
- **Let's Encrypt**
- **GlobalSign**

---

## Aroturuki me te Logging

### Whakahohe logging taipitopito

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

### Tauira karere log

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Whakamātautau

### Unit tests

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Whakamātautau ā-ringa

1. Whakaweto OCSP — me mahi tonu te taupānga
2. URL hē — whakamātau i `ServerUnavailableBehavior`
3. Tiwhikete tika — me hoki `OcspStatus.Good`
4. Whakamātautau cache — me kitea e mahi ana

---

## Whakaaro Mahi

### Tautuhinga cache

```json
"CacheDurationMinutes": 60
```

### Tradeoff
- **Poto (5-15 min):** raraunga hou ake, nui ake te uta OCSP
- **Roa (60-120 min):** pai ake te mahi, tūraru raraunga tawhito

### Timeout/retry

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Tohutohu:** timeout 10-30 hēkona, retry 2-3.

---

## Rapurongoā

### Raru: Ka kī tonu "OCSP server unavailable"
- Tirohia te `OcspServerUrl`
- Tirohia te firewall outbound HTTPS
- Whakau kei te rere te OCSP server
- Arotakehia ngā logs mō timeout

### Raru: Ka hinga ngā tiwhikete katoa
- Tirohia kei te wātea ngā raraunga status i te OCSP server
- Tirohia te chain o te tiwhikete
- Whakau te signature o te urupare OCSP
- Arotakehia ngā logs o te OCSP server

### Raru: Kāore te cache e mahi
- Tirohia `CacheDurationMinutes > 0`
- Whakau he ōrite te thumbprint
- Tīmata anō te taupānga hei muku cache

---

## Ngā hipanga whai muri

Hei whakakī katoa i te OCSP:

1. ✅ Kua oti te whirihoranga
2. ✅ Kua oti te service interface
3. ✅ Kua oti 30+ unit tests
4. ⏳ Me whakatinana RFC 6960 protocol
5. ⏳ Me tuku OCSP responder
6. ⏳ Me hono ki te mTLS authentication

---

## Tohutoro

- [RFC 6960](https://tools.ietf.org/html/rfc6960) - OCSP Specification
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Āhua:** ✅ Tātauira kua rite  
**Kawa OCSP:** ⏳ Me whakatinana  
**OCSP Server:** ⏳ Me tuku  
**Whakamātautau:** ✅ 30+ tests kei roto
