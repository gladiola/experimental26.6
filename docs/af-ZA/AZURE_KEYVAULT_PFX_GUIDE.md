# Azure Key Vault PFX-Sertifikaatgids

## Datum: 2024-12-20

## Oorsig

Hierdie gids dokumenteer die **korrekte benadering** vir die stoor en herwinning van volledige PFX-sertifikate (met privaat sleutels) in Azure Key Vault, gebaseer op lesse wat uit produksie-implementasie geleer is.

---

## ⚠️ **Algemene Foute om te Vermy**

### ❌ **VERKEERD: PFX as Base64-Geheim Stoor**

```powershell
# MOENIE DIT DOEN NIE - Dit werk nie!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Waarom dit misluk:**
1. **Groottelimiet**: Key Vault-geheime het 'n 25 KB-limiet — PFX-lêers oorskry dikwels dit
2. **Koderingsprobleme**: Base64-kodering kan reëlbreuke en korrupsie invoer
3. **Tipe-wanpassing**: Geheime is vir eenvoudige strings, nie binêre sertifikaatdata nie
4. **Geen sertifikaat-metadata**: Verloor vervaldatums, onderwerpinligting, ens.

---

## ✅ **KORREK: Gebruik Sertifikaat-Spesifieke API's**

### **Metode 1: Importeer Sertifikaat Direk (Aanbeveel)**

Dit is die **beste benadering** en wat tans in die kodebaseer werk.

#### Laai Sertifikaat Op (PowerShell)

```powershell
# Definieer veranderlikes
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# Skakel wagwoord na SecureString om
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Importeer sertifikaat na Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Voordele:**
- ✅ Hanteer sertifikate van enige grootte
- ✅ Behou alle sertifikaat-metadata
- ✅ Skep outomaties 'n geheimweergawe met privaat sleutel
- ✅ Ondersteun sertifikaat-rotasie
- ✅ Integreer met Azure RBAC en toegangsbeleide

#### Haal Sertifikaat Op (C#)

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
        // Skep geloofsbrief
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // Initialiseer sertifikaat-kliënt
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        
        // Haal sertifikaat (dit kry publieke sleutel en metadata)
        KeyVaultCertificateWithPolicy certificate = 
            await certificateClient.GetCertificateAsync(certificateName);
        
        // Haal die geheim wat die privaat sleutel bevat
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        // Die geheimwaarde is die Base64-gekodeerde PKCS12 (PFX) met privaat sleutel
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        // Skep X509Certificate2 met privaat sleutel
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // Geen wagwoord nodig nie — Key Vault hanteer dekripsie
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

### **Metode 2: Gebruik Beheerde Identiteit (Produksie)**

Vir produksie-omgewings, gebruik **Beheerde Identiteit** eerder as kliëntgeheime.

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // Gebruik DefaultAzureCredential — gebruik outomaties Beheerde Identiteit in Azure
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

## 🔧 **Implementasie in WebAppExperimental266**

### Huidige Implementasiestatus

**Ligging:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Status:** 📋 Sjabloon-implementasie — benodig produksiekode

**Huidige Kode (Sjabloon):**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // Sjabloon-implementasie — gebruikers moet dit implementeer gebaseer op hul Key Vault-opstel
    _logger.LogWarning("GetCertificateFromKeyVault called - implement this method for production use");
    
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### Aanbevole Opdatering

Vervang met die produksie-gereed implementasie:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public class AzureKeyVaultCertificateOperations : IAzureKeyVaultCertificateOperations
{
    private readonly ILogger<AzureKeyVaultCertificateOperations> _logger;

    public AzureKeyVaultCertificateOperations(ILogger<AzureKeyVaultCertificateOperations> logger)
    {
        _logger = logger;
    }

    public async Task<X509Certificate2?> GetCertificateFromKeyVault(
        string tenantId,
        string clientId,
        string keyVaultURL,
        string certificateName,
        string certPasswordName)
    {
        try
        {
            _logger.LogInformation("Retrieving certificate '{CertName}' from Key Vault", certificateName);
            
            // Opsie 1: Gebruik DefaultAzureCredential (aanbeveel vir produksie)
            var credential = new DefaultAzureCredential();
            
            // Opsie 2: Gebruik ClientSecretCredential (as jy 'n kliëntgeheim het)
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // Haal sertifikaat-metadata
            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate = 
                await certificateClient.GetCertificateAsync(certificateName);
            
            _logger.LogDebug("Certificate found. Thumbprint: {Thumbprint}, Expires: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);
            
            // Haal die geheim wat die privaat sleutel bevat
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
            
            // Skakel Base64 PKCS12 na X509Certificate2 om
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            
            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null, // Key Vault hanteer dekripsie
                X509KeyStorageFlags.MachineKeySet | 
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.PersistKeySet);
            
            _logger.LogInformation("Successfully loaded certificate with private key");
            
            return x509Certificate;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error loading certificate '{CertName}'", certificateName);
            return null;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Key Vault error: {StatusCode} - {Message}", 
                ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving certificate");
            return null;
        }
    }

    public async Task<KeyVaultSecret> GetSecretFromKeyVault(
        string tenantId,
        string clientId,
        string clientSecret,
        string keyVaultURL,
        string secretName)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            
            return await secretClient.GetSecretAsync(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}'", secretName);
            throw;
        }
    }
}
```

---

## 📦 **Vereiste NuGet-Pakkette**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Nota:** Reeds geïnstalleer in die WebAppExperimental266-projek.

---

## ⚙️ **Konfigurasie**

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

### Gebruikersgeheime

```powershell
# Vir kliëntgeheim-verifikasie
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Vir Beheerde Identiteit (produksie)
# Geen geheime nodig nie — identiteit word deur Azure hanteer
```

---

## 🔐 **Azure Key Vault-Toegangsbeleide**

### Vereiste Toestemmings

Vir die toepassingsidentiteit (Diensprinsipaal of Beheerde Identiteit):

**Sertifikaat-Toestemmings:**
- ✅ Kry
- ✅ Lys

**Geheim-Toestemmings:**
- ✅ Kry
- ✅ Lys

**Waarom beide Sertifikaat- EN Geheim-toestemmings?**
- Sertifikaat-toestemmings kry metadata
- Geheim-toestemmings kry die privaat sleutel

### Opstel via Azure-Portaal

1. Navigeer na Key Vault → Toegangsbeleide
2. Klik "Voeg Toegangsbeleid By"
3. Kies Sertifikaat-toestemmings: Kry, Lys
4. Kies Geheim-toestemmings: Kry, Lys
5. Kies prinsipaal (jou toepassing of beheerde identiteit)
6. Stoor

### Opstel via Azure CLI

```bash
# Kry die Objek-ID van jou toepassing of beheerde identiteit
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# Gee toestemmings
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 **Toetsing van die Implementasie**

### Eenheidstoets-Voorbeeld

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    // Reël
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);
    
    // Doen
    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");
    
    // Beweer
    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### Integrasietoets

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    // Dit vereis werklike Azure-hulpbronne
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

## 🔒 **Gebruik in mTLS**

### Integrasie met Sertifikaat-Verifikasie

```csharp
// In Program.cs
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    // Haal bedienersertifikaat van Key Vault
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();
    
    if (serverCertificate != null)
    {
        // Stel Kestrel in om die sertifikaat te gebruik
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

## 📊 **Vergelyking: Geheim vs Sertifikaatstoor**

| Kenmerk | Stoor as Geheim | Stoor as Sertifikaat |
|---------|----------------|---------------------|
| **Groottelimiet** | 25 KB | Onbeperk |
| **Privaat Sleutel** | ❌ Handmatige hantering | ✅ Outomaties |
| **Metadata** | ❌ Geen | ✅ Volledige sertifikaatinligting |
| **Rotasie** | ❌ Handmatig | ✅ Ingeboude |
| **Verstryking** | ❌ Handmatige opsporing | ✅ Outomaties opgespoor |
| **RBAC** | Basiese | Sertifikaat-spesifiek |
| **Kompleksiteit** | Hoog | Laag |
| **Aanbeveling** | ❌ Gebruik dit nie | ✅ **Gebruik dit** |

---

## 🔄 **Sertifikaat-Rotasie**

### Outomatiese Rotasie

Key Vault-sertifikate ondersteun outomatiese rotasie:

```powershell
# Stel outomatiese rotasiebeleid in
az keyvault certificate set-policy `
    --vault-name your-keyvault `
    --name server-cert `
    --policy @policy.json
```

policy.json:
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

### Toepassingskode

Jou toepassing kry outomaties die nuutste weergawe:

```csharp
// Dit kry altyd die huidige weergawe
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Om 'n spesifieke weergawe te kry:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "specific-version-id");
```

---

## 🔍 **Probleemoplossing**

### Fout: "Certificate not found"

**Kontroleer:**
1. Sertifikaatnaam is korrek
2. Sertifikaat bestaan in Key Vault
3. Toegangsbeleide is gekonfigureer

```bash
# Lys sertifikate
az keyvault certificate list --vault-name your-keyvault
```

### Fout: "Access denied"

**Kontroleer:**
1. Diensprinsipaal het korrekte toestemmings
2. Beide Sertifikaat- EN Geheim-toestemmings verleen
3. Beheerde Identiteit is geaktiveer (as gebruik word)

```bash
# Kontroleer toegangsbeleide
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Fout: "Certificate has no private key"

**Kontroleer:**
1. Gebruik `.GetSecretAsync()` nie net `.GetCertificateAsync()` nie
2. Sertifikaat is ingevoer met privaat sleutel
3. Korrekte geheimweergawe word gebruik

```csharp
// VERKEERD - Geen privaat sleutel
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // Slegs publieke sleutel

// KORREK - Het privaat sleutel
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // Het privaat sleutel
```

### Fout: "CryptographicException"

**Algemene oorsake:**
1. PFX-data is gekorrupteer
2. Verkeerde sertifikaatformaat
3. Ongeldige wagwoord (behoort nie nodig te wees vir KV nie)

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

## ✅ **Migrasie-Kontrolelys**

- [ ] Installeer vereiste NuGet-pakkette
- [ ] Werk `AzureKeyVaultCertificateOperations.cs` op met produksiekode
- [ ] Importeer sertifikaat na Key Vault deur `Import-AzKeyVaultCertificate` te gebruik
- [ ] Stel toegangsbeleide in (Sertifikaat: Kry/Lys, Geheim: Kry/Lys)
- [ ] Werk konfigurasie in `appsettings.json` op
- [ ] Stel Beheerde Identiteit in (produksie) of kliëntgeheim (onwikkeling)
- [ ] Toets sertifikaat-herwinning
- [ ] Verifieer dat privaat sleutel teenwoordig is
- [ ] Toets mTLS met opgehaalde sertifikaat
- [ ] Stel sertifikaat-rotasiebeleid in
- [ ] Dokumenteer sertifikaat-bestuursprosedures

---

## 📋 **Opsomming**

### ✅ **DOEN:**
- Gebruik `Import-AzKeyVaultCertificate` om PFX op te laai
- Gebruik `CertificateClient` + `SecretClient` om te haal
- Gebruik Beheerde Identiteit in produksie
- Verleen beide Sertifikaat- en Geheim-toestemmings
- Toets dat sertifikaat privaat sleutel het

### ❌ **MOENIE:**
- PFX as Base64-geheim stoor
- Probeer om sertifikaatdata handmatig te bestuur
- Kliëntgeheime in produksie gebruik
- Vergeet om Geheim-toestemmings te verleen
- Sertifikaatvervalsdatums ignoreer

---

## 📚 **Verwysings**

- [Azure Key Vault Sertifikate Oorsig](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Pakket](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Beheerde Identiteit Dokumentasie](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Klas](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Status:** ✅ Gids Voltooi  
**Laas Opgedateer:** 2024-12-20  
**Weergawe:** 1.0  
**Projek:** WebAppExperimental266
