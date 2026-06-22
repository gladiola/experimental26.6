# Jagorar mTLS (Mutual TLS) don Tabbatar da Takardar Shaidar Abokin Ciniki

## Bayani

Wannan aikin yana tallafa wa **mTLS**, wato uwar garke da abokin ciniki duka suna gabatar da takardun shaida masu inganci.

## Menene mTLS?

mTLS yana ƙara kariya ga TLS ta hanyar buƙatar:
1. **Takardar shaidar uwar garke** (HTTPS na yau da kullum)
2. **Takardar shaidar abokin ciniki** (ƙarin mTLS)

## Saiti

### 1. Feature Flag

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Saitunan mTLS

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

| Saiti | Nau'i | Tsoho | Bayani |
|---|---|---|---|
| `RequireClientCertificate` | bool | `true` | Takardar shaidar abokin ciniki dole |
| `AllowCertificateChains` | bool | `true` | Yarda da takardar shaida mai CA chain |
| `AllowSelfSignedCertificates` | bool | `false` | Yarda da self-signed (dev kawai) |
| `CheckCertificateRevocation` | bool | `false` | Duba revocation online |
| `ClientCertificateName` | string | null | Sunan certificate a Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Duba issuer |

### 3. Server Certificate daga Azure Key Vault

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Matakan saitawa

### Abubuwan da ake buƙata
1. Azure Key Vault tare da izini da ya dace
2. Server certificate a Key Vault (PFX)
3. Client certificates (self-signed ko daga CA)

### Mataki na 1: Loda server certificate zuwa Key Vault

```bash
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Mataki na 2: Samar da client certificates

#### Zabii A: Self-signed (development)

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

#### Zabii B: CA-signed (production)

Yi aiki da Certificate Authority ɗinku don samun takardun shaida na abokan ciniki.

### Mataki na 3: Sabunta appsettings.json

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

### Mataki na 4: Gwaji da client certificate

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

## Halayen muhalli

### Development
- Server cert daga Key Vault (idan akwai)
- Client cert **na zaɓi**
- Self-signed za a iya yarda

### Production
- Server cert daga Key Vault
- Client cert **dole** idan `EnableMtls = true`
- A fi son chained certificates

## Mafi kyawun tsaro

### ✅ A yi:
- Yi amfani da CA-signed certificates a production
- Ajiye certificates a Azure Key Vault
- Kunna revocation checking a production
- Duba issuer
- Juya certificates akai-akai

### ❌ Kada a yi:
- Self-signed a production
- Commit certificates zuwa source control
- Raba client certificates tsakanin masu amfani
- Kashe certificate validation a production

## Warware matsala

### “No client certificate provided”
- Tabbatar an saka takardar shaida a client
- Duba `RequireClientCertificate`
- Tabbatar system na yarda da cert ɗin

### “Certificate chain validation failed”
- Saka CA root certificate
- Don gwaji kawai, `AllowSelfSignedCertificates = true`
- Tabbatar cert bai ƙare ba

### “Server certificate not retrieved from Key Vault”
- Duba Key Vault permissions
- Duba Azure AD credentials
- Tabbatar an saita managed identity

## Logging

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Haɗuwa da Azure AD

mTLS na aiki tare da Azure AD:
1. Certificate validation (transport layer)
2. Azure AD auth (application layer)

Ana iya kunna su duka don kariya mai zurfi.

## Manazarta

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Wurin lambar misali

- `Models/Settings/MtlsSettings.cs`
- `Models/Settings/FeatureFlags.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Program.cs`

## Ƙarin albarkatu

Duba `SupportingScripts/CertificateUploaderToAzureExample.ps1`.
