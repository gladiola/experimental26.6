# Nonce-Generering Optimaliseringsgids

## Huidige Probleem

Die nonce word tans **by elke HTTP-versoek** gegenereer, insluitend:
- Statiese lêerversoeke (CSS, JS, beelde)
- API-aanroepe
- Wolk-diensgesondheidskontroles
- Azure-taakverdeler-peilings

Dit veroorsaak:
- Oortollige Azure Key Vault-aanroepe
- Onnodige kriptografiese bewerkings
- Prestasie-agteruitgang
- Verhoogde Azure-koste

## Oplossing: Respons-Slegs Nonce-Generering

Genereer 'n vars nonce **slegs vir HTTP-responsse wat HTML-bladsye met CSP-opskrifte sal weergee**.

---

## Geoptimaliseerde Implementasie

### 1. Skep Respons-Slegs Nonce-Middleware

Die sleutel is om die nonce **voor die stuur van die respons** te genereer, nie by elke versoek nie.

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
        // Genereer slegs nonce vir HTML-responsse
        var originalBodyStream = context.Response.Body;

        try
        {
            // Onderskep respons
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Gaan voort met pyplyn
            await _next(context);

            // Kontroleer of dit 'n HTML-respons is wat 'n nonce benodig
            if (ShouldGenerateNonce(context))
            {
                // Genereer vars nonce
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // Stoor in konteks vir CSP-opskrif-generering
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("Generated nonce for response: {Path}", context.Request.Path);
            }

            // Kopieer respons terug
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
        // Genereer slegs vir suksesvolle HTML-responsse
        if (context.Response.StatusCode != 200)
            return false;

        // Kontroleer inhoudstype
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // Slegs HTML-responsse benodig nonces
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Vereenvoudigde Versoek-Nonce-Middleware (Hergebruik Bestaande)

Hou bestaande nonce vir die versoekeduur:

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
        // Kry BESTAANDE nonce van katalogus (genereer nie nuwe een nie)
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // As geen nonce nog bestaan nie (eerste versoek), gebruik 'n standaard
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("No nonce available yet, using placeholder");
        }

        // Stoor in konteks
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## Implementasiestrategie

### Opsie 1: Filter na Versoekpad (Eenvoudigste)

Genereer slegs nonce vir Razor Page-versoeke:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Slaan nonce-generering oor vir statiese lêers en API-aanroepe
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        // Gaan net deur sonder om nonce te genereer
        await _next(context);
        return;
    }

    // Genereer nonce slegs vir bladsyversoeke
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### Opsie 2: Een Nonce Per Respons (Aanbeveel)

Genereer nonce in responspyplyn:

```csharp
app.Use(async (context, next) =>
{
    // Koppel in OnStarting-geleentheid (loop voor opskrifte gestuur word)
    context.Response.OnStarting(async () =>
    {
        // Genereer slegs vir HTML-responsse
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

### Opsie 3: Lui Nonce-Generering (Mees Doeltreffend)

Genereer slegs wanneer CSP-opskrif gebou word:

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
        // Kontroleer of huidige nonce nog geldig is
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        // Genereer nuwe nonce
        await _lock.WaitAsync();
        try
        {
            // Dubbelkontroleer na die verkryging van die slot
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

            // Genereer vars nonce
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

## Prestasieverbetering

### Voor Optimalisering
```
Versoeke per minuut: 1 000
- Statiese lêers: 700 (70%)
- Gesondheidskontroles: 200 (20%)
- Bladsyversoeke: 100 (10%)

Nonce-generings: 1 000 (een per versoek)
Key Vault-aanroepe: 2 000 (IV + Sleutel per nonce)
```

### Na Optimalisering
```
Versoeke per minuut: 1 000
- Statiese lêers: 700 (geïgnoreer)
- Gesondheidskontroles: 200 (geïgnoreer)
- Bladsyversoeke: 100 (nonce gegenereer)

Nonce-generings: 100 (slegs vir bladsye)
Key Vault-aanroepe: 200 (90% vermindering!)
```

---

## Aanbevole Oplossing

**Gebruik Opsie 1 (Padfiltering) + Berging:**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // Paaie wat NIE nonce-generering moet aktiveer nie
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
        // Kontroleer of versoek geïgnoreer moet word
        if (ShouldIgnoreRequest(context.Request))
        {
            // Gebruik bestaande nonce of plekhouer
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = "static-content-nonce";
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

        // Genereer vars nonce vir bladsyversoeke
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

        // Ignoreer statiese lêerversoeke
        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Ignoreer versoeke met lêeruitbreidings (behalwe .cshtml)
        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## Toetsing

### Verifieer dat Optimalisering Werk

```powershell
# Monitor nonce-generering
dotnet run

# Maak versoeke en kontroleer logs
Invoke-WebRequest "https://localhost:5001/"           # Moet nonce genereer
Invoke-WebRequest "https://localhost:5001/css/site.css"  # Moet NIE nonce genereer nie
Invoke-WebRequest "https://localhost:5001/Privacy"   # Moet nonce genereer
```

### Prestasiemaatstaf

Voeg aantekening by om nonce-generering op te spoor:

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

## Migrasistappe

1. ✅ **Rugsteun huidige NonceMiddleware.cs**
2. ✅ **Skep OptimizedNonceMiddleware.cs** (nuwe lêer)
3. ✅ **Werk Program.cs op** om geoptimaliseerde middleware te gebruik
4. ✅ **Toets met statiese lêers**
5. ✅ **Toets met bladsyversoeke**
6. ✅ **Monitor Azure Key Vault-statistieke**
7. ✅ **Verwyder ou middleware na verifikasie**

---

## Verwagte Resultate

- **90% vermindering** in nonce-generings
- **90% vermindering** in Azure Key Vault-aanroepe
- **Vinniger respons**-tye vir statiese inhoud
- **Laer Azure-koste**
- **Dieselfde sekuriteit** vir HTML-bladsye

---

## Konfigurasie

Voeg instelling by om gedrag te beheer:

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

## Volgende Stappe

1. Implementeer `OptimizedNonceMiddleware.cs`
2. Werk `Program.cs` middleware-registrasie op
3. Toets en verifieer vermindering in Key Vault-aanroepe
4. Monitor toepassingslogs
5. Verwyder ou middleware wanneer tevrede
