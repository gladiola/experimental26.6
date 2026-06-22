# Azure Key Vault PFX 证书指南

## 日期：2024-12-20

## 概述

本指南根据生产实施的经验教训，记录在 Azure Key Vault 中存储和检索完整 PFX 证书（含私钥）的**正确方法**。

---

## ⚠️ **常见错误**

### ❌ **错误：将 PFX 存储为 Base64 机密**

```powershell
# 不要这样做——此方法不起作用！
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**失败原因：**
1. **大小限制**：Key Vault 机密有 25 KB 的限制，PFX 文件通常超过此限制
2. **编码问题**：Base64 编码可能引入换行符和数据损坏
3. **类型不匹配**：机密用于简单字符串，而非二进制证书数据
4. **无证书元数据**：丢失到期日期、主题信息等

---

## ✅ **正确：使用证书专用 API**

### **方法一：直接导入证书（推荐）**

这是**最佳方法**，也是代码库中当前可用的方法。

#### 上传证书（PowerShell）

```powershell
# 定义变量
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# 将密码转换为 SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# 将证书导入 Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**优势：**
- ✅ 处理任意大小的证书
- ✅ 保留所有证书元数据
- ✅ 自动创建含私钥的机密版本
- ✅ 支持证书轮换
- ✅ 与 Azure RBAC 和访问策略集成

#### 检索证书（C#）

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
        // 创建凭据
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // 初始化证书客户端
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        
        // 获取证书（获取公钥和元数据）
        KeyVaultCertificateWithPolicy certificate = 
            await certificateClient.GetCertificateAsync(certificateName);
        
        // 获取包含私钥的机密
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
        
        // 机密值是 Base64 编码的 PKCS12 (PFX)，包含私钥
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);
        
        // 创建含私钥的 X509Certificate2
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // 无需密码——Key Vault 处理解密
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

### **方法二：使用托管标识（生产环境）**

对于生产环境，请使用**托管标识**而非客户端机密。

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // 使用 DefaultAzureCredential——在 Azure 中自动使用托管标识
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

## 🏗️ **WebAppExperimental266 中的实现**

### 当前实现状态

**位置：** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**状态：** 🔧 模板实现——需要生产代码

**当前代码（模板）：**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // 模板实现——用户应根据其 Key Vault 设置实现此方法
    _logger.LogWarning("GetCertificateFromKeyVault called - implement this method for production use");
    
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### 推荐更新

将其替换为生产就绪的实现：

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
            
            // 选项一：使用 DefaultAzureCredential（生产环境推荐）
            var credential = new DefaultAzureCredential();
            
            // 选项二：使用 ClientSecretCredential（如果有客户端机密）
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            
            // 获取证书元数据
            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate = 
                await certificateClient.GetCertificateAsync(certificateName);
            
            _logger.LogDebug("Certificate found. Thumbprint: {Thumbprint}, Expires: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);
            
            // 获取包含私钥的机密
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);
            
            // 将 Base64 PKCS12 转换为 X509Certificate2
            byte[] pfxBytes = Convert.FromBase64String(secret.Value);
            
            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null, // Key Vault 处理解密
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

## 📦 **所需 NuGet 包**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**注意：** 已安装在 WebAppExperimental266 项目中。

---

## ⚙️ **配置**

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

### 用户密钥

```powershell
# 客户端机密身份验证
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# 托管标识（生产环境）
# 无需机密——标识由 Azure 处理
```

---

## 🔐 **Azure Key Vault 访问策略**

### 所需权限

对于应用程序标识（服务主体或托管标识）：

**证书权限：**
- ✅ 获取
- ✅ 列出

**机密权限：**
- ✅ 获取
- ✅ 列出

**为何同时需要证书和机密权限？**
- 证书权限用于获取元数据
- 机密权限用于获取私钥

### 通过 Azure 门户设置

1. 导航到 Key Vault → 访问策略
2. 点击"添加访问策略"
3. 选择证书权限：获取、列出
4. 选择机密权限：获取、列出
5. 选择主体（您的应用程序或托管标识）
6. 保存

### 通过 Azure CLI 设置

```bash
# 获取应用程序或托管标识的对象 ID
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# 授予权限
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 **测试实现**

### 单元测试示例

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    // 准备
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);
    
    // 执行
    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");
    
    // 断言
    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### 集成测试

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    // 此测试需要实际 Azure 资源
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

## 🔗 **在 mTLS 中的使用**

### 与证书身份验证集成

```csharp
// 在 Program.cs 中
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    // 从 Key Vault 获取服务器证书
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();
    
    if (serverCertificate != null)
    {
        // 配置 Kestrel 使用该证书
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

## 📊 **比较：机密存储与证书存储**

| 功能 | 存储为机密 | 存储为证书 |
|------|-----------|-----------|
| **大小限制** | 25 KB | 无限制 |
| **私钥** | ❌ 需手动处理 | ✅ 自动 |
| **元数据** | ❌ 无 | ✅ 完整证书信息 |
| **轮换** | ❌ 手动 | ✅ 内置 |
| **到期** | ❌ 手动跟踪 | ✅ 自动跟踪 |
| **RBAC** | 基本 | 证书专用 |
| **复杂性** | 高 | 低 |
| **推荐** | ❌ 不要使用 | ✅ **使用此方法** |

---

## 🔄 **证书轮换**

### 自动轮换

Key Vault 证书支持自动轮换：

```powershell
# 设置自动轮换策略
az keyvault certificate set-policy `
    --vault-name your-keyvault `
    --name server-cert `
    --policy @policy.json
```

policy.json：
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

### 应用程序代码

您的应用程序会自动获取最新版本：

```csharp
// 这始终获取当前版本
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

要获取特定版本：
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName, 
    version: "specific-version-id");
```

---

## 🔧 **故障排查**

### 错误："Certificate not found"（未找到证书）

**检查：**
1. 证书名称是否正确
2. 证书是否存在于 Key Vault 中
3. 访问策略是否已配置

```bash
# 列出证书
az keyvault certificate list --vault-name your-keyvault
```

### 错误："Access denied"（访问被拒绝）

**检查：**
1. 服务主体是否具有正确权限
2. 是否同时授予了证书和机密权限
3. 托管标识是否已启用（如果使用）

```bash
# 检查访问策略
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### 错误："Certificate has no private key"（证书无私钥）

**检查：**
1. 使用的是 `.GetSecretAsync()` 而不仅仅是 `.GetCertificateAsync()`
2. 证书导入时包含了私钥
3. 使用了正确的机密版本

```csharp
// 错误——无私钥
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // 只有公钥

// 正确——有私钥
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // 有私钥
```

### 错误："CryptographicException"（密码学异常）

**常见原因：**
1. PFX 数据损坏
2. 证书格式错误
3. 密码无效（KV 通常不需要密码）

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

## ✅ **迁移清单**

- [ ] 安装所需 NuGet 包
- [ ] 使用生产代码更新 `AzureKeyVaultCertificateOperations.cs`
- [ ] 使用 `Import-AzKeyVaultCertificate` 将证书导入 Key Vault
- [ ] 配置访问策略（证书：获取/列出，机密：获取/列出）
- [ ] 在 `appsettings.json` 中更新配置
- [ ] 设置托管标识（生产环境）或客户端机密（开发环境）
- [ ] 测试证书检索
- [ ] 验证私钥存在
- [ ] 使用检索到的证书测试 mTLS
- [ ] 设置证书轮换策略
- [ ] 记录证书管理程序

---

## 📋 **总结**

### ✅ **应该做：**
- 使用 `Import-AzKeyVaultCertificate` 上传 PFX
- 使用 `CertificateClient` + `SecretClient` 检索
- 在生产环境使用托管标识
- 同时授予证书和机密权限
- 测试证书是否含有私钥

### ❌ **不应该做：**
- 将 PFX 存储为 Base64 机密
- 尝试手动管理证书数据
- 在生产环境使用客户端机密
- 忘记授予机密权限
- 忽略证书到期日期

---

## 📚 **参考资料**

- [Azure Key Vault 证书概述](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates 包](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [托管标识文档](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 类](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**状态：** ✅ 指南完成
**最后更新：** 2024-12-20
**版本：** 1.0
**项目：** WebAppExperimental266
