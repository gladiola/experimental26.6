# Nonce 生成优化指南

## 当前问题

nonce 当前在**每个 HTTP 请求**时生成，包括：
- 静态文件请求（CSS、JS、图片）
- API 调用
- 云服务健康检查
- Azure 负载均衡器探测

这导致：
- 过多的 Azure Key Vault 调用
- 不必要的密码学操作
- 性能下降
- Azure 成本增加

## 解决方案：仅响应时生成 Nonce

**仅在需要渲染 HTML 页面并包含 CSP 标头的 HTTP 响应时**生成全新 nonce。

---

## 优化实现

### 1. 创建仅响应的 Nonce 中间件

关键是在**发送响应之前**生成 nonce，而非在每个请求时生成。

```csharp
public class NonceResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<NonceResponseMiddleware> _logger;

    public NonceResponseMiddleware(
        RequestDelegate next,
        INonceRefresherService nonceRefresherService,
        INonceCatalogService nonceCatalogService,
        ILogger<NonceResponseMiddleware> logger)
    {
        _next = next;
        _nonceRefresherService = nonceRefresherService;
        _nonceCatalogService = nonceCatalogService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 仅为 HTML 响应生成 nonce
        var originalBodyStream = context.Response.Body;

        try
        {
            // 拦截响应
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // 继续管道
            await _next(context);

            // 检查是否是需要 nonce 的 HTML 响应
            if (ShouldGenerateNonce(context))
            {
                // 生成全新 nonce
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // 存储在上下文中用于 CSP 标头生成
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("Generated nonce for response: {Path}", context.Request.Path);
            }

            // 复制响应回原始流
            context.Response.Body = originalBodyStream;
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldGenerateNonce(HttpContext context)
    {
        // 仅为成功的 HTML 响应生成
        if (context.Response.StatusCode != 200)
            return false;

        // 检查内容类型
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // 只有 HTML 响应需要 nonce
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. 简化的请求 Nonce 中间件（重用现有 nonce）

保留请求期间的现有 nonce：

```csharp
public class NonceRequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<NonceRequestMiddleware> _logger;

    public NonceRequestMiddleware(
        RequestDelegate next,
        INonceCatalogService nonceCatalogService,
        ILogger<NonceRequestMiddleware> logger)
    {
        _next = next;
        _nonceCatalogService = nonceCatalogService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 从目录获取现有 nonce（不生成新 nonce）
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // 如果还没有 nonce（首次请求），使用默认值
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("No nonce available yet, using placeholder");
        }

        // 存储在上下文中
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## 实现策略

### 选项一：按请求路径过滤（最简单）

仅为 Razor Page 请求生成 nonce：

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // 跳过静态文件和 API 调用的 nonce 生成
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        // 不生成 nonce，直接传递
        await _next(context);
        return;
    }

    // 仅为页面请求生成 nonce
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### 选项二：每个响应一个 Nonce（推荐）

在响应管道中生成 nonce：

```csharp
app.Use(async (context, next) =>
{
    // 挂接到 OnStarting 事件（在发送标头之前运行）
    context.Response.OnStarting(async () =>
    {
        // 仅为 HTML 响应生成
        if (context.Response.ContentType?.Contains("text/html") == true)
        {
            await _nonceRefresherService.RefreshNonceAsync();
            var nonce = _nonceCatalogService.GetANonce("CSPNonce");
            context.Items["Nonce"] = nonce;
        }
    });

    await next();
});
```

### 选项三：延迟 Nonce 生成（最高效）

仅在构建 CSP 标头时生成：

```csharp
public class LazyNonceService : INonceService
{
    private readonly INonceRefresherService _refresher;
    private readonly INonceCatalogService _catalog;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _currentNonce;
    private DateTime _lastGenerated;
    private readonly TimeSpan _nonceLifetime = TimeSpan.FromMinutes(5);

    public async Task<string> GetOrGenerateNonceAsync()
    {
        // 检查当前 nonce 是否仍有效
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        // 生成新 nonce
        await _lock.WaitAsync();
        try
        {
            // 获取锁后再次检查
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

            // 生成全新 nonce
            _currentNonce = await _refresher.RefreshNonceAsync();
            _lastGenerated = DateTime.UtcNow;
            return _currentNonce;
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

---

## 性能提升

### 优化前
```
每分钟请求数：1,000
- 静态文件：700（70%）
- 健康检查：200（20%）
- 页面请求：100（10%）

Nonce 生成次数：1,000（每请求一次）
Key Vault 调用次数：2,000（每个 nonce 调用 IV + Key）
```

### 优化后
```
每分钟请求数：1,000
- 静态文件：700（忽略）
- 健康检查：200（忽略）
- 页面请求：100（生成 nonce）

Nonce 生成次数：100（仅页面请求）
Key Vault 调用次数：200（减少 90%！）
```

---

## 推荐方案

**使用选项一（路径过滤）+ 缓存：**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // 不应触发 nonce 生成的路径
    private static readonly string[] IgnorePaths = new[]
    {
        "/css", "/js", "/lib", "/images", "/fonts",
        "/favicon.ico", "/_framework", "/api"
    };

    public OptimizedNonceMiddleware(
        RequestDelegate next,
        INonceRefresherService nonceRefresherService,
        INonceCatalogService nonceCatalogService,
        ILogger<OptimizedNonceMiddleware> logger)
    {
        _next = next;
        _nonceRefresherService = nonceRefresherService;
        _nonceCatalogService = nonceCatalogService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 检查是否应忽略该请求
        if (ShouldIgnoreRequest(context.Request))
        {
            // 使用现有 nonce 或占位符
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = "static-content-nonce";
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

        // 为页面请求生成全新 nonce
        _logger.LogDebug("Generating nonce for: {Path}", context.Request.Path);
        await _nonceRefresherService.RefreshNonceAsync();
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");
        context.Items["Nonce"] = nonce;

        await _next(context);
    }

    private bool ShouldIgnoreRequest(HttpRequest request)
    {
        var path = request.Path.Value;
        if (string.IsNullOrEmpty(path))
            return false;

        // 忽略静态文件请求
        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 忽略带有文件扩展名的请求（.cshtml 除外）
        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## 测试

### 验证优化效果

```powershell
# 监控 nonce 生成
dotnet run

# 发送请求并检查日志
Invoke-WebRequest "https://localhost:5001/"           # 应生成 nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"  # 不应生成 nonce
Invoke-WebRequest "https://localhost:5001/Privacy"   # 应生成 nonce
```

### 性能指标

添加日志以跟踪 nonce 生成：

```csharp
private static int _nonceGenerationCount = 0;

public async Task InvokeAsync(HttpContext context)
{
    if (ShouldIgnoreRequest(context.Request))
    {
        _logger.LogTrace("Skipped nonce for: {Path}", context.Request.Path);
        // ...
    }
    else
    {
        Interlocked.Increment(ref _nonceGenerationCount);
        _logger.LogInformation("Generated nonce #{Count} for: {Path}",
            _nonceGenerationCount, context.Request.Path);
        // ...
    }
}
```

---

## 迁移步骤

1. ✅ **备份当前 NonceMiddleware.cs**
2. ✅ **创建 OptimizedNonceMiddleware.cs**（新文件）
3. ✅ **更新 Program.cs** 以使用优化后的中间件
4. ✅ **使用静态文件测试**
5. ✅ **使用页面请求测试**
6. ✅ **监控 Azure Key Vault 指标**
7. ✅ **验证后删除旧中间件**

---

## 预期结果

- nonce 生成次数**减少 90%**
- Azure Key Vault 调用次数**减少 90%**
- 静态内容响应时间**更快**
- **降低 Azure 成本**
- HTML 页面**安全性不变**

---

## 配置

添加设置以控制行为：

```json
{
  "NonceGeneration": {
    "GenerateForStaticFiles": false,
    "GenerateForApiCalls": false,
    "NonceLifetimeMinutes": 5,
    "EnableOptimization": true
  }
}
```

---

## 后续步骤

1. 实现 `OptimizedNonceMiddleware.cs`
2. 更新 `Program.cs` 中间件注册
3. 测试并验证 Key Vault 调用减少情况
4. 监控应用程序日志
5. 满意后删除旧中间件
