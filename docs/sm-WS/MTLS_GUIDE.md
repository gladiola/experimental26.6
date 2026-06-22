# Taʻiala mTLS (Mutual TLS) mo Client Certificate Authentication

## Aotelega

E lagolagoina e lenei poloketi le **mTLS**, o lona uiga e faʻamaonia uma:

1. le server certificate,
2. ma le client certificate.

O lenei mea e faaopoopo ai le puipuiga e lua itu.

---

## O le ā le mTLS?

mTLS e faalautele TLS masani:

- e le gata o le server e faamaonia,
- ae faamaonia foʻi le client i le tusi faamaonia.

---

## Faatulagaga

### 1) Feature flag

I `appsettings.json`:

- `FeatureFlags:EnableMtls = true`
- (pe a manaʻomia cert mai Key Vault) `FeatureFlags:EnableKeyVault = true`

### 2) MtlsSettings

Faatulagaga masani i `MtlsSettings`:

- `RequireClientCertificate`
- `AllowCertificateChains`
- `AllowSelfSignedCertificates`
- `CheckCertificateRevocation`
- `ValidateClientCertificateIssuer`
- `AllowedIssuers`

---

## Laasaga Setup

1. Saunia Azure Key Vault ma server certificate (PFX).
2. Fausia pe maua client certificates.
3. Faafou `appsettings.json` ma feature flags + mtls settings.
4. Suʻe i cURL, PowerShell, poʻo browser.

---

## Amioga i siosiomaga

### Development
- e mafai ona faatagaina self-signed certs (pe a faʻatulagaina),
- e mafai ona vaivai policy mo suʻega.

### Production
- manaʻomia cert chains talitonuina,
- fautuaina revocation checking,
- issuer validation ma `AllowedIssuers` manino.

---

## Best practices

### Fai
- faaaoga CA-signed certs i gaosiga,
- teu certs i Key Vault,
- sui certs i taimi masani,
- tausia issuer/revocation checks.

### Aua le faia
- faaaoga self-signed certs i gaosiga,
- commit certs po o passwords i repo,
- tape validation checks i gaosiga.

---

## Troubleshooting

### "No client certificate provided"
- siaki ua faʻapipiʻi cert i client,
- siaki `RequireClientCertificate`.

### "Certificate chain validation failed"
- siaki trust chain/CA root,
- siaki expiry.

### "Server certificate not retrieved from Key Vault"
- siaki Key Vault permissions,
- siaki URL ma identity.

---

## Logging

Ia tusia mea tutupu mTLS (manuia/teena) ae aua le faʻaalia mea lilo.

---

## Tuʻufaʻatasia ma Azure AD

mTLS e mafai ona galue faatasi ma Azure AD:

1. cert validation i transport layer,
2. Azure AD auth i application layer.

---

## Nofoaga code aoga

- `Models/Settings/MtlsSettings.cs`
- `Models/Settings/FeatureFlags.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Program.cs`
