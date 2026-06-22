# የOCSP (Online Certificate Status Protocol) መተግበሪያ መመሪያ

## አጠቃላይ

ይህ ፕሮጀክት ለOCSP validation **template support** አለው። OCSP ምስክር ወረቀት ከመተግበሪያ በፊት የrevocation ሁኔታን በቅጽበት ለማረጋገጥ ይረዳል።

## OCSP ምንድን ነው?

OCSP ለCRL አማራጭ ነው:
- በቅጽበት ምርመራ
- የሚያስፈልገውን cert ብቻ ማጣራት
- ዝቅተኛ payload
- የቅርብ ጊዜ status መረጃ

## ቅንብር

### 1) Feature flag

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2) OcspSettings

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

| ቅንብር | አይነት | ነባሪ | ማብራሪያ |
|---|---|---|---|
| `EnableOcspValidation` | bool | `false` | OCSP ማብራት/ማጥፋት |
| `OcspServerUrl` | string | `null` | የOCSP responder URL |
| `RequestTimeoutSeconds` | int | `30` | request timeout |
| `MaxRetryAttempts` | int | `3` | retry ብዛት |
| `CacheDurationMinutes` | int | `60` | cache ዕድሜ |
| `ServerUnavailableBehavior` | string | `Warn` | `Fail`, `Allow`, `Warn` |
| `EnableDetailedLogging` | bool | `false` | verbose logging |
| `SkipValidationInDevelopment` | bool | `true` | dev ላይ OCSP መዝለል |

---

## የአሁኑ የአፈጻጸም ሁኔታ

የአሁኑ implementation **template** ነው። ለproduction:
1. RFC 6960 መሠረት የOCSP ፕሮቶኮል በሙሉ ይተግብሩ
2. OCSP responder server ያቋቁሙ ወይም managed service ይጠቀሙ

ተተካትሎ የሚሰራ method:

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

## የአጠቃቀም ምሳሌ

### Basic validation

```csharp
return await _ocspService.ValidateCertificateAsync(clientCert);
```

### Detailed validation

`result.Status` (`Good`, `Revoked`, `Unknown`, `ServerUnavailable`) በመመርመር ተገቢ እርምጃ ይውሰዱ።

---

## ከmTLS ጋር ማዋሃድ

- `AddMtlsAuthentication(...)` ያብሩ
- `AddOcspValidation(...)` ያብሩ
- `OnCertificateValidated` ውስጥ OCSP service ይጠሩ
- ሲወድቅ `context.Fail(...)` ይጠቀሙ

---

## OCSP server ካልተገኘ ባህሪ

### `Fail` (ከፍተኛ ደህንነት)
OCSP server ካልተገኘ request ይከለከላል።

### `Allow` (availability ይበልጥ)
request ይቀጥላል፣ warning ይመዘገባል።

### `Warn` (ነባሪ ሚዛናዊ)
request ይቀጥላል፣ warning + monitoring ይኖራል።

---

## Caching

```json
"CacheDurationMinutes": 60
```

ጥቅሞች:
- OCSP server ጥሪ ቅነሳ
- performance ማሻሻያ
- አጭር outage ጊዜ መቋቋም

---

## የደህንነት መመሪያዎች

### ✅ ያድርጉ
- ለOCSP URL HTTPS ይጠቀሙ
- OCSP response signature ያረጋግጡ
- ተገቢ cache duration ይመድቡ
- server availability ይከታተሉ
- የOCSP መውደቆችን ሁሉ ይመዝግቡ

### ❌ አታድርጉ
- production ላይ HTTP መጠቀም
- signature validation መዝለል
- በጣም ረጅም cache (>24h)
- OCSP failures ላይ ዝም መባል

---

## ሙከራ

### Unit tests

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Manual tests
1. OCSP አጥፉ፣ app እንደሚቀጥል ያረጋግጡ
2. የተሳሳተ URL በመስጠት `ServerUnavailableBehavior` ይፈትሹ
3. valid cert ላይ `OcspStatus.Good` ይጠብቁ
4. cache behavior ይፈትሹ

---

## ችግኝ ፍቺ

### OCSP server ሁልጊዜ unavailable
- `OcspServerUrl` ይፈትሹ
- firewall/outbound HTTPS ይፈትሹ
- server እንደሚሰራ ያረጋግጡ
- timeout logs ይመልከቱ

### ሁሉም certificates ይከለከላሉ
- OCSP server status data ይፈትሹ
- ሙሉ certificate chain ይፈትሹ
- response signature ይፈትሹ

### cache አይሰራም
- `CacheDurationMinutes > 0` ያረጋግጡ
- ተመሳሳይ thumbprint እንደሚጠቀም ያረጋግጡ
- app restart በማድረግ cache ያጽዱ

---

## ቀጣይ እርምጃዎች

1. ✅ ቅንብር ዝግጁ ነው
2. ✅ service interface ዝግጁ ነው
3. ✅ unit tests ተገናኝተዋል
4. ⏳ RFC 6960 implementation
5. ⏳ OCSP responder setup
6. ⏳ production mTLS integration

---

## ማጣቀሻዎች

- [RFC 6960](https://tools.ietf.org/html/rfc6960)
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
