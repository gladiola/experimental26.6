# mTLS（双向 TLS）客户端证书身份验证指南

## 概述

本项目现支持**双向 TLS（mTLS）**身份验证，要求服务器和客户端均提供有效证书。这通过双向身份验证提供了增强的安全性。

## 什么是 mTLS？

mTLS 通过要求以下内容扩展了标准 TLS：
1. **服务器证书**：服务器提供证书以证明其身份（标准 HTTPS）
2. **客户端证书**：客户端同样提供证书以证明其身份（mTLS 新增）

## 配置

### 1. 功能标志

在 `appsettings.json` 中启用 mTLS：

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. mTLS 设置

在 `appsettings.json` 中配置 mTLS 行为：

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

#### 配置选项

| 设置 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `RequireClientCertificate` | bool | `true` | 若为 true，则客户端证书为必填 |
| `AllowCertificateChains` | bool | `true` | 允许链式（CA 签发的）证书 |
| `AllowSelfSignedCertificates` | bool | `false` | 允许自签名证书（仅用于开发） |
| `CheckCertificateRevocation` | bool | `false` | 执行在线吊销检查 |
| `ClientCertificateName` | string | null | Azure Key Vault 中的证书名称 |
| `ValidateClientCertificateIssuer` | bool | `true` | 验证证书颁发者 |

### 3. 服务器证书（Azure Key Vault）

服务器证书从 Azure Key Vault 检索：

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## 安装说明

### 前提条件

1. 具有适当权限的 Azure Key Vault
2. 存储在 Azure Key Vault 中的服务器证书（PFX 格式的机密）
3. 客户端证书（可自行生成或从 CA 获取）

### 第一步：将服务器证书上传到 Key Vault

```bash
# 如有需要，将证书转换为 PFX
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# 使用 Azure CLI 上传到 Key Vault
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# 将密码存储为单独的机密
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### 第二步：生成客户端证书

#### 选项 A：自签名证书（仅用于开发）

```powershell
# 生成客户端证书
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# 导出为 PFX
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### 选项 B：CA 签发的证书（生产环境）

与您的证书颁发机构合作以获取客户端证书。

### 第三步：配置应用程序

更新 `appsettings.json`：

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

### 第四步：使用客户端证书进行测试

#### 使用 cURL：

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### 使用 PowerShell：

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### 使用浏览器：

1. 将客户端证书导入浏览器证书存储
2. 导航到您的应用程序
3. 浏览器将提示选择客户端证书

## 特定环境行为

### 开发环境
- 从 Key Vault 加载服务器证书（如果可用）
- 客户端证书为**可选**（`AllowCertificate` 模式）
- 可允许自签名证书

### 生产环境
- 从 Key Vault 加载服务器证书
- 如果 `EnableMtls = true`，则客户端证书为**必填**
- 仅推荐使用链式证书

## 安全最佳实践

### ✅ 应该做：
- 在生产环境使用 CA 签发的证书
- 将证书存储在 Azure Key Vault 中
- 在生产环境启用证书吊销检查
- 验证证书颁发者
- 为 PFX 文件使用强密码
- 定期轮换证书

### ❌ 不应该做：
- 在生产环境使用自签名证书
- 将证书提交到源代码控制
- 在用户间共享客户端证书
- 在生产环境禁用证书验证

## 故障排查

### 错误："No client certificate provided"（未提供客户端证书）

**原因**：客户端未发送证书
**解决方案**：
- 验证客户端证书是否已安装
- 检查 `RequireClientCertificate` 设置
- 确保证书受系统信任

### 错误："Certificate chain validation failed"（证书链验证失败）

**原因**：证书不受信任
**解决方案**：
- 安装 CA 根证书
- 设置 `AllowSelfSignedCertificates = true` 进行测试
- 验证证书是否已过期

### 错误："Server certificate not retrieved from Key Vault"（未能从 Key Vault 检索服务器证书）

**原因**：Azure Key Vault 访问问题
**解决方案**：
- 验证 Key Vault 权限
- 检查 Azure AD 凭据
- 确保托管标识已配置

## 日志记录

mTLS 身份验证事件会被记录：

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## 与现有身份验证的集成

mTLS 与 Azure AD 身份验证协同工作：

1. **客户端证书验证**先进行（传输层）
2. **Azure AD 身份验证**随后进行（应用层）

两者可同时启用以实现纵深防御安全。

## 参考资料

- [Microsoft 文档：证书身份验证](https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/certauth)
- [Azure Key Vault 集成](https://learn.microsoft.com/zh-cn/azure/app-service/configure-ssl-certificate-in-code)

## 示例代码

实现可在以下位置找到：
- `Models/Settings/MtlsSettings.cs` — 配置模型
- `Models/Settings/FeatureFlags.cs` — 功能标志
- `Extensions/ServiceCollectionExtensions.cs` — 服务注册
- `Program.cs` — 应用程序启动

## 附加资源

有关证书上传示例，请参阅 `SupportingScripts/CertificateUploaderToAzureExample.ps1`。
