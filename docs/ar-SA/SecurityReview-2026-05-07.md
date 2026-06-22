# مراجعة الأمان — WebAppExperimental266

**التاريخ:** 2026-05-07
**النطاق:** تحليل ثابت شامل لقاعدة الكود (متابعة لمراجعة 2026-05-06)
**المراجع:** مراجعة الأمان الآلية

---

## الملخص التنفيذي

تؤكد هذه المراجعة المتابعة أن 3 من أصل 5 ثغرات أمنية تم تحديدها في مراجعة الأمان بتاريخ 2026-05-06 قد تم معالجتها بالكامل، مع بقاء 1 معالجة جزئية. تحدد المراجعة أيضاً 4 نتائج جديدة. يستمر التحسن في الوضع الأمني العام للتطبيق.

---

## حالة النتائج السابقة (2026-05-06)

| # | النتيجة | الخطورة | الحالة |
|---|---------|----------|--------|
| 20 | يحتفظ NonceRefresherService بتبعيات مُنشئ Key Vault غير المستخدمة | 🟠 عالية | ✅ تم الإصلاح |
| 21 | تستخدم ذاكرة التخزين المؤقت الداخلية لـ OcspValidationService قاموساً Dictionary غير آمن للخيوط | 🟡 متوسطة | ✅ تم الإصلاح |
| 22 | لا يزال stub التحقق من OCSP موجوداً — يفشل في الوضع المغلق لكنه غير منفَّذ | 🔵 منخفضة | ⚠️ مقبول (بحسب التصميم) |
| 23 | يرفض mTLS مع AllowedIssuers الفارغة جميع الشهادات (fail-closed، غير موثق) | 🔵 منخفضة | ✅ تم الإصلاح |
| 24 | الافتراضي لـ OcspSettings.ServerUnavailableBehavior هو "Warn" (يسمح بالمرور عند الخطأ) | 🔵 منخفضة | ⚠️ تم الإصلاح جزئياً |

---

## الحالة التفصيلية للنتائج السابقة

### ✅ 20. تبعيات DI غير المستخدمة في NonceRefresherService — تم الإصلاح

**الملف:** `Services/NonceRefresherService.cs`

يُعلن مُنشئ `NonceRefresherService` الآن فقط عن `ILogger<NonceRefresherService>` و`ILoggerFactory` و`INonceCatalogService`. تمت إزالة التبعيات الأربع غير المستخدمة سابقاً (`IKeyVaultSettingsService`، و`INonceEncryptionSettingsService`، و`IAzureADSettingsService`، و`IAzureKeyVaultOperationsService`). يحل هذا مخاطر رفض الخدمة التي كانت تمنع التطبيق من البدء عندما يكون `EnableKeyVault = false` (الافتراضي) و`EnableNonceServices = true` (الافتراضي).

---

### ✅ 21. ذاكرة التخزين المؤقت غير الآمنة للخيوط في OcspValidationService — تم الإصلاح

**الملف:** `Services/OcspValidationService.cs`

تم استبدال `Dictionary<string, CachedOcspResponse> _cache` بـ`ConcurrentDictionary<string, CachedOcspResponse>`. تم تحديث استدعاء `_cache.Remove` إلى `_cache.TryRemove`. أصبحت ذاكرة التخزين المؤقت الآن آمنة للوصول المتزامن.

---

### ⚠️ 22. Stub التحقق من OCSP — مقبول (بحسب التصميم)

**الملف:** `Services/OcspValidationService.cs`

يظل الـstub موجوداً لكنه يفشل بشكل صحيح في الوضع المغلق. بما أن `EnableOcspValidation` يكون افتراضياً `false`، فهذا لا يؤثر على الإنتاج. يُقبل هذا كنتيجة إعلامية ريثما يتم تنفيذ OCSP الكامل.

---

### ✅ 23. AllowedIssuers الفارغة في mTLS — تم الإصلاح

**الملف:** `Extensions/ServiceCollectionExtensions.cs`

يتم الآن تسجيل تحذير بدء التشغيل عندما يكون `ValidateClientCertificateIssuer = true` و`AllowedIssuers` فارغة:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

يوفر هذا إرشادات واضحة للمشغّلين الذين يواجهون سلوك fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — تم الإصلاح جزئياً

**الملفات:** `appsettings.template.json` (تم الإصلاح)، `Models/Settings/OcspSettings.cs` (لم يتم الإصلاح بعد)

يحدد القالب الآن بشكل صحيح `"ServerUnavailableBehavior": "Fail"`. ومع ذلك، يظل الافتراضي لفئة C# في `OcspSettings.cs` (السطر 39) هو `"Warn"`. إذا قام مشغّل بتفعيل OCSP وحذف `ServerUnavailableBehavior` من ملف التكوين الخاص به، يُطبَّق الافتراضي `"Warn"` للفئة بصمت، مما يسمح بالمرور خلال انقطاعات خادم OCSP. يجب تغيير الافتراضي للفئة ليتطابق مع توصية القالب.

---

## النتائج الجديدة

| # | المجال | الخطورة |
|---|------|----------|
| 25 | يختلف الافتراضي لفئة OcspSettings ("Warn") عن القالب ("Fail") | 🔵 منخفضة |
| 26 | مفتاح nonce المشترك الوحيد في NonceCatalogService يسمح بتصادم nonce عبر الطلبات | 🟡 متوسطة |
| 27 | تستخدم العدادات الثابتة في OptimizedNonceMiddleware أعداداً صحيحة 32 بت موقَّعة (خطر الفيضان) | 🔵 منخفضة |
| 28 | يسجّل Program.cs منفرداً ILoggerFactory فارغاً يحجب مسجّل الإطار | 🟡 متوسطة |

---

## 🟡 متوسطة

### 26. مفتاح Nonce المشترك في NonceCatalogService يسمح بتصادم Nonce عبر الطلبات

**الملفات:** `Services/NonceCatalogService.cs`، `Services/NonceMiddleware.cs`، `Services/OptimizedNonceMiddleware.cs`

يخزّن كتالوج nonce جميع الـnonces تحت مفتاح مشترك واحد `"CSPNonce"`. في ظل التحميل المتزامن، يكون حالة السباق التالية ممكنة:

1. الطلب A يستدعي `RefreshNonceAsync()` — يُخزَّن nonce A1 بوصفه `_nonceCollection["CSPNonce"]`.
2. الطلب B يستدعي `RefreshNonceAsync()` — يُحلّ nonce B1 محلّ `_nonceCollection["CSPNonce"]`.
3. الطلب A يستدعي `GetANonce("CSPNonce")` — يستقبل B1، وليس A1.
4. يحتوي كلٌّ من رأس CSP ونونس التخطيط في الطلب A على B1.
5. الطلب B يحتوي أيضاً على B1.

يشترك ردّان متزامنان في نفس الـnonce. بينما لا تزال كلتا القيمتين عشوائيتين من الناحية التشفيرية وغير قابلتين للتنبؤ (لا توجد سلسلة مُشفَّرة)، تظهر قيمة nonce نفسها في ردود متزامنة متعددة، مما يُضعف ضمان التفرد لكل طلب الذي تشترطه مواصفات CSP. يمتلك مهاجم يمكنه مراقبة nonce أحد الردود nonce صالحاً لرد متزامن آخر على الأقل.

**التوصية:** أنشئ الـnonce مباشرة داخل البرمجية الوسيطة لكل طلب (مثلاً `Nonce.GenerateSecureNonce()`) وخزّنه فقط في `HttpContext.Items["Nonce"]`، متجاوزاً الكتالوج المشترك لـnonces لكل طلب. سيكون الكتالوج المشترك ضرورياً فقط إذا كان يجب مشاركة nonce عبر طبقات البرمجية الوسيطة داخل طلب واحد، وهو ما يتعامل معه `HttpContext.Items` بالفعل بشكل أصلي.

---

### 28. Program.cs يسجّل منفرداً ILoggerFactory فارغاً

**الملف:** `Program.cs` (السطر 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

يسجّل ASP.NET Core تلقائياً `ILoggerFactory` مُهيَّأة بالكامل (مع جميع موفري التسجيل من تكوين `builder.Logging`) أثناء `WebApplication.CreateBuilder`. يضيف هذا التسجيل الصريح `AddSingleton` مثيلاً ثانياً غير مُهيَّأ من `LoggerFactory` بدون موفّرين. نظراً لأن `GetRequiredService<ILoggerFactory>()` يُعيد التنفيذ المسجَّل مؤخراً، ستستخدم الخدمات التي تستلم `ILoggerFactory` عبر حقن التبعية (مثل `NonceRefresherService`) هذا المصنع الفارغ ولن تُنتج أي مخرجات تسجيل عبر `_loggerFactory.CreateLogger<T>()`.

**المخاطر:** تسجيل صامت في `NonceRefresherService` — لا يتم إرسال نجاحات وإخفاقات توليد nonce إلى أي بالوعة تسجيل مُهيَّأة. يقلل هذا من إمكانية مراقبة التطبيق أثناء العمليات الحساسة أمنياً دون التأثير على الوظائف.

**التوصية:** أزِل تسجيل `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` الصريح. سيتم حينئذٍ حل `ILoggerFactory` المُهيَّأة للإطار (مع Console وأي موفّرين آخرين) بشكل صحيح من قِبَل الخدمات التي تعتمد عليها.

---

## 🔵 منخفضة / إعلامية

### 25. الافتراضي لفئة OcspSettings يختلف عن القالب

**الملف:** `Models/Settings/OcspSettings.cs` (السطر 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

يحدد القالب (`appsettings.template.json`) قيمة `"ServerUnavailableBehavior": "Fail"`، لكن الافتراضي لفئة C# هو `"Warn"`. إذا كان `ServerUnavailableBehavior` غائباً من ملف التكوين النشط، يُطبَّق الافتراضي للفئة بصمت بدلاً من توصية القالب. هذا متبقٍّ من النتيجة #24.

**التوصية:** غيِّر الافتراضي للفئة من `"Warn"` إلى `"Fail"` للتوافق مع القالب ومبدأ أقل الامتيازات.

---

### 27. قد تفيض العدادات الثابتة في OptimizedNonceMiddleware

**الملف:** `Services/OptimizedNonceMiddleware.cs` (السطران 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

تُزاد هذه العدادات 32 بت الموقَّعة بشكل ذري عبر `Interlocked.Increment`. بعد ما يقارب 2.1 مليار زيادة، ستتدور إلى `int.MinValue` (−2,147,483,648)، مما يجعل حساب الكفاءة `(total - generated) * 100.0 / total` يُنتج نتائج خاطئة أو بلا معنى. عند 1,000 طلب في الثانية، يحدث الفيضان بعد نحو 24.8 يوماً من التشغيل المستمر.

**التوصية:** غيِّر أنواع حقول العدادات من `int` إلى `long` واستخدم تحميل `long` الزائد من `Interlocked.Increment` لمنع الفيضان.

---

## تقييم رؤوس الأمان (الحالة الراهنة)

يتم تطبيق الرؤوس التالية عبر `UseStandardSecurityHeaders` — دون تغيير عن المراجعة السابقة:

| الرأس | القيمة | التقييم |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ جيد |
| `X-XSS-Protection` | `0` | ✅ جيد (يُعطّل المدقق القديم) |
| `X-Content-Type-Options` | `nosniff` | ✅ جيد |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ جيد |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ جيد |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ جيد |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ جيد |
| `Permissions-Policy` | الموقع الجغرافي والكاميرا والميكروفون وinterest-cohort معطَّلة | ✅ جيد |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ جيد |
| `Content-Security-Policy` | قائم على nonce، يُطبَّق عند تفعيل CSP | ✅ جيد |
| `Server` | مُخفَّى إلى `"webserver"` | ✅ جيد |
| `X-Powered-By` | محذوف | ✅ جيد |

---

## التقييم العام

تمت معالجة جميع النتائج عالية الخطورة من المراجعات السابقة. تقتصر النتائج الحالية على مشكلتين متوسطتي الخطورة (#26 مفتاح nonce المشترك، #28 ILoggerFactory الفارغة) وعنصرين إعلاميين منخفضَي الخطورة (#25 عدم تطابق الافتراضي للفئة، #27 فيضان الأعداد الصحيحة في العدادات). يُوصى بالاهتمام الفوري بالنتيجة #28 (المنفرد ILoggerFactory الفارغ) لأنها تُسكت تسجيل التشخيص ذي الصلة بالأمان أثناء عمليات nonce. يجب معالجة النتيجة #26 (مفتاح nonce المشترك) لاستعادة ضمان تفرد nonce لكل طلب الذي تشترطه مواصفات CSP.
