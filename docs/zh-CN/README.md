# WebAppExperimental266

一个 ASP.NET Core 9 Razor Pages Web 应用程序，集成了 Azure AD 身份验证、双向 TLS（mTLS）、Azure Key Vault 证书管理、Azure Cosmos DB、Azure Blob Storage、**AWS Secrets Manager**、**Amazon DynamoDB**、**Google Cloud Secret Manager**、**Google Cloud Firestore**，以及基于 nonce 的内容安全策略所强化的 HTTP 安全层。

---

## 目录

- [功能](#功能)
- [功能标志](#功能标志)
- [前提条件](#前提条件)
- [安装 – Windows Azure（App Service）](#安装--windows-azureapp-service)
- [安装 – 与 Azure 服务通信的 OpenBSD 服务器](#安装--与-azure-服务通信的-openbsd-服务器)
- [配置参考](#配置参考)
- [辅助脚本](#辅助脚本)
- [安全说明](#安全说明)

---

## 功能

### Azure AD 身份验证（OpenID Connect）
应用程序通过 `Microsoft.Identity.Web` 使用 OpenID Connect 协议，经由 **Microsoft 标识平台** 对用户进行身份验证。`/Experimental` 下的所有路由都需要经过身份验证的 Azure AD 标识。`/Privacy`、`/Error` 和 `/About` 页面可公开访问。

### mTLS 客户端证书身份验证
启用时，客户端必须提供有效的 X.509 证书。`MtlsSettings` 中的设置控制是否允许链式证书、自签名证书或两者，证书吊销验证，以及允许的证书颁发机构。

### Azure Key Vault 集成
应用程序在启动时从 Azure Key Vault 检索 TLS **服务器证书**。加载的 `X509Certificate2` 直接注入 Kestrel 的 HTTPS 配置中，因此磁盘上无需存在 PFX 文件。

### 每个请求基于 Nonce 的内容安全策略
启用时，每个 HTTP 响应都携带一个 `Content-Security-Policy` 标头，其 `script-src` 指令包含每个请求的**加密随机 nonce**。CSP 还支持内联脚本的 SHA-256 哈希白名单。

### 标准 HTTP 安全标头
`UseStandardSecurityHeaders` 向每个响应添加：`X-Frame-Options`、`X-Content-Type-Options`、`Strict-Transport-Security`、`Referrer-Policy`、`Cross-Origin-Opener-Policy`、`Cross-Origin-Resource-Policy`、`Permissions-Policy`，以及移除 `Server`、`X-Powered-By` 和 `X-AspNetMvc-Version` 响应标头。

### Azure Blob Storage
启用时，`BlobSettingsService` 提供一个由连接字符串和可配置的最大附件数量支持的 Scoped 服务。

### Azure Cosmos DB
启用时，应用程序在启动时通过调用 `database.ReadAsync()` 验证 Cosmos DB 连接。

### AWS Secrets Manager
启用时，`AwsSecretsManagerOperationsService` 从 AWS Secrets Manager 检索密钥和证书。在 `AwsSecretsManager` 节配置：`Region`、`CertificateSecretName`、`IVSecretName`、`NonceKeySecretName`，以及 `AccessKeyId`/`SecretAccessKey` 凭据。

### Amazon DynamoDB
启用时，`AwsDynamoDbService` 在启动时验证 DynamoDB 表的连接性。在 `AwsDynamoDb` 节配置：`Region`、`TableName`，以及 `AccessKeyId`/`SecretAccessKey` 凭据。

### GCP Secret Manager
启用时，`GcpSecretManagerOperationsService` 从 Google Cloud Secret Manager 检索密钥。在 `GcpSecretManager` 节配置：`ProjectId`、`CertificateSecretId`、`IVSecretId`、`NonceKeySecretId`，以及 `CredentialFilePath`（可选，为空时使用 ADC）。

### GCP Firestore
启用时，`GcpFirestoreService` 在启动时构建 Firestore 客户端。在 `GcpFirestore` 节配置：`ProjectId`、`DatabaseId`（默认："(default)"）、`CollectionName`，以及 `CredentialFilePath`（可选）。

### AWS Cognito 身份管理
启用时，`AddAwsCognitoAuthentication` 针对 **Amazon Cognito 用户池** 配置 OpenID Connect 身份验证——相当于 AWS 版本的 Microsoft Entra ID / Azure AD。OIDC 发现端点：
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
在 `AwsCognito` 节配置：`Region`、`UserPoolId`、`AppClientId`、`AppClientSecret`（存储在用户密钥中）以及 `Domain`。

### GCP 标识平台
启用时，`AddGcpIdentityAuthentication` 使用 **Google OAuth 2.0 / OIDC** 配置 OpenID Connect 身份验证——相当于 GCP 版本的 Microsoft Entra ID / Azure AD。OIDC 发现端点：
`https://accounts.google.com/.well-known/openid-configuration`
在 `GcpIdentity` 节配置：`ClientId`、`ClientSecret`（存储在用户密钥中），以及可选的 `ProjectId`。

### 安全会话管理
会话使用进程内分布式内存缓存，**空闲超时时间为 30 分钟**。会话 Cookie 配置了 `HttpOnly`、`Secure = Always` 和 `SameSite = Strict`。

### 本地化
应用程序支持 **25 种语言**：en-US、de-DE、es-ES、fr-FR、pt-PT、it-IT、zh-HK、ko-KR、hi-IN、ru-RU、ar-SA、sw-KE、ja-JP、ht-HT、haw-US、sm-WS、mi-NZ、af-ZA、nl-NL、ha-NG、am-ET、yo-NG、bn-BD、zh-CN 和 ga-IE。阿拉伯语包含自动 RTL 布局切换。

### PII 安全日志记录
`LoggingHelper` 使用 HMAC-SHA256 对日志输出中的个人身份信息进行哈希处理。可通过 `Logging:PiiHmacKey` 提供稳定的 32 字节密钥。

---

## 功能标志

所有主要子系统通过 `appsettings.json` 中的布尔功能标志进行控制。

| 标志 | 默认值 | 说明 |
|---|---|---|
| `EnableSession` | `true` | 服务器端会话和会话 Cookie |
| `EnableLocalization` | `true` | 多语言支持（25 种语言） |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect 身份验证 |
| `EnableAuthorization` | `true` | 路由级别授权策略 |
| `EnableKeyVault` | `false` | 从 Azure Key Vault 加载 TLS 服务器证书 |
| `EnableNonceServices` | `false` | 每请求 CSP nonce 生成 |
| `EnableCSP` | `false` | 附加 `Content-Security-Policy` 标头 |
| `EnableSecurityHeaders` | `true` | 附加标准 HTTP 安全标头 |
| `EnableBlobStorage` | `false` | Azure Blob Storage 服务 |
| `EnableCosmosDb` | `false` | Azure Cosmos DB 服务 |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager（存根） |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect 身份管理 |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager（存根） |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP 标识平台（Google OAuth 2.0 / OIDC） |
| `EnableMtls` | `false` | 要求客户端 TLS 证书 |
| `EnableOcspValidation` | `false` | OCSP 证书吊销检查（存根） |

---

## 前提条件

1. **Azure AD 应用注册** – 包含重定向 URI、客户端密钥或证书凭据。
2. **Azure Key Vault** – 包含作为密钥的 PFX 服务器证书。
3. **Azure Cosmos DB 帐户**（可选）。
4. **Azure Blob Storage 帐户**（可选）。
5. **.NET 9 SDK / Runtime** – 版本 9.0 或更高。
6. **AWS 凭据**（具有 `secretsmanager` 和 `dynamodb` 权限的 IAM 用户或角色）– 启用 `EnableAwsSecretsManager` 或 `EnableAwsDynamoDb` 时需要。
7. **GCP 服务帐户或 ADC**（具有 `secretmanager` 和 `datastore` 权限）– 启用 `EnableGcpSecretManager` 或 `EnableGcpFirestore` 时需要。

---

## 安装 – Windows Azure（App Service）

### 1. 创建 Azure 资源

```powershell
az login
az group create --name MyResourceGroup --location eastus
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. 注册 Azure AD 应用程序

在 [Azure 门户](https://portal.azure.com)中：
1. 导航至 **Microsoft Entra ID → 应用注册 → 新建注册**。
2. 将重定向 URI 设置为 `https://<your-app>.azurewebsites.net/signin-oidc`。
3. 在**证书和密钥**下，创建客户端密钥并复制其值。
4. 在"概述"边栏选项卡中记录**租户 ID** 和**客户端 ID**。

### 3. 创建 Azure Key Vault 并上传服务器证书

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. 配置应用程序设置

将 `appsettings.template.json` 复制到 `appsettings.json` 并填写占位符值。密钥**不应**存储在源代码管理中 — 将其设置为 App Service 应用程序设置，或在本地通过用户密钥设置：

```powershell
az webapp config appsettings set --name MyWebApp26 --resource-group MyResourceGroup --settings \
  "AzureAd__TenantId=<TENANT_ID>" \
  "AzureAd__ClientId=<CLIENT_ID>" \
  "AzureAd__ClientSecret=<CLIENT_SECRET>" \
  "AzureKeyVault__KeyVaultURL=https://MyKeyVault26.vault.azure.net/" \
  "AzureKeyVault__KeyVaultSecret=<KV_SECRET>" \
  "AzureKeyVault__KeyVaultPassName=ServerCert" \
  "FeatureFlags__EnableKeyVault=true" \
  "FeatureFlags__EnableAzureAd=true"
```

### 5. 部署应用程序

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. 启用 HTTPS 和自定义域（推荐）

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. 在 Azure App Service 上启用 mTLS（可选）

1. 转到 **App Service → TLS/SSL 设置 → 客户端证书**。
2. 将**传入客户端证书**设置为**必需**。

然后在应用程序设置中设置 `FeatureFlags__EnableMtls=true`。

---

## 安装 – 与 Azure 服务通信的 OpenBSD 服务器

> **重要提示：** .NET 9 **没有**适用于 OpenBSD 的官方 Microsoft 版本。以下说明使用**与 Linux 兼容的容器**（通过 [Podman](https://podman.io/)）在 OpenBSD 上运行 ASP.NET Core 9 应用程序，同时通过 HTTPS 与 Azure 服务通信。

### 1. 在 OpenBSD 上安装前提条件

```sh
pkg_add podman
pkg_add curl git
```

### 2. 拉取 ASP.NET Core 9 Runtime 镜像

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. 构建应用程序（在 Linux 或 Windows 构建计算机上）

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. 创建配置文件

在 OpenBSD 主机上创建 `/etc/webappexp26/appsettings.json`：

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": {
    "EnableAzureAd": true,
    "EnableKeyVault": true,
    "EnableSecurityHeaders": true,
    "EnableMtls": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/",
    "KeyVaultPassName": "ServerCert"
  }
}
```

### 5. 启动容器

```sh
podman run -d --name webappexp26 -p 443:8443 \
  -v /etc/webappexp26:/app/config:ro -v /path/to/publish:/app:ro \
  -e ASPNETCORE_ENVIRONMENT=Production -e ASPNETCORE_URLS="https://+:8443" \
  -e AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
  -e AzureKeyVault__KeyVaultSecret="YOUR_KV_SECRET" \
  -e Logging__PiiHmacKey="YOUR_32_BYTE_BASE64_KEY" \
  mcr.microsoft.com/dotnet/aspnet:9.0 \
  dotnet /app/WebAppExperimental266.dll \
    --contentRoot /app \
    --configDir /app/config
```

### 6. 配置 OpenBSD 数据包过滤器 (pf) 防火墙

将以下内容添加到 `/etc/pf.conf`：

```
# 允许入站 HTTPS
pass in on egress proto tcp to port 443

# 允许出站到 Azure AD、Key Vault、Cosmos DB、Blob Storage
pass out on egress proto tcp to port { 443 }
```

重新加载规则集：

```sh
pfctl -f /etc/pf.conf
```

### 7. 出站 Azure 服务连接

| 服务 | 端点 |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

---

## 配置参考

将 `appsettings.template.json` 复制到 `appsettings.json` 并替换所有 `{{PLACEHOLDER}}` 值。

| 节 | 键 | 说明 |
|---|---|---|
| `AzureAd` | `TenantId`、`ClientId`、`ClientSecret` | Azure AD 应用注册 |
| `AzureKeyVault` | `KeyVaultURL`、`KeyVaultSecret`、`KeyVaultPassName` | Key Vault 和证书名称 |
| `MtlsSettings` | `RequireClientCertificate`、`AllowedIssuers` | mTLS 客户端证书策略 |
| `NonceEncryption` | `Key`、`IV` | nonce 加密的 32 字节密钥和 16 字节 IV（base64） |
| `BlobSettings` | `BlobConnectionString`、`MaxAttachments` | Blob Storage 连接 |
| `CosmosDb` | `CosmosConnectionString`、`DatabaseName`、`ContainerName` | Cosmos DB 连接 |
| `AwsSecretsManager` | `Region`、`CertificateSecretName`、`IVSecretName`、`NonceKeySecretName`、`AccessKeyId`、`SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`、`TableName`、`AccessKeyId`、`SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`、`CertificateSecretId`、`IVSecretId`、`NonceKeySecretId`、`CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`、`DatabaseId`、`CollectionName`、`CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`、`CacheDurationMinutes` | OCSP 验证（存根） |
| `Logging` | `PiiHmacKey` | 用于日志中 PII 哈希处理的 32 字节 base64 HMAC 密钥 |

使用附带的 PowerShell 脚本生成加密密钥和 IV：

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

将所有密钥存储在 **.NET 用户密钥**中以进行本地开发：

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
```

> 对于 GCP，将 `GOOGLE_APPLICATION_CREDENTIALS` 环境变量设置为服务帐户 JSON 密钥文件的路径，或在本地开发时运行 `gcloud auth application-default login`。

---

## 辅助脚本

`SupportingScripts/` 目录包含 PowerShell 工具：

| 脚本 | 用途 |
|---|---|
| `IVandKeySampleGenerator.ps1` | 生成随机 32 字节 AES 密钥和 16 字节 IV（base64） |
| `HashInlineScriptPowerShell.ps1` | 计算内联脚本的 SHA-256 哈希（用于 CSP 白名单） |
| `HashInlineScriptPowerShellBase64Output.ps1` | 与上相同，以 base64 格式输出哈希 |
| `CertificateUploaderToAzureExample.ps1` | 将 PFX 证书上传到 Azure Key Vault |
| `CheckRoles.ps1` | 验证应用程序的 Azure RBAC 角色分配 |
| `ExportResourceGroups.ps1` | 导出 Azure 资源组配置 |
| `TroubleshootingCosmosDBInfo.ps1` | 诊断 Cosmos DB 连接性 |
| `SetupFromTemplate.ps1` | 从 `appsettings.template.json` 自动化初始配置 |

---

## 安全说明

- **切勿将密钥提交**到源代码管理。本地使用 .NET 用户密钥，生产环境使用 Azure App 设置 / Key Vault 引用。
- OCSP 验证实现是一个**存根**，会拒绝所有证书。在生产环境中启用 `EnableOcspValidation` 之前，请替换 `OcspValidationService.cs` 中的 `PerformOcspValidationAsync`。
- Nonce 值**永远不会被记录** — 以明文记录 nonce 会使拥有日志访问权限的攻击者能够注入任意内联脚本。
- `Server` 响应标头被屏蔽为 `webserver`，以避免泄露平台信息。
- AWS `AccessKeyId` 和 `SecretAccessKey` **绝不应**出现在 `appsettings.json` 中 — 使用用户密钥、环境变量或 IAM 实例角色。
- GCP 凭据应使用**应用程序默认凭据 (ADC)**，而不是提交服务帐户 JSON 文件。
