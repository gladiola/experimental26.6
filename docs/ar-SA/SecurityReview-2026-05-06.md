# مراجعة الأمان — WebAppExperimental266

**التاريخ:** 2026-05-06
**النطاق:** تحليل ثابت كامل لقاعدة الكود (متابعة لمراجعة 2026-05-05)
**المراجع:** مراجعة الأمان الآلية

---

## الملخص التنفيذي

تؤكد مراجعة المتابعة هذه أن جميع الثغرات الأمنية الـ19 التي تم تحديدها في مراجعة الأمان بتاريخ 2026-05-05 قد تم معالجتها. تحدد المراجعة أيضاً 5 نتائج جديدة أو متبقية تم اكتشافها خلال هذه الجلسة. تحسّن الوضع الأمني العام للتطبيق بشكل ملحوظ منذ المراجعة السابقة.

---

## حالة النتائج السابقة (2026-05-05)

جميع النتائج الـ19 السابقة **مؤكدة كمُصلَحة**:

| # | النتيجة | الخطورة | الحالة |
|---|---------|---------|--------|
| 1 | إعادة استخدام IV في AES-GCM عند توليد nonce | 🔴 حرجة | ✅ تم الإصلاح |
| 2 | تسجيل Nonce بنص واضح | 🔴 حرجة | ✅ تم الإصلاح |
| 3 | سلاسل nonce احتياطية مُرمَّزة بشكل ثابت | 🔴 حرجة | ✅ تم الإصلاح |
| 4 | قاموس nonce العام غير آمن للخيوط | 🟠 عالية | ✅ تم الإصلاح |
| 5 | التحقق من مُصدِر mTLS مُعطَّل في الكود | 🟠 عالية | ✅ تم الإصلاح |
| 6 | فحص إلغاء mTLS معطّل افتراضياً | 🟠 عالية | ✅ تم الإصلاح |
| 7 | OCSP يعيد دائماً "صالح" (stub) | 🟠 عالية | ✅ تم الإصلاح |
| 8 | المصادقة/التفويض معطّلان افتراضياً في الإعدادات | 🟠 عالية | ✅ تم الإصلاح |
| 9 | رؤوس الأمان تُطبَّق متأخرة جداً في المسار | 🟠 عالية | ✅ تم الإصلاح |
| 10 | ملف تعريف ارتباط الجلسة يفتقر إلى `Secure` + `SameSite` | 🟡 متوسطة | ✅ تم الإصلاح |
| 11 | رأس `Set-Cookie` العام مشوّه | 🟡 متوسطة | ✅ تم الإصلاح |
| 12 | إجبار `Content-Type` على `text/html` في كل مكان | 🟡 متوسطة | ✅ تم الإصلاح |
| 13 | `AllowedHosts` مضبوط على حرف بدل | 🟡 متوسطة | ✅ تم الإصلاح |
| 14 | عدم تطبيق Nonce على وسوم `<script>` في التخطيط | 🟡 متوسطة | ✅ تم الإصلاح |
| 15 | رأس `Referrer-Policy` مفقود | 🟡 متوسطة | ✅ تم الإصلاح |
| 16 | تسجيل PII بنص واضح | 🔵 منخفضة | ✅ تم الإصلاح |
| 17 | سلسلة اتصال جزئية في السجلات | 🔵 منخفضة | ✅ تم الإصلاح |
| 18 | عمليات Key Vault عبارة عن stubs | 🔵 منخفضة | ✅ تم الإصلاح |
| 19 | `X-XSS-Protection: 1; mode=block` المهجور | 🔵 منخفضة | ✅ تم الإصلاح |

---

## النتائج الجديدة / المتبقية

| # | المجال | الخطورة |
|---|--------|---------|
| 20 | NonceRefresherService يحتفظ بتبعيات مُنشئ Key Vault غير المستخدمة | 🟠 عالية |
| 21 | ذاكرة التخزين المؤقت الداخلية في OcspValidationService تستخدم Dictionary غير آمن للخيوط | 🟡 متوسطة |
| 22 | stub التحقق من OCSP لا يزال موجوداً — يفشل بشكل مغلق لكنه غير مُنفَّذ | 🔵 منخفضة |
| 23 | mTLS مع AllowedIssuers الفارغ يرفض جميع الشهادات (fail-closed، غير موثّق) | 🔵 منخفضة |
| 24 | OcspSettings.ServerUnavailableBehavior يُعيَّن افتراضياً على "Warn" (يسمح بالمرور عند الخطأ) | 🔵 منخفضة |

---

## النتائج التفصيلية

### ✅ الإصلاحات المؤكدة من 2026-05-05

#### 1. إعادة استخدام IV في AES-GCM — تم الإصلاح

**الملف:** `Models/Main_Objects/Nonce.cs`

تم استبدال توليد nonce المستند إلى AES-GCM بالكامل. تستدعي `Nonce.GenerateSecureNonce()` الآن `RandomNumberGenerator.Fill(randomBytes)` على 16 بايت عشوائياً وتُعيد سلسلة Base64. لا توجد تبعية على Key Vault، ولا IV، ولا تشفير — هذا بالضبط النهج الصحيح لـ CSP nonce.

---

#### 2. قيم Nonce لم تعد تُسجَّل — تم الإصلاح

**الملفات:** `Services/NonceMiddleware.cs`، `Services/NonceRefresherService.cs`

يُسجّل كلا الملفين الآن رسائل الحالة فقط (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) ولا يُسجّلان قيمة nonce نفسها أبداً.

---

#### 3. إزالة Nonces الاحتياطية المُرمَّزة بشكل ثابت — تم الإصلاح

**الملف:** `Services/OptimizedNonceMiddleware.cs`

تم استبدال جميع السلاسل الحرفية الثلاث المُرمَّزة بشكل ثابت (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) باستدعاءات `Nonce.GenerateSecureNonce()` في كلا المسارين الطبيعي والاحتياطي للاستثناءات.

---

#### 4. قاموس Nonce آمن للخيوط — تم الإصلاح

**الملف:** `Services/NonceCatalogService.cs`

تم استبدال `Dictionary<string, Nonce>` بـ `ConcurrentDictionary<string, Nonce>`. تستخدم `GetANonce` الآن استدعاء `TryGetValue` ذرياً واحداً بدلاً من التحقق ثم البحث في خطوتين.

---

#### 5. التحقق من مُصدِر mTLS يعمل الآن — تم الإصلاح

**الملف:** `Extensions/ServiceCollectionExtensions.cs`، `Models/Settings/MtlsSettings.cs`

تم استبدال كتلة التحقق من المُصدِر المُعطَّلة في الكود باستدعاء `mtlsSettings.IsIssuerAllowed(issuer)`، الذي يُجري مطابقة سلسلة فرعية غير حساسة لحالة الأحرف مقابل `AllowedIssuers`. عندما تكون القائمة فارغة (غير مُهيَّأة)، تُعيد الطريقة `false`، رافضةً جميع الشهادات (fail-closed).

---

#### 6. فحص إلغاء mTLS مُفعَّل افتراضياً — تم الإصلاح

**الملف:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` يُعيَّن افتراضياً على `true` الآن. يُحدِّد `appsettings.template.json` أيضاً `"CheckCertificateRevocation": true`.

---

#### 7. Stub OCSP يفشل الآن بشكل مغلق — تم الإصلاح

**الملف:** `Services/OcspValidationService.cs`

تُعيد `PerformOcspValidationAsync` الآن `IsValid = false` مع `OcspStatus.Error` وتُسجِّل خطأً، بدلاً من إعادة `IsValid = true` بصمت. سيؤدي تفعيل OCSP في الإعدادات الآن إلى رفض جميع الشهادات حتى يُوفَّر تنفيذ حقيقي، بدلاً من قبولها بصمت.

---

#### 8. المصادقة والتفويض مُفعَّلان افتراضياً — تم الإصلاح

**الملف:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` و`EnableAuthorization` كلاهما الآن بقيمة افتراضية `true` في فئة `FeatureFlags`. يُعيِّن `appsettings.json` أيضاً كليهما على `true`.

---

#### 9. رؤوس الأمان تُطبَّق قبل التوجيه — تم الإصلاح

**الملف:** `Program.cs`

تُستدعى `UseNonceAndSecurityHeadersAsync` و`UseStandardSecurityHeaders` الآن قبل `UseRouting` و`UseAuthentication` و`UseAuthorization`. تتلقى جميع الاستجابات، بما فيها دوائر القصر 401/403، رؤوس الأمان.

---

#### 10–15. ملف تعريف الارتباط، Content-Type، AllowedHosts، Nonce في التخطيط، Referrer-Policy — تم الإصلاح

**الملفات:** `Extensions/ServiceCollectionExtensions.cs`، `Extensions/ApplicationBuilderExtensions.cs`، `Views/Shared/_Layout.cshtml`، `appsettings.json`

- يُعيِّن ملف تعريف ارتباط الجلسة الآن `CookieSecurePolicy.Always` و`SameSiteMode.Strict`.
- تمت إزالة رأس `Set-Cookie` المشوّه بلا اسم.
- تمت إزالة إعادة تعيين `Content-Type: text/html` العالمية.
- أصبح `AllowedHosts` في `appsettings.json` الآن `"localhost;127.0.0.1"`؛ يستخدم القالب `"{{YOUR_HOSTNAME}}"`.
- تتضمن جميع وسوم `<script>` الثلاثة في `_Layout.cshtml` الآن `nonce="@Context.Items["Nonce"]"`.
- تُضيف `UseStandardSecurityHeaders` الآن `Referrer-Policy: strict-origin-when-cross-origin`.

---

#### 16–19. تسجيل PII، سجل سلسلة الاتصال، Stubs Key Vault، X-XSS-Protection — تم الإصلاح

**الملفات:** `Services/LoggingHelper.cs`، `Extensions/ServiceCollectionExtensions.cs`، `Extensions/ApplicationBuilderExtensions.cs`

- جميع PII (OID، البريد الإلكتروني، الاسم، SID، الأدوار) تُجزَّأ الآن بـ HMAC-SHA256 عبر `LoggingHelper.HashPii()` قبل الكتابة في السجلات. يمكن توفير مفتاح HMAC ثابت عبر `Logging:PiiHmacKey` في الإعدادات؛ يُستخدم مفتاح عشوائي لكل عملية عند عدم الإعداد.
- تُؤكِّد عبارة سجل Cosmos DB الآن فقط وجود سلسلة الاتصال (`!string.IsNullOrEmpty`)، وليس محتواها.
- تُطلق `AzureKeyVaultCertificateOperations` الآن `InvalidOperationException` عند بدء التشغيل عندما تكون الشهادة null، بدلاً من إعادة قيم وهمية بصمت.
- `X-XSS-Protection` مُعيَّن الآن على `"0"` (لتعطيل مدقق XSS المهجور)، بما يتوافق مع إرشادات المتصفحات الحديثة.

---

## 🟠 عالية

### 20. NonceRefresherService يحتفظ بتبعيات مُنشئ Key Vault غير المستخدمة

**الملف:** `Services/NonceRefresherService.cs`

لا تزال `NonceRefresherService` تُعلن عن معاملات مُنشئ لـ `IKeyVaultSettingsService` و`INonceEncryptionSettingsService` و`IAzureADSettingsService` و`IAzureKeyVaultOperationsService`. نظراً لأن توليد nonce تم تبسيطه لاستخدام `RandomNumberGenerator` مباشرةً، لا تُستخدم أيٌّ من هذه التبعيات.

**المخاطرة:** عندما يكون `EnableNonceServices = true` و`EnableKeyVault = false` (الافتراضي)، لا تكون هذه الخدمات مُسجَّلة في حاوية DI، مما يُسبِّب `InvalidOperationException` في وقت التشغيل عند أول تحليل لخدمة nonce. هذا في الواقع شرط رفض الخدمة الذي يُفعَّل بالإعدادات الافتراضية. تُعيِّن فئة `FeatureFlags` افتراضياً `EnableNonceServices = true`، لذا فإن أي بيئة تعتمد فقط على قيم الفئة الافتراضية (دون تجاوزات `appsettings.json`) ستفشل في البدء.

**التوصية:** إزالة المعاملات الأربعة غير المستخدمة للمُنشئ وحقولها الخاصة المقابلة من `NonceRefresherService`. تحتاج الخدمة فقط إلى `ILogger<NonceRefresherService>` و`ILoggerFactory` و`INonceCatalogService`.

---

## 🟡 متوسطة

### 21. ذاكرة التخزين المؤقت الداخلية في OcspValidationService تستخدم Dictionary غير آمن للخيوط

**الملف:** `Services/OcspValidationService.cs` (السطر 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` ليس آمناً للخيوط بالنسبة للقراءات والكتابات المتزامنة. إذا تم تسجيل `OcspValidationService` كـ singleton (أو إذا تمت مشاركة نفس المثيل بين الطلبات بأي آلية أخرى)، فقد تُفسد عمليات التحقق المتزامنة من OCSP ذاكرة التخزين المؤقت، مما يُسبِّب فقدان المدخلات، أو إطلاق استثناءات، أو إعادة بيانات قديمة.

**التوصية:** استبدال `Dictionary<string, CachedOcspResponse>` بـ `ConcurrentDictionary<string, CachedOcspResponse>`. تحديث استدعاء `_cache.Remove` (السطر 103) إلى `_cache.TryRemove`.

---

## 🔵 منخفضة / معلوماتية

### 22. Stub التحقق من OCSP — يفشل بشكل مغلق لكنه غير مُنفَّذ

**الملف:** `Services/OcspValidationService.cs` (الأسطر 157–173)

لا تزال `PerformOcspValidationAsync` عبارة عن stub. أدّى إصلاح النتيجة رقم 7 بشكل صحيح إلى تغيير السلوك من "صالح دائماً" إلى "غير صالح دائماً (fail-closed)". ومع ذلك، لا يزال الأسلوب ليس تنفيذاً حقيقياً لـ OCSP. طالما `EnableOcspValidation = false` (الافتراضي)، فإن ذلك لن يؤثر على الإنتاج. قبل تفعيل OCSP في أي بيئة، يجب تنفيذ عميل OCSP بجودة إنتاجية.

---

### 23. mTLS مع AllowedIssuers الفارغ يرفض جميع شهادات العملاء

**الملف:** `Models/Settings/MtlsSettings.cs`

عندما يكون `ValidateClientCertificateIssuer = true` (الافتراضي) و`AllowedIssuers` فارغاً (أيضاً الافتراضي عند عدم الإعداد)، تُعيد `IsIssuerAllowed()` القيمة `false`، مما يؤدي إلى رفض جميع شهادات العملاء. هذا سلوك صحيح للإغلاق عند الفشل، لكنه غير موثّق بشكل بارز. قد يجد المشغّلون الذين يُفعِّلون mTLS دون قراءة القالب بعناية أن جميع اتصالات العملاء تُرفض دون تفسير واضح.

**التوصية:** إضافة رسالة سجل تحذيرية عند بدء التشغيل عندما يكون `ValidateClientCertificateIssuer = true` و`AllowedIssuers` فارغاً.

---

### 24. OcspSettings.ServerUnavailableBehavior مُعيَّن افتراضياً على "Warn"

**الملف:** `appsettings.template.json` (السطر 134)، `Services/OcspValidationService.cs`

يُعيَّن إعداد `ServerUnavailableBehavior` افتراضياً على `"Warn"` في القالب، مما يسمح للطلبات بالمرور عندما لا يمكن الوصول إلى خادم OCSP. بالنسبة للبيئات عالية الأمان، يجب أن يكون هذا `"Fail"` حتى لا تُؤدي انقطاعات خادم OCSP إلى إضعاف صامت لفحص إلغاء الشهادات.

**التوصية:** توثيق الخيارات الثلاثة (`Fail`، `Allow`، `Warn`) بوضوح في القالب والنظر في تغيير الافتراضي إلى `"Fail"` لمطابقة مبدأ أقل الامتيازات.

---

## تقييم رؤوس الأمان (الحالة الراهنة)

تُطبَّق الرؤوس التالية الآن عبر `UseStandardSecurityHeaders`:

| الرأس | القيمة | التقييم |
|-------|--------|---------|
| `X-Frame-Options` | `DENY` | ✅ جيد |
| `X-XSS-Protection` | `0` | ✅ جيد (يُعطِّل المدقق المهجور) |
| `X-Content-Type-Options` | `nosniff` | ✅ جيد |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ جيد |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ جيد |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ جيد |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ جيد |
| `Permissions-Policy` | الموقع الجغرافي، الكاميرا، الميكروفون، interest-cohort مُعطَّلة | ✅ جيد |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ جيد |
| `Content-Security-Policy` | مستند إلى Nonce، يُطبَّق عند تفعيل CSP | ✅ جيد |
| `Server` | مُقنَّع بـ `"webserver"` | ✅ جيد |
| `X-Powered-By` | تمت إزالته | ✅ جيد |

---

## التقييم العام

عالج التطبيق جميع الثغرات ذات الخطورة الحرجة والعالية من المراجعة السابقة. تقتصر النتائج الحالية على مشكلة واحدة في التهيئة/DI ذات خطورة عالية (النتيجة رقم 20) وعناصر معلوماتية ذات خطورة أقل. تحسّن الوضع الأمني بشكل ملحوظ. يُوصى باتخاذ إجراء فوري بشأن النتيجة رقم 20 (تبعيات DI غير المستخدمة في NonceRefresherService) لأنها قد تمنع التطبيق من البدء بالإعدادات الافتراضية.
