# Azure Key Vault PFX-certificaatgids

## Datum: 2024-12-20

## Overzicht

Deze gids documenteert de **juiste aanpak** voor het opslaan en ophalen van volledige PFX-certificaten (met private keys) in Azure Key Vault, gebaseerd op lessen uit productie-implementaties.

---

## ⚠️ Veelgemaakte fouten die je moet vermijden

### ❌ FOUT: PFX opslaan als Base64-secret

```powershell
# DOE DIT NIET - werkt niet!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Waarom dit faalt:**
1. **Groottelimiet**: Key Vault-secrets hebben een limiet van 25 KB - PFX-bestanden zitten daar vaak boven
2. **Encoding-problemen**: Base64 kan regeleinden/corruptie introduceren
3. **Type mismatch**: Secrets zijn voor eenvoudige strings, niet voor binaire certificaatdata
4. **Geen certificaatmetadata**: vervaldatum, subject-info enz. gaan verloren

---

## ✅ JUIST: gebruik certificaat-specifieke API's

### Methode 1: certificaat direct importeren (aanbevolen)

Dit is de **beste aanpak** en werkt nu in de codebase.

#### Certificaat uploaden (PowerShell)

```powershell
# Variabelen definiëren
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# Wachtwoord naar SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Certificaat importeren naar Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Voordelen:**
- ✅ Werkt met certificaten van elke grootte
- ✅ Behoudt alle certificaatmetadata
- ✅ Maakt automatisch een secret-versie met private key
- ✅ Ondersteunt certificaatrotatie
- ✅ Integreert met Azure RBAC en access policies

#### Certificaat ophalen (C#)

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public async Task<X509Certificate2?> GetCertificateFromKeyVaultAsync(
    string tenantId,
    string clientId,
    string clientSecret,
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);

        KeyVaultCertificateWithPolicy certificate =
            await certificateClient.GetCertificateAsync(certificateName);

        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Error loading PFX certificate from Key Vault");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error retrieving certificate");
        return null;
    }
}
```

---

### Methode 2: Managed Identity gebruiken (productie)

Gebruik in productie **Managed Identity** in plaats van client secrets.

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        var credential = new DefaultAzureCredential();

        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        var certificate = await certificateClient.GetCertificateAsync(certificateName);

        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        var secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving certificate with Managed Identity");
        return null;
    }
}
```

---

## 📌 Implementatie in WebAppExperimental266

### Huidige implementatiestatus

**Locatie:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`  
**Status:** ⚠️ Template-implementatie - productiecode nodig

### Aanbevolen update

Vervang de template door productieklare code en gebruik `CertificateClient` + `SecretClient` voor metadata + private key-ophaling.

---

## 📦 Vereiste NuGet-packages

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Opmerking:** Al geïnstalleerd in `WebAppExperimental266`.

---

## ⚙️ Configuratie

### appsettings.json

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "{{USE_USER_SECRETS}}",
    "KeyVaultPassName": "server-cert"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "ClientCertificateName": "client-cert"
  }
}
```

### User Secrets

```powershell
# Voor client-secret authenticatie
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Voor Managed Identity (productie)
# Geen secrets nodig - identity wordt door Azure afgehandeld
```

---

## 🔐 Azure Key Vault access policies

### Vereiste rechten

Voor de applicatie-identiteit (Service Principal of Managed Identity):

**Certificaatrechten:**
- ✅ Get
- ✅ List

**Secretrechten:**
- ✅ Get
- ✅ List

**Waarom zowel Certificate als Secret rechten?**
- Certificate-rechten geven metadata
- Secret-rechten geven de private key

### Instellen via Azure Portal

1. Ga naar Key Vault → Access policies
2. Klik op "Add Access Policy"
3. Selecteer Certificate permissions: Get, List
4. Selecteer Secret permissions: Get, List
5. Selecteer principal (jouw app of managed identity)
6. Opslaan

### Instellen via Azure CLI

```bash
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 De implementatie testen

### Unit test-voorbeeld

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);

    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");

    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### Integratietest

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    var keyVaultUrl = TestConfiguration["AzureKeyVault:KeyVaultURL"];
    var certName = TestConfiguration["AzureKeyVault:CertificateName"];

    var operations = new AzureKeyVaultCertificateOperations(_logger);

    var cert = await operations.GetCertificateFromKeyVault(
        tenantId: TestConfiguration["AzureAd:TenantId"],
        clientId: TestConfiguration["AzureAd:ClientId"],
        keyVaultURL: keyVaultUrl,
        certificateName: certName,
        certPasswordName: "");

    Assert.NotNull(cert);
    Assert.True(cert.HasPrivateKey, "Certificate must have private key");
}
```

---

## 🔗 Gebruik in mTLS

```csharp
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();

    if (serverCertificate != null)
    {
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = serverCertificate;
            });
        });

        logger.LogInformation("mTLS enabled with Key Vault certificate");
    }
}
```

---

## 📊 Vergelijking: opslaan als secret vs als certificaat

| Feature | Opslaan als Secret | Opslaan als Certificaat |
|---------|----------------|---------------------|
| **Groottelimiet** | 25 KB | Onbeperkt |
| **Private key** | ❌ Handmatig | ✅ Automatisch |
| **Metadata** | ❌ Geen | ✅ Volledige cert-info |
| **Rotatie** | ❌ Handmatig | ✅ Ingebouwd |
| **Vervaldatum** | ❌ Handmatig volgen | ✅ Automatisch gevolgd |
| **RBAC** | Basis | Certificaat-specifiek |
| **Complexiteit** | Hoog | Laag |
| **Aanbeveling** | ❌ Niet gebruiken | ✅ **Gebruik dit** |

---

## 🔄 Certificaatrotatie

### Automatische rotatie

```powershell
az keyvault certificate set-policy `
    --vault-name your-keyvault `
    --name server-cert `
    --policy @policy.json
```

`policy.json`:
```json
{
  "lifetimeActions": [
    {
      "trigger": {
        "daysBeforeExpiry": 30
      },
      "action": {
        "actionType": "AutoRenew"
      }
    }
  ]
}
```

### Applicatiecode

Je applicatie haalt automatisch de nieuwste versie op:

```csharp
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Voor een specifieke versie:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName,
    version: "specific-version-id");
```

---

## 🛠️ Troubleshooting

### Fout: "Certificate not found"

**Controleer:**
1. Certificaatnaam klopt
2. Certificaat bestaat in Key Vault
3. Access policies zijn geconfigureerd

```bash
az keyvault certificate list --vault-name your-keyvault
```

### Fout: "Access denied"

**Controleer:**
1. Service Principal heeft juiste rechten
2. Zowel Certificate- als Secret-rechten toegekend
3. Managed Identity staat aan (indien gebruikt)

```bash
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Fout: "Certificate has no private key"

**Controleer:**
1. Gebruik `.GetSecretAsync()` en niet alleen `.GetCertificateAsync()`
2. Certificaat is mét private key geïmporteerd
3. Juiste secretversie wordt gebruikt

```csharp
// FOUT - geen private key
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // Alleen publieke sleutel

// GOED - bevat private key
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // Bevat private key
```

### Fout: "CryptographicException"

**Veelvoorkomende oorzaken:**
1. PFX-data is corrupt
2. Verkeerd certificaatformaat
3. Ongeldig wachtwoord (normaliter niet nodig bij Key Vault)

```csharp
try
{
    var cert = new X509Certificate2(pfxBytes);
}
catch (CryptographicException ex)
{
    _logger.LogError("PFX data length: {Length}, First 20 chars: {Preview}",
        pfxBytes.Length,
        Convert.ToBase64String(pfxBytes.Take(20).ToArray()));
    throw;
}
```

---

## ✅ Migratiechecklist

- [ ] Vereiste NuGet-packages geïnstalleerd
- [ ] `AzureKeyVaultCertificateOperations.cs` bijgewerkt met productiecode
- [ ] Certificaat geïmporteerd in Key Vault met `Import-AzKeyVaultCertificate`
- [ ] Access policies geconfigureerd (Certificate: Get/List, Secret: Get/List)
- [ ] Configuratie in `appsettings.json` bijgewerkt
- [ ] Managed Identity (prod) of client secret (dev) ingesteld
- [ ] Certificaatophaling getest
- [ ] Bevestigd dat private key aanwezig is
- [ ] mTLS getest met opgehaald certificaat
- [ ] Certificaatrotatiebeleid ingesteld
- [ ] Certificaatbeheerprocedure gedocumenteerd

---

## 📌 Samenvatting

### ✅ WEL DOEN:
- Gebruik `Import-AzKeyVaultCertificate` om PFX te uploaden
- Gebruik `CertificateClient` + `SecretClient` om op te halen
- Gebruik Managed Identity in productie
- Geef zowel Certificate- als Secret-rechten
- Test dat certificaat een private key bevat

### ❌ NIET DOEN:
- PFX opslaan als Base64-secret
- Certificaatdata handmatig beheren
- Client secrets gebruiken in productie
- Secret-rechten vergeten toe te kennen
- Certificaatvervaldatums negeren

---

## 📚 Referenties

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Status:** ✅ Gids compleet  
**Laatst bijgewerkt:** 2024-12-20  
**Versie:** 1.0  
**Project:** WebAppExperimental266
