# Azure Key Vault PFX Certificate Guide

## Date: 2024-12-20

## Overview

This guide documents the **correct approach** for storing and retrieving full PFX certificates (with private keys) in Azure Key Vault, based on lessons learned from production implementation.

---

## ?? **Common Mistakes to Avoid**

### ? **WRONG: Storing PFX as Base64 Secret**

```powershell
# DON'T DO THIS - It doesn't work!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Why it fails:**
1. **Size limit**: Key Vault secrets have a 25 KB limit - PFX files often exceed this
2. **Encoding issues**: Base64 encoding can introduce line breaks and corruption
3. **Type mismatch**: Secrets are for simple strings, not binary certificate data
4. **No certificate metadata**: Loses expiration dates, subject info, etc.

---

## ? **CORRECT: Use Certificate-Specific APIs**

### **Method 1: Import Certificate Directly (Recommended)**

This is the **best approach** and what's currently working in the codebase.

#### Upload Certificate (PowerShell)

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

**Benefits:**
- ? Handles certificates of any size
- ? Preserves all certificate metadata
- ? Automatically creates a secret version with private key
- ? Supports certificate rotation
- ? Integrates with Azure RBAC and access policies

#### Retrieve Certificate (C#)

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

### **Method 2: Use Managed Identity (Production)**

For production environments, use **Managed Identity** instead of client secrets.

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

## ?? **Implementation in WebAppExperimental266**

### Current Implementation Status

**Location:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Status:** ?? Template implementation - needs production code

**Current Code (Template):**
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

### Recommended Update

Replace with the production-ready implementation:

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

## ?? **Required NuGet Packages**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Note:** Already installed in WebAppExperimental266 project.

---

## ?? **Configuration**

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

## ?? **Azure Key Vault Access Policies**

### Required Permissions

For the application identity (Service Principal or Managed Identity):

**Certificate Permissions:**
- ? Get
- ? List

**Secret Permissions:**
- ? Get
- ? List

**Why both Certificate AND Secret permissions?**
- Certificate permissions get metadata
- Secret permissions get the private key

### Setup via Azure Portal

1. Navigate to Key Vault ? Access policies
2. Click "Add Access Policy"
3. Select Certificate permissions: Get, List
4. Select Secret permissions: Get, List
5. Select principal (your app or managed identity)
6. Save

### Setup via Azure CLI

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

## ?? **Testing the Implementation**

### Unit Test Example

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

## ?? **Usage in mTLS**

### Integration with Certificate Authentication

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

## ?? **Comparison: Secret vs Certificate Storage**

| Feature | Store as Secret | Store as Certificate |
|---------|----------------|---------------------|
| **Size Limit** | 25 KB | Unlimited |
| **Private Key** | ? Manual handling | ? Automatic |
| **Metadata** | ? None | ? Full cert info |
| **Rotation** | ? Manual | ? Built-in |
| **Expiration** | ? Manual tracking | ? Auto-tracked |
| **RBAC** | Basic | Certificate-specific |
| **Complexity** | High | Low |
| **Recommendation** | ? Don't use | ? **Use this** |

---

## ?? **Certificate Rotation**

### Automatic Rotation

Key Vault certificates support automatic rotation:

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

### Application Code

Your application automatically gets the latest version:

```csharp
// This always gets the current version
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

To get a specific version:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "specific-version-id");
```

---

## ?? **Troubleshooting**

### Error: "Certificate not found"

**Check:**
1. Certificate name is correct
2. Certificate exists in Key Vault
3. Access policies are configured

```bash
# List certificates
az keyvault certificate list --vault-name your-keyvault
```

### Error: "Access denied"

**Check:**
1. Service Principal has correct permissions
2. Both Certificate AND Secret permissions granted
3. Managed Identity is enabled (if using)

```bash
# Check access policies
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Error: "Certificate has no private key"

**Check:**
1. Using `.GetSecretAsync()` not just `.GetCertificateAsync()`
2. Certificate was imported with private key
3. Using correct secret version

```csharp
// WRONG - No private key
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // Only public key

// CORRECT - Has private key
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // Has private key
```

### Error: "CryptographicException"

**Common causes:**
1. PFX data is corrupted
2. Wrong certificate format
3. Invalid password (shouldn't be needed for KV)

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

## ?? **Migration Checklist**

- [ ] Install required NuGet packages
- [ ] Update `AzureKeyVaultCertificateOperations.cs` with production code
- [ ] Import certificate to Key Vault using `Import-AzKeyVaultCertificate`
- [ ] Configure access policies (Certificate: Get/List, Secret: Get/List)
- [ ] Update configuration in `appsettings.json`
- [ ] Set up Managed Identity (production) or client secret (dev)
- [ ] Test certificate retrieval
- [ ] Verify private key is present
- [ ] Test mTLS with retrieved certificate
- [ ] Set up certificate rotation policy
- [ ] Document certificate management procedures

---

## ?? **Summary**

### ? **DO:**
- Use `Import-AzKeyVaultCertificate` to upload PFX
- Use `CertificateClient` + `SecretClient` to retrieve
- Use Managed Identity in production
- Grant both Certificate and Secret permissions
- Test that certificate has private key

### ? **DON'T:**
- Store PFX as Base64 secret
- Try to manually manage certificate data
- Use client secrets in production
- Forget to grant Secret permissions
- Ignore certificate expiration dates

---

## ?? **References**

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Status:** ? Guide Complete  
**Last Updated:** 2024-12-20  
**Version:** 1.0  
**Project:** WebAppExperimental266
