# دليل تحسين توليد Nonce

## المشكلة الحالية

يُولَّد nonce حالياً **عند كل طلب HTTP**، بما في ذلك:
- طلبات الملفات الثابتة (CSS وJS والصور)
- استدعاءات API
- فحوصات صحة الخدمة السحابية
- مجسّات موازن تحميل Azure

يتسبب ذلك في:
- استدعاءات مفرطة لـ Azure Key Vault
- عمليات تشفير غير ضرورية
- تدهور الأداء
- ارتفاع تكاليف Azure

## الحل: توليد Nonce للاستجابات فقط

ولّد nonce جديداً **فقط لاستجابات HTTP التي ستعرض صفحات HTML** مع رؤوس CSP.

---

## التنفيذ المُحسَّن

### 1. إنشاء Middleware خاص بالاستجابات لـ Nonce

المفتاح هو توليد nonce **قبل إرسال الاستجابة**، وليس عند كل طلب.

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
        // توليد nonce فقط لاستجابات HTML
        var originalBodyStream = context.Response.Body;

        try
        {
            // اعتراض الاستجابة
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // متابعة المسار
            await _next(context);

            // التحقق مما إذا كانت هذه استجابة HTML تحتاج nonce
            if (ShouldGenerateNonce(context))
            {
                // توليد nonce جديد
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // التخزين في السياق لتوليد رأس CSP
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("Generated nonce for response: {Path}", context.Request.Path);
            }

            // نسخ الاستجابة مجدداً
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
        // التوليد فقط للاستجابات HTML الناجحة
        if (context.Response.StatusCode != 200)
            return false;

        // التحقق من نوع المحتوى
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // استجابات HTML فقط تحتاج nonce
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Middleware مبسّط لطلب Nonce (إعادة استخدام الموجود)

الإبقاء على nonce الحالي طوال مدة الطلب:

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
        // الحصول على nonce الموجود من الكتالوج (عدم توليد جديد)
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // إذا لم يوجد nonce بعد (الطلب الأول)، استخدم افتراضياً
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("No nonce available yet, using placeholder");
        }

        // التخزين في السياق
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## استراتيجية التنفيذ

### الخيار 1: التصفية حسب مسار الطلب (الأبسط)

توليد nonce فقط لطلبات Razor Page:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // تخطي توليد nonce للملفات الثابتة واستدعاءات API
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        // تمرير بدون توليد nonce
        await _next(context);
        return;
    }

    // توليد nonce فقط لطلبات الصفحات
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### الخيار 2: nonce واحد لكل استجابة (موصى به)

توليد nonce في مسار الاستجابة:

```csharp
app.Use(async (context, next) =>
{
    // الربط بحدث OnStarting (يعمل قبل إرسال الرؤوس)
    context.Response.OnStarting(async () =>
    {
        // التوليد فقط لاستجابات HTML
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

### الخيار 3: توليد Nonce الكسول (الأكثر كفاءة)

التوليد فقط عند بناء رأس CSP:

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
        // التحقق من أن nonce الحالي لا يزال صالحاً
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        // توليد nonce جديد
        await _lock.WaitAsync();
        try
        {
            // إعادة التحقق بعد الحصول على القفل
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

            // توليد nonce جديد
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

## تحسينات الأداء

### قبل التحسين
```
الطلبات في الدقيقة: 1,000
- الملفات الثابتة: 700 (70%)
- فحوصات الصحة: 200 (20%)
- طلبات الصفحات: 100 (10%)

توليد nonce: 1,000 (واحد لكل طلب)
استدعاءات Key Vault: 2,000 (IV + مفتاح لكل nonce)
```

### بعد التحسين
```
الطلبات في الدقيقة: 1,000
- الملفات الثابتة: 700 (تُتجاهل)
- فحوصات الصحة: 200 (تُتجاهل)
- طلبات الصفحات: 100 (يُولَّد nonce)

توليد nonce: 100 (للصفحات فقط)
استدعاءات Key Vault: 200 (تقليص بنسبة 90%!)
```

---

## الحل الموصى به

**استخدام الخيار 1 (تصفية المسار) + التخزين المؤقت:**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // المسارات التي لا يجب أن تُشغّل توليد nonce
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
        // التحقق مما إذا كان يجب تجاهل الطلب
        if (ShouldIgnoreRequest(context.Request))
        {
            // استخدام nonce الموجود أو عنصر نائب
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = "static-content-nonce";
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

        // توليد nonce جديد لطلبات الصفحات
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

        // تجاهل طلبات الملفات الثابتة
        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // تجاهل الطلبات ذات امتدادات الملفات (باستثناء .cshtml)
        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## الاختبار

### التحقق من عمل التحسين

```powershell
# مراقبة توليد nonce
dotnet run

# إجراء طلبات والتحقق من السجلات
Invoke-WebRequest "https://localhost:5001/"           # يجب توليد nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"  # يجب ألا يُولَّد nonce
Invoke-WebRequest "https://localhost:5001/Privacy"   # يجب توليد nonce
```

### مقاييس الأداء

إضافة تسجيل لتتبع توليد nonce:

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

## خطوات الترحيل

1. ✅ **نسخ احتياطي من NonceMiddleware.cs الحالي**
2. ✅ **إنشاء OptimizedNonceMiddleware.cs** (ملف جديد)
3. ✅ **تحديث Program.cs** لاستخدام middleware المُحسَّن
4. ✅ **اختبار مع الملفات الثابتة**
5. ✅ **اختبار مع طلبات الصفحات**
6. ✅ **مراقبة مقاييس Azure Key Vault**
7. ✅ **إزالة middleware القديم بعد التحقق**

---

## النتائج المتوقعة

- **تقليص بنسبة 90%** في توليد nonce
- **تقليص بنسبة 90%** في استدعاءات Azure Key Vault
- **أوقات استجابة أسرع** للمحتوى الثابت
- **تكاليف Azure أقل**
- **نفس الأمان** لصفحات HTML

---

## التكوين

إضافة إعداد للتحكم في السلوك:

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

## الخطوات التالية

1. تنفيذ `OptimizedNonceMiddleware.cs`
2. تحديث تسجيل middleware في `Program.cs`
3. اختبار والتحقق من تقليص استدعاءات Key Vault
4. مراقبة سجلات التطبيق
5. إزالة middleware القديم عند الاقتناع بالنتائج
