# নিরাপত্তা পর্যালোচনা — WebAppExperimental266

**তারিখ:** ২০২৬-০৫-০৭
**পরিধি:** কোডবেসের সম্পূর্ণ স্ট্যাটিক বিশ্লেষণ (২০২৬-০৫-০৬ পর্যালোচনার ফলো-আপ)
**পর্যালোচক:** স্বয়ংক্রিয় নিরাপত্তা পর্যালোচনা

---

## নির্বাহী সারসংক্ষেপ

এই ফলো-আপ পর্যালোচনা নিশ্চিত করে যে ২০২৬-০৫-০৬ নিরাপত্তা পর্যালোচনায় চিহ্নিত ৫টি দুর্বলতার মধ্যে ৩টি সম্পূর্ণভাবে সমাধান হয়েছে, ১টি আংশিকভাবে সমাধান হয়েছে। পর্যালোচনায় ৪টি নতুন অনুসন্ধানও চিহ্নিত হয়েছে। অ্যাপ্লিকেশনটির সামগ্রিক নিরাপত্তা অবস্থান ক্রমাগত উন্নতি হচ্ছে।

---

## পূর্ববর্তী অনুসন্ধানের স্থিতি (২০২৬-০৫-০৬)

| # | অনুসন্ধান | তীব্রতা | স্থিতি |
|---|---------|----------|--------|
| 20 | NonceRefresherService অব্যবহৃত Key Vault কনস্ট্রাক্টর নির্ভরতা ধরে রাখে | 🟠 উচ্চ | ✅ সমাধান হয়েছে |
| 21 | OcspValidationService-এর অভ্যন্তরীণ ক্যাশে থ্রেড-অনিরাপদ Dictionary ব্যবহার করে | 🟡 মাঝারি | ✅ সমাধান হয়েছে |
| 22 | OCSP যাচাইকরণ স্টাব এখনও বিদ্যমান — fail-closed কিন্তু বাস্তবায়িত নয় | 🔵 নিম্ন | ⚠️ গৃহীত (ডিজাইন অনুযায়ী) |
| 23 | খালি AllowedIssuers সহ mTLS সমস্ত সার্টিফিকেট প্রত্যাখ্যান করে (fail-closed, অনথিভুক্ত) | 🔵 নিম্ন | ✅ সমাধান হয়েছে |
| 24 | OcspSettings.ServerUnavailableBehavior "Warn"-এ ডিফল্ট (ত্রুটিতে পাস করার অনুমতি দেয়) | 🔵 নিম্ন | ⚠️ আংশিকভাবে সমাধান হয়েছে |

---

## পূর্ববর্তী অনুসন্ধানের বিস্তারিত স্থিতি

### ✅ ২০. NonceRefresherService অব্যবহৃত DI নির্ভরতা — সমাধান হয়েছে

**ফাইল:** `Services/NonceRefresherService.cs`

`NonceRefresherService` কনস্ট্রাক্টর এখন শুধুমাত্র `ILogger<NonceRefresherService>`, `ILoggerFactory` এবং `INonceCatalogService` ঘোষণা করে। চারটি পূর্বে অব্যবহৃত নির্ভরতা (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) সরানো হয়েছে। এটি সেই সার্ভিস-অস্বীকার ঝুঁকি সমাধান করে যা `EnableKeyVault = false` (ডিফল্ট) এবং `EnableNonceServices = true` (ডিফল্ট) হলে অ্যাপ্লিকেশনটিকে শুরু হতে বাধা দিত।

---

### ✅ ২১. OcspValidationService থ্রেড-অনিরাপদ ক্যাশে — সমাধান হয়েছে

**ফাইল:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` কে `ConcurrentDictionary<string, CachedOcspResponse>` দিয়ে প্রতিস্থাপিত করা হয়েছে। `_cache.Remove` কলটি `_cache.TryRemove`-এ আপডেট করা হয়েছে। ক্যাশেটি এখন সমান্তরাল অ্যাক্সেসের জন্য নিরাপদ।

---

### ⚠️ ২২. OCSP যাচাইকরণ স্টাব — গৃহীত (ডিজাইন অনুযায়ী)

**ফাইল:** `Services/OcspValidationService.cs`

স্টাবটি এখনও বিদ্যমান কিন্তু সঠিকভাবে fail-closed। যেহেতু `EnableOcspValidation` ডিফল্ট `false`, তাই এটির কোনো উৎপাদন প্রভাব নেই। এটি সম্পূর্ণ OCSP বাস্তবায়ন মুলতুবি থাকা পর্যন্ত একটি তথ্যমূলক অনুসন্ধান হিসেবে গৃহীত হয়েছে।

---

### ✅ ২৩. mTLS খালি AllowedIssuers — সমাধান হয়েছে

**ফাইল:** `Extensions/ServiceCollectionExtensions.cs`

`ValidateClientCertificateIssuer = true` এবং `AllowedIssuers` খালি থাকলে এখন একটি স্টার্টআপ সতর্কতা লগ করা হয়:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

এটি fail-closed আচরণের সম্মুখীন অপারেটরদের স্পষ্ট নির্দেশনা প্রদান করে।

---

### ⚠️ ২৪. OcspSettings.ServerUnavailableBehavior — আংশিকভাবে সমাধান হয়েছে

**ফাইলগুলি:** `appsettings.template.json` (সমাধান হয়েছে), `Models/Settings/OcspSettings.cs` (এখনও সমাধান হয়নি)

টেমপ্লেটটি এখন সঠিকভাবে `"ServerUnavailableBehavior": "Fail"` নির্দিষ্ট করে। তবে, `OcspSettings.cs` (লাইন ৩৯)-এর C# ক্লাস ডিফল্ট `"Warn"` থেকে যায়। যদি কোনো অপারেটর OCSP সক্রিয় করেন এবং তার কনফিগারেশন ফাইল থেকে `ServerUnavailableBehavior` বাদ দেন, তাহলে ক্লাস ডিফল্ট `"Warn"` নীরবে প্রয়োগ হয়, OCSP সার্ভার বিঘ্নে পাস করার অনুমতি দেয়। টেমপ্লেট সুপারিশের সাথে মিলানোর জন্য ক্লাস ডিফল্ট পরিবর্তন করা উচিত।

---

## নতুন অনুসন্ধান

| # | এলাকা | তীব্রতা |
|---|------|----------|
| 25 | OcspSettings ক্লাস ডিফল্ট ("Warn") টেমপ্লেট ("Fail") থেকে বিচ্যুত | 🔵 নিম্ন |
| 26 | NonceCatalogService-এর একক ভাগ করা nonce কী ক্রস-রিকোয়েস্ট nonce সংঘর্ষের অনুমতি দেয় | 🟡 মাঝারি |
| 27 | OptimizedNonceMiddleware স্ট্যাটিক কাউন্টার স্বাক্ষরিত ৩২-বিট পূর্ণসংখ্যা ব্যবহার করে (ওভারফ্লো ঝুঁকি) | 🔵 নিম্ন |
| 28 | Program.cs খালি ILoggerFactory singleton নিবন্ধন করে, ফ্রেমওয়ার্ক লগার ঢেকে দেয় | 🟡 মাঝারি |

---

## 🟡 মাঝারি

### ২৬. NonceCatalogService ভাগ করা Nonce কী ক্রস-রিকোয়েস্ট Nonce সংঘর্ষের অনুমতি দেয়

**ফাইলগুলি:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

nonce ক্যাটালগ একটি একক ভাগ করা কী `"CSPNonce"`-এর অধীনে সমস্ত nonce সংরক্ষণ করে। সমান্তরাল লোডের অধীনে নিম্নলিখিত রেস কন্ডিশন সম্ভব:

১. রিকোয়েস্ট A `RefreshNonceAsync()` কল করে — nonce A1 `_nonceCollection["CSPNonce"]` হিসেবে সংরক্ষিত হয়।
২. রিকোয়েস্ট B `RefreshNonceAsync()` কল করে — nonce B1 `_nonceCollection["CSPNonce"]` ওভাররাইট করে।
৩. রিকোয়েস্ট A `GetANonce("CSPNonce")` কল করে — A1 নয়, B1 পায়।
৪. রিকোয়েস্ট A-এর CSP হেডার এবং ইনলাইন nonce উভয়ই B1 ধারণ করে।
৫. রিকোয়েস্ট B-ও B1 ধারণ করে।

দুটি সমান্তরাল প্রতিক্রিয়া একই nonce ভাগ করে নেয়। উভয় মান এখনও ক্রিপ্টোগ্রাফিকভাবে র্যান্ডম এবং অননুমানযোগ্য হলেও (কোনো হার্ডকোডেড স্ট্রিং নেই), একই nonce মান একাধিক সমান্তরাল প্রতিক্রিয়ায় উপস্থিত হয়, CSP স্পেসিফিকেশন দ্বারা প্রয়োজনীয় প্রতি-রিকোয়েস্ট অনন্যতার গ্যারান্টি দুর্বল করে। একটি আক্রমণকারী যিনি একটি প্রতিক্রিয়ার nonce পর্যবেক্ষণ করতে পারেন তার কমপক্ষে আরেকটি সমান্তরাল প্রতিক্রিয়ার জন্য একটি বৈধ nonce থাকবে।

**সুপারিশ:** প্রতিটি রিকোয়েস্টের জন্য সরাসরি middleware-এ nonce তৈরি করুন (যেমন `Nonce.GenerateSecureNonce()`) এবং শুধুমাত্র `HttpContext.Items["Nonce"]`-এ সংরক্ষণ করুন, প্রতি-রিকোয়েস্ট nonces-এর জন্য ভাগ করা ক্যাটালগ বাইপাস করে। ভাগ করা ক্যাটালগ তখনই প্রয়োজন হবে যখন একটি একক রিকোয়েস্টের মধ্যে middleware স্তরগুলির মধ্যে nonce ভাগ করার প্রয়োজন হয়, যা `HttpContext.Items` ইতিমধ্যে নেটিভলি পরিচালনা করে।

---

### ২৮. Program.cs খালি ILoggerFactory Singleton নিবন্ধন করে

**ফাইল:** `Program.cs` (লাইন ৮৫)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core `WebApplication.CreateBuilder`-এর সময় স্বয়ংক্রিয়ভাবে একটি সম্পূর্ণ কনফিগার করা `ILoggerFactory` (সমস্ত লগিং প্রদানকারী সহ `builder.Logging` কনফিগারেশন থেকে) নিবন্ধন করে। এই স্পষ্ট `AddSingleton` নিবন্ধন কোনো প্রদানকারী ছাড়া একটি দ্বিতীয়, অকনফিগার `LoggerFactory` উদাহরণ যোগ করে। যেহেতু `GetRequiredService<ILoggerFactory>()` সবচেয়ে সম্প্রতি নিবন্ধিত বাস্তবায়ন ফেরত দেয়, DI-এর মাধ্যমে `ILoggerFactory` গ্রহণকারী সার্ভিসগুলি (যেমন `NonceRefresherService`) এই খালি ফ্যাক্টরি ব্যবহার করবে এবং `_loggerFactory.CreateLogger<T>()`-এর মাধ্যমে কোনো লগিং আউটপুট উৎপন্ন করবে না।

**ঝুঁকি:** `NonceRefresherService`-এ নীরব লগিং — nonce তৈরির সাফল্য এবং ব্যর্থতা কনফিগার করা লগিং সিঙ্কে নির্গত হয় না। এটি কার্যকারিতাকে প্রভাবিত না করে নিরাপত্তা-সংবেদনশীল অপারেশনের সময় অ্যাপ্লিকেশনের পর্যবেক্ষণযোগ্যতা হ্রাস করে।

**সুপারিশ:** স্পষ্ট `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` নিবন্ধন সরিয়ে দিন। ফ্রেমওয়ার্কের কনফিগার করা `ILoggerFactory` (Console এবং অন্যান্য প্রদানকারী সহ) তখন এটির উপর নির্ভরশীল সার্ভিসগুলি দ্বারা সঠিকভাবে সমাধান করা হবে।

---

## 🔵 নিম্ন / তথ্যমূলক

### ২৫. OcspSettings ক্লাস ডিফল্ট টেমপ্লেট থেকে বিচ্যুত

**ফাইল:** `Models/Settings/OcspSettings.cs` (লাইন ৩৯)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

টেমপ্লেট (`appsettings.template.json`) `"ServerUnavailableBehavior": "Fail"` নির্দিষ্ট করে, কিন্তু C# ক্লাস ডিফল্ট `"Warn"`। যদি `ServerUnavailableBehavior` সক্রিয় কনফিগারেশন ফাইলে অনুপস্থিত থাকে, তাহলে টেমপ্লেট সুপারিশের পরিবর্তে ক্লাস ডিফল্ট নীরবে প্রয়োগ হয়। এটি অনুসন্ধান #২৪ থেকে অবশিষ্ট।

**সুপারিশ:** ক্লাস ডিফল্ট `"Warn"` থেকে `"Fail"`-এ পরিবর্তন করুন টেমপ্লেট এবং সর্বনিম্ন সুবিধার নীতির সাথে সামঞ্জস্য রাখতে।

---

### ২৭. OptimizedNonceMiddleware স্ট্যাটিক কাউন্টার ওভারফ্লো হতে পারে

**ফাইল:** `Services/OptimizedNonceMiddleware.cs` (লাইন ২৫–২৬)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

এই স্বাক্ষরিত ৩২-বিট কাউন্টারগুলি `Interlocked.Increment` দ্বারা পারমাণবিকভাবে বৃদ্ধি পায়। প্রায় ২.১ বিলিয়ন বৃদ্ধির পরে, সেগুলি `int.MinValue` (−২,১৪৭,৪৮৩,৬৪৮)-এ রোলব্যাক করবে, যা দক্ষতা গণনা `(total - generated) * 100.0 / total` ভুল বা অর্থহীন ফলাফল উৎপন্ন করতে পারে। প্রতি সেকেন্ডে ১,০০০ রিকোয়েস্টে, ওভারফ্লো প্রায় ২৪.৮ দিনের ক্রমাগত অপারেশনের পরে ঘটে।

**সুপারিশ:** কাউন্টার ফিল্ড টাইপ `int` থেকে `long`-এ পরিবর্তন করুন এবং ওভারফ্লো প্রতিরোধ করতে `Interlocked.Increment`-এর `long` ওভারলোড ব্যবহার করুন।

---

## নিরাপত্তা হেডার মূল্যায়ন (বর্তমান অবস্থা)

নিম্নলিখিত হেডারগুলি `UseStandardSecurityHeaders`-এর মাধ্যমে প্রয়োগ করা হয় — পূর্ববর্তী পর্যালোচনা থেকে অপরিবর্তিত:

| হেডার | মান | মূল্যায়ন |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ ভালো |
| `X-XSS-Protection` | `0` | ✅ ভালো (পুরানো অডিটর নিষ্ক্রিয় করে) |
| `X-Content-Type-Options` | `nosniff` | ✅ ভালো |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ ভালো |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ ভালো |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ ভালো |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ ভালো |
| `Permissions-Policy` | geolocation, camera, microphone, interest-cohort নিষ্ক্রিয় | ✅ ভালো |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ ভালো |
| `Content-Security-Policy` | Nonce-ভিত্তিক, CSP সক্রিয় থাকলে প্রয়োগ | ✅ ভালো |
| `Server` | `"webserver"`-এ মাস্ক করা | ✅ ভালো |
| `X-Powered-By` | সরানো হয়েছে | ✅ ভালো |

---

## সামগ্রিক মূল্যায়ন

পূর্ববর্তী পর্যালোচনার সমস্ত উচ্চ-তীব্রতার অনুসন্ধান সমাধান হয়েছে। বর্তমান অনুসন্ধানগুলি দুটি মাঝারি-তীব্রতার সমস্যা (#২৬ ভাগ করা nonce কী, #২৮ খালি ILoggerFactory) এবং দুটি নিম্ন-তীব্রতার তথ্যমূলক আইটেম (#২৫ ক্লাস ডিফল্ট মিসম্যাচ, #২৭ কাউন্টারে পূর্ণসংখ্যা ওভারফ্লো) এ সীমাবদ্ধ। অনুসন্ধান #২৮ (খালি ILoggerFactory singleton)-এর জন্য তাৎক্ষণিক মনোযোগ সুপারিশ করা হয় কারণ এটি nonce অপারেশনের সময় নিরাপত্তা-সম্পর্কিত ডায়াগনস্টিক লগিং নীরবে দমন করে। CSP স্পেসিফিকেশন দ্বারা প্রয়োজনীয় প্রতি-রিকোয়েস্ট nonce অনন্যতার গ্যারান্টি পুনরুদ্ধার করতে অনুসন্ধান #২৬ (ভাগ করা nonce কী) সমাধান করা উচিত।
