# 安全评审 — WebAppExperimental266

**日期：** 2026-05-05
**范围：** 代码库完整静态分析

---

## 汇总表

| # | 领域 | 严重性 |
|---|------|--------|
| 1 | nonce 生成中的 AES-GCM IV 重用 | 🔴 严重 ✅ |
| 2 | nonce 以明文写入日志 | 🔴 严重 ✅ |
| 3 | 硬编码的后备 nonce 字符串 | 🔴 严重 ✅ |
| 4 | 全局 nonce 字典非线程安全 | 🟠 高 |
| 5 | mTLS 颁发者验证被注释掉 | 🟠 高 |
| 6 | mTLS 吊销检查默认关闭 | 🟠 高 |
| 7 | OCSP 始终返回有效（存根） | 🟠 高 |
| 8 | 身份验证/授权在配置中默认关闭 | 🟠 高 |
| 9 | 安全标头在管道中添加过晚 | 🟠 高 |
| 10 | 会话 Cookie 缺少 Secure + SameSite 属性 | 🟡 中 |
| 11 | 格式错误的全局 Set-Cookie 标头 | 🟡 中 |
| 12 | 所有响应的 Content-Type 强制为 text/html | 🟡 中 |
| 13 | AllowedHosts 设置为通配符 | 🟡 中 |
| 14 | 布局中 `<script>` 标签未应用 nonce | 🟡 中 |
| 15 | 缺少 Referrer-Policy 标头 | 🟡 中 |
| 16 | PII 以明文写入日志 | 🔵 低 |
| 17 | 日志中部分连接字符串 | 🔵 低 |
| 18 | Key Vault 操作为存根 | 🔵 低 |
| 19 | 已弃用的 X-XSS-Protection 标头 | 🔵 低 |

---

## 🔴 严重

### 1. AES-GCM IV 重用 — nonce 生成存在密码学缺陷 ✅ 已在提交 45ae31b 中修复

**文件：** `Models/Main_Objects/Nonce.cs`、`Services/NonceRefresherService.cs`

生成 CSP nonce 的 AES-GCM 加密对每次调用均使用**从 Key Vault 获取的固定 IV**。当 IV 与同一密钥重复使用时，AES-GCM 将遭到破坏：攻击者观察到两段密文后可通过异或运算恢复明文的异或值，且认证标签可被伪造。

修复方案十分直接——CSP nonce 根本无需加密。CSP nonce 只需满足**不可预测且每次请求唯一**两个属性；调用 `RandomNumberGenerator.GetBytes(16)` 并转换为 Base64 即已足够且正确。

---

### 2. nonce 值以明文写入日志 ✅ 已在提交 bb6f27a 中修复

**文件：** `Services/NonceMiddleware.cs`（第 31 行）、`Services/NonceRefresherService.cs`（第 82 行）

生成的 CSP nonce 被逐字记录到应用程序日志中：

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

任何能访问日志的人都可获得有效 nonce，从而轻易绕过 CSP 注入内联脚本。

---

### 3. 硬编码的后备 nonce ✅ 已在提交 11cc9f7 中修复

**文件：** `Services/OptimizedNonceMiddleware.cs`（第 53、78、92 行）

当 nonce 生成失败或 nonce 目录为空时，中间件会回退到字符串字面量 `"bootstrap-nonce-placeholder"`、`"fallback-nonce"` 和 `"error-fallback-nonce"`。这些字符串已提交到源代码中，攻击者知晓后可利用错误条件（如 Key Vault 不可用）将可预测的、可被利用的 nonce 注入 CSP 标头。

---

## 🟠 高

### 4. NonceCatalogService 使用非线程安全的静态字典 ✅ 已在提交 ae2b6c9 中修复

**文件：** `Services/NonceCatalogService.cs`（第 20 行）

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` 对并发读写不安全。在高负载下，两个竞争更新同一 nonce 键的请求可能导致数据损坏或抛出异常。nonce 目录作为单例（实际上是全局变量），意味着一个请求的 nonce 可能在飞行中被另一个请求覆盖——造成请求间的 nonce 冲突。应使用 `ConcurrentDictionary`，并在 `HttpContext.Items` 中按请求存储 nonce，而非存储在全局共享变量中。

---

### 5. mTLS 证书颁发者验证被注释掉 ✅ 已在提交 fd3d4fb 中修复

**文件：** `Extensions/ServiceCollectionExtensions.cs`（第 305–313 行）

`ValidateClientCertificateIssuer` 设置存在且默认为 `true`，但实际验证代码被注释掉了：

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

启用 mTLS 后，任何来自任意颁发者（且能链接到受信任根）的客户端证书均可通过身份验证——没有租户/颁发者限制。

---

### 6. mTLS 证书吊销检查默认禁用 ✅ 已在提交 fd3d7b3 中修复

**文件：** `Models/Settings/MtlsSettings.cs`（第 26 行）、`appsettings.template.json`

`CheckCertificateRevocation` 在模型和模板中均默认为 `false`。已吊销的客户端证书可无限期用于身份验证。对于生产 mTLS，吊销检查应默认启用。

---

### 7. OCSP 验证为始终返回有效的存根 ✅ 已在提交 b4c3807 中修复

**文件：** `Services/OcspValidationService.cs`（第 149–163 行）

`PerformOcspValidationAsync` 方法明确是"模板实现"，在 `Task.Delay(100)` 后始终返回 `IsValid = true`。如果在配置中启用了 OCSP 验证，它将悄无声息地通过所有证书（包括已吊销的）的验证，并仅记录一条容易被忽视的警告。

---

### 8. 身份验证和授权默认禁用 ✅ 已在提交 b392c47 中修复

**文件：** `appsettings.json`（第 16–17 行）

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

默认配置未启用身份验证或授权。未仔细阅读文档便将 `appsettings.template.json`（该文件同样关闭这两项）复制使用的开发者，将部署一个完全开放的应用程序。模板默认值应要求有意识地选择退出，而非选择加入。

---

### 9. 安全标头在路由/身份验证之后添加 ✅ 已在提交 016e57c 中修复

**文件：** `Program.cs`（第 130–152 行）

`UseNonceAndSecurityHeadersAsync` 和 `UseStandardSecurityHeaders` 在 `UseRouting`、`UseAuthentication` 和 `UseAuthorization` 之后调用。在到达这些中间件之前短路管道的响应（例如 401 重定向、403 拒绝）可能不会收到安全标头。安全标头应尽早添加到管道中。

---

## 🟡 中

### 10. 会话 Cookie 缺少 `Secure` 和 `SameSite` 属性 ✅ 已在提交 8f2223c 中修复

**文件：** `Extensions/ServiceCollectionExtensions.cs`（第 41–46 行）

会话 Cookie 设置了 `HttpOnly = true` 和 `IsEssential = true`，但省略了 `Cookie.SecurePolicy = CookieSecurePolicy.Always` 和 `Cookie.SameSite = SameSiteMode.Strict`。Cookie 可能通过纯 HTTP 传输（如果重定向尚未触发）或跨站点发送。

---

### 11. 格式错误的全局 `Set-Cookie` 标头 ✅ 已在提交 8f2223c 中修复

**文件：** `Extensions/ApplicationBuilderExtensions.cs`（第 73 行）

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

此代码向每个响应追加一个无名称、无值的 `Set-Cookie` 标头。这是无效的，浏览器将忽略（或拒绝）它，但会在所有响应（包括静态文件、JSON API 响应和健康检查）中产生令人困惑的痕迹。Cookie 安全性应在所配置的特定 Cookie 的选项中设置，而非作为原始标头全局注入。

---

### 12. 所有响应的 `Content-Type` 强制设置为 `text/html` ✅ 已在提交 8f2223c 中修复

**文件：** `Extensions/ApplicationBuilderExtensions.cs`（第 72 行）

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

此代码覆盖了每个响应的 Content-Type——API 端点、JSON、二进制下载和静态文件都将声称为 `text/html`。这与 `X-Content-Type-Options: nosniff` 冲突，后者阻止浏览器覆盖已声明的内容类型。

---

### 13. `AllowedHosts` 设置为通配符 ✅ 已在提交 8f2223c 中修复

**文件：** `appsettings.json`（第 11 行）、`appsettings.template.json`（第 36 行）

```json
"AllowedHosts": "*"
```

这禁用了 ASP.NET Core 的内置主机标头验证。主机标头注入攻击允许缓存投毒、密码重置链接投毒和开放重定向。此处应设置为应用程序所服务的特定域名。

---

### 14. 布局中 `<script>` 标签未应用 nonce ✅ 已在提交 8f2223c 中修复

**文件：** `Views/Shared/_Layout.cshtml`

布局加载了若干 JavaScript 文件（`jquery.min.js`、`bootstrap.bundle.min.js`、`site.js`），但没有任何 `<script>` 标签包含 `nonce="@Context.Items["Nonce"]"`。如果启用了基于 nonce 的 CSP，这些脚本将被浏览器阻止。nonce 实现已在中间件中连接，但未在视图中使用，使 CSP nonce 系统形同虚设。

---

### 15. 缺少 Referrer-Policy 标头 ✅ 已在提交 8f2223c 中修复

**文件：** `Extensions/ApplicationBuilderExtensions.cs`

标准安全标头中不包含 `Referrer-Policy`。没有此标头，浏览器会在 `Referer` 标头中将完整 URL 发送给第三方资源（例如 CSP 中包含的 ArcGIS CDN），可能泄露已认证的会话路径。

---

## 🔵 低 / 信息性

### 16. PII 以明文写入日志 ✅ 已在提交 93bb4e9 中修复

**文件：** `Services/LoggingHelper.cs`（第 85、105 行）

用户 OID、电子邮件、姓名、会话 ID 和角色在每个已认证请求时被逐字记录：

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

根据适用的隐私法规（GDPR、CCPA、HIPAA），这可能引发合规问题。应考虑在日志输出中对标识符进行掩码或哈希处理，并将包含 PII 的日志定向到受适当控制的接收端。通过记录标识符的一致性 HMAC-SHA256 哈希值而非明文值，可保留法证会话重建的目的。

---

### 17. 日志中的部分连接字符串 ✅ 已在提交 93bb4e9 中修复

**文件：** `Extensions/ServiceCollectionExtensions.cs`（第 404 行）

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

即使是日志中的部分机密也不是最佳实践。日志语句应改为确认连接字符串存在（非空），而非记录其任何部分。

---

### 18. Key Vault 操作为存根 ✅ 已在提交 93bb4e9 中修复

**文件：** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

`GetCertificateFromKeyVault` 和 `GetSecretFromKeyVault` 均为模板存根，返回 `null`/虚拟值。启用 Key Vault 后，`GetCertificateFromKeyVault` 返回 `null`，导致启动时抛出 `InvalidOperationException`——这是良好的快速失败行为，但也意味着没有实际的 Key Vault 集成可供审计机密处理方式。

---

### 19. `X-XSS-Protection: 1; mode=block` 已弃用 ✅ 已在提交 93bb4e9 中修复

**文件：** `Extensions/ApplicationBuilderExtensions.cs`（第 70 行）

现代浏览器已移除对 `X-XSS-Protection` 的支持。该标头无害，但会产生虚假的安全感。推荐方法是依赖强 CSP。`0` 值（禁用 XSS 审计器）有时被认为比 `1; mode=block` 对旧版浏览器更安全，因为审计器本身存在可被利用的行为。
