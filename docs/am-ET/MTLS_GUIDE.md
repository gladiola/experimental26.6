# የmTLS (Mutual TLS) መመሪያ — የደንበኛ ሰርተፊኬት ማረጋገጫ

## አጠቃላይ

ይህ ፕሮጀክት **mTLS** ይደግፋል፤ ማለትም ሁለቱም አገልጋይ እና ደንበኛ ትክክለኛ ሰርተፊኬት ያቀርባሉ።

## mTLS ምንድን ነው?

1. **የአገልጋይ ሰርተፊኬት** (መደበኛ HTTPS)
2. **የደንበኛ ሰርተፊኬት** (ተጨማሪ mTLS)

## ቅንብር

### 1) Feature flag

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2) MtlsSettings

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

| ቅንብር | አይነት | ነባሪ | ማብራሪያ |
|---|---|---|---|
| `RequireClientCertificate` | bool | `true` | የደንበኛ ሰርተፊኬት ግዴታ |
| `AllowCertificateChains` | bool | `true` | CA chain ያላቸውን ይፈቅዳል |
| `AllowSelfSignedCertificates` | bool | `false` | self-signed (ለdev ብቻ) |
| `CheckCertificateRevocation` | bool | `false` | online revocation ምርመራ |
| `ClientCertificateName` | string | null | በKey Vault ያለ የcert ስም |
| `ValidateClientCertificateIssuer` | bool | `true` | issuer ማረጋገጫ |

### 3) የአገልጋይ cert ከAzure Key Vault

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## የማቀናበር ደረጃዎች

### የሚያስፈልጉ ነገሮች
1. ተገቢ ፈቃድ ያለው Azure Key Vault
2. በKey Vault ውስጥ server certificate (PFX)
3. client certificates (self-signed ወይም CA-signed)

### ደረጃ 1: server certificate ወደ Key Vault ጫን

```bash
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### ደረጃ 2: client certificates ፍጠር

#### አማራጭ A: Self-signed (development)

```powershell
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### አማራጭ B: CA-signed (production)

ከCertificate Authority ጋር በመስራት የደንበኛ ሰርተፊኬቶችን ያግኙ።

### ደረጃ 3: `appsettings.json` ያዘምኑ

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

### ደረጃ 4: ከclient cert ጋር ሞክር

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

## በenvironment የሚለያዩ ባህሪያት

### Development
- server cert ከKey Vault (ካለ)
- client cert **አማራጭ**
- self-signed ሊፈቀድ ይችላል

### Production
- server cert ከKey Vault
- `EnableMtls = true` ከሆነ client cert **ግዴታ**
- CA chained certificates ይመከራሉ

## የደህንነት ልምዶች

### ✅ ያድርጉ
- production ላይ CA-signed certs ይጠቀሙ
- certificates በAzure Key Vault ውስጥ ያከማቹ
- revocation checking በproduction ላይ ያብሩ
- issuer validation ያረጋግጡ
- certificate rotation ያድርጉ

### ❌ አታድርጉ
- self-signed certs በproduction
- certificates ወደ source control መcommit
- client cert በተጠቃሚዎች መጋራት
- production ላይ certificate validation ማጥፋት

## ችግኝ ፍቺ

### “No client certificate provided”
- ደንበኛው cert እንደላከ ያረጋግጡ
- `RequireClientCertificate` ይፈትሹ
- ስርዓቱ cert እንደሚያምን ያረጋግጡ

### “Certificate chain validation failed”
- CA root certificate ይጨምሩ
- ለሙከራ ብቻ `AllowSelfSignedCertificates = true`
- cert እንዳልተበላሸ/እንዳልተጠናቀቀ ያረጋግጡ

### “Server certificate not retrieved from Key Vault”
- Key Vault permissions ይፈትሹ
- Azure AD credentials ይፈትሹ
- managed identity እንደተዋቀረ ያረጋግጡ

## ሎግ ምሳሌ

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## ከAzure AD ጋር መጣጣም

mTLS ከAzure AD ጋር በአንድ ላይ ሊሰራ ይችላል:
1. Certificate validation (transport layer)
2. Azure AD auth (application layer)

## ማጣቀሻዎች

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## የኮድ ቦታዎች

- `Models/Settings/MtlsSettings.cs`
- `Models/Settings/FeatureFlags.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Program.cs`

## ተጨማሪ ምንጮች

`SupportingScripts/CertificateUploaderToAzureExample.ps1` ይመልከቱ።
