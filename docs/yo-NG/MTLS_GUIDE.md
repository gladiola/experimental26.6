# mTLS (Mutual TLS) Client Certificate Authentication Guide

## Overview

This project now supports **mutual TLS (mTLS)** authentication, which requires both the server and client to present valid certificates. This provides enhanced security through two-way authentication.

## What is mTLS?

mTLS extends standard TLS by requiring:
1. **Server Certificate**: The server presents a certificate to prove its identity (standard HTTPS)
2. **Client Certificate**: The client also presents a certificate to prove its identity (mTLS addition)

## Configuration

### 1. Feature Flag

Enable mTLS in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. mTLS Settings

Configure mTLS behavior in `appsettings.json`:

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "my-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

#### Configuration Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | If true, client certificate is mandatory |
| `AllowCertificateChains` | bool | `true` | Allow chained (CA-signed) certificates |
| `AllowSelfSignedCertificates` | bool | `false` | Allow self-signed certificates (dev only) |
| `CheckCertificateRevocation` | bool | `false` | Perform online revocation check |
| `ClientCertificateName` | string | null | Certificate name in Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Validate certificate issuer |

### 3. Server Certificate (Azure Key Vault)

The server certificate is retrieved from Azure Key Vault:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Setup Instructions

### Prerequisites

1. Azure Key Vault with appropriate permissions
2. Server certificate stored in Azure Key Vault as a secret (PFX format)
3. Client certificates (can be generated or obtained from CA)

### Step 1: Upload Server Certificate to Key Vault

```bash
# Convert certificate to PFX if needed
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Upload to Key Vault using Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Store password as separate secret
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Step 2: Generate Client Certificates

#### Option A: Self-Signed (Development Only)

```powershell
# Generate client certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Export to PFX
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Option B: CA-Signed (Production)

Work with your Certificate Authority to obtain client certificates.

### Step 3: Configure Application

Update `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert",
    "KeyVaultPassName": "server-cert-password"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false
  }
}
```

### Step 4: Test with Client Certificate

#### Using cURL:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### Using PowerShell:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Using Browser:

1. Import client certificate into browser certificate store
2. Navigate to your application
3. Browser will prompt to select client certificate

## Environment-Specific Behavior

### Development
- Server certificate is loaded from Key Vault (if available)
- Client certificates are **optional** (`AllowCertificate` mode)
- Self-signed certificates can be allowed

### Production
- Server certificate is loaded from Key Vault
- Client certificates are **required** if `EnableMtls = true`
- Only chained certificates are recommended

## Security Best Practices

### ? DO:
- Use CA-signed certificates in production
- Store certificates in Azure Key Vault
- Enable certificate revocation checking in production
- Validate certificate issuer
- Use strong passwords for PFX files
- Rotate certificates regularly

### ? DON'T:
- Use self-signed certificates in production
- Commit certificates to source control
- Share client certificates across users
- Disable certificate validation in production

## Troubleshooting

### Error: "No client certificate provided"

**Cause**: Client didn't send certificate
**Solution**: 
- Verify client certificate is installed
- Check `RequireClientCertificate` setting
- Ensure certificate is trusted by the system

### Error: "Certificate chain validation failed"

**Cause**: Certificate not trusted
**Solution**:
- Install CA root certificate
- Set `AllowSelfSignedCertificates = true` for testing
- Verify certificate hasn't expired

### Error: "Server certificate not retrieved from Key Vault"

**Cause**: Azure Key Vault access issue
**Solution**:
- Verify Key Vault permissions
- Check Azure AD credentials
- Ensure managed identity is configured

## Logging

mTLS authentication events are logged:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Integration with Existing Authentication

mTLS works alongside Azure AD authentication:

1. **Client Certificate Validation** happens first (transport layer)
2. **Azure AD Authentication** happens next (application layer)

Both can be enabled simultaneously for defense-in-depth security.

## References

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Example Code

The implementation can be found in:
- `Models/Settings/MtlsSettings.cs` - Configuration model
- `Models/Settings/FeatureFlags.cs` - Feature flag
- `Extensions/ServiceCollectionExtensions.cs` - Service registration
- `Program.cs` - Application startup

## Additional Resources

See `SupportingScripts/CertificateUploaderToAzureExample.ps1` for certificate upload examples.
