# Mwongozo wa Uboreshaji wa Uzalishaji wa Nonce

## Tatizo la Sasa

Nonce kwa sasa huzalishwa **kwa kila ombi la HTTP**, ikijumuisha:
- Maombi ya faili statiki (CSS, JS, picha)
- Miito ya API
- Ukaguzi wa afya wa cloud service
- Azure load balancer probes

Hii husababisha:
- Miito mingi kupita kiasi ya Azure Key Vault
- Operesheni za cryptographic zisizo za lazima
- Kushuka kwa utendaji
- Kuongezeka kwa gharama za Azure

## Suluhisho: Uzalishaji wa Nonce kwa Responses Pekee

Zalisha nonce mpya **kwa HTTP responses pekee ambazo zitatoa HTML pages** zenye vichwa vya CSP.

---

## Utekelezaji Ulioboreshwa

### 1. Unda Response-Only Nonce Middleware

Jambo kuu ni kuzalisha nonce **kabla ya kutuma response**, si kwa kila request.

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

### 2. Nonce Middleware Rahisi ya Request (Tumia Iliyopo)

Hifadhi nonce iliyopo kwa muda wa request:

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

        // If no nonce exists yet (first request), use a default
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("No nonce available yet, using placeholder");
        }

        // Store in context
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## Mkakati wa Utekelezaji

### Chaguo la 1: Chuja kwa Request Path (Rahisi Zaidi)

Zalisha nonce kwa maombi ya Razor Page pekee:

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

### Chaguo la 2: Nonce Moja kwa Kila Response (Inapendekezwa)

Zalisha nonce katika response pipeline:

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

### Chaguo la 3: Lazy Nonce Generation (Yenye Ufanisi Zaidi)

Zalisha tu wakati kichwa cha CSP kinajengwa:

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

## Maboresho ya Utendaji

### Kabla ya Uboreshaji
```
Requests per minute: 1,000
- Static files: 700 (70%)
- Health checks: 200 (20%)
- Page requests: 100 (10%)

Nonce generations: 1,000 (one per request)
Key Vault calls: 2,000 (IV + Key per nonce)
```

### Baada ya Uboreshaji
```
Requests per minute: 1,000
- Static files: 700 (ignored)
- Health checks: 200 (ignored)
- Page requests: 100 (nonce generated)

Nonce generations: 100 (only for pages)
Key Vault calls: 200 (90% reduction!)
```

---

## Suluhisho Linalopendekezwa

**Tumia Chaguo la 1 (Path Filtering) + Caching:**

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
            // Use existing nonce or placeholder
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = "static-content-nonce";
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

## Upimaji

### Thibitisha Uboreshaji Unafanya Kazi

```powershell
# Monitor nonce generation
dotnet run

# Make requests and check logs
Invoke-WebRequest "https://localhost:5001/"           # Should generate nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"  # Should NOT generate nonce
Invoke-WebRequest "https://localhost:5001/Privacy"   # Should generate nonce
```

### Vipimo vya Utendaji

Ongeza uandishi wa kumbukumbu kufuatilia nonce generation:

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

## Hatua za Uhamishaji

1. ? **Hifadhi nakala ya sasa ya `NonceMiddleware.cs`**
2. ? **Unda `OptimizedNonceMiddleware.cs`** (faili mpya)
3. ? **Sasisha `Program.cs`** ili kutumia middleware iliyoboreshwa
4. ? **Jaribu kwa faili statiki**
5. ? **Jaribu kwa page requests**
6. ? **Fuatilia metrics za Azure Key Vault**
7. ? **Ondoa middleware ya zamani baada ya uthibitishaji**

---

## Matokeo Yanayotarajiwa

- **Upungufu wa 90%** katika nonce generations
- **Upungufu wa 90%** katika Azure Key Vault calls
- **Muda wa response wa haraka zaidi** kwa maudhui statiki
- **Gharama za Azure zilizo chini**
- **Usalama uleule** kwa HTML pages

---

## Usanidi

Ongeza setting ya kudhibiti tabia:

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

## Hatua Zinazofuata

1. Tekeleza `OptimizedNonceMiddleware.cs`
2. Sasisha usajili wa middleware katika `Program.cs`
3. Jaribu na uthibitishe kupungua kwa Key Vault calls
4. Fuatilia kumbukumbu za programu
5. Ondoa middleware ya zamani utakaporidhika

Je, ungependa nitekeleze optimized middleware sasa?

