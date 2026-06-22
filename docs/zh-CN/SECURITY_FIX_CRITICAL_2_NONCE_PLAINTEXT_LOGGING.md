# 安全修复：nonce 值以明文写入日志（严重问题 #2）

**修复于：** `Services/NonceMiddleware.cs`、`Services/NonceRefresherService.cs`
**测试：** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## 问题描述

有两处代码将实际 CSP nonce 值逐字记录到应用程序日志流中：

**`Services/NonceMiddleware.cs`（第 31 行）：**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs`（第 82 行）：**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### 为何严重

CSP nonce 是 CSP 强制执行后*唯一*能阻止内联脚本注入的机制。其安全性完全依赖于它在**单个响应生命周期内保持机密**。

在云/企业环境中，应用程序日志通常可被以下各方读取：
* 运营团队
* 日志聚合服务（如 Azure Monitor、Splunk、ELK）
* 对日志接收端具有读取权限的任何账户

任何能读取包含 `Nonce: <值>` 日志行的人，都可以注入带有该 nonce 值的内联 `<script>` 标签，使浏览器执行它，从而完全绕过 CSP。即使 nonce 按请求轮换，具有实时日志访问权限的攻击者也可以在同一请求的窗口内采取行动。

---

## 修复内容

两条日志语句均已替换为仅确认 nonce 生成*状态*而不透露值的消息：

**`NonceMiddleware.cs`：**
```csharp
// 修复前（存在漏洞）：
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// 修复后（安全）：
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`：**
```csharp
// 修复前（存在漏洞）：
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// 修复后（安全）：
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## 如何保持修复状态

1. **永远不要记录 nonce 值。** 日志消息可以确认 nonce 已生成或已检索（成功/失败状态），但 nonce 字符串本身绝不能出现在任何日志参数、结构化日志字段或字符串插值中。

2. **审查 nonce 相关代码中的任何新日志语句**（`NonceMiddleware`、`OptimizedNonceMiddleware`、`NonceRefresherService`、`NonceCatalogService`），确保其中不包含 nonce 值。

3. **出于同样的原因，不要在遥测、指标或分布式追踪中暴露 nonce。** 追踪属性和 Span 标签通常会被转发到日志聚合后端。

4. **nonce 必须被视为每请求的机密。** 它可以存储在 `HttpContext.Items` 中以在单个请求的渲染管道内使用，但它除了通过 HTTP 响应标头以及它所保护的 HTML 中的 `nonce="..."` 属性之外，不得通过任何可观察的渠道离开进程。

### 执行此修复的测试

| 测试 | 捕获的问题 |
|------|-----------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | 若 nonce 字符串被重新引入 `NonceRefresherService` 的任何日志消息中则失败 |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | 若 nonce 字符串被重新引入 `NonceMiddleware` 的任何日志消息中则失败 |
