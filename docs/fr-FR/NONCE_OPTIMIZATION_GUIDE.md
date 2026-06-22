# Guide d'optimisation de la génération de nonces

## Problème actuel

Le nonce est actuellement généré **à chaque requête HTTP**, y compris pour :
- Les fichiers statiques (CSS, JS, images)
- Les appels d'API
- Les vérifications de santé des services cloud
- Les sondes de l'équilibreur de charge Azure

Cela entraîne :
- Des appels Azure Key Vault excessifs
- Des opérations cryptographiques inutiles
- Une dégradation des performances
- Des coûts Azure accrus

## Solution : Génération de nonces uniquement pour les réponses

Générer un nouveau nonce **uniquement pour les réponses HTTP qui renverront des pages HTML** avec des en-têtes CSP.

---

## Implémentation optimisée

### 1. Créer un middleware de nonce pour les réponses uniquement

La clé est de générer le nonce **avant d'envoyer la réponse**, pas à chaque requête.

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
        // Générer un nonce uniquement pour les réponses HTML
        var originalBodyStream = context.Response.Body;

        try
        {
            // Intercepter la réponse
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Continuer le pipeline
            await _next(context);

            // Vérifier si la réponse est HTML et nécessite un nonce
            if (ShouldGenerateNonce(context))
            {
                // Générer un nouveau nonce
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // Stocker dans le contexte pour la génération de l'en-tête CSP
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("Nonce généré pour la réponse : {Path}", context.Request.Path);
            }

            // Copier la réponse en retour
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
        // Générer uniquement pour les réponses HTML réussies
        if (context.Response.StatusCode != 200)
            return false;

        // Vérifier le type de contenu
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // Seules les réponses HTML ont besoin de nonces
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Middleware de nonce pour les requêtes simplifié (réutiliser l'existant)

Conserver le nonce existant pour la durée de la requête :

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
        // Obtenir le nonce EXISTANT du catalogue (ne pas en générer un nouveau)
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // Si aucun nonce n'existe encore (première requête), utiliser un par défaut
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("Aucun nonce disponible, utilisation du placeholder");
        }

        // Stocker dans le contexte
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## Stratégie d'implémentation

### Option 1 : Filtrage par chemin de requête (la plus simple)

Générer un nonce uniquement pour les requêtes de pages Razor :

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Ignorer la génération de nonces pour les fichiers statiques et les appels d'API
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        // Passer sans générer de nonce
        await _next(context);
        return;
    }

    // Générer un nonce uniquement pour les requêtes de pages
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### Option 2 : Un nonce par réponse (recommandé)

Générer un nonce dans le pipeline de réponse :

```csharp
app.Use(async (context, next) =>
{
    // S'accrocher à l'événement OnStarting (s'exécute avant l'envoi des en-têtes)
    context.Response.OnStarting(async () =>
    {
        // Générer uniquement pour les réponses HTML
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

### Option 3 : Génération de nonce paresseux (la plus efficace)

Générer uniquement lors de la construction de l'en-tête CSP :

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
        // Vérifier si le nonce actuel est encore valide
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        // Générer un nouveau nonce
        await _lock.WaitAsync();
        try
        {
            // Double vérification après acquisition du verrou
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

            // Générer un nouveau nonce
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

## Améliorations de performances

### Avant optimisation
```
Requêtes par minute : 1 000
- Fichiers statiques : 700 (70 %)
- Vérifications de santé : 200 (20 %)
- Requêtes de pages : 100 (10 %)

Générations de nonces : 1 000 (une par requête)
Appels Key Vault : 2 000 (IV + Clé par nonce)
```

### Après optimisation
```
Requêtes par minute : 1 000
- Fichiers statiques : 700 (ignorés)
- Vérifications de santé : 200 (ignorées)
- Requêtes de pages : 100 (nonce généré)

Générations de nonces : 100 (uniquement pour les pages)
Appels Key Vault : 200 (réduction de 90 % !)
```

---

## Solution recommandée

**Utiliser l'Option 1 (filtrage par chemin) + mise en cache :**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // Chemins qui ne doivent PAS déclencher la génération de nonces
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
        // Vérifier si la requête doit être ignorée
        if (ShouldIgnoreRequest(context.Request))
        {
            // Utiliser le nonce existant ou un placeholder
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = "static-content-nonce";
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

        // Générer un nouveau nonce pour les requêtes de pages
        _logger.LogDebug("Génération du nonce pour : {Path}", context.Request.Path);
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

        // Ignorer les requêtes de fichiers statiques
        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Ignorer les requêtes avec des extensions de fichiers (sauf .cshtml)
        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## Tests

### Vérifier que l'optimisation fonctionne

```powershell
# Surveiller la génération de nonces
dotnet run

# Effectuer des requêtes et vérifier les journaux
Invoke-WebRequest "https://localhost:5001/"              # Doit générer un nonce
Invoke-WebRequest "https://localhost:5001/css/site.css" # Ne doit PAS générer de nonce
Invoke-WebRequest "https://localhost:5001/Privacy"      # Doit générer un nonce
```

### Métriques de performance

Ajouter de la journalisation pour suivre la génération de nonces :

```csharp
private static int _nonceGenerationCount = 0;

public async Task InvokeAsync(HttpContext context)
{
    if (ShouldIgnoreRequest(context.Request))
    {
        _logger.LogTrace("Nonce ignoré pour : {Path}", context.Request.Path);
        // ...
    }
    else
    {
        Interlocked.Increment(ref _nonceGenerationCount);
        _logger.LogInformation("Nonce #{Count} généré pour : {Path}",
            _nonceGenerationCount, context.Request.Path);
        // ...
    }
}
```

---

## Étapes de migration

1. ✅ **Sauvegarder le NonceMiddleware.cs actuel**
2. ✅ **Créer OptimizedNonceMiddleware.cs** (nouveau fichier)
3. ✅ **Mettre à jour Program.cs** pour utiliser le middleware optimisé
4. ✅ **Tester avec les fichiers statiques**
5. ✅ **Tester avec les requêtes de pages**
6. ✅ **Surveiller les métriques Azure Key Vault**
7. ✅ **Supprimer l'ancien middleware après vérification**

---

## Résultats attendus

- **Réduction de 90 %** des générations de nonces
- **Réduction de 90 %** des appels Azure Key Vault
- **Temps de réponse plus rapides** pour le contenu statique
- **Coûts Azure réduits**
- **Même niveau de sécurité** pour les pages HTML

---

## Configuration

Ajouter un paramètre pour contrôler le comportement :

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

## Prochaines étapes

1. Implémenter `OptimizedNonceMiddleware.cs`
2. Mettre à jour l'enregistrement du middleware dans `Program.cs`
3. Tester et vérifier la réduction des appels Key Vault
4. Surveiller les journaux de l'application
5. Supprimer l'ancien middleware quand satisfait

Voulez-vous que j'implémente maintenant le middleware optimisé ?
