# OCSP（在线证书状态协议）实现指南

## 概述

本项目包含对 OCSP 证书验证的**模板支持**。OCSP 允许在处理 Web 请求之前实时检查证书吊销状态。

## 什么是 OCSP？

OCSP 提供了证书吊销列表（CRL）的替代方案，用于检查证书是否已被吊销：

- **实时验证**：立即检查证书状态
- **高效**：仅查询特定证书的状态
- **轻量级**：响应比完整 CRL 下载更小
- **最新信息**：始终包含当前的吊销信息

## 配置

### 1. 功能标志

在 `appsettings.json` 中启用 OCSP 验证：

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. OCSP 设置

在 `appsettings.json` 中配置 OCSP 行为：

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.yourcompany.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

### 配置选项

| 设置 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableOcspValidation` | bool | `false` | 启用/禁用 OCSP 验证 |
| `OcspServerUrl` | string | `null` | OCSP 响应服务器的 URL |
| `RequestTimeoutSeconds` | int | `30` | OCSP 请求超时时间 |
| `MaxRetryAttempts` | int | `3` | 失败时的重试次数 |
| `CacheDurationMinutes` | int | `60` | OCSP 响应的缓存时长 |
| `ServerUnavailableBehavior` | string | `"Warn"` | 服务器不可用时的行为：`"Fail"`、`"Allow"` 或 `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | 启用详细日志记录 |
| `SkipValidationInDevelopment` | bool | `true` | 在开发模式下跳过 OCSP |

---

## 模板实现

当前实现是一个**模板**，演示了结构和 API 设计。要在生产中使用 OCSP，必须：

### 1. 实现 OCSP 协议

将 `OcspValidationService.cs` 中的模板 `PerformOcspValidationAsync` 方法替换为实际的 OCSP 协议实现：

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO：实现实际 OCSP 协议
    // 1. 构建 OCSP 请求
    // 2. 发送到 OCSP 服务器
    // 3. 解析 OCSP 响应
    // 4. 验证响应签名
    // 5. 返回证书状态
}
```

### 2. 搭建 OCSP 服务器

您需要一个单独的 OCSP 响应服务器，该服务器：
- 接受 OCSP 请求（RFC 6960 格式）
- 根据 CA 数据库检查证书状态
- 返回已签名的 OCSP 响应

**选项：**
- 使用商业 OCSP 服务（如 DigiCert、Let's Encrypt）
- 使用以下库构建自定义 OCSP 响应服务器：
  - **OpenSSL** — 支持 OCSP 的 C/C++ 库
  - **BouncyCastle** — 支持 OCSP 的 .NET 库
  - **Python** — 支持 OCSP 的 `cryptography` 库

---

## 使用示例

### 基本证书验证

```csharp
public class MyCertificateHandler
{
    private readonly IOcspValidationService _ocspService;

    public MyCertificateHandler(IOcspValidationService ocspService)
    {
        _ocspService = ocspService;
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
    {
        // 简单布尔检查
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### 详细状态验证

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    // 检查状态
    switch (result.Status)
    {
        case OcspStatus.Good:
            logger.LogInformation("Certificate is valid");
            return result;

        case OcspStatus.Revoked:
            logger.LogError("Certificate has been revoked!");
            throw new SecurityException("Certificate revoked");

        case OcspStatus.Unknown:
            logger.LogWarning("Certificate status unknown");
            // 根据策略处理
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP server unavailable");
            // 根据 ServerUnavailableBehavior 设置的后备行为
            break;
    }

    return result;
}
```

---

## 与 mTLS 集成

OCSP 与 mTLS 证书身份验证无缝协作：

```csharp
// 在 ServiceCollectionExtensions.cs 中
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// 在证书验证事件中
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        // 执行 OCSP 验证
        var ocspService = context.HttpContext.RequestServices
            .GetRequiredService<IOcspValidationService>();

        var isValid = await ocspService.ValidateCertificateAsync(
            context.ClientCertificate);

        if (!isValid)
        {
            context.Fail("Certificate validation failed via OCSP");
        }
    }
};
```

---

## 服务器不可用行为

### "Fail" — 严格安全

```json
"ServerUnavailableBehavior": "Fail"
```

- OCSP 服务器不可用时拒绝请求
- 最安全的选项
- 可能导致可用性问题

**使用场景：** 需要最高安全性，证书验证至关重要

### "Allow" — 高可用性

```json
"ServerUnavailableBehavior": "Allow"
```

- OCSP 服务器不可用时允许请求
- 优先考虑可用性而非安全性
- 记录警告

**使用场景：** 服务可用性比实时验证更重要

### "Warn" — 平衡（默认）

```json
"ServerUnavailableBehavior": "Warn"
```

- 允许请求但记录警告
- 均衡方法
- 支持监控/告警

**使用场景：** 希望监控 OCSP 问题而不阻塞流量

---

## 缓存

OCSP 响应被缓存以减少服务器负载：

```json
"CacheDurationMinutes": 60
```

**优势：**
- 减少 OCSP 服务器查询
- 提高性能
- 在短暂中断期间提供弹性

**缓存失效：**
- 缓存持续时间到期后自动失效
- 手动清除：重启应用程序

---

## 安全注意事项

### ✅ 应该做：

- 为 OCSP 服务器 URL 使用 HTTPS
- 验证 OCSP 响应签名
- 设置适当的缓存时间（平衡新鲜度与性能）
- 在高安全性环境使用 "Fail" 行为
- 监控 OCSP 服务器可用性
- 为瞬时故障实现重试逻辑
- 记录所有 OCSP 验证失败

### ❌ 不应该做：

- 在生产环境使用 HTTP 访问 OCSP
- 跳过 OCSP 响应签名验证
- 缓存时间过长（> 24 小时）
- 静默忽略 OCSP 服务器故障
- 在没有充分理由的情况下在生产环境禁用 OCSP

---

## 实现 OCSP 服务器

### 选项一：OpenSSL OCSP 响应服务器

```bash
# 启动 OpenSSL OCSP 响应服务器
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### 选项二：BouncyCastle（.NET）

```csharp
// 使用 BouncyCastle 库的示例
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. 解析请求
        // 2. 在数据库中检查证书状态
        // 3. 构建响应
        // 4. 签名响应
        // 5. 返回已签名的响应
    }
}
```

### 选项三：商业 OCSP 服务

- **DigiCert**：托管 OCSP 服务
- **Let's Encrypt**：为其证书提供免费 OCSP
- **GlobalSign**：企业级 OCSP 解决方案

---

## 监控与日志记录

### 启用详细日志记录

```json
{
  "OcspSettings": {
    "EnableDetailedLogging": true
  },
  "Logging": {
    "LogLevel": {
      "WebAppExperimental266.Services.OcspValidationService": "Debug"
    }
  }
}
```

### 日志消息

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## 测试

### 单元测试

运行 OCSP 测试：

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### 手动测试

1. **禁用 OCSP** — 验证应用程序在没有 OCSP 的情况下正常工作
2. **无效 URL** — 测试 ServerUnavailableBehavior 设置
3. **有效证书** — 应返回 `OcspStatus.Good`
4. **缓存响应** — 验证缓存正常工作

---

## 性能考虑

### 缓存配置

```json
"CacheDurationMinutes": 60  // 1 小时缓存
```

**权衡：**
- **短时间（5-15 分钟）**：数据更新，OCSP 负载更高
- **长时间（60-120 分钟）**：性能更好，数据可能过期

### 超时设置

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**建议：**
- 超时：生产环境 10-30 秒
- 重试：2-3 次用于处理瞬时故障

---

## 故障排查

### 问题：OCSP 服务器始终不可用

**解决方案：**
1. 检查 `OcspServerUrl` 是否正确
2. 验证防火墙是否允许出站 HTTPS
3. 检查 OCSP 服务器是否正在运行
4. 查看超时错误的日志

### 问题：所有证书验证失败

**解决方案：**
1. 验证 OCSP 服务器是否有证书状态数据
2. 检查证书链是否完整
3. 确保 OCSP 响应签名有效
4. 查看 OCSP 服务器日志

### 问题：缓存不工作

**解决方案：**
1. 验证 `CacheDurationMinutes > 0`
2. 检查使用的是否是相同的证书指纹
3. 重启应用程序以清除缓存

---

## 后续步骤

要使 OCSP 完全可用：

1. ✅ **配置完成** — 设置已就绪
2. ✅ **服务接口完成** — API 已定义
3. ✅ **测试完成** — 包含 30+ 个单元测试
4. 🔧 **OCSP 协议** — 需要实现 RFC 6960
5. 🔧 **OCSP 服务器** — 需要部署 OCSP 响应服务器
6. 🔧 **集成** — 与 mTLS 身份验证连接

---

## 参考资料

- [RFC 6960](https://tools.ietf.org/html/rfc6960) — OCSP 规范
- [BouncyCastle 文档](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft 证书身份验证](https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/certauth)

---

**状态：** ✅ 模板就绪
**OCSP 协议：** 🔧 待实现
**OCSP 服务器：** 🔧 待部署
**测试：** ✅ 包含 30+ 个测试
