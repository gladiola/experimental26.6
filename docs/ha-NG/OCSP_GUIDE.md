# Jagorar Aiwatar da OCSP (Online Certificate Status Protocol)

## Bayani

Wannan aikin ya ƙunshi **template support** na OCSP validation. OCSP yana ba da damar duba matsayin soke takardar shaida (revocation) a ainihin lokaci kafin a sarrafa request.

## Menene OCSP?

OCSP madadin CRL ne don binciken revocation:
- Duba a lokaci na gaske
- Inganci: ana tambayar cert ɗin da ake buƙata kawai
- Sako mai sauƙi fiye da cikakken CRL
- Bayanai na sabo

## Saiti

### 1. Feature Flag

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. OcspSettings

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

| Saiti | Nau'i | Tsoho | Bayani |
|---|---|---|---|
| `EnableOcspValidation` | bool | `false` | Kunna/kashe OCSP |
| `OcspServerUrl` | string | `null` | URL na OCSP responder |
| `RequestTimeoutSeconds` | int | `30` | Timeout na requests |
| `MaxRetryAttempts` | int | `3` | Yawan retries |
| `CacheDurationMinutes` | int | `60` | Tsawon lokacin cache |
| `ServerUnavailableBehavior` | string | `Warn` | `Fail`, `Allow`, ko `Warn` |
| `EnableDetailedLogging` | bool | `false` | Verbose logging |
| `SkipValidationInDevelopment` | bool | `true` | Tsallake OCSP a dev |

---

## Matsayin aiwatarwa yanzu

Aiwatarwar yanzu **template ce**. Don production:
1. A aiwatar da ainihin OCSP protocol (RFC 6960)
2. A kafa OCSP responder server (ko amfani da managed service)

Template method da ake maye gurbinsa:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO:
    // 1. Build request
    // 2. Send to OCSP server
    // 3. Parse response
    // 4. Validate signature
    // 5. Return status
}
```

---

## Misalin amfani

### Basic validation

```csharp
return await _ocspService.ValidateCertificateAsync(clientCert);
```

### Detailed validation

A duba `result.Status` (`Good`, `Revoked`, `Unknown`, `ServerUnavailable`) kuma a ɗauki matakin da ya dace.

---

## Haɗuwa da mTLS

- Kunna `AddMtlsAuthentication(...)`
- Kunna `AddOcspValidation(...)`
- A cikin `OnCertificateValidated`, kira OCSP service
- Idan bai yi nasara ba, `context.Fail(...)`

---

## Halayen idan server bai samu ba

### `Fail` (tsaro mai tsauri)
- Ana ƙin request idan OCSP server ya faɗi

### `Allow` (availability mafi yawa)
- Ana barin request, a rubuta gargadi

### `Warn` (daidaito, tsoho)
- Ana barin request, a rubuta gargadi da monitoring

---

## Caching

```json
"CacheDurationMinutes": 60
```

Fa'idodi:
- Rage kiran OCSP server
- Inganta aiki
- Taimako a gajerun outages

---

## Ka'idojin tsaro

### ✅ A yi:
- Yi amfani da HTTPS don OCSP URL
- Duba signature na OCSP response
- Saita cache duration mai kyau
- Saka idanu ga availability
- Rubuta duk gazawar OCSP

### ❌ Kada a yi:
- HTTP a production
- Tsallake signature validation
- Cache na tsawon lokaci sosai (>24h)
- Yin shiru kan gazawar OCSP

---

## Gwaji

### Unit tests

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Manual tests
1. Kashe OCSP: app ya ci gaba da aiki
2. Saka URL mara kyau: gwada `ServerUnavailableBehavior`
3. Valid cert: ya kamata `OcspStatus.Good`
4. Duba cache behavior

---

## Matsaloli da gyara

### OCSP server kullum unavailable
- Duba `OcspServerUrl`
- Duba firewall/outbound HTTPS
- Duba server yana aiki
- Duba logs na timeout

### Duk certificates suna faduwa
- Duba status data na OCSP server
- Duba cikakken certificate chain
- Duba response signature

### Cache ba ya aiki
- Duba `CacheDurationMinutes > 0`
- Duba thumbprint iri ɗaya ake amfani da shi
- Restart app don clear cache

---

## Matakai na gaba

1. ✅ Saiti ya shirya
2. ✅ Service interface ya shirya
3. ✅ Unit tests sun haɗu
4. ⏳ Aiwatar da RFC 6960
5. ⏳ Kafa OCSP responder
6. ⏳ Haɗa da mTLS a production

---

## Manazarta

- [RFC 6960](https://tools.ietf.org/html/rfc6960)
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
