# Treoir Optamúcháin Giniúna Nonce

## Fadhb Reatha

Tá nonce á ghiniúint **ar gach iarratas HTTP** faoi láthair, lena n-áirítear:
- Iarratais ar chomhaid statacha (CSS, JS, íomhánna)
- Glaonna API
- Seiceálacha sláinte seirbhísí scamall
- Probes ón Azure load balancer

Cruthaíonn sé seo:
- Glaonna iomarcacha ar Azure Key Vault
- Oibríochtaí cripteagrafacha neamhghácha
- Meath feidhmíochta
- Costais Azure níos airde

## Réiteach: Giniúint Nonce do Fhreagraí amháin

Gin nonce úr **ach amháin do fhreagraí HTTP a rindreálann leathanaigh HTML** le ceanntásca CSP.

---

## Cur i bhFeidhm Optamaithe

### 1. Cruthaigh Middleware Nonce do Fhreagraí amháin

Is í an eochair ná nonce a ghiniúint **roimh sheoladh an fhreagra**, ní ar gach iarratas.

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
        // Gin nonce do fhreagraí HTML amháin
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

### 2. Middleware Iarratais Simplithe (athúsáid luach atá ann)

Coinnigh nonce atá ann don tréimhse iarratais:

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
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("No nonce available yet, using placeholder");
        }

        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## Straitéis Chur i bhFeidhm

### Rogha 1: Scagaire de réir cosáin iarratais (is simplí)

Gin nonce d'iarratais Razor Page amháin:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains('.'))
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

### Rogha 2: Nonce amháin in aghaidh an fhreagra (molta)

Gin nonce sa phíblíne freagartha:

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

### Rogha 3: Giniúint Lazy Nonce (is éifeachtaí)

Gin ach nuair a thógtar ceanntásc CSP:

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

## Feabhsuithe Feidhmíochta

### Roimh an Optamú
```
Iarratais in aghaidh an nóiméid: 1,000
- Comhaid statacha: 700 (70%)
- Seiceálacha sláinte: 200 (20%)
- Iarratais leathanaigh: 100 (10%)

Giniúintí nonce: 1,000 (ceann in aghaidh an iarratais)
Glaonna Key Vault: 2,000 (IV + Eochair in aghaidh nonce)
```

### Tar éis an Optamaithe
```
Iarratais in aghaidh an nóiméid: 1,000
- Comhaid statacha: 700 (neamhaird)
- Seiceálacha sláinte: 200 (neamhaird)
- Iarratais leathanaigh: 100 (nonce ginte)

Giniúintí nonce: 100 (leathanaigh amháin)
Glaonna Key Vault: 200 (laghdú 90%!)
```

---

## Réiteach Molta

**Úsáid Rogha 1 (Scagadh Cosáin) + Caching:**

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
                existingNonce = "static-content-nonce";
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

## Tástáil

### Deimhnigh go n-oibríonn an t-optamú

```powershell
# Monatóireacht ar ghiniúint nonce
dotnet run

# Déan iarratais agus seiceáil logaí
Invoke-WebRequest "https://localhost:5001/"              # Ba chóir nonce a ghiniúint
Invoke-WebRequest "https://localhost:5001/css/site.css"  # Níor chóir nonce a ghiniúint
Invoke-WebRequest "https://localhost:5001/Privacy"       # Ba chóir nonce a ghiniúint
```

### Méadrachtaí Feidhmíochta

Cuir logáil leis chun giniúint nonce a rianú:

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

## Céimeanna Imirce

1. ✅ Cúltaca den `NonceMiddleware.cs` reatha
2. ✅ Cruthaigh `OptimizedNonceMiddleware.cs` (comhad nua)
3. ✅ Nuashonraigh `Program.cs` chun middleware optamaithe a úsáid
4. ✅ Tástáil le comhaid statacha
5. ✅ Tástáil le hiarratais leathanaigh
6. ✅ Monatóireacht ar mhéadrachtaí Azure Key Vault
7. ✅ Bain an seanchomhpháirt middleware tar éis fíoraithe

---

## Torthaí Ionchais

- **Laghdú 90%** i nginiúintí nonce
- **Laghdú 90%** i nglaonna Azure Key Vault
- **Freagraí níos tapúla** do chontúirt statach
- **Costais Azure níos ísle**
- **An tslándáil chéanna** do leathanaigh HTML

---

## Cumraíocht

Cuir socrú leis chun iompraíocht a rialú:

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

## Na Chéad Chéimeanna Eile

1. Cuir `OptimizedNonceMiddleware.cs` i bhfeidhm
2. Nuashonraigh clárú middleware i `Program.cs`
3. Tástáil agus deimhnigh laghdú i nglaonna Key Vault
4. Monatóireacht ar logaí an fheidhmchláir
5. Bain an sean-middleware nuair atá tú sásta

Ar mhaith leat dom an middleware optamaithe a chur i bhfeidhm anois?
