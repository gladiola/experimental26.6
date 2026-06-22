# Mwongozo wa Uthibitishaji wa Cheti cha Mteja wa mTLS (Mutual TLS)

## Muhtasari

Mradi huu sasa unaunga mkono uthibitishaji wa **mutual TLS (mTLS)**, unaohitaji seva na mteja wote wawili kuwasilisha vyeti halali. Hii hutoa usalama ulioimarishwa kupitia uthibitishaji wa pande mbili.

## mTLS ni nini?

mTLS huongeza TLS ya kawaida kwa kuhitaji:
1. **Server Certificate**: Seva huwasilisha cheti kuthibitisha utambulisho wake (HTTPS ya kawaida)
2. **Client Certificate**: Mteja pia huwasilisha cheti kuthibitisha utambulisho wake (nyongeza ya mTLS)

## Usanidi

### 1. Alama ya Kipengele

Wezesha mTLS katika `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Mipangilio ya mTLS

Sanidi tabia ya mTLS katika `appsettings.json`:

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

#### Chaguo za Usanidi

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | Ikiwa ni kweli, client certificate ni ya lazima |
| `AllowCertificateChains` | bool | `true` | Ruhusu chained (CA-signed) certificates |
| `AllowSelfSignedCertificates` | bool | `false` | Ruhusu self-signed certificates (dev pekee) |
| `CheckCertificateRevocation` | bool | `false` | Fanya ukaguzi wa online revocation |
| `ClientCertificateName` | string | null | Jina la certificate katika Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Thibitisha issuer wa certificate |

### 3. Server Certificate (Azure Key Vault)

Server certificate hurejeshwa kutoka Azure Key Vault:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Maelekezo ya Usanidi

### Mahitaji ya Awali

1. Azure Key Vault yenye ruhusa zinazofaa
2. Server certificate iliyohifadhiwa katika Azure Key Vault kama secret (muundo wa PFX)
3. Client certificates (zinaweza kuzalishwa au kupatikana kutoka CA)

### Hatua ya 1: Pakia Server Certificate kwenye Key Vault

```bash
# Convert certificate to PFX if needed
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Upload to Key Vault using Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Store password as separate secret
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Hatua ya 2: Zalisha Client Certificates

#### Chaguo A: Self-Signed (Development Pekee)

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

#### Chaguo B: CA-Signed (Production)

Fanya kazi na Certificate Authority yako ili kupata client certificates.

### Hatua ya 3: Sanidi Programu

Sasisha `appsettings.json`:

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

### Hatua ya 4: Jaribu kwa Client Certificate

#### Kwa kutumia cURL:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### Kwa kutumia PowerShell:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Kwa kutumia Browser:

1. Leta client certificate kwenye browser certificate store
2. Nenda kwenye programu yako
3. Browser itatoa ombi la kuchagua client certificate

## Tabia Mahususi kwa Mazingira

### Development
- Server certificate hupakiwa kutoka Key Vault (ikiwa inapatikana)
- Client certificates ni **za hiari** (`AllowCertificate` mode)
- Self-signed certificates zinaweza kuruhusiwa

### Production
- Server certificate hupakiwa kutoka Key Vault
- Client certificates **zinahitajika** ikiwa `EnableMtls = true`
- Ni vyema kutumia chained certificates pekee

## Mbinu Bora za Usalama

### ? FANYA:
- Tumia CA-signed certificates katika production
- Hifadhi certificates katika Azure Key Vault
- Wezesha ukaguzi wa certificate revocation katika production
- Thibitisha issuer wa certificate
- Tumia manenosiri madhubuti kwa faili za PFX
- Badilisha certificates mara kwa mara

### ? USIFANYE:
- Kutumia self-signed certificates katika production
- Kuweka certificates kwenye source control
- Kushirikisha client certificates kati ya watumiaji
- Kuzima certificate validation katika production

## Utatuzi wa Matatizo

### Kosa: "No client certificate provided"

**Sababu**: Mteja hakutuma certificate  
**Suluhisho**: 
- Thibitisha client certificate imesakinishwa
- Angalia setting ya `RequireClientCertificate`
- Hakikisha certificate inaaminika na mfumo

### Kosa: "Certificate chain validation failed"

**Sababu**: Certificate haiaminiki  
**Suluhisho**:
- Sakinisha CA root certificate
- Weka `AllowSelfSignedCertificates = true` kwa majaribio
- Thibitisha certificate haijaisha muda wake

### Kosa: "Server certificate not retrieved from Key Vault"

**Sababu**: Tatizo la ufikiaji wa Azure Key Vault  
**Suluhisho**:
- Thibitisha ruhusa za Key Vault
- Angalia vitambulisho vya Azure AD
- Hakikisha managed identity imesanidiwa

## Uandishi wa Kumbukumbu

Matukio ya uthibitishaji wa mTLS yanaandikwa kwenye kumbukumbu:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Ushirikiano na Uthibitishaji Uliopo

mTLS hufanya kazi sambamba na uthibitishaji wa Azure AD:

1. **Client Certificate Validation** hutokea kwanza (transport layer)
2. **Azure AD Authentication** hutokea baadaye (application layer)

Vyote viwili vinaweza kuwezeshwa kwa wakati mmoja kwa usalama wa defense-in-depth.

## Marejeo

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Mfano wa Msimbo

Utekelezaji unaweza kupatikana katika:
- `Models/Settings/MtlsSettings.cs` - Configuration model
- `Models/Settings/FeatureFlags.cs` - Feature flag
- `Extensions/ServiceCollectionExtensions.cs` - Usajili wa huduma
- `Program.cs` - Uanzishaji wa programu

## Rasilimali za Ziada

Tazama `SupportingScripts/CertificateUploaderToAzureExample.ps1` kwa mifano ya upakiaji wa certificate.

