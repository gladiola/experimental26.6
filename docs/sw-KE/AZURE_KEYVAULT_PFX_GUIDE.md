# Mwongozo wa Cheti cha PFX wa Azure Key Vault

## Tarehe: 2024-12-20

## Muhtasari

Mwongozo huu unaandika **mbinu sahihi** ya kuhifadhi na kurejesha vyeti kamili vya PFX (vyenye private keys) katika Azure Key Vault, kulingana na mafunzo yaliyopatikana kutoka utekelezaji wa production.

---

## ?? **Makosa ya Kawaida ya Kuepuka**

### ? **SI SAHIHI: Kuhifadhi PFX kama Siri ya Base64**

```powershell
# DON'T DO THIS - It doesn't work!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Kwa nini inashindwa:**
1. **Kikomo cha ukubwa**: siri za Key Vault zina kikomo cha 25 KB - faili za PFX mara nyingi huzidi kiwango hiki
2. **Masuala ya encoding**: encoding ya Base64 inaweza kuingiza line breaks na uharibifu
3. **Kutolingana kwa aina**: Secrets ni za strings rahisi, si data ya cheti ya binary
4. **Hakuna metadata ya cheti**: Hupoteza tarehe za kuisha muda, taarifa za subject, n.k.

---

## ? **SAHIHI: Tumia API Mahususi za Cheti**

### **Mbinu ya 1: Ingiza Cheti Moja kwa Moja (Inapendekezwa)**

Hii ndiyo **mbinu bora zaidi** na ndiyo inayofanya kazi kwa sasa katika codebase.

#### Pakia Cheti (PowerShell)

```powershell
# Define variables
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# Convert password to SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Import certificate to Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Faida:**
- ? Hushughulikia vyeti vya ukubwa wowote
- ? Huhifadhi metadata yote ya cheti
- ? Hutengeneza kiotomatiki toleo la secret lenye private key
- ? Husaidia certificate rotation
- ? Huunganishwa na Azure RBAC na access policies

#### Rejesha Cheti (C#)

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
        // Create credential
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // Initialize certificate client
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        
        // Get certificate (this gets public key and metadata)
        KeyVaultCertificateWithPolicy certificate = 
            await certificateClient.GetCertificateAsync(certificateName);
        
        // Get the secret that contains the private key
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        // The secret value is the Base64-encoded PKCS12 (PFX) with private key
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        // Create X509Certificate2 with private key
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // No password needed - Key Vault handles decryption
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

### **Mbinu ya 2: Tumia Managed Identity (Production)**

Kwa mazingira ya production, tumia **Managed Identity** badala ya client secrets.

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // Use DefaultAzureCredential - automatically uses Managed Identity in Azure
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

## ?? **Utekelezaji katika WebAppExperimental266**

### Hali ya Utekelezaji wa Sasa

**Mahali:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Hali:** ?? Template implementation - inahitaji production code

**Msimbo wa Sasa (Template):**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // Template implementation - users should implement based on their Key Vault setup
    _logger.LogWarning("GetCertificateFromKeyVault called - implement this method for production use");
    
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### Sasisho Linalopendekezwa

Badilisha na utekelezaji ulio tayari kwa production:

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
            
            // Option 1: Use DefaultAzureCredential (recommended for production)
            var credential = new DefaultAzureCredential();
            
            // Option 2: Use ClientSecretCredential (if you have a client secret)
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // Get certificate metadata
            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate = 
                await certificateClient.GetCertificateAsync(certificateName);
            
            _logger.LogDebug("Certificate found. Thumbprint: {Thumbprint}, Expires: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);
            
            // Get the secret that contains the private key
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
            
            // Convert Base64 PKCS12 to X509Certificate2
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            
            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null, // Key Vault handles decryption
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

## ?? **NuGet Packages Zinazohitajika**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Kumbuka:** Tayari zimesakinishwa katika mradi wa WebAppExperimental266.

---

## ?? **Usanidi**

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
# For client secret authentication
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# For Managed Identity (production)
# No secrets needed - identity is handled by Azure
```

---

## ?? **Access Policies za Azure Key Vault**

### Ruhusa Zinazohitajika

Kwa application identity (Service Principal au Managed Identity):

**Certificate Permissions:**
- ? Get
- ? List

**Secret Permissions:**
- ? Get
- ? List

**Kwa nini ruhusa za Certificate NA Secret zote mbili?**
- Ruhusa za Certificate hupata metadata
- Ruhusa za Secret hupata private key

### Usanidi kupitia Azure Portal

1. Nenda kwenye Key Vault ? Access policies
2. Bofya "Add Access Policy"
3. Chagua Certificate permissions: Get, List
4. Chagua Secret permissions: Get, List
5. Chagua principal (programu yako au managed identity)
6. Hifadhi

### Usanidi kupitia Azure CLI

```bash
# Get the Object ID of your application or managed identity
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# Grant permissions
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## ?? **Kupima Utekelezaji**

### Mfano wa Unit Test

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    // Arrange
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);
    
    // Act
    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");
    
    // Assert
    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### Integration Test

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    // This requires actual Azure resources
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

## ?? **Matumizi katika mTLS**

### Ushirikiano na Certificate Authentication

```csharp
// In Program.cs
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    // Fetch server certificate from Key Vault
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();
    
    if (serverCertificate != null)
    {
        // Configure Kestrel to use the certificate
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

## ?? **Ulinganisho: Uhifadhi wa Secret dhidi ya Certificate**

| Kipengele | Hifadhi kama Secret | Hifadhi kama Certificate |
|---------|----------------|---------------------|
| **Size Limit** | 25 KB | Unlimited |
| **Private Key** | ? Ushughulikiaji wa manual | ? Kiotomatiki |
| **Metadata** | ? Hakuna | ? Taarifa kamili za cert |
| **Rotation** | ? Manual | ? Imejengewa ndani |
| **Expiration** | ? Ufuatiliaji wa manual | ? Hufuatiliwa kiotomatiki |
| **RBAC** | Ya msingi | Mahususi kwa certificate |
| **Complexity** | Juu | Chini |
| **Recommendation** | ? Usitumie | ? **Tumia hii** |

---

## ?? **Certificate Rotation**

### Automatic Rotation

Vyeti vya Key Vault vinaunga mkono automatic rotation:

```powershell
# Set up auto-rotation policy
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

### Msimbo wa Programu

Programu yako hupata kiotomatiki toleo la karibuni zaidi:

```csharp
// This always gets the current version
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Kupata toleo mahususi:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "specific-version-id");
```

---

## ?? **Utatuzi wa Matatizo**

### Kosa: "Certificate not found"

**Angalia:**
1. Jina la certificate ni sahihi
2. Certificate ipo katika Key Vault
3. Access policies zimesanidiwa

```bash
# List certificates
az keyvault certificate list --vault-name your-keyvault
```

### Kosa: "Access denied"

**Angalia:**
1. Service Principal ina ruhusa sahihi
2. Ruhusa za Certificate NA Secret zote mbili zimetolewa
3. Managed Identity imewezeshwa (ikiwa inatumika)

```bash
# Check access policies
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Kosa: "Certificate has no private key"

**Angalia:**
1. Unatumia `.GetSecretAsync()` si `.GetCertificateAsync()` pekee
2. Certificate iliingizwa ikiwa na private key
3. Unatumia toleo sahihi la secret

```csharp
// WRONG - No private key
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // Only public key

// CORRECT - Has private key
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // Has private key
```

### Kosa: "CryptographicException"

**Sababu za kawaida:**
1. Data ya PFX imeharibika
2. Muundo wa certificate si sahihi
3. Nenosiri si sahihi (halipaswi kuhitajika kwa KV)

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

## ?? **Orodha ya Ukaguzi wa Uhamishaji**

- [ ] Sakinisha NuGet packages zinazohitajika
- [ ] Sasisha `AzureKeyVaultCertificateOperations.cs` kwa production code
- [ ] Ingiza certificate katika Key Vault kwa kutumia `Import-AzKeyVaultCertificate`
- [ ] Sanidi access policies (Certificate: Get/List, Secret: Get/List)
- [ ] Sasisha usanidi katika `appsettings.json`
- [ ] Sanidi Managed Identity (production) au client secret (dev)
- [ ] Jaribu urejeshaji wa certificate
- [ ] Thibitisha private key ipo
- [ ] Jaribu mTLS kwa certificate iliyorejeshwa
- [ ] Sanidi sera ya certificate rotation
- [ ] Andika taratibu za usimamizi wa certificate

---

## ?? **Muhtasari**

### ? **FANYA:**
- Tumia `Import-AzKeyVaultCertificate` kupakia PFX
- Tumia `CertificateClient` + `SecretClient` kurejesha
- Tumia Managed Identity katika production
- Toa ruhusa za Certificate na Secret zote mbili
- Jaribu kuwa certificate ina private key

### ? **USIFANYE:**
- Kuhifadhi PFX kama siri ya Base64
- Kujaribu kusimamia data ya certificate kwa manual
- Kutumia client secrets katika production
- Kusahau kutoa ruhusa za Secret
- Kupuuza tarehe za kuisha muda za certificate

---

## ?? **Marejeo**

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Hali:** ? Mwongozo Umekamilika  
**Ilisasishwa Mwisho:** 2024-12-20  
**Toleo:** 1.0  
**Mradi:** WebAppExperimental266

