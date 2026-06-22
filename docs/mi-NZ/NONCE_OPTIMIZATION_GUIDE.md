# Aratohu Arotautanga Hanga Nonce

## Te Raru o Nāianei

I tēnei wā ka hangaia te nonce **i ia tono HTTP**, tae atu ki:
- Tono kōnae tūmau (CSS, JS, whakaahua)
- Karanga API
- Arowhai hauora ratonga kapua
- Azure load balancer probes

Ka hua mai:
- Nui rawa ngā karanga ki Azure Key Vault
- Mahi whakamunatanga kāore e hiahiatia
- Heke mahi
- Nui ake te utu Azure

## Otinga: Hanga Nonce mō ngā Whakautu Anake

Hangaia he nonce hou **mō ngā whakautu HTTP ka tuku whārangi HTML anake** me ngā pane CSP.

---

## Whakatinanatanga Arotau

### 1. Waihangatia he Response-Only Nonce Middleware

Ko te mea matua: hanga nonce **i mua i te tuku whakautu**, kaua i ia tono.

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
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            if (ShouldGenerateNonce(context))
            {
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");
                context.Items["Nonce"] = nonce;
                _logger.LogDebug("Generated nonce for response: {Path}", context.Request.Path);
            }

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
        if (context.Response.StatusCode != 200)
            return false;

        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Request Nonce Middleware māmā ake (whakamahi anō i te nonce o nāianei)

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
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        if (string.IsNullOrEmpty(nonce))
        {
            nonce = Nonce.GenerateSecureNonce();
            _logger.LogWarning("No nonce available yet, i waihanga nonce haumaru mō te fallback");
        }

        context.Items["Nonce"] = nonce;
        await _next(context);
    }
}
```

---

## Rautaki Whakatinana

### Kōwhiringa 1: Tātari ara tono (māmā rawa)

Hanga nonce anake mō ngā tono Razor Page.

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        await _next(context);
        return;
    }

    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### Kōwhiringa 2: Kotahi nonce mō ia whakautu (tūtohu)

```csharp
app.Use(async (context, next) =>
{
    context.Response.OnStarting(async () =>
    {
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

### Kōwhiringa 3: Lazy nonce generation (pai rawa)

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
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        await _lock.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

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

## Whakapikinga Mahi

### I mua i te arotautanga
```
Tono ia meneti: 1,000
- Kōnae tūmau: 700 (70%)
- Health checks: 200 (20%)
- Tono whārangi: 100 (10%)

Hanga nonce: 1,000
Karanga Key Vault: 2,000 (IV + Key)
```

### I muri i te arotautanga
```
Tono ia meneti: 1,000
- Kōnae tūmau: 700 (ignorea)
- Health checks: 200 (ignorea)
- Tono whārangi: 100 (hanga nonce)

Hanga nonce: 100
Karanga Key Vault: 200 (90% hekenga)
```

---

## Otinga tūtohu

**Whakamahia Kōwhiringa 1 (Path filtering) + Caching**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

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
        if (ShouldIgnoreRequest(context.Request))
        {
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = Nonce.GenerateSecureNonce();
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

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

        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## Whakamātautau

### Whakau i te arotautanga

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"              # Me hanga nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"  # Kaua e hanga nonce
Invoke-WebRequest "https://localhost:5001/Privacy"       # Me hanga nonce
```

### Performance metrics

```csharp
private static int _nonceGenerationCount = 0;

public async Task InvokeAsync(HttpContext context)
{
    if (ShouldIgnoreRequest(context.Request))
    {
        _logger.LogTrace("Skipped nonce for: {Path}", context.Request.Path);
    }
    else
    {
        Interlocked.Increment(ref _nonceGenerationCount);
        _logger.LogInformation("Generated nonce #{Count} for: {Path}",
            _nonceGenerationCount, context.Request.Path);
    }
}
```

---

## Hipanga Heke

1. ✅ Backup i te `NonceMiddleware.cs` o nāianei
2. ✅ Waihanga `OptimizedNonceMiddleware.cs` (kōnae hou)
3. ✅ Whakahōu `Program.cs` kia whakamahi i te middleware hou
4. ✅ Whakamātautau me ngā kōnae tūmau
5. ✅ Whakamātautau me ngā tono whārangi
6. ✅ Aroturuki Azure Key Vault metrics
7. ✅ Tangohia te middleware tawhito i muri i te whakau

---

## Hua e tūmanakohia ana

- **90% hekenga** i te hanga nonce
- **90% hekenga** i ngā karanga Key Vault
- **Whakautu tere ake** mō te static content
- **Utu Azure iti ake**
- **Haumaru ōrite** mō ngā whārangi HTML

---

## Whirihoranga

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

## Ngā mahi whai muri

1. Whakatinana `OptimizedNonceMiddleware.cs`
2. Whakahōu i te rēhitatanga middleware i `Program.cs`
3. Whakamātautau, whakau i te hekenga karanga Key Vault
4. Aroturuki i ngā logs
5. Tangohia te middleware tawhito ina makona

E hiahia ana koe kia whakatinanahia tēnei middleware arotautanga ināianei?
