# Aratohu mTLS (Mutual TLS) mō te Whakamana Tiwhikete Kiritaki

## Tirohanga Whānui

E tautoko ana tēnei kaupapa i te **mutual TLS (mTLS)**: me whakaatu tiwhikete tika te tūmau me te kiritaki.
Ka whakakaha tēnei i te haumaru mā te manatoko taha-rua.

## He aha te mTLS?

Ka whakawhānui mTLS i te TLS paerewa mā te tono:
1. **Tiwhikete Tūmau**: hei whakamana i te tuakiri o te tūmau (HTTPS paerewa)
2. **Tiwhikete Kiritaki**: hei whakamana i te tuakiri o te kiritaki (tāpiritanga mTLS)

## Whirihoranga

### 1. Haki Āhuatanga

Whakahohea i `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Tautuhinga mTLS

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

#### Kōwhiringa whirihoranga

| Tautuhinga | Momo | Taunoa | Whakamārama |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | Mēnā he `true`, me tuku tiwhikete te kiritaki |
| `AllowCertificateChains` | bool | `true` | Whakaae ki ngā tiwhikete kua hainatia e te CA |
| `AllowSelfSignedCertificates` | bool | `false` | Whakaae self-signed (dev anake) |
| `CheckCertificateRevocation` | bool | `false` | Tirohia te status whakakorenga |
| `ClientCertificateName` | string | null | Ingoa tiwhikete i Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Manatoko issuer o te tiwhikete |

### 3. Tiwhikete Tūmau (Azure Key Vault)

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Tohutohu Tatūnga

### Ngā mea e hiahiatia ana

1. Azure Key Vault me ngā whakaaetanga tika
2. Tiwhikete tūmau kei Key Vault hei secret (PFX)
3. Tiwhikete kiritaki (hanga ā-ringa, tiki rānei i te CA)

### Hipanga 1: Tukuna te tiwhikete tūmau ki Key Vault

```bash
# Huri ki PFX mēnā e hiahiatia ana
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Tukuna mā Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Tiakina te kupuhipa hei secret motuhake
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Hipanga 2: Waihanga tiwhikete kiritaki

#### Kōwhiringa A: Self-signed (Whakawhanake anake)

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

#### Kōwhiringa B: CA-signed (Production)

Mahi tahi me tō Certificate Authority hei tiki tiwhikete kiritaki.

### Hipanga 3: Whirihora i te taupānga

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

### Hipanga 4: Whakamātautau me te tiwhikete kiritaki

#### cURL
```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### PowerShell
```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Pūtirotiro
1. Kawemai te tiwhikete kiritaki
2. Haere ki te taupānga
3. Kōwhiria te tiwhikete ina tonoa

## Whanonga ā-Taiao

### Development
- Ka utaina te tiwhikete tūmau i Key Vault (mēnā e wātea ana)
- He **kōwhiringa** ngā tiwhikete kiritaki (`AllowCertificate` mode)
- Ka taea ngā tiwhikete self-signed

### Production
- Ka utaina te tiwhikete tūmau i Key Vault
- Mēnā `EnableMtls = true`, he **herea** ngā tiwhikete kiritaki
- E tūtohua ana ko ngā tiwhikete chained anake

## Tikanga Pai Haumaru

### ✅ ME MAHI:
- Whakamahia ngā tiwhikete CA-signed i production
- Penapena ngā tiwhikete ki Azure Key Vault
- Whakahohea te revocation checking i production
- Manatoko issuer
- Whakamahia kupuhipa kaha mō ngā PFX
- Hurihia ngā tiwhikete i ngā wā auau

### ❌ KAUA E MAHI:
- Kaua e whakamahi self-signed i production
- Kaua e commit i ngā tiwhikete ki te source control
- Kaua e tiritiri i ngā tiwhikete kiritaki ki ngā kaiwhakamahi maha
- Kaua e whakaweto i te manatoko tiwhikete i production

## Rapurongoā

### Hapa: "No client certificate provided"
**Take:** Kāore te kiritaki i tuku tiwhikete  
**Rongoā:**
- Tirohia kua tāuta te tiwhikete kiritaki
- Tirohia `RequireClientCertificate`
- Whakau kei te whakawhirinaki te pūnaha ki te tiwhikete

### Hapa: "Certificate chain validation failed"
**Take:** Kāore te tiwhikete i te trusted  
**Rongoā:**
- Tāuta te CA root certificate
- Tautuhia `AllowSelfSignedCertificates = true` mō te whakamātautau
- Tirohia te paunga o te tiwhikete

### Hapa: "Server certificate not retrieved from Key Vault"
**Take:** Raru uru Key Vault  
**Rongoā:**
- Tirohia ngā whakaaetanga Key Vault
- Tirohia ngā tohu Azure AD
- Whakau kua whirihora te managed identity

## Logging

Ka tuhia ngā takahanga mTLS:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Hononga ki te Whakamana o Nāianei

Ka mahi tahi a mTLS me Azure AD:

1. Ka rere tuatahi te manatoko tiwhikete kiritaki (transport layer)
2. Ka whai mai te whakamana Azure AD (application layer)

Ka taea te whakahohe i ngā mea e rua mō te defense-in-depth.

## Tohutoro

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Waehere Tauira

Kei konei te whakatinanatanga:
- `Models/Settings/MtlsSettings.cs` - Tauira whirihoranga
- `Models/Settings/FeatureFlags.cs` - Haki āhuatanga
- `Extensions/ServiceCollectionExtensions.cs` - Rēhita ratonga
- `Program.cs` - Tīmatanga taupānga

## Rauemi Tāpiri

Tirohia `SupportingScripts/CertificateUploaderToAzureExample.ps1` mō ngā tauira tuku tiwhikete.
