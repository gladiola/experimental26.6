# Gids voor optimalisatie van nonce-generatie

## Huidig probleem

De nonce wordt nu **bij elk HTTP-request** gegenereerd, inclusief:
- Requests voor statische bestanden (CSS, JS, afbeeldingen)
- API-calls
- Health checks van cloudservices
- Azure load balancer-probes

Dit veroorzaakt:
- Overmatig veel Azure Key Vault-calls
- Onnodige cryptografische bewerkingen
- Prestatieverlies
- Hogere Azure-kosten

## Oplossing: nonce-generatie alleen voor responses

Genereer alleen een verse nonce voor HTTP-responses die HTML-pagina's met CSP-headers renderen.

---

## Geoptimaliseerde implementatie

### 1. Maak response-only nonce-middleware

De kern is de nonce **vóór het verzenden van de response** te genereren, niet op elk request.

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
        // Genereer nonce alleen voor HTML-responses
        var originalBodyStream = context.Response.Body;

        try
        {
            // Response onderscheppen
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Pipeline vervolgen
            await _next(context);

            // Check of dit een HTML-response is die nonce nodig heeft
            if (ShouldGenerateNonce(context))
            {
                // Verse nonce genereren
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // Opslaan in context voor CSP-headergeneratie
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("Generated nonce for response: {Path}", context.Request.Path);
            }

            // Response terugkopiëren
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
        // Alleen voor succesvolle HTML-responses
        if (context.Response.StatusCode != 200)
            return false;

        // Content type controleren
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // Alleen HTML-responses hebben nonces nodig
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Vereenvoudigde request nonce-middleware (bestaande hergebruiken)

Houd bestaande nonce voor duur van het request:

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
        // BESTAANDE nonce uit catalogus ophalen (geen nieuwe genereren)
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // Als nog geen nonce bestaat (eerste request), gebruik standaard
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = Nonce.GenerateSecureNonce();
            _logger.LogWarning("No nonce available yet, using placeholder");
        }

        // In context opslaan
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## Implementatiestrategie

### Optie 1: filteren op requestpad (eenvoudigst)

Genereer nonce alleen voor Razor Page-requests:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Nonce-generatie overslaan voor statische bestanden en API-calls
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        await _next(context);
        return;
    }

    // Nonce alleen voor paginarequests genereren
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### Optie 2: één nonce per response (aanbevolen)

Genereer nonce in response-pipeline:

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

### Optie 3: lazy nonce-generatie (meest efficiënt)

Genereer alleen wanneer CSP-header wordt opgebouwd:

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

## Prestatieverbeteringen

### Vóór optimalisatie
```
Requests per minuut: 1,000
- Statische bestanden: 700 (70%)
- Health checks: 200 (20%)
- Paginarequests: 100 (10%)

Nonce-generaties: 1,000 (één per request)
Key Vault-calls: 2,000 (IV + Key per nonce)
```

### Ná optimalisatie
```
Requests per minuut: 1,000
- Statische bestanden: 700 (genegeerd)
- Health checks: 200 (genegeerd)
- Paginarequests: 100 (nonce gegenereerd)

Nonce-generaties: 100 (alleen voor pagina's)
Key Vault-calls: 200 (90% reductie!)
```

---

## Aanbevolen oplossing

**Gebruik optie 1 (padfiltering) + caching:**

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

## Testen

### Verifiëren dat optimalisatie werkt

```powershell
# Nonce-generatie monitoren
dotnet run

# Requests doen en logs controleren
Invoke-WebRequest "https://localhost:5001/"              # Moet nonce genereren
Invoke-WebRequest "https://localhost:5001/css/site.css"  # Moet GEEN nonce genereren
Invoke-WebRequest "https://localhost:5001/Privacy"       # Moet nonce genereren
```

### Prestatiemetrics

Logging toevoegen om nonce-generatie te volgen:

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

## Migratiestappen

1. ✅ **Maak backup van huidige `NonceMiddleware.cs`**
2. ✅ **Maak `OptimizedNonceMiddleware.cs`** (nieuw bestand)
3. ✅ **Werk `Program.cs` bij** voor geoptimaliseerde middleware
4. ✅ **Test met statische bestanden**
5. ✅ **Test met paginarequests**
6. ✅ **Monitor Azure Key Vault-metrics**
7. ✅ **Verwijder oude middleware na verificatie**

---

## Verwachte resultaten

- **90% reductie** in nonce-generaties
- **90% reductie** in Azure Key Vault-calls
- **Snellere responses** voor statische content
- **Lagere Azure-kosten**
- **Zelfde securityniveau** voor HTML-pagina's

---

## Configuratie

Instelling toevoegen om gedrag te sturen:

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

## Volgende stappen

1. Implementeer `OptimizedNonceMiddleware.cs`
2. Werk middleware-registratie in `Program.cs` bij
3. Test en verifieer reductie in Key Vault-calls
4. Monitor applicatielogs
5. Verwijder oude middleware wanneer tevreden

Wil je dat ik de geoptimaliseerde middleware nu implementeer?
