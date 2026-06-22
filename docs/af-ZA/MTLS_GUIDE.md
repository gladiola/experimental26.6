# mTLS (Wedersydse TLS) Kliëntsertifikaat-Verifikasiegids

## Oorsig

Hierdie projek ondersteun nou **wedersydse TLS (mTLS)**-verifikasie, wat vereis dat beide die bediener en die kliënt geldige sertifikate aanbied. Dit bied verbeterde sekuriteit deur tweerigtingverifikasie.

## Wat is mTLS?

mTLS brei standaard TLS uit deur te vereis:
1. **Bedienersertifikaat**: Die bediener bied 'n sertifikaat aan om sy identiteit te bewys (standaard HTTPS)
2. **Kliëntsertifikaat**: Die kliënt bied ook 'n sertifikaat aan om sy identiteit te bewys (mTLS-byvoeging)

## Konfigurasie

### 1. Funksievlag

Aktiveer mTLS in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. mTLS-Instellings

Stel mTLS-gedrag in `appsettings.json` in:

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

#### Konfigurasie-Opsies

| Instelling | Tipe | Standaard | Beskrywing |
|-----------|------|-----------|-----------|
| `RequireClientCertificate` | bool | `true` | Indien waar, is kliëntsertifikaat verpligtend |
| `AllowCertificateChains` | bool | `true` | Laat gekettingde (CA-ondertekende) sertifikate toe |
| `AllowSelfSignedCertificates` | bool | `false` | Laat selfondertekende sertifikate toe (slegs ontwikkeling) |
| `CheckCertificateRevocation` | bool | `false` | Voer aanlyn herroepingskontrolering uit |
| `ClientCertificateName` | string | null | Sertifikaatnaam in Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Valideer sertifikaat-uitreiker |

### 3. Bedienersertifikaat (Azure Key Vault)

Die bedienersertifikaat word van Azure Key Vault gehaal:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Opstel-Instruksies

### Vereistes

1. Azure Key Vault met toepaslike toestemmings
2. Bedienersertifikaat gestoor in Azure Key Vault as 'n geheim (PFX-formaat)
3. Kliëntsertifikate (kan gegenereer of van 'n CA verkry word)

### Stap 1: Laai Bedienersertifikaat na Key Vault Op

```bash
# Skakel sertifikaat na PFX om indien nodig
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Laai na Key Vault op deur Azure CLI te gebruik
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Stoor wagwoord as aparte geheim
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Stap 2: Genereer Kliëntsertifikate

#### Opsie A: Selfondertekend (Slegs Ontwikkeling)

```powershell
# Genereer kliëntsertifikaat
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Voer na PFX uit
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Opsie B: CA-ondertekend (Produksie)

Werk met jou Sertifikaat-Outoriteit om kliëntsertifikate te bekom.

### Stap 3: Stel Toepassing In

Werk `appsettings.json` op:

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

### Stap 4: Toets met Kliëntsertifikaat

#### Deur cURL te gebruik:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### Deur PowerShell te gebruik:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Deur Blaaier te gebruik:

1. Importeer kliëntsertifikaat in die blaaier se sertifikaatwinkel
2. Navigeer na jou toepassing
3. Blaaier sal vra om kliëntsertifikaat te kies

## Omgewing-Spesifieke Gedrag

### Ontwikkeling
- Bedienersertifikaat word van Key Vault gelaai (indien beskikbaar)
- Kliëntsertifikate is **opsioneel** (`AllowCertificate`-modus)
- Selfondertekende sertifikate kan toegelaat word

### Produksie
- Bedienersertifikaat word van Key Vault gelaai
- Kliëntsertifikate is **vereis** as `EnableMtls = true`
- Slegs gekettingde sertifikate word aanbeveel

## Sekuriteit Beste Praktyke

### ✅ DOEN:
- Gebruik CA-ondertekende sertifikate in produksie
- Stoor sertifikate in Azure Key Vault
- Aktiveer sertifikaat-herroepingskontrolering in produksie
- Valideer sertifikaat-uitreiker
- Gebruik sterk wagwoorde vir PFX-lêers
- Roteer sertifikate gereeld

### ❌ MOENIE:
- Selfondertekende sertifikate in produksie gebruik
- Sertifikate na bronbeheer pleeg
- Kliëntsertifikate oor gebruikers deel
- Sertifikaat-validasie in produksie deaktiveer

## Probleemoplossing

### Fout: "No client certificate provided"

**Oorsaak**: Kliënt het nie sertifikaat gestuur nie  
**Oplossing**: 
- Verifieer dat kliëntsertifikaat geïnstalleer is
- Kontroleer `RequireClientCertificate`-instelling
- Verseker dat sertifikaat deur die stelsel vertrou word

### Fout: "Certificate chain validation failed"

**Oorsaak**: Sertifikaat nie vertrou nie  
**Oplossing**:
- Installeer CA-wortel-sertifikaat
- Stel `AllowSelfSignedCertificates = true` vir toetsing
- Verifieer dat sertifikaat nie verstryk het nie

### Fout: "Server certificate not retrieved from Key Vault"

**Oorsaak**: Azure Key Vault-toegangsprobleem  
**Oplossing**:
- Verifieer Key Vault-toestemmings
- Kontroleer Azure AD-geloofsbriewe
- Verseker dat beheerde identiteit gekonfigureer is

## Aantekening

mTLS-verifikasie-gebeure word aangeteken:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Integrasie met Bestaande Verifikasie

mTLS werk saam met Azure AD-verifikasie:

1. **Kliëntsertifikaat-Validasie** gebeur eerste (vervoerlaag)
2. **Azure AD-Verifikasie** gebeur daarna (toepassingslaag)

Beide kan gelyktydig geaktiveer word vir diepte-in-diepte-sekuriteit.

## Verwysings

- [Microsoft Docs: Sertifikaat-Verifikasie](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integrasie](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Voorbeeldkode

Die implementasie kan gevind word in:
- `Models/Settings/MtlsSettings.cs` — Konfigurasiemodel
- `Models/Settings/FeatureFlags.cs` — Funksievlag
- `Extensions/ServiceCollectionExtensions.cs` — Diensregistrasie
- `Program.cs` — Toepassingopstart

## Addisionele Hulpbronne

Sien `SupportingScripts/CertificateUploaderToAzureExample.ps1` vir sertifikaat-oplaaivoorbeelde.
