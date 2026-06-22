# 快速参考卡 — Razor Pages 模板

## 🚀 快速开始（5 分钟）

```powershell
# 1. 运行设置脚本
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. 构建并运行
dotnet build
dotnet run
```

## 📁 配置文件

| 文件 | 用途 | 是否提交？ |
|------|------|-----------|
| `appsettings.template.json` | 带占位符的模板 | ✅ 是 |
| `appsettings.json` | 您的实际配置 | ❌ 否（git 忽略） |
| 用户密钥 | 敏感值 | ❌ 否（仅本地） |

## 🚩 功能标志（快速启用/禁用）

编辑 `appsettings.json` → `FeatureFlags` 节：

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // 🔐 开启以启用身份验证
  "EnableNonceServices": false,  // 🛡️ 开启以启用 CSP
  "EnableCosmosDb": false,       // 🗄️ 开启以启用数据库
  "EnableBlobStorage": false     // 📦 开启以启用文件存储
}
```

## 🔑 用户密钥命令

```powershell
# 初始化
dotnet user-secrets init

# 设置密钥
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# 列出所有密钥
dotnet user-secrets list

# 删除密钥
dotnet user-secrets remove "AzureAd:ClientSecret"

# 清除所有密钥
dotnet user-secrets clear
```

## 📋 各功能所需密钥

### Azure AD 身份验证
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# 先生成：.\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
dotnet user-secrets set "CosmosDb:AccountKey" "your-account-key"
```

### Blob Storage
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## 🛠️ 实用脚本

| 脚本 | 用途 | 用法 |
|------|------|------|
| `SetupFromTemplate.ps1` | 初始设置 | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | 更改命名空间 | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | 生成密钥 | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | 计算 CSP 哈希 | `.\HashInlineScriptPowerShell.ps1` |

## 📈 开发阶段

### 第一阶段：基础（5 分钟设置）
- ✅ 会话
- ✅ 本地化
- ✅ 安全标头
- ❌ 无身份验证
- ❌ 无数据库

**配置**：除 `EnableSession`、`EnableLocalization`、`EnableSecurityHeaders` 外，所有标志均为 `false`

### 第二阶段：+ 身份验证（30 分钟设置）
- ✅ 第一阶段功能
- ✅ Azure AD
- ✅ 授权
- ✅ CSP + Nonce
- ❌ 无数据库

**配置**：启用 `EnableAzureAd`、`EnableAuthorization`、`EnableNonceServices`、`EnableCSP`

**需要**：
- Azure AD 应用注册
- 生成的加密密钥

### 第三阶段：+ Azure 服务（1-2 小时设置）
- ✅ 第二阶段功能
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**配置**：启用 `EnableCosmosDb`、`EnableBlobStorage`、`EnableKeyVault`

**需要**：
- 已创建 Azure 资源
- 用户密钥中的连接字符串

## 🔧 快速故障排查

### 构建错误
```powershell
# 清理并重新构建
dotnet clean
dotnet build

# 检查缺失的包
dotnet restore
```

### "Configuration not found"（未找到配置）
```powershell
# 验证文件是否存在
Test-Path appsettings.json

# 如果缺失，从模板复制
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"（未找到密钥）
```powershell
# 列出密钥
dotnet user-secrets list

# 重新运行设置
.\SupportingScripts\SetupFromTemplate.ps1
```

### 身份验证循环 / 401 错误
1. 检查 Azure AD 重定向 URI 是否匹配
2. 验证 appsettings.json 中 `EnableAzureAd: true`
3. 检查用户密钥中的客户端机密
4. 清除浏览器 Cookie

### CSP 违规
1. 验证 `EnableNonceServices: true`
2. 检查加密密钥是否已设置
3. 查看浏览器控制台的 CSP 错误
4. 暂时禁用 CSP 进行测试：`EnableCSP: false`

## 📚 文档

- **完整文档**：`TEMPLATE_README.md`
- **配置**：`appsettings.template.json`
- **命名空间**：运行 `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ✅ 安全检查清单

部署到生产环境前：

- [ ] 所有密钥存储在 Azure Key Vault 或用户密钥中
- [ ] `appsettings.json` 已被 git 忽略
- [ ] `.gitignore` 包含模板特定的忽略项
- [ ] 已启用安全标头
- [ ] 已配置带 nonce 的 CSP
- [ ] 已强制 HTTPS
- [ ] 受保护页面已启用身份验证
- [ ] 已从默认值轮换密钥

## 💡 提示

- **从简单开始**：从第一阶段开始，逐步添加功能
- **使用 WhatIf**：应用前使用 `-WhatIf` 测试脚本
- **检查日志**：在 `Logging:LogLevel` 中启用 `"Default": "Debug"` 进行故障排查
- **验证密钥**：运行 `dotnet user-secrets list` 查看已配置的内容
- **清理构建**：如果出现奇怪错误，尝试 `dotnet clean && dotnet build`

## ❓ 帮助

1. 阅读 `TEMPLATE_README.md`
2. 检查 `appsettings.template.json` 注释
3. 运行 `dotnet user-secrets list`
4. 启用调试日志记录
5. 检查 Azure 门户中的资源状态

---

**模板版本**：1.0
**ASP.NET Core**：9.0
**最后更新**：2024-12-20
