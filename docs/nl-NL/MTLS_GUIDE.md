# mTLS (Mutual TLS) Clientcertificaat-authenticatiegids

## Overzicht

Dit project ondersteunt nu **mutual TLS (mTLS)**-authenticatie, waarbij zowel server als client een geldig certificaat moeten presenteren. Dit geeft extra beveiliging via tweezijdige authenticatie.

## Wat is mTLS?

mTLS breidt standaard TLS uit met:
1. **Servercertificaat**: de server toont een certificaat om identiteit te bewijzen (standaard HTTPS)
2. **Clientcertificaat**: de client toont ook een certificaat om identiteit te bewijzen (mTLS-toevoeging)

## Configuratie

### 1. Featureflag

Zet mTLS aan in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. mTLS-instellingen

Configureer mTLS-gedrag in `appsettings.json`:

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

#### Configuratieopties

| Instelling | Type | Standaard | Beschrijving |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | Als true, is clientcertificaat verplicht |
| `AllowCertificateChains` | bool | `true` | CA-ondertekende certificaatketens toestaan |
| `AllowSelfSignedCertificates` | bool | `false` | Zelfondertekende certificaten toestaan (alleen dev) |
| `CheckCertificateRevocation` | bool | `false` | Online intrekkingscontrole uitvoeren |
| `ClientCertificateName` | string | null | Certificaatnaam in Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Certificaat-uitgever valideren |

### 3. Servercertificaat (Azure Key Vault)

Het servercertificaat wordt uit Azure Key Vault opgehaald:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Setup-instructies

### Vereisten

1. Azure Key Vault met juiste rechten
2. Servercertificaat opgeslagen in Azure Key Vault als secret (PFX-formaat)
3. Clientcertificaten (zelf genereren of van CA)

### Stap 1: servercertificaat uploaden naar Key Vault

```bash
# Converteer certificaat naar PFX indien nodig
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Upload naar Key Vault via Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Sla wachtwoord op als apart secret
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Stap 2: clientcertificaten genereren

#### Optie A: Zelfondertekend (alleen ontwikkeling)

```powershell
# Clientcertificaat genereren
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Exporteren naar PFX
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Optie B: CA-ondertekend (productie)

Werk met je Certificate Authority om clientcertificaten te verkrijgen.

### Stap 3: applicatie configureren

Werk `appsettings.json` bij:

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

### Stap 4: testen met clientcertificaat

#### Met cURL:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### Met PowerShell:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Met browser:

1. Importeer clientcertificaat in browsercertificaatstore
2. Ga naar de applicatie
3. Browser vraagt om clientcertificaat te kiezen

## Omgevingsspecifiek gedrag

### Ontwikkeling
- Servercertificaat wordt uit Key Vault geladen (indien beschikbaar)
- Clientcertificaten zijn **optioneel** (`AllowCertificate`-modus)
- Zelfondertekende certificaten kunnen worden toegestaan

### Productie
- Servercertificaat wordt uit Key Vault geladen
- Clientcertificaten zijn **verplicht** als `EnableMtls = true`
- Alleen ketencertificaten aanbevolen

## Security best practices

### ✅ DO:
- Gebruik CA-ondertekende certificaten in productie
- Sla certificaten op in Azure Key Vault
- Zet intrekkingscontrole aan in productie
- Valideer certificaat-uitgever
- Gebruik sterke wachtwoorden voor PFX-bestanden
- Roteer certificaten regelmatig

### ❌ DON'T:
- Gebruik geen zelfondertekende certificaten in productie
- Commit geen certificaten naar source control
- Deel clientcertificaten niet tussen gebruikers
- Schakel certificaatvalidatie niet uit in productie

## Troubleshooting

### Fout: "No client certificate provided"

**Oorzaak**: client stuurde geen certificaat mee  
**Oplossing**:
- Controleer of clientcertificaat geïnstalleerd is
- Controleer `RequireClientCertificate`
- Zorg dat certificaat door systeem wordt vertrouwd

### Fout: "Certificate chain validation failed"

**Oorzaak**: certificaat niet vertrouwd  
**Oplossing**:
- Installeer CA-rootcertificaat
- Zet `AllowSelfSignedCertificates = true` voor testen
- Controleer of certificaat niet verlopen is

### Fout: "Server certificate not retrieved from Key Vault"

**Oorzaak**: toegang tot Azure Key Vault mislukt  
**Oplossing**:
- Verifieer Key Vault-rechten
- Controleer Azure AD-credentials
- Zorg dat managed identity is geconfigureerd

## Logging

mTLS-authenticatiegebeurtenissen worden gelogd:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Integratie met bestaande authenticatie

mTLS werkt naast Azure AD-authenticatie:

1. **Clientcertificaatvalidatie** eerst (transportlaag)
2. **Azure AD-authenticatie** daarna (applicatielaag)

Beide kunnen tegelijk aanstaan voor defense-in-depth.

## Referenties

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Voorbeeldcode

Implementatie staat in:
- `Models/Settings/MtlsSettings.cs` - configuratiemodel
- `Models/Settings/FeatureFlags.cs` - featureflag
- `Extensions/ServiceCollectionExtensions.cs` - serviceregistratie
- `Program.cs` - applicatiestart

## Extra bronnen

Zie `SupportingScripts/CertificateUploaderToAzureExample.ps1` voor uploadvoorbeelden van certificaten.
