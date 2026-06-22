# Taʻiala OCSP (Online Certificate Status Protocol)

## Aotelega

O le poloketi e iai **template support** mo OCSP certificate validation. OCSP e fesoasoani e siaki i le taimi moni pe ua revoked se certificate.

---

## O le ā le OCSP?

OCSP e sui pe fesoasoani i CRL:

- validation i taimi moni,
- query mo cert patino,
- tali mama nai lo CRL tele,
- faamatalaga fou atu.

---

## Faatulagaga

### 1) Feature flag

I `appsettings.json`:

- `FeatureFlags:EnableOcspValidation = true`

### 2) OcspSettings

Faatulagaga autu:

- `OcspServerUrl`
- `RequestTimeoutSeconds`
- `MaxRetryAttempts`
- `CacheDurationMinutes`
- `ServerUnavailableBehavior` (`Fail`, `Allow`, `Warn`)
- `EnableDetailedLogging`
- `SkipValidationInDevelopment`

---

## Tulaga o le implementation

O le implementation i le repo e tele vaega o loo template pea. Mo gaosiga, e tatau ona:

1. fausia OCSP request e tusa ma RFC,
2. auina atu i responder,
3. parse response,
4. validate response signature,
5. toe faafoʻi status saʻo.

---

## Tuʻufaʻatasia ma mTLS

I certificate validation event, valaau `IOcspValidationService` ma teena request pe a le valid cert status.

---

## ServerUnavailableBehavior

### `Fail`
- sili ona saogalemu,
- teena requests pe a paʻū OCSP server.

### `Allow`
- maualuga availability,
- lamatiaga saogalemu maualuga atu.

### `Warn`
- paleni i le va,
- talia request ae tusia warning.

---

## Caching

OCSP responses e mafai ona cache mo:

- faaitiitia load,
- faaleleia performance,
- puipuiga i outages puʻupuʻu.

Aua le umi tele cache i gaosiga.

---

## Best practices

### Fai
- HTTPS mo OCSP URL,
- validate signatures,
- monitor server availability,
- log validation failures.

### Aua le faia
- HTTP plain i gaosiga,
- skip signature validation,
- faatumau cache umi tele e mafua stale revocation state.

---

## Troubleshooting puʻupuʻu

### "Server unavailable"
- siaki URL,
- siaki firewall/network,
- siaki timeout/retry settings.

### "All certificates failing"
- siaki trust chain,
- siaki responder data,
- siaki response signature validation.

### "Cache not working"
- siaki `CacheDurationMinutes > 0`,
- siaki thumbprint consistency.

---

## Laasaga Sosoo

- Faamaeʻa OCSP protocol implementation,
- Deploy pe faaaoga OCSP responder,
- Tuʻufaʻatasi atoa i mTLS validation pipeline.
