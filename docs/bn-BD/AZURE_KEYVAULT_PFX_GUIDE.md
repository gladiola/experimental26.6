# Azure Key Vault PFX সার্টিফিকেট গাইড

## তারিখ: 2024-12-20

## সংক্ষিপ্ত বিবরণ

এই গাইডটি Azure Key Vault-এ সম্পূর্ণ PFX সার্টিফিকেট (প্রাইভেট কী সহ) সংরক্ষণ এবং পুনরুদ্ধারের **সঠিক পদ্ধতি** নথিভুক্ত করে, যা প্রোডাকশন বাস্তবায়ন থেকে শেখা অভিজ্ঞতার উপর ভিত্তি করে।

---

## ?? **সাধারণ ভুল যা এড়ানো উচিত**

### ? **ভুল: PFX কে Base64 সিক্রেট হিসেবে সংরক্ষণ করা**

```powershell
# এটি করবেন না - এটি কাজ করে না!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**কেন এটি ব্যর্থ হয়:**
1. **আকারের সীমা**: Key Vault সিক্রেটের 25 KB সীমা আছে - PFX ফাইল প্রায়ই এটি ছাড়িয়ে যায়
2. **এনকোডিং সমস্যা**: Base64 এনকোডিং লাইন ব্রেক এবং দুর্নীতি আনতে পারে
3. **টাইপ অমিল**: সিক্রেট সাধারণ স্ট্রিংয়ের জন্য, বাইনারি সার্টিফিকেট ডেটার জন্য নয়
4. **সার্টিফিকেট মেটাডেটা নেই**: মেয়াদ শেষের তারিখ, বিষয়ের তথ্য ইত্যাদি হারিয়ে যায়

---

## ? **সঠিক: সার্টিফিকেট-নির্দিষ্ট API ব্যবহার করুন**

### **পদ্ধতি ১: সার্টিফিকেট সরাসরি আমদানি করুন (প্রস্তাবিত)**

এটি **সেরা পদ্ধতি** এবং বর্তমানে কোডবেসে কার্যকর।

#### সার্টিফিকেট আপলোড করুন (PowerShell)

```powershell
# ভেরিয়েবল সংজ্ঞায়িত করুন
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# পাসওয়ার্ডকে SecureString-এ রূপান্তর করুন
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Key Vault-এ সার্টিফিকেট আমদানি করুন
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**সুবিধাসমূহ:**
- ? যেকোনো আকারের সার্টিফিকেট পরিচালনা করে
- ? সমস্ত সার্টিফিকেট মেটাডেটা সংরক্ষণ করে
- ? স্বয়ংক্রিয়ভাবে প্রাইভেট কী সহ একটি সিক্রেট ভার্সন তৈরি করে
- ? সার্টিফিকেট রোটেশন সমর্থন করে
- ? Azure RBAC এবং অ্যাক্সেস নীতির সাথে একীভূত

#### সার্টিফিকেট পুনরুদ্ধার করুন (C#)

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
        // ক্রেডেনশিয়াল তৈরি করুন
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // সার্টিফিকেট ক্লায়েন্ট শুরু করুন
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        
        // সার্টিফিকেট পান (পাবলিক কী এবং মেটাডেটা)
        KeyVaultCertificateWithPolicy certificate = 
            await certificateClient.GetCertificateAsync(certificateName);
        
        // প্রাইভেট কী সহ সিক্রেট পান
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        // সিক্রেট মান হল Base64-এনকোড করা PKCS12 (PFX) প্রাইভেট কী সহ
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        // প্রাইভেট কী সহ X509Certificate2 তৈরি করুন
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // কোনো পাসওয়ার্ড প্রয়োজন নেই - Key Vault ডিক্রিপশন পরিচালনা করে
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Key Vault থেকে PFX সার্টিফিকেট লোড করতে ত্রুটি");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "সার্টিফিকেট পুনরুদ্ধারে অপ্রত্যাশিত ত্রুটি");
        return null;
    }
}
```

---

### **পদ্ধতি ২: ম্যানেজড আইডেন্টিটি ব্যবহার করুন (প্রোডাকশন)**

প্রোডাকশন পরিবেশের জন্য, ক্লায়েন্ট সিক্রেটের পরিবর্তে **ম্যানেজড আইডেন্টিটি** ব্যবহার করুন।

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // DefaultAzureCredential ব্যবহার করুন - Azure-এ স্বয়ংক্রিয়ভাবে ম্যানেজড আইডেন্টিটি ব্যবহার করে
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
        _logger.LogError(ex, "ম্যানেজড আইডেন্টিটি দিয়ে সার্টিফিকেট পুনরুদ্ধারে ত্রুটি");
        return null;
    }
}
```

---

## ?? **WebAppExperimental266-এ বাস্তবায়ন**

### বর্তমান বাস্তবায়নের অবস্থা

**অবস্থান:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**অবস্থা:** ?? টেমপ্লেট বাস্তবায়ন - প্রোডাকশন কোড প্রয়োজন

**বর্তমান কোড (টেমপ্লেট):**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // টেমপ্লেট বাস্তবায়ন - ব্যবহারকারীদের তাদের Key Vault সেটআপের উপর ভিত্তি করে এই পদ্ধতি বাস্তবায়ন করা উচিত
    _logger.LogWarning("GetCertificateFromKeyVault কল করা হয়েছে - প্রোডাকশন ব্যবহারের জন্য এই পদ্ধতি বাস্তবায়ন করুন");
    
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### প্রস্তাবিত আপডেট

প্রোডাকশন-রেডি বাস্তবায়ন দিয়ে প্রতিস্থাপন করুন:

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
            _logger.LogInformation("Key Vault থেকে '{CertName}' সার্টিফিকেট পুনরুদ্ধার করা হচ্ছে", certificateName);
            
            // বিকল্প ১: DefaultAzureCredential ব্যবহার করুন (প্রোডাকশনের জন্য প্রস্তাবিত)
            var credential = new DefaultAzureCredential();
            
            // বিকল্প ২: ClientSecretCredential ব্যবহার করুন (যদি আপনার কাছে ক্লায়েন্ট সিক্রেট থাকে)
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // সার্টিফিকেট মেটাডেটা পান
            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate = 
                await certificateClient.GetCertificateAsync(certificateName);
            
            _logger.LogDebug("সার্টিফিকেট পাওয়া গেছে। থাম্বপ্রিন্ট: {Thumbprint}, মেয়াদ শেষ: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);
            
            // প্রাইভেট কী সহ সিক্রেট পান
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
            
            // Base64 PKCS12 থেকে X509Certificate2-এ রূপান্তর করুন
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            
            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null, // Key Vault ডিক্রিপশন পরিচালনা করে
                X509KeyStorageFlags.MachineKeySet | 
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.PersistKeySet);
            
            _logger.LogInformation("প্রাইভেট কী সহ সার্টিফিকেট সফলভাবে লোড করা হয়েছে");
            
            return x509Certificate;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "'{CertName}' সার্টিফিকেট লোড করতে ক্রিপ্টোগ্রাফিক ত্রুটি", certificateName);
            return null;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Key Vault ত্রুটি: {StatusCode} - {Message}", 
                ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "সার্টিফিকেট পুনরুদ্ধারে অপ্রত্যাশিত ত্রুটি");
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
            _logger.LogError(ex, "'{SecretName}' সিক্রেট পুনরুদ্ধারে ত্রুটি", secretName);
            throw;
        }
    }
}
```

---

## ?? **প্রয়োজনীয় NuGet প্যাকেজসমূহ**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**নোট:** WebAppExperimental266 প্রজেক্টে ইতিমধ্যে ইনস্টল করা আছে।

---

## ?? **কনফিগারেশন**

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

### ইউজার সিক্রেটস

```powershell
# ক্লায়েন্ট সিক্রেট প্রমাণীকরণের জন্য
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# ম্যানেজড আইডেন্টিটির জন্য (প্রোডাকশন)
# কোনো সিক্রেট প্রয়োজন নেই - পরিচয় Azure দ্বারা পরিচালিত হয়
```

---

## ?? **Azure Key Vault অ্যাক্সেস নীতিসমূহ**

### প্রয়োজনীয় অনুমতিসমূহ

অ্যাপ্লিকেশন পরিচয়ের জন্য (সার্ভিস প্রিন্সিপাল বা ম্যানেজড আইডেন্টিটি):

**সার্টিফিকেট অনুমতি:**
- ? Get
- ? List

**সিক্রেট অনুমতি:**
- ? Get
- ? List

**কেন সার্টিফিকেট এবং সিক্রেট উভয় অনুমতি প্রয়োজন?**
- সার্টিফিকেট অনুমতি মেটাডেটা পায়
- সিক্রেট অনুমতি প্রাইভেট কী পায়

### Azure পোর্টালের মাধ্যমে সেটআপ

1. Key Vault → অ্যাক্সেস নীতিতে যান
2. "অ্যাক্সেস নীতি যোগ করুন" ক্লিক করুন
3. সার্টিফিকেট অনুমতি নির্বাচন করুন: Get, List
4. সিক্রেট অনুমতি নির্বাচন করুন: Get, List
5. প্রিন্সিপাল নির্বাচন করুন (আপনার অ্যাপ বা ম্যানেজড আইডেন্টিটি)
6. সংরক্ষণ করুন

### Azure CLI-এর মাধ্যমে সেটআপ

```bash
# আপনার অ্যাপ্লিকেশন বা ম্যানেজড আইডেন্টিটির Object ID পান
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# অনুমতি প্রদান করুন
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## ?? **বাস্তবায়ন পরীক্ষা করা**

### ইউনিট টেস্টের উদাহরণ

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

### ইন্টিগ্রেশন টেস্ট

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    // এর জন্য প্রকৃত Azure রিসোর্স প্রয়োজন
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
    Assert.True(cert.HasPrivateKey, "সার্টিফিকেটে প্রাইভেট কী থাকতে হবে");
}
```

---

## ?? **mTLS-এ ব্যবহার**

### সার্টিফিকেট প্রমাণীকরণের সাথে ইন্টিগ্রেশন

```csharp
// Program.cs-এ
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    // Key Vault থেকে সার্ভার সার্টিফিকেট আনুন
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();
    
    if (serverCertificate != null)
    {
        // Kestrel-কে সার্টিফিকেট ব্যবহার করতে কনফিগার করুন
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = serverCertificate;
            });
        });
        
        logger.LogInformation("Key Vault সার্টিফিকেট সহ mTLS সক্ষম করা হয়েছে");
    }
}
```

---

## ?? **তুলনা: সিক্রেট বনাম সার্টিফিকেট স্টোরেজ**

| বৈশিষ্ট্য | সিক্রেট হিসেবে সংরক্ষণ | সার্টিফিকেট হিসেবে সংরক্ষণ |
|---------|----------------|---------------------|
| **আকারের সীমা** | 25 KB | সীমাহীন |
| **প্রাইভেট কী** | ? ম্যানুয়াল পরিচালনা | ? স্বয়ংক্রিয় |
| **মেটাডেটা** | ? নেই | ? সম্পূর্ণ সার্টিফিকেট তথ্য |
| **রোটেশন** | ? ম্যানুয়াল | ? অন্তর্নির্মিত |
| **মেয়াদ শেষ** | ? ম্যানুয়াল ট্র্যাকিং | ? স্বয়ংক্রিয় ট্র্যাকড |
| **RBAC** | মৌলিক | সার্টিফিকেট-নির্দিষ্ট |
| **জটিলতা** | বেশি | কম |
| **সুপারিশ** | ? ব্যবহার করবেন না | ? **এটি ব্যবহার করুন** |

---

## ?? **সার্টিফিকেট রোটেশন**

### স্বয়ংক্রিয় রোটেশন

Key Vault সার্টিফিকেটগুলি স্বয়ংক্রিয় রোটেশন সমর্থন করে:

```powershell
# অটো-রোটেশন নীতি সেট আপ করুন
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

### অ্যাপ্লিকেশন কোড

আপনার অ্যাপ্লিকেশন স্বয়ংক্রিয়ভাবে সর্বশেষ ভার্সন পায়:

```csharp
// এটি সর্বদা বর্তমান ভার্সন পায়
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

একটি নির্দিষ্ট ভার্সন পেতে:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "specific-version-id");
```

---

## ?? **সমস্যা সমাধান**

### ত্রুটি: "Certificate not found"

**পরীক্ষা করুন:**
1. সার্টিফিকেটের নাম সঠিক
2. Key Vault-এ সার্টিফিকেট বিদ্যমান
3. অ্যাক্সেস নীতি কনফিগার করা আছে

```bash
# সার্টিফিকেটের তালিকা করুন
az keyvault certificate list --vault-name your-keyvault
```

### ত্রুটি: "Access denied"

**পরীক্ষা করুন:**
1. সার্ভিস প্রিন্সিপালের সঠিক অনুমতি আছে
2. সার্টিফিকেট এবং সিক্রেট উভয় অনুমতি দেওয়া আছে
3. ম্যানেজড আইডেন্টিটি সক্ষম করা আছে (যদি ব্যবহার করা হয়)

```bash
# অ্যাক্সেস নীতি পরীক্ষা করুন
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### ত্রুটি: "Certificate has no private key"

**পরীক্ষা করুন:**
1. `.GetCertificateAsync()` নয়, `.GetSecretAsync()` ব্যবহার করা হচ্ছে
2. প্রাইভেট কী সহ সার্টিফিকেট আমদানি করা হয়েছে
3. সঠিক সিক্রেট ভার্সন ব্যবহার করা হচ্ছে

```csharp
// ভুল - প্রাইভেট কী নেই
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // শুধুমাত্র পাবলিক কী

// সঠিক - প্রাইভেট কী আছে
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // প্রাইভেট কী আছে
```

### ত্রুটি: "CryptographicException"

**সাধারণ কারণসমূহ:**
1. PFX ডেটা নষ্ট
2. ভুল সার্টিফিকেট ফরম্যাট
3. ভুল পাসওয়ার্ড (KV-এর জন্য প্রয়োজন হওয়া উচিত নয়)

```csharp
try
{
    var cert = new X509Certificate2(pfxBytes);
}
catch (CryptographicException ex)
{
    _logger.LogError("PFX ডেটার দৈর্ঘ্য: {Length}, প্রথম 20 অক্ষর: {Preview}",
        pfxBytes.Length,
        Convert.ToBase64String(pfxBytes.Take(20).ToArray()));
    throw;
}
```

---

## ?? **মাইগ্রেশন চেকলিস্ট**

- [ ] প্রয়োজনীয় NuGet প্যাকেজ ইনস্টল করুন
- [ ] প্রোডাকশন কোড দিয়ে `AzureKeyVaultCertificateOperations.cs` আপডেট করুন
- [ ] `Import-AzKeyVaultCertificate` ব্যবহার করে Key Vault-এ সার্টিফিকেট আমদানি করুন
- [ ] অ্যাক্সেস নীতি কনফিগার করুন (সার্টিফিকেট: Get/List, সিক্রেট: Get/List)
- [ ] `appsettings.json`-এ কনফিগারেশন আপডেট করুন
- [ ] ম্যানেজড আইডেন্টিটি (প্রোডাকশন) বা ক্লায়েন্ট সিক্রেট (ডেভ) সেট আপ করুন
- [ ] সার্টিফিকেট পুনরুদ্ধার পরীক্ষা করুন
- [ ] প্রাইভেট কী আছে কিনা যাচাই করুন
- [ ] পুনরুদ্ধার করা সার্টিফিকেট দিয়ে mTLS পরীক্ষা করুন
- [ ] সার্টিফিকেট রোটেশন নীতি সেট আপ করুন
- [ ] সার্টিফিকেট ব্যবস্থাপনা পদ্ধতি নথিভুক্ত করুন

---

## ?? **সারসংক্ষেপ**

### ? **করুন:**
- PFX আপলোড করতে `Import-AzKeyVaultCertificate` ব্যবহার করুন
- পুনরুদ্ধার করতে `CertificateClient` + `SecretClient` ব্যবহার করুন
- প্রোডাকশনে ম্যানেজড আইডেন্টিটি ব্যবহার করুন
- সার্টিফিকেট এবং সিক্রেট উভয় অনুমতি দিন
- সার্টিফিকেটে প্রাইভেট কী আছে কিনা পরীক্ষা করুন

### ? **করবেন না:**
- PFX কে Base64 সিক্রেট হিসেবে সংরক্ষণ করবেন না
- সার্টিফিকেট ডেটা ম্যানুয়ালি পরিচালনা করার চেষ্টা করবেন না
- প্রোডাকশনে ক্লায়েন্ট সিক্রেট ব্যবহার করবেন না
- সিক্রেট অনুমতি দিতে ভুলবেন না
- সার্টিফিকেটের মেয়াদ শেষের তারিখ উপেক্ষা করবেন না

---

## ?? **রেফারেন্সসমূহ**

- [Azure Key Vault সার্টিফিকেট সংক্ষিপ্ত বিবরণ](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates প্যাকেজ](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [ম্যানেজড আইডেন্টিটি ডকুমেন্টেশন](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 ক্লাস](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**অবস্থা:** ? গাইড সম্পূর্ণ  
**সর্বশেষ আপডেট:** 2024-12-20  
**ভার্সন:** 1.0  
**প্রজেক্ট:** WebAppExperimental266
