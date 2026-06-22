# 安全评审 — WebAppExperimental266

**日期：** 2026-05-07
**范围：** 代码库完整静态分析（2026-05-06 评审跟进）
**评审人：** 自动化安全评审

---

## 执行摘要

本次跟进评审确认，2026-05-06 安全评审中发现的 5 个漏洞中有 3 个已完全修复，1 个仍处于部分修复状态。评审还发现了 4 个新问题。应用程序的整体安全态势持续改善。

---

## 历史发现状态（2026-05-06）

| # | 发现 | 严重性 | 状态 |
|---|---------|----------|--------|
| 20 | NonceRefresherService 保留了未使用的 Key Vault 构造函数依赖项 | 🟠 高 | ✅ 已修复 |
| 21 | OcspValidationService 内部缓存使用非线程安全的 Dictionary | 🟡 中 | ✅ 已修复 |
| 22 | OCSP 验证存根仍然存在 — fail-closed 但未实现 | 🔵 低 | ⚠️ 已接受（设计如此） |
| 23 | mTLS 的 AllowedIssuers 为空时拒绝所有证书（fail-closed，未记录） | 🔵 低 | ✅ 已修复 |
| 24 | OcspSettings.ServerUnavailableBehavior 默认为 "Warn"（出错时允许通过） | 🔵 低 | ⚠️ 部分修复 |

---

## 历史发现详细状态

### ✅ 20. NonceRefresherService 未使用的 DI 依赖项 — 已修复

**文件：** `Services/NonceRefresherService.cs`

`NonceRefresherService` 构造函数现在仅声明 `ILogger<NonceRefresherService>`、`ILoggerFactory` 和 `INonceCatalogService`。之前四个未使用的依赖项（`IKeyVaultSettingsService`、`INonceEncryptionSettingsService`、`IAzureADSettingsService`、`IAzureKeyVaultOperationsService`）已被移除。这解决了在 `EnableKeyVault = false`（默认值）且 `EnableNonceServices = true`（默认值）时阻止应用程序启动的拒绝服务风险。

---

### ✅ 21. OcspValidationService 非线程安全缓存 — 已修复

**文件：** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` 已替换为 `ConcurrentDictionary<string, CachedOcspResponse>`。`_cache.Remove` 调用已更新为 `_cache.TryRemove`。缓存现在对并发访问是安全的。

---

### ⚠️ 22. OCSP 验证存根 — 已接受（设计如此）

**文件：** `Services/OcspValidationService.cs`

存根仍然存在，但正确地 fail-closed。由于 `EnableOcspValidation` 默认为 `false`，它没有生产影响。在完整的 OCSP 实现完成之前，将其作为信息性发现接受。

---

### ✅ 23. mTLS 空 AllowedIssuers — 已修复

**文件：** `Extensions/ServiceCollectionExtensions.cs`

当 `ValidateClientCertificateIssuer = true` 且 `AllowedIssuers` 为空时，现在会记录启动警告：

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

这为遇到 fail-closed 行为的运维人员提供了清晰的指导。

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — 部分修复

**文件：** `appsettings.template.json`（已修复）、`Models/Settings/OcspSettings.cs`（尚未修复）

模板现在正确地指定了 `"ServerUnavailableBehavior": "Fail"`。但是，`OcspSettings.cs`（第 39 行）中的 C# 类默认值仍为 `"Warn"`。如果运维人员启用了 OCSP 但在配置文件中省略了 `ServerUnavailableBehavior`，则类默认值 `"Warn"` 会被静默应用，在 OCSP 服务器中断时允许通过。类默认值需要更改以与模板建议保持一致。

---

## 新发现

| # | 区域 | 严重性 |
|---|------|----------|
| 25 | OcspSettings 类默认值（"Warn"）与模板（"Fail"）不一致 | 🔵 低 |
| 26 | NonceCatalogService 的单一共享 nonce 键允许跨请求 nonce 碰撞 | 🟡 中 |
| 27 | OptimizedNonceMiddleware 静态计数器使用有符号 32 位整数（溢出风险） | 🔵 低 |
| 28 | Program.cs 注册空的 ILoggerFactory 单例，覆盖了框架日志记录器 | 🟡 中 |

---

## 🟡 中

### 26. NonceCatalogService 共享 Nonce 键允许跨请求 Nonce 碰撞

**文件：** `Services/NonceCatalogService.cs`、`Services/NonceMiddleware.cs`、`Services/OptimizedNonceMiddleware.cs`

nonce 目录将所有 nonce 存储在单一共享键 `"CSPNonce"` 下。在并发负载下，以下竞态条件是可能的：

1. 请求 A 调用 `RefreshNonceAsync()` — nonce A1 存储为 `_nonceCollection["CSPNonce"]`。
2. 请求 B 调用 `RefreshNonceAsync()` — nonce B1 覆盖 `_nonceCollection["CSPNonce"]`。
3. 请求 A 调用 `GetANonce("CSPNonce")` — 获取 B1，而非 A1。
4. 请求 A 的 CSP 头和内联 nonce 都包含 B1。
5. 请求 B 也包含 B1。

两个并发响应共享同一个 nonce。尽管两个值仍然是密码学随机且不可预测的（没有硬编码字符串），但相同的 nonce 值出现在多个并发响应中，削弱了 CSP 规范要求的每请求唯一性保证。能够观察到一个响应的 nonce 的攻击者至少对另一个并发响应拥有有效的 nonce。

**建议：** 在中间件中直接为每个请求生成 nonce（例如 `Nonce.GenerateSecureNonce()`），并仅存储在 `HttpContext.Items["Nonce"]` 中，绕过共享目录用于每请求 nonce。共享目录仅在需要在单个请求的中间件层之间共享 nonce 时才需要，而这已由 `HttpContext.Items` 原生处理。

---

### 28. Program.cs 注册空的 ILoggerFactory 单例

**文件：** `Program.cs`（第 85 行）

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core 在 `WebApplication.CreateBuilder` 期间自动注册一个完整配置的 `ILoggerFactory`（包含来自 `builder.Logging` 配置的所有日志记录提供程序）。这个显式的 `AddSingleton` 注册添加了一个没有提供程序的第二个未配置的 `LoggerFactory` 实例。由于 `GetRequiredService<ILoggerFactory>()` 返回最近注册的实现，通过 DI 接收 `ILoggerFactory` 的服务（如 `NonceRefresherService`）将使用这个空工厂，并且不会通过 `_loggerFactory.CreateLogger<T>()` 产生任何日志输出。

**风险：** `NonceRefresherService` 中的静默日志记录 — nonce 生成的成功和失败不会发出到配置的日志记录接收器。这在不影响功能的情况下降低了应用程序在安全敏感操作期间的可观察性。

**建议：** 删除显式的 `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` 注册。框架的配置 `ILoggerFactory`（包含 Console 和任何其他提供程序）将被依赖它的服务正确解析。

---

## 🔵 低 / 信息性

### 25. OcspSettings 类默认值与模板不一致

**文件：** `Models/Settings/OcspSettings.cs`（第 39 行）

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

模板（`appsettings.template.json`）指定 `"ServerUnavailableBehavior": "Fail"`，但 C# 类默认值为 `"Warn"`。如果 `ServerUnavailableBehavior` 在活动配置文件中缺失，则类默认值会被静默应用而非模板建议。这是发现 #24 的遗留问题。

**建议：** 将类默认值从 `"Warn"` 更改为 `"Fail"`，以与模板和最小权限原则保持一致。

---

### 27. OptimizedNonceMiddleware 静态计数器可能溢出

**文件：** `Services/OptimizedNonceMiddleware.cs`（第 25–26 行）

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

这些有符号 32 位计数器通过 `Interlocked.Increment` 以原子方式递增。在约 21 亿次递增后，它们将回滚到 `int.MinValue`（−2,147,483,648），导致效率计算 `(total - generated) * 100.0 / total` 产生不正确或无意义的结果。以每秒 1,000 次请求的速度，大约在持续运行 24.8 天后发生溢出。

**建议：** 将计数器字段类型从 `int` 更改为 `long`，并使用 `Interlocked.Increment` 的 `long` 重载以防止溢出。

---

## 安全头部评估（当前状态）

以下头部通过 `UseStandardSecurityHeaders` 应用 — 与上次评审相比未变化：

| 头部 | 值 | 评估 |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ 良好 |
| `X-XSS-Protection` | `0` | ✅ 良好（禁用过时的审计员） |
| `X-Content-Type-Options` | `nosniff` | ✅ 良好 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 良好 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 良好 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 良好 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 良好 |
| `Permissions-Policy` | geolocation、camera、microphone、interest-cohort 已禁用 | ✅ 良好 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 良好 |
| `Content-Security-Policy` | 基于 Nonce，在 CSP 启用时应用 | ✅ 良好 |
| `Server` | 已遮盖为 `"webserver"` | ✅ 良好 |
| `X-Powered-By` | 已删除 | ✅ 良好 |

---

## 整体评估

上次评审中所有高严重性发现均已修复。当前发现仅限于两个中等严重性问题（#26 共享 nonce 键，#28 空 ILoggerFactory）和两个低严重性信息性条目（#25 类默认值不匹配，#27 计数器中的整数溢出）。建议立即关注发现 #28（空 ILoggerFactory 单例），因为它在 nonce 操作期间静默抑制了与安全相关的诊断日志记录。应解决发现 #26（共享 nonce 键）以恢复 CSP 规范要求的每请求 nonce 唯一性保证。
