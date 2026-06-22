# دليل شهادة PFX في Azure Key Vault

## التاريخ: 2024-12-20

## نظرة عامة

يوثّق هذا الدليل **النهج الصحيح** لتخزين شهادات PFX الكاملة (مع المفاتيح الخاصة) واسترداديها في Azure Key Vault، بناءً على دروس مستخلصة من التنفيذ الإنتاجي.

---

## ⚠️ **الأخطاء الشائعة التي يجب تجنبها**

### ❌ **خطأ: تخزين PFX كسر Base64**

```powershell
# لا تفعل هذا - لن يعمل!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**لماذا يفشل:**
1. **حد الحجم**: سرائر Key Vault محدودة بـ 25 كيلوبايت — ملفات PFX كثيراً ما تتجاوز هذا الحد
2. **مشاكل الترميز**: قد يُدخل ترميز Base64 فواصل أسطر وتلفاً في البيانات
3. **عدم تطابق النوع**: السرائر للسلاسل البسيطة، وليس لبيانات الشهادات الثنائية
4. **لا بيانات وصفية للشهادة**: يُفقد تواريخ الانتهاء ومعلومات الموضوع وما إلى ذلك

---

## ✅ **الصحيح: استخدام واجهات برمجة التطبيقات الخاصة بالشهادات**

### **الطريقة 1: استيراد الشهادة مباشرةً (موصى به)**

هذا **أفضل نهج** وما يعمل حالياً في قاعدة الكود.

#### تحميل الشهادة (PowerShell)

```powershell
# تعريف المتغيرات
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# تحويل كلمة المرور إلى SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# استيراد الشهادة إلى Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**الفوائد:**
- ✅ يتعامل مع شهادات بأي حجم
- ✅ يحتفظ بجميع بيانات الشهادة الوصفية
- ✅ ينشئ تلقائياً نسخة سر مع المفتاح الخاص
- ✅ يدعم تدوير الشهادات
- ✅ يتكامل مع Azure RBAC وسياسات الوصول

#### استرداد الشهادة (C#)

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
        // إنشاء بيانات الاعتماد
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // تهيئة عميل الشهادات
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        
        // الحصول على الشهادة (يجلب المفتاح العام والبيانات الوصفية)
        KeyVaultCertificateWithPolicy certificate = 
            await certificateClient.GetCertificateAsync(certificateName);
        
        // الحصول على السر الذي يحتوي على المفتاح الخاص
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        // قيمة السر هي PKCS12 (PFX) المُرمَّزة بـ Base64 مع المفتاح الخاص
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        // إنشاء X509Certificate2 مع المفتاح الخاص
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // لا حاجة لكلمة مرور — Key Vault يتولى فك التشفير
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

### **الطريقة 2: استخدام Managed Identity (بيئة الإنتاج)**

في بيئات الإنتاج، استخدم **Managed Identity** بدلاً من أسرار العميل.

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // استخدام DefaultAzureCredential — يستخدم Managed Identity تلقائياً في Azure
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

## 🔧 **التنفيذ في WebAppExperimental266**

### حالة التنفيذ الحالية

**الموقع:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**الحالة:** 📋 تنفيذ قالب — يحتاج كود إنتاجي

**الكود الحالي (قالب):**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // تنفيذ قالب — يجب على المستخدمين التنفيذ بناءً على إعداد Key Vault الخاص بهم
    _logger.LogWarning("GetCertificateFromKeyVault called - implement this method for production use");
    
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### التحديث الموصى به

استبدل بالتنفيذ الجاهز للإنتاج:

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
            
            // الخيار 1: استخدام DefaultAzureCredential (موصى به للإنتاج)
            var credential = new DefaultAzureCredential();
            
            // الخيار 2: استخدام ClientSecretCredential (إذا كان لديك سر عميل)
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // الحصول على بيانات الشهادة الوصفية
            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate = 
                await certificateClient.GetCertificateAsync(certificateName);
            
            _logger.LogDebug("Certificate found. Thumbprint: {Thumbprint}, Expires: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);
            
            // الحصول على السر الذي يحتوي على المفتاح الخاص
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
            
            // تحويل PKCS12 المُرمَّز بـ Base64 إلى X509Certificate2
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            
            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null, // Key Vault يتولى فك التشفير
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

## 📦 **حزم NuGet المطلوبة**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**ملاحظة:** مُثبَّتة بالفعل في مشروع WebAppExperimental266.

---

## ⚙️ **التكوين**

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

### أسرار المستخدم

```powershell
# للمصادقة بسر العميل
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# لـ Managed Identity (الإنتاج)
# لا حاجة لأسرار — الهوية تتولاها Azure
```

---

## 🔑 **سياسات وصول Azure Key Vault**

### الأذونات المطلوبة

لهوية التطبيق (Service Principal أو Managed Identity):

**أذونات الشهادات:**
- ✅ Get
- ✅ List

**أذونات الأسرار:**
- ✅ Get
- ✅ List

**لماذا نحتاج أذونات الشهادات والأسرار معاً؟**
- أذونات الشهادات تجلب البيانات الوصفية
- أذونات الأسرار تجلب المفتاح الخاص

### الإعداد عبر Azure Portal

1. انتقل إلى Key Vault ← سياسات الوصول
2. انقر "إضافة سياسة وصول"
3. حدد أذونات الشهادات: Get وList
4. حدد أذونات الأسرار: Get وList
5. حدد المدير (تطبيقك أو managed identity)
6. احفظ

### الإعداد عبر Azure CLI

```bash
# الحصول على Object ID لتطبيقك أو managed identity
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# منح الأذونات
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 **اختبار التنفيذ**

### مثال اختبار وحدوي

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

### اختبار تكاملي

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    // يتطلب موارد Azure فعلية
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

## 🔒 **الاستخدام في mTLS**

### التكامل مع مصادقة الشهادات

```csharp
// في Program.cs
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    // جلب شهادة الخادم من Key Vault
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();
    
    if (serverCertificate != null)
    {
        // تكوين Kestrel لاستخدام الشهادة
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

## 📊 **المقارنة: تخزين كسر مقابل شهادة**

| الميزة | التخزين كسر | التخزين كشهادة |
|---------|----------------|---------------------|
| **حد الحجم** | 25 كيلوبايت | غير محدود |
| **المفتاح الخاص** | ❌ معالجة يدوية | ✅ تلقائي |
| **البيانات الوصفية** | ❌ لا شيء | ✅ معلومات كاملة |
| **التدوير** | ❌ يدوي | ✅ مدمج |
| **انتهاء الصلاحية** | ❌ تتبع يدوي | ✅ تتبع تلقائي |
| **RBAC** | أساسي | خاص بالشهادات |
| **التعقيد** | عالٍ | منخفض |
| **التوصية** | ❌ لا تستخدم | ✅ **استخدم هذا** |

---

## 🔄 **تدوير الشهادات**

### التدوير التلقائي

تدعم شهادات Key Vault التدوير التلقائي:

```powershell
# إعداد سياسة التدوير التلقائي
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

### كود التطبيق

يحصل تطبيقك تلقائياً على أحدث نسخة:

```csharp
// يجلب دائماً النسخة الحالية
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

للحصول على نسخة محددة:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "specific-version-id");
```

---

## 🔍 **استكشاف الأخطاء وإصلاحها**

### الخطأ: "Certificate not found"

**التحقق:**
1. اسم الشهادة صحيح
2. الشهادة موجودة في Key Vault
3. سياسات الوصول مُكوَّنة

```bash
# سرد الشهادات
az keyvault certificate list --vault-name your-keyvault
```

### الخطأ: "Access denied"

**التحقق:**
1. Service Principal يملك الأذونات الصحيحة
2. منح أذونات الشهادات والأسرار معاً
3. Managed Identity مُفعَّل (إذا كان مستخدماً)

```bash
# التحقق من سياسات الوصول
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### الخطأ: "Certificate has no private key"

**التحقق:**
1. استخدام `.GetSecretAsync()` وليس فقط `.GetCertificateAsync()`
2. استيراد الشهادة مع المفتاح الخاص
3. استخدام نسخة السر الصحيحة

```csharp
// خطأ — بدون مفتاح خاص
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // المفتاح العام فقط

// صحيح — مع مفتاح خاص
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // مع المفتاح الخاص
```

### الخطأ: "CryptographicException"

**الأسباب الشائعة:**
1. بيانات PFX تالفة
2. تنسيق شهادة خاطئ
3. كلمة مرور خاطئة (لا ينبغي الحاجة لها مع KV)

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

## ✅ **قائمة التحقق للترحيل**

- [ ] تثبيت حزم NuGet المطلوبة
- [ ] تحديث `AzureKeyVaultCertificateOperations.cs` بكود الإنتاج
- [ ] استيراد الشهادة إلى Key Vault باستخدام `Import-AzKeyVaultCertificate`
- [ ] تكوين سياسات الوصول (الشهادات: Get/List، الأسرار: Get/List)
- [ ] تحديث التكوين في `appsettings.json`
- [ ] إعداد Managed Identity (الإنتاج) أو سر العميل (التطوير)
- [ ] اختبار استرداد الشهادة
- [ ] التحقق من وجود المفتاح الخاص
- [ ] اختبار mTLS مع الشهادة المسترداة
- [ ] إعداد سياسة تدوير الشهادات
- [ ] توثيق إجراءات إدارة الشهادات

---

## 📝 **ملخص**

### ✅ **افعل:**
- استخدم `Import-AzKeyVaultCertificate` لتحميل PFX
- استخدم `CertificateClient` + `SecretClient` للاسترداد
- استخدم Managed Identity في الإنتاج
- امنح أذونات الشهادات والأسرار معاً
- اختبر أن الشهادة تمتلك مفتاحاً خاصاً

### ❌ **لا تفعل:**
- تخزين PFX كسر Base64
- محاولة إدارة بيانات الشهادة يدوياً
- استخدام أسرار العميل في الإنتاج
- نسيان منح أذونات الأسرار
- تجاهل تواريخ انتهاء صلاحية الشهادات

---

## 📚 **المراجع**

- [نظرة عامة على شهادات Azure Key Vault](https://docs.microsoft.com/azure/key-vault/certificates/)
- [حزمة Azure.Security.KeyVault.Certificates](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [توثيق Managed Identity](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [فئة X509Certificate2](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**الحالة:** ✅ الدليل مكتمل  
**آخر تحديث:** 2024-12-20  
**الإصدار:** 1.0  
**المشروع:** WebAppExperimental266
