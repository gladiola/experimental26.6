# OCSP (Online Certificate Status Protocol) Implementation Guide

## Overview

This project includes **template support** for OCSP certificate validation. OCSP allows real-time checking of certificate revocation status before processing web requests.

## What is OCSP?

OCSP provides an alternative to Certificate Revocation Lists (CRLs) for checking if a certificate has been revoked:

- **Real-time validation**: Checks certificate status immediately
- **Efficient**: Only queries status of specific certificates
- **Lightweight**: Smaller responses than full CRL downloads
- **Up-to-date**: Always has current revocation information

## Configuration

### 1. Feature Flag

Enable OCSP validation in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. OCSP Settings

Configure OCSP behavior in `appsettings.json`:

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

### Configuration Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | Enable/disable OCSP validation |
| `OcspServerUrl` | string | `null` | URL of your OCSP responder server |
| `RequestTimeoutSeconds` | int | `30` | Timeout for OCSP requests |
| `MaxRetryAttempts` | int | `3` | Number of retries on failure |
| `CacheDurationMinutes` | int | `60` | How long to cache OCSP responses |
| `ServerUnavailableBehavior` | string | `"Warn"` | Behavior when server is down: `"Fail"`, `"Allow"`, or `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Enable verbose logging |
| `SkipValidationInDevelopment` | bool | `true` | Skip OCSP in development mode |

---

## Template Implementation

The current implementation is a **template** that demonstrates the structure and API design. To use OCSP in production, you must:

### 1. Implement the OCSP Protocol

Replace the template `PerformOcspValidationAsync` method in `OcspValidationService.cs` with actual OCSP protocol implementation:

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

### 2. Build an OCSP Server

You need a separate OCSP responder server that:
- Accepts OCSP requests (RFC 6960 format)
- Checks certificate status against your CA database
- Returns signed OCSP responses

**Options:**
- Use commercial OCSP service (e.g., DigiCert, Let's Encrypt)
- Build custom OCSP responder using libraries:
  - **OpenSSL** - C/C++ library with OCSP support
  - **BouncyCastle** - .NET library for OCSP
  - **Python** - `cryptography` library with OCSP support

---

## Usage Example

### Basic Certificate Validation

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

### Detailed Status Validation

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

## Integration with mTLS

OCSP works seamlessly with mTLS certificate authentication:

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

## Server Unavailable Behaviors

### "Fail" - Strict Security

```json
"ServerUnavailableBehavior": "Fail"
```

- Rejects requests when OCSP server is down
- Most secure option
- May cause availability issues

**Use when:** Maximum security is required, certificate validation is critical

### "Allow" - High Availability

```json
"ServerUnavailableBehavior": "Allow"
```

- Allows requests when OCSP server is down
- Prioritizes availability over security
- Logs warnings

**Use when:** Service availability is more important than real-time validation

### "Warn" - Balanced (Default)

```json
"ServerUnavailableBehavior": "Warn"
```

- Allows requests but logs warnings
- Balanced approach
- Enables monitoring/alerting

**Use when:** You want to monitor OCSP issues without blocking traffic

---

## Caching

OCSP responses are cached to reduce server load:

```json
"CacheDurationMinutes": 60
```

**Benefits:**
- Reduces OCSP server queries
- Improves performance
- Provides resilience during brief outages

**Cache invalidation:**
- Automatic after cache duration expires
- Manual clearing: restart application

---

## Security Considerations

### ? DO:

- Use HTTPS for OCSP server URL
- Validate OCSP response signatures
- Set appropriate cache duration (balance freshness vs performance)
- Use "Fail" behavior in high-security environments
- Monitor OCSP server availability
- Implement retry logic for transient failures
- Log all OCSP validation failures

### ? DON'T:

- Use HTTP for OCSP in production
- Skip OCSP response signature validation
- Cache responses for too long (> 24 hours)
- Ignore OCSP server failures silently
- Disable OCSP in production without justification

---

## Implementing an OCSP Server

### Option 1: OpenSSL OCSP Responder

```bash
# Start OpenSSL OCSP responder
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Option 2: BouncyCastle (.NET)

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

### Option 3: Commercial OCSP Service

- **DigiCert**: Managed OCSP service
- **Let's Encrypt**: Free OCSP for their certificates
- **GlobalSign**: Enterprise OCSP solutions

---

## Monitoring and Logging

### Enable Detailed Logging

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

### Log Messages

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Testing

### Unit Tests

Run OCSP tests:

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Manual Testing

1. **Disable OCSP** - Verify application works without OCSP
2. **Invalid URL** - Test ServerUnavailableBehavior settings
3. **Valid Certificate** - Should return `OcspStatus.Good`
4. **Cached Response** - Verify cache is working

---

## Performance Considerations

### Cache Configuration

```json
"CacheDurationMinutes": 60  // 1 hour cache
```

**Tradeoffs:**
- **Short duration (5-15 min)**: More current data, higher OCSP load
- **Long duration (60-120 min)**: Better performance, stale data risk

### Timeout Settings

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**Recommendations:**
- Timeout: 10-30 seconds for production
- Retries: 2-3 attempts for transient failures

---

## Troubleshooting

### Issue: OCSP server always unavailable

**Solutions:**
1. Check `OcspServerUrl` is correct
2. Verify firewall allows outbound HTTPS
3. Check OCSP server is running
4. Review logs for timeout errors

### Issue: All certificates failing validation

**Solutions:**
1. Verify OCSP server has certificate status data
2. Check certificate chain is complete
3. Ensure OCSP response signature is valid
4. Review OCSP server logs

### Issue: Cache not working

**Solutions:**
1. Verify `CacheDurationMinutes > 0`
2. Check same certificate thumbprint is used
3. Restart application to clear cache

---

## Next Steps

To make OCSP fully functional:

1. ? **Configuration Complete** - Settings are ready
2. ? **Service Interface Complete** - API is defined
3. ? **Tests Complete** - 30+ unit tests included
4. ?? **OCSP Protocol** - Need to implement RFC 6960
5. ?? **OCSP Server** - Need to deploy OCSP responder
6. ?? **Integration** - Connect with mTLS authentication

---

## References

- [RFC 6960](https://tools.ietf.org/html/rfc6960) - OCSP Specification
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Status:** ? Template Ready  
**OCSP Protocol:** ?? To Be Implemented  
**OCSP Server:** ?? To Be Deployed  
**Tests:** ? 30+ Tests Included
