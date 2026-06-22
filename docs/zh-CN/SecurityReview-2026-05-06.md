# 安全审查 — WebAppExperimental266

**日期：** 2026-05-06
**范围：** 完整代码库审计（2026-05-05 审查的后续跟进）
**审查员：** 自动化安全审查

---

## 管理层摘要

本次后续审查确认，在 2026-05-05 安全审查中发现的全部十九（19）项安全漏洞均已成功修复。本次审查还发现了五（5）项新的或残留的问题。与上次审查相比，应用程序的整体安全状况已显著改善。

---

## 历史发现状态（2026-05-05）

全部十九（19）项历史发现**已确认修复**：

| # | 发现 | 严重性 | 状态 |
|---|------|--------|------|
| 1 | 在 nonce 生成中重用 AES-GCM IV | 🔴 严重 | ✅ 已修复 |
| 2 | 将 nonce 写入明文日志 | 🔴 严重 | ✅ 已修复 |
| 3 | 硬编码 nonce 配置字符串 | 🔴 严重 | ✅ 已修复 |
| 4 | 全局 nonce 存储非线程安全 | 🟠 高 | ✅ 已修复 |
| 5 | mTLS 客户端证书颁发者验证未执行 | 🟠 高 | ✅ 已修复 |
| 6 | mTLS 证书吊销检查默认禁用 | 🟠 高 | ✅ 已修复 |
| 7 | OCSP 始终返回有效（stub） | 🟠 高 | ✅ 已修复 |
| 8 | 认证/授权在功能开关中默认禁用 | 🟠 高 | ✅ 已修复 |
| 9 | 安全标头在管道末尾添加 | 🟠 高 | ✅ 已修复 |
| 10 | 会话 Cookie 缺少 `Secure` + `SameSite` | 🟡 中 | ✅ 已修复 |
| 11 | 全局 `Set-Cookie` 标头被注入 | 🟡 中 | ✅ 已修复 |
| 12 | `Content-Type` 在所有位置硬编码为 `text/html` | 🟡 中 | ✅ 已修复 |
| 13 | `AllowedHosts` 设置为通配符令牌 | 🟡 中 | ✅ 已修复 |
| 14 | 模板中 Nonce 未附加到 `<script>` 标签 | 🟡 中 | ✅ 已修复 |
| 15 | `Referrer-Policy` 标头缺失 | 🟡 中 | ✅ 已修复 |
| 16 | PII 写入明文日志 | 🔵 低 | ✅ 已修复 |
| 17 | 消息中的字符串拼接 | 🔵 低 | ✅ 已修复 |
| 18 | Key Vault 操作为 stub | 🔵 低 | ✅ 已修复 |
| 19 | `X-XSS-Protection: 1; mode=block` 已过时 | 🔵 低 | ✅ 已修复 |

---

## 新发现 / 残留问题

| # | 位置 | 严重性 |
|---|------|--------|
| 20 | NonceRefresherService 保留了未使用的 Key Vault 构造函数依赖 | 🟠 高 |
| 21 | OcspValidationService 内存缓存使用非线程安全的 Dictionary | 🟡 中 |
| 22 | OCSP 验证 stub 仍存在——失败关闭但未实现 | 🔵 低 |
| 23 | mTLS 空 AllowedIssuers 拒绝所有客户端证书（失败关闭，未记录） | 🔵 低 |
| 24 | OcspSettings.ServerUnavailableBehavior 默认为 "Warn"（错误时允许通过） | 🔵 低 |

---

## 详细发现

### ✅ 来自 2026-05-05 的已确认修复

#### 1. AES-GCM IV 重用 — 已修复

**文件：** `Models/Main_Objects/Nonce.cs`

nonce 生成已从 AES-GCM 完全重写。`Nonce.GenerateSecureNonce()` 现在调用 `RandomNumberGenerator.Fill(randomBytes)` 生成 16 个随机字节并返回 Base64 字符串。无 Key Vault 依赖，无 IV，无加密——这是 CSP nonce 的正确方法。

---

#### 2. nonce 值未写入日志 — 已修复

**文件：** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

两个文件仅写入状态消息（`"Nonce retrieved for request."`, `"Nonce generated successfully."`），从不写入 nonce 值本身。

---

#### 3. 硬编码配置 nonce 已移除 — 已修复

**文件：** `Services/OptimizedNonceMiddleware.cs`

三个硬编码字符串（`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`）已替换为正常路径中的 `Nonce.GenerateSecureNonce()` 调用和两个配置错误指示器。

---

#### 4. 线程安全的 nonce 存储 — 已修复

**文件：** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` 已更改为 `ConcurrentDictionary<string, Nonce>`。`GetANonce` 现在使用单个原子 `TryGetValue` 调用代替两步检查。

---

#### 5. mTLS 客户端证书颁发者验证现在正常工作 — 已修复

**文件：** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

硬编码的颁发者验证上下文已替换为 `mtlsSettings.IsIssuerAllowed(issuer)` 调用，该调用对 `AllowedIssuers` 执行不区分大小写的字符串比较。如果列表为空（未配置），方法返回 `false`，拒绝所有证书（失败关闭）。

---

#### 6. mTLS 证书吊销检查现在默认启用 — 已修复

**文件：** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` 现在默认为 `true`。`appsettings.template.json` 也设置 `"CheckCertificateRevocation": true`。

---

#### 7. OCSP stub 现在失败关闭 — 已修复

**文件：** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` 现在返回 `IsValid = false` 和 `OcspStatus.Error` 并记录错误，而不是静默返回 `IsValid = true`。启用 OCSP 将拒绝所有证书，直到有真实实现为止。

---

#### 8. 认证和授权现在默认启用 — 已修复

**文件：** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` 和 `EnableAuthorization` 现在在 `FeatureFlags` 类中默认均为 `true`。`appsettings.json` 也将两者设置为 `true`。

---

#### 9. 安全标头在管道早期添加 — 已修复

**文件：** `Program.cs`

`UseNonceAndSecurityHeadersAsync` 和 `UseStandardSecurityHeaders` 现在在 `UseRouting`、`UseAuthentication` 和 `UseAuthorization` 之前调用。所有响应（包括短路 401/403 响应）都将收到其安全标头。

---

#### 10–15. Cookie、Content-Type、AllowedHosts、模板中的 Nonce、Referrer-Policy — 已修复

**文件：** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- 会话 Cookie 现在设置为 `CookieSecurePolicy.Always` 和 `SameSiteMode.Strict`。
- 不需要的全局 `Set-Cookie` 标头已移除。
- 全局 `Content-Type: text/html` 变量已移除。
- `appsettings.json` 中的 `AllowedHosts` 现在为 `"localhost;127.0.0.1"`；模板使用 `"{{YOUR_HOSTNAME}}"`。
- `_Layout.cshtml` 中的所有三个 `<script>` 标签现在都有 `nonce="@Context.Items["Nonce"]"`。
- `Referrer-Policy: strict-origin-when-cross-origin` 已由 `UseStandardSecurityHeaders` 添加。

---

#### 16–19. 日志中的 PII、字符串拼接、Key Vault Stub、X-XSS-Protection — 已修复

**文件：** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- 所有 PII（OID、电子邮件、姓名、SID、角色）在写入日志前通过 `LoggingHelper.HashPii()` 进行 HMAC-SHA256 哈希处理。可通过配置中的 `Logging:PiiHmacKey` 提供稳定的 HMAC 密钥；若未配置则使用每进程密钥。
- Cosmos DB 日志消息仅验证拼接字符串是否存在（`!string.IsNullOrEmpty`），而不是其内容。
- `AzureKeyVaultCertificateOperations` 现在在启动时证书为 null 时抛出 `InvalidOperationException`，而不是静默返回元数据值。
- `X-XSS-Protection` 现在设置为 `"0"`（禁用旧版 XSS 过滤器），符合现代浏览器指南。

---

## 🟠 高

### 20. NonceRefresherService 保留了未使用的 Key Vault 构造函数依赖

**文件：** `Services/NonceRefresherService.cs`

`NonceRefresherService` 注入了 `IKeyVaultSettingsService`、`INonceEncryptionSettingsService`、`IAzureADSettingsService` 和 `IAzureKeyVaultOperationsService` 的构造函数参数。由于 nonce 生成已简化为直接使用 `RandomNumberGenerator`，这些依赖项均未被使用。

**问题：** 当 `EnableNonceServices = true` 且 `EnableKeyVault = false`（默认值）时，这些服务未在 DI 容器中注册，导致在首次解析 nonce 服务时启动时抛出 `InvalidOperationException`。这是默认配置导致的服务不可用状态。`FeatureFlags` 类默认将 `EnableNonceServices = true`，因此任何仅依赖类默认值（未修改 `appsettings.json`）的环境都将失败。

**建议：** 从 `NonceRefresherService` 中删除四个未使用的构造函数参数及其对应的私有字段。该服务只需要 `ILogger<NonceRefresherService>`、`ILoggerFactory` 和 `INonceCatalogService`。

---

## 🟡 中

### 21. OcspValidationService 内存缓存使用非线程安全的 Dictionary

**文件：** `Services/OcspValidationService.cs`（第 47 行）

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` 对并发读写不安全。如果 `OcspValidationService` 注册为单例（或在请求间共享同一实例），并发 OCSP 验证可能损坏缓存，导致条目丢失、意外异常或返回过期数据。

**建议：** 将 `Dictionary<string, CachedOcspResponse>` 替换为 `ConcurrentDictionary<string, CachedOcspResponse>`。将 `_cache.Remove` 调用（第 103 行）更新为 `_cache.TryRemove`。

---

## 🔵 低 / 信息性

### 22. OCSP 验证 Stub — 失败关闭但未实现

**文件：** `Services/OcspValidationService.cs`（第 157–173 行）

`PerformOcspValidationAsync` 仍然是 stub。发现 #7 的修复已将行为从"始终有效"正确更改为"始终无效（失败关闭）"。但这不是真实的 OCSP 实现。`EnableOcspValidation = false`（默认）时，对业务没有影响。在任何环境中启用 OCSP 之前，必须实现真实的 OCSP 状态实现。

---

### 23. 空 AllowedIssuers 的 mTLS 拒绝所有客户端证书

**文件：** `Models/Settings/MtlsSettings.cs`

当 `ValidateClientCertificateIssuer = true`（默认）且 `AllowedIssuers` 为空（未配置时也是默认值）时，`IsIssuerAllowed()` 返回 `false`，拒绝所有客户端证书。这是所需的失败关闭行为，但未明确记录。未仔细阅读模板即启用 mTLS 的操作人员可能发现所有客户端证书都被拒绝，却没有明确说明。

**建议：** 在 `ValidateClientCertificateIssuer = true` 且 `AllowedIssuers` 为空时启动时添加日志警告。

---

### 24. OcspSettings.ServerUnavailableBehavior 默认为 "Warn"

**文件：** `appsettings.template.json`（第 134 行），`Services/OcspValidationService.cs`

`ServerUnavailableBehavior` 的默认值在模板中为 `"Warn"`，当 OCSP 服务不可达时允许请求通过。对于高安全性环境，应将其设置为 `"Fail"` 以防止 OCSP 服务故障静默绕过证书吊销检查。

**建议：** 在模板中明确记录所有三个选项（`Fail`、`Allow`、`Warn`），并考虑根据最小权限原则将默认值更改为 `"Fail"`。

---

## 安全标头评估（当前状态）

以下标头由 `UseStandardSecurityHeaders` 添加：

| 标头 | 值 | 评估 |
|------|----|------|
| `X-Frame-Options` | `DENY` | ✅ 良好 |
| `X-XSS-Protection` | `0` | ✅ 良好（禁用旧版 XSS 过滤器） |
| `X-Content-Type-Options` | `nosniff` | ✅ 良好 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 良好 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 良好 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 良好 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 良好 |
| `Permissions-Policy` | 地理位置、摄像头、麦克风、interest-cohort 已禁用 | ✅ 良好 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 良好 |
| `Content-Security-Policy` | 基于 Nonce，启用 CSP 时包含 | ✅ 良好 |
| `Server` | 隐藏为 `"webserver"` | ✅ 良好 |
| `X-Powered-By` | 已移除 | ✅ 良好 |

---

## 总体评估

应用程序已修复了上次审查中所有严重和高严重性漏洞。当前发现仅限于一个高严重性配置/DI 问题（发现 #20）和低严重性信息性项目。安全状况已显著改善。建议对发现 #20（NonceRefresherService 中未使用的 DI 依赖）采取紧急行动，因为它可能阻止应用程序在默认配置下启动。
