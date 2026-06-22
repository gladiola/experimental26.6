# নন্স জেনারেশন অপ্টিমাইজেশন গাইড

## বর্তমান সমস্যা

নন্স বর্তমানে **প্রতিটি HTTP অনুরোধে** তৈরি হচ্ছে, যার মধ্যে রয়েছে:
- স্ট্যাটিক ফাইল অনুরোধ (CSS, JS, ইমেজ)
- API কল
- ক্লাউড সার্ভিস হেলথ চেক
- Azure লোড ব্যালান্সার প্রোব

এর ফলে:
- অতিরিক্ত Azure Key Vault কল
- অপ্রয়োজনীয় ক্রিপ্টোগ্রাফিক অপারেশন
- পারফরম্যান্স অবনতি
- বর্ধিত Azure খরচ

## সমাধান: শুধুমাত্র রেসপন্সের জন্য নন্স জেনারেশন

শুধুমাত্র **CSP হেডার সহ HTML পেজ রেন্ডার করবে এমন HTTP রেসপন্সের জন্য** একটি নতুন নন্স তৈরি করুন।

---

## অপ্টিমাইজড বাস্তবায়ন

### ১. রেসপন্স-শুধুমাত্র নন্স মিডলওয়্যার তৈরি করুন

মূল বিষয় হল প্রতিটি অনুরোধে নয়, **রেসপন্স পাঠানোর আগে** নন্স তৈরি করা।

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
        // শুধুমাত্র HTML রেসপন্সের জন্য নন্স তৈরি করুন
        var originalBodyStream = context.Response.Body;

        try
        {
            // রেসপন্স ইন্টারসেপ্ট করুন
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // পাইপলাইন চালিয়ে যান
            await _next(context);

            // এটি নন্স প্রয়োজন এমন HTML রেসপন্স কিনা পরীক্ষা করুন
            if (ShouldGenerateNonce(context))
            {
                // নতুন নন্স তৈরি করুন
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");

                // CSP হেডার জেনারেশনের জন্য কনটেক্সটে সংরক্ষণ করুন
                context.Items["Nonce"] = nonce;

                _logger.LogDebug("রেসপন্সের জন্য নন্স তৈরি হয়েছে: {Path}", context.Request.Path);
            }

            // রেসপন্স ফিরিয়ে কপি করুন
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
        // শুধুমাত্র সফল HTML রেসপন্সের জন্য তৈরি করুন
        if (context.Response.StatusCode != 200)
            return false;

        // কন্টেন্ট টাইপ পরীক্ষা করুন
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // শুধুমাত্র HTML রেসপন্সে নন্স প্রয়োজন
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### ২. সরলীকৃত রিকোয়েস্ট নন্স মিডলওয়্যার (বিদ্যমান পুনরায় ব্যবহার করুন)

অনুরোধের সময়কালের জন্য বিদ্যমান নন্স রাখুন:

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
        // ক্যাটালগ থেকে বিদ্যমান নন্স পান (নতুন তৈরি করবেন না)
        var nonce = _nonceCatalogService.GetANonce("CSPNonce");

        // যদি এখনো কোনো নন্স না থাকে (প্রথম অনুরোধ), একটি ডিফল্ট ব্যবহার করুন
        if (string.IsNullOrEmpty(nonce))
        {
            nonce = "initial-nonce-placeholder";
            _logger.LogWarning("এখনো কোনো নন্স পাওয়া যায়নি, প্লেসহোল্ডার ব্যবহার করা হচ্ছে");
        }

        // কনটেক্সটে সংরক্ষণ করুন
        context.Items["Nonce"] = nonce;

        await _next(context);
    }
}
```

---

## বাস্তবায়ন কৌশল

### বিকল্প ১: রিকোয়েস্ট পাথ দ্বারা ফিল্টার (সবচেয়ে সহজ)

শুধুমাত্র Razor Page অনুরোধের জন্য নন্স তৈরি করুন:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // স্ট্যাটিক ফাইল এবং API কলের জন্য নন্স জেনারেশন এড়িয়ে যান
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib") ||
        context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.Value.Contains("."))
    {
        // নন্স তৈরি না করে পাস করুন
        await _next(context);
        return;
    }

    // শুধুমাত্র পেজ অনুরোধের জন্য নন্স তৈরি করুন
    await _nonceRefresherService.RefreshNonceAsync();
    var nonce = _nonceCatalogService.GetANonce("CSPNonce");
    context.Items["Nonce"] = nonce;

    await _next(context);
}
```

### বিকল্প ২: প্রতি রেসপন্সে একটি নন্স (প্রস্তাবিত)

রেসপন্স পাইপলাইনে নন্স তৈরি করুন:

```csharp
app.Use(async (context, next) =>
{
    // OnStarting ইভেন্টে হুক করুন (হেডার পাঠানোর আগে চলে)
    context.Response.OnStarting(async () =>
    {
        // শুধুমাত্র HTML রেসপন্সের জন্য তৈরি করুন
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

### বিকল্প ৩: লেজি নন্স জেনারেশন (সবচেয়ে দক্ষ)

CSP হেডার তৈরি হওয়ার সময় শুধুমাত্র তৈরি করুন:

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
        // বর্তমান নন্স এখনও বৈধ কিনা পরীক্ষা করুন
        if (!string.IsNullOrEmpty(_currentNonce) &&
            DateTime.UtcNow - _lastGenerated < _nonceLifetime)
        {
            return _currentNonce;
        }

        // নতুন নন্স তৈরি করুন
        await _lock.WaitAsync();
        try
        {
            // লক পাওয়ার পরে পুনরায় পরীক্ষা করুন
            if (!string.IsNullOrEmpty(_currentNonce) &&
                DateTime.UtcNow - _lastGenerated < _nonceLifetime)
            {
                return _currentNonce;
            }

            // নতুন নন্স তৈরি করুন
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

## পারফরম্যান্স উন্নতি

### অপ্টিমাইজেশনের আগে
```
প্রতি মিনিটে অনুরোধ: ১,০০০
- স্ট্যাটিক ফাইল: ৭০০ (৭০%)
- হেলথ চেক: ২০০ (২০%)
- পেজ অনুরোধ: ১০০ (১০%)

নন্স জেনারেশন: ১,০০০ (প্রতি অনুরোধে একটি)
Key Vault কল: ২,০০০ (প্রতি নন্সে IV + Key)
```

### অপ্টিমাইজেশনের পরে
```
প্রতি মিনিটে অনুরোধ: ১,০০০
- স্ট্যাটিক ফাইল: ৭০০ (উপেক্ষিত)
- হেলথ চেক: ২০০ (উপেক্ষিত)
- পেজ অনুরোধ: ১০০ (নন্স তৈরি হয়েছে)

নন্স জেনারেশন: ১০০ (শুধুমাত্র পেজের জন্য)
Key Vault কল: ২০০ (৯০% হ্রাস!)
```

---

## প্রস্তাবিত সমাধান

**বিকল্প ১ (পাথ ফিল্টারিং) + ক্যাশিং ব্যবহার করুন:**

```csharp
public class OptimizedNonceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly INonceRefresherService _nonceRefresherService;
    private readonly INonceCatalogService _nonceCatalogService;
    private readonly ILogger<OptimizedNonceMiddleware> _logger;

    // পাথগুলি যা নন্স জেনারেশন ট্রিগার করবে না
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
        // অনুরোধটি উপেক্ষা করা উচিত কিনা পরীক্ষা করুন
        if (ShouldIgnoreRequest(context.Request))
        {
            // বিদ্যমান নন্স বা প্লেসহোল্ডার ব্যবহার করুন
            var existingNonce = _nonceCatalogService.GetANonce("CSPNonce");
            if (string.IsNullOrEmpty(existingNonce))
            {
                existingNonce = "static-content-nonce";
            }
            context.Items["Nonce"] = existingNonce;
            await _next(context);
            return;
        }

        // পেজ অনুরোধের জন্য নতুন নন্স তৈরি করুন
        _logger.LogDebug("{Path}-এর জন্য নন্স তৈরি হচ্ছে", context.Request.Path);
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

        // স্ট্যাটিক ফাইল অনুরোধ উপেক্ষা করুন
        foreach (var ignorePath in IgnorePaths)
        {
            if (path.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // ফাইল এক্সটেনশন সহ অনুরোধ উপেক্ষা করুন (.cshtml ছাড়া)
        if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

---

## পরীক্ষা করা

### অপ্টিমাইজেশন কাজ করছে কিনা যাচাই করুন

```powershell
# নন্স জেনারেশন মনিটর করুন
dotnet run

# অনুরোধ করুন এবং লগ পরীক্ষা করুন
Invoke-WebRequest "https://localhost:5001/"           # নন্স তৈরি হওয়া উচিত
Invoke-WebRequest "https://localhost:5001/css/site.css"  # নন্স তৈরি হওয়া উচিত নয়
Invoke-WebRequest "https://localhost:5001/Privacy"   # নন্স তৈরি হওয়া উচিত
```

### পারফরম্যান্স মেট্রিক্স

নন্স জেনারেশন ট্র্যাক করতে লগিং যোগ করুন:

```csharp
private static int _nonceGenerationCount = 0;

public async Task InvokeAsync(HttpContext context)
{
    if (ShouldIgnoreRequest(context.Request))
    {
        _logger.LogTrace("{Path}-এর জন্য নন্স এড়িয়ে যাওয়া হয়েছে", context.Request.Path);
        // ...
    }
    else
    {
        Interlocked.Increment(ref _nonceGenerationCount);
        _logger.LogInformation("{Path}-এর জন্য নন্স #{Count} তৈরি হয়েছে",
            _nonceGenerationCount, context.Request.Path);
        // ...
    }
}
```

---

## মাইগ্রেশন ধাপসমূহ

1. ? **বর্তমান NonceMiddleware.cs ব্যাকআপ করুন**
2. ? **OptimizedNonceMiddleware.cs তৈরি করুন** (নতুন ফাইল)
3. ? **অপ্টিমাইজড মিডলওয়্যার ব্যবহার করতে Program.cs আপডেট করুন**
4. ? **স্ট্যাটিক ফাইল দিয়ে পরীক্ষা করুন**
5. ? **পেজ অনুরোধ দিয়ে পরীক্ষা করুন**
6. ? **Azure Key Vault মেট্রিক্স মনিটর করুন**
7. ? **যাচাইয়ের পরে পুরানো মিডলওয়্যার সরিয়ে দিন**

---

## প্রত্যাশিত ফলাফল

- নন্স জেনারেশনে **৯০% হ্রাস**
- Azure Key Vault কলে **৯০% হ্রাস**
- স্ট্যাটিক কন্টেন্টের জন্য **দ্রুত রেসপন্স** সময়
- **কম Azure খরচ**
- HTML পেজের জন্য **একই নিরাপত্তা**

---

## কনফিগারেশন

আচরণ নিয়ন্ত্রণ করতে সেটিং যোগ করুন:

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

## পরবর্তী পদক্ষেপসমূহ

1. `OptimizedNonceMiddleware.cs` বাস্তবায়ন করুন
2. মিডলওয়্যার নিবন্ধনের জন্য `Program.cs` আপডেট করুন
3. Key Vault কলে হ্রাস পরীক্ষা করুন এবং যাচাই করুন
4. অ্যাপ্লিকেশন লগ মনিটর করুন
5. সন্তুষ্ট হলে পুরানো মিডলওয়্যার সরিয়ে দিন
