# OCSP (Online Certificate Status Protocol) कार्यान्वयन मार्गदर्शिका

## अवलोकन

यह प्रोजेक्ट OCSP certificate validation के लिए **template support** देता है। OCSP से प्रमाणपत्र निरसन स्थिति real-time में जाँची जा सकती है।

## OCSP क्या है?

CRL के विकल्प के रूप में OCSP:
- real-time status check
- target certificate-specific query
- कम payload
- अद्यतन revocation जानकारी

## कॉन्फ़िगरेशन

### 1) Feature Flag

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2) OCSP Settings

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

| Setting | Default | विवरण |
|---|---|---|
| `EnableOcspValidation` | `false` | OCSP enable/disable |
| `OcspServerUrl` | `null` | OCSP responder URL |
| `RequestTimeoutSeconds` | `30` | request timeout |
| `MaxRetryAttempts` | `3` | retries |
| `CacheDurationMinutes` | `60` | response cache time |
| `ServerUnavailableBehavior` | `Warn` | `Fail`/`Allow`/`Warn` |
| `EnableDetailedLogging` | `false` | verbose logging |
| `SkipValidationInDevelopment` | `true` | dev में skip |

---

## Template implementation की स्थिति

`OcspValidationService.cs` में `PerformOcspValidationAsync` अभी template है। production में RFC 6960 के अनुसार:
1. OCSP request बनाएं
2. responder को भेजें
3. response parse करें
4. signature validate करें
5. status return करें

---

## mTLS के साथ एकीकरण

mTLS certificate events के दौरान OCSP validation call करें; invalid स्थिति में request fail करें।

---

## ServerUnavailableBehavior

### `Fail`
- server down पर request reject
- उच्च सुरक्षा, कम availability

### `Allow`
- server down पर request allow
- availability प्राथमिकता

### `Warn` (default)
- allow + warning logs
- संतुलित विकल्प

---

## Caching

`CacheDurationMinutes` से OCSP load कम होता है और short outages में resilience बढ़ती है।

---

## सुरक्षा निर्देश

### करें
- OCSP URL के लिए HTTPS
- response signature validation
- सही cache duration
- critical workloads में `Fail` mode
- failures/logging/monitoring

### न करें
- production में HTTP OCSP
- signature validation skip करना
- अत्यधिक लंबा cache (उदा. 24h से अधिक)

---

## परीक्षण

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

मैनुअल जाँच:
1. OCSP off पर app behavior
2. invalid URL पर fallback behavior
3. valid cert status
4. cache reuse verification

---

## Troubleshooting

### OCSP server हमेशा unavailable
- `OcspServerUrl` जांचें
- outbound HTTPS firewall जांचें
- responder health देखें

### सभी certs fail हो रहे
- responder में status data
- cert chain completeness
- signature validation

### cache काम नहीं कर रहा
- `CacheDurationMinutes > 0`
- same certificate thumbprint
- ऐप restart से cache clear

---

## Next Steps

1. configuration तैयार ✅
2. service contract तैयार ✅
3. tests उपलब्ध ✅
4. RFC 6960 implementation लंबित ⏳
5. OCSP responder deployment लंबित ⏳
6. mTLS integration hardening लंबित ⏳
