# Nonce Generation Optimization Guide

## Current Problem

The nonce is currently generated **on every HTTP request**, including:
- Static file requests (CSS, JS, images)
- API calls
- Cloud service health checks
- Azure load balancer probes

This causes:
- Excessive Azure Key Vault calls
- Unnecessary cryptographic operations
- Performance degradation
- Increased Azure costs

## Solution: Response-Only Nonce Generation

Generate a fresh nonce **only for HTTP responses that will render HTML pages** with CSP headers.

---

## Optimized Implementation

### 1. Create Response-Only Nonce Middleware

The key is to generate the nonce **before sending the response**, not on every request.

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
        // Only generate nonce for HTML responses
        var originalBodyStream = context.Response.Body;

        try
        {
            // Intercept response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Continue pipeline
            await _next(context);

            // Check if this is an HTML response that needs a nonce
            if (ShouldGenerateNonce(context))
            {
                // Generate fresh nonce
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // Store in context for CSP header generation
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("Generated nonce for response: {Path}", context.Request.Path);
            }

            // Copy response back
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
        // Only generate for successful HTML responses
        if (context.Response.StatusCode != 200)
            return false;

        // Check content type
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // Only HTML responses need nonces
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Simplified Request Nonce Middleware (Reuse Existing)

Keep existing nonce for the request duration:

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
        // Get EXISTING nonce from catalog (don't generate new one)
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // If no nonce exists yet (first request), generate a secure nonce
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = Nonce.GenerateSecureNonce();
            _logger.LogWarning("No nonce available yet, generated secure fallback nonce");
        }

        // Store in context
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## Implementation Strategy

### Option 1: Filter by Request Path (Simplest)

Only generate nonce for Razor Page requests:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Skip nonce generation for static files and API calls
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        // Just pass through without generating nonce
        await _next(context);
        return;
    }

    // Generate nonce only for page requests
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### Option 2: One Nonce Per Response (Recommended)

Generate nonce in response pipeline:

```csharp
app.Use(async (context, next) =>
{
    // Hook into OnStarting event (runs before headers are sent)
    context.Response.OnStarting(async () =>
    {
        // Only generate for HTML responses
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

### Option 3: Lazy Nonce Generation (Most Efficient)

Only generate when CSP header is being built:

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
        // Check if current nonce is still valid
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        // Generate new nonce
        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

            // Generate fresh nonce
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

## Performance Improvements

### Before Optimization
```
Requests per minute: 1,000
- Static files: 700 (70%)
- Health checks: 200 (20%)
- Page requests: 100 (10%)

Nonce generations: 1,000 (one per request)
Key Vault calls: 2,000 (IV + Key per nonce)
```

### After Optimization
```
Requests per minute: 1,000
- Static files: 700 (ignored)
- Health checks: 200 (ignored)
- Page requests: 100 (nonce generated)

Nonce generations: 100 (only for pages)
Key Vault calls: 200 (90% reduction!)
```

---

## Recommended Solution

**Use Option 1 (Path Filtering) + Caching:**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // Paths that should NOT trigger nonce generation
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
        // Check if request should be ignored
        if (ShouldIgnoreRequest(context.Request))
        {
            // Use existing nonce or a secure fallback nonce
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = Nonce.GenerateSecureNonce();
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

        // Generate fresh nonce for page requests
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

        // Ignore static file requests
        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Ignore requests with file extensions (except .cshtml)
        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## Testing

### Verify Optimization Works

```powershell
# Monitor nonce generation
dotnet run

# Make requests and check logs
Invoke-WebRequest "https://localhost:5001/"           # Should generate nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"  # Should NOT generate nonce
Invoke-WebRequest "https://localhost:5001/Privacy"   # Should generate nonce
```

### Performance Metrics

Add logging to track nonce generation:

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

## Migration Steps

1. ? **Backup current NonceMiddleware.cs**
2. ? **Create OptimizedNonceMiddleware.cs** (new file)
3. ? **Update Program.cs** to use optimized middleware
4. ? **Test with static files**
5. ? **Test with page requests**
6. ? **Monitor Azure Key Vault metrics**
7. ? **Remove old middleware after verification**

---

## Expected Results

- **90% reduction** in nonce generations
- **90% reduction** in Azure Key Vault calls
- **Faster response** times for static content
- **Lower Azure costs**
- **Same security** for HTML pages

---

## Configuration

Add setting to control behavior:

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

## Next Steps

1. Implement `OptimizedNonceMiddleware.cs`
2. Update `Program.cs` middleware registration
3. Test and verify reduction in Key Vault calls
4. Monitor application logs
5. Remove old middleware when satisfied

Would you like me to implement the optimized middleware now?
