# Руководство по оптимизации генерации nonce

## Текущая проблема

Nonce в настоящее время генерируется **при каждом HTTP-запросе**, включая:
- Запросы статических файлов (CSS, JS, изображения)
- API-вызовы
- Проверки работоспособности облачных сервисов
- Запросы балансировщиков нагрузки Azure

Это приводит к:
- Избыточным вызовам Azure Key Vault
- Ненужным криптографическим операциям
- Снижению производительности
- Увеличению затрат на Azure

## Решение: генерация nonce только при рендеринге ответов

Генерируйте новый nonce **только для HTTP-ответов, отображающих HTML-страницы** с заголовками CSP.

---

## Оптимизированная реализация

### 1. Middleware генерации nonce только для ответов

Ключевой момент: nonce следует генерировать **перед отправкой ответа**, а не при каждом запросе.

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

            // Генерировать nonce только для HTML-ответов
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

### 2. Упрощённое middleware запроса (переиспользование существующего nonce)

Сохраняйте существующий nonce на время запроса:

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
        // Получить СУЩЕСТВУЮЩИЙ nonce из каталога (без генерации нового)
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

## Стратегии реализации

### Вариант 1: Фильтрация по пути запроса (простейший)

Генерировать nonce только для запросов Razor Pages:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Пропускать статические файлы и API-вызовы
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        await _next(context);
        return;
    }

    // Генерировать nonce только для страниц
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### Вариант 2: Один nonce на ответ (рекомендуется)

Генерация nonce в pipeline ответа:

```csharp
app.Use(async (context, next) =>
{
    // Подключение к событию OnStarting (выполняется до отправки заголовков)
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

### Вариант 3: Ленивая генерация nonce (наиболее эффективный)

Генерировать nonce только при формировании CSP-заголовка:

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

## Улучшение производительности

### До оптимизации
```
Запросов в минуту: 1 000
- Статические файлы: 700 (70%)
- Проверки работоспособности: 200 (20%)
- Запросы страниц: 100 (10%)

Генераций nonce: 1 000 (один на запрос)
Вызовов Key Vault: 2 000 (IV + Key на каждый nonce)
```

### После оптимизации
```
Запросов в минуту: 1 000
- Статические файлы: 700 (игнорируются)
- Проверки работоспособности: 200 (игнорируются)
- Запросы страниц: 100 (генерируется nonce)

Генераций nonce: 100 (только для страниц)
Вызовов Key Vault: 200 (снижение на 90%!)
```

---

## Рекомендованное решение

**Используйте Вариант 1 (фильтрация по пути) + кэширование:**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // Пути, для которых НЕ нужна генерация nonce
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
                return true;
        }

        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
```

---

## Тестирование

### Проверка работы оптимизации

```powershell
dotnet run

# Сделать запросы и проверить журналы
Invoke-WebRequest "https://localhost:5001/"               # Должен генерировать nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"   # НЕ должен генерировать nonce
Invoke-WebRequest "https://localhost:5001/Privacy"        # Должен генерировать nonce
```

### Метрики производительности

Добавьте логирование для отслеживания генерации nonce:

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

## Этапы миграции

1. ✅ **Создать резервную копию NonceMiddleware.cs**
2. ✅ **Создать OptimizedNonceMiddleware.cs** (новый файл)
3. ✅ **Обновить Program.cs** для использования оптимизированного middleware
4. ✅ **Протестировать со статическими файлами**
5. ✅ **Протестировать с запросами страниц**
6. ✅ **Отслеживать метрики Azure Key Vault**
7. ✅ **Удалить старый middleware после проверки**

---

## Ожидаемые результаты

- **Снижение на 90%** числа генераций nonce
- **Снижение на 90%** вызовов Azure Key Vault
- **Ускорение** времени ответа для статического контента
- **Снижение затрат** на Azure
- **Та же безопасность** для HTML-страниц

---

## Конфигурация

Добавьте параметр для управления поведением:

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

## Следующие шаги

1. Реализовать `OptimizedNonceMiddleware.cs`
2. Обновить регистрацию middleware в `Program.cs`
3. Проверить и убедиться в снижении вызовов Key Vault
4. Отслеживать журналы приложения
5. Удалить старый middleware по завершении проверки
