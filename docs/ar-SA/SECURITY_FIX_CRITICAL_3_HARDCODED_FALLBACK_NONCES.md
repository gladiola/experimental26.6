# إصلاح أمني: قيم nonce احتياطية ثابتة في الكود (حرج #3)

**تم الإصلاح في:** `Services/OptimizedNonceMiddleware.cs`  
**الاختبارات:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## ما كان خاطئاً

احتوت `OptimizedNonceMiddleware` على ثلاثة نصوص نصية ثابتة في الكود كانت تُستخدم كقيم nonce احتياطية عند فشل توليد nonce الطبيعي أو عدم تشغيله بعد:

| الموقع | القيمة الثابتة |
|----------|-----------------|
| `InvokeAsync` — الطلب الأول، الكتالوج فارغ | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — أعاد التوليد سلسلة فارغة | `"fallback-nonce"` |
| `InvokeAsync` — مسار الاستثناء | `"error-fallback-nonce"` |

### لماذا هذا أمر حرج

**nonce آمن فقط إذا كان المهاجم غير قادر على التنبؤ به.** النصوص الثابتة في الكود مُدرجة في التحكم بالمصدر وبالتالي معروفة لأي شخص يملك وصولاً للمستودع (بما في ذلك أي مهاجم حصل على الكود أو فكّك الملف الثنائي).

الخطر المحدد هو أن مسارات الاحتياط هذه تُفعَّل بـ **حالات الخطأ** — وهي بالضبط المواقف التي يسعى المهاجم لاصطناعها (مثل جعل Key Vault غير متاحاً مؤقتاً عبر تحديد المعدل أو تعطيل الشبكة). عندما يتدهور التطبيق بلطف إلى nonce قابل للتنبؤ، يصبح رأس CSP ديكورياً: يحقن المهاجم ببساطة `<script nonce="fallback-nonce">` وينفّذ المتصفح ذلك.

### الكود الجذري للمشكلة (قبل الإصلاح)

```csharp
// الطلب الأول قبل توليد أي nonce
existingNonce = "bootstrap-nonce-placeholder";

// أعاد التوليد قيمة فارغة
nonce = "fallback-nonce";

// مسار الاستثناء
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## ما تم إصلاحه

تستدعي الآن مسارات الاحتياط الثلاثة `Nonce.GenerateSecureNonce()` لإنتاج nonce عشوائي جديد غير قابل للتنبؤ من 16 بايت أثناء التشغيل:

```csharp
// قبل (قابل للاستغلال):
existingNonce = "bootstrap-nonce-placeholder";
// بعد (آمن):
existingNonce = Nonce.GenerateSecureNonce();

// قبل (قابل للاستغلال):
nonce = "fallback-nonce";
// بعد (آمن):
nonce = Nonce.GenerateSecureNonce();

// قبل (قابل للاستغلال):
context.Items["Nonce"] = "error-fallback-nonce";
// بعد (آمن):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

تستخدم `Nonce.GenerateSecureNonce()` دالة `RandomNumberGenerator.Fill` (وهي CSPRNG) لتوليد 16 بايت عشوائية مُشفَّرة آمنياً مُرمَّزة بـ Base64. نظراً لأنها دالة ثابتة بلا تبعية لـ Key Vault، فهي آمنة للاستدعاء حتى عند تعذّر الوصول إلى Key Vault — وهي بالضبط حالة الخطأ التي كانت تُظهر الاحتياطي الثابت سابقاً.

---

## كيفية الحفاظ على الإصلاح

1. **لا تُدخل نصاً nonce ثابتاً** في أي مكان من قاعدة الكود، بغض النظر عن السياق (احتياطي، اختبار، عنصر نائب، مثال في تعليق قد يُنسخ، إلخ).

2. **كل مسار كود يُعيّن `context.Items["Nonce"]` يجب أن يستخدم قيمة عشوائية مُشفَّرة آمنياً.** استدع `Nonce.GenerateSecureNonce()` أو `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **لا تُخزّن nonce واحداً عبر الطلبات.** يجب أن يحصل كل طلب على nonce جديد خاص به.

4. **مسارات الخطأ هي الأكثر خطورة.** إذا فشل توليد nonce لأي سبب، يجب أن تحصل الاستجابة على nonce عشوائي، وليس احتياطياً قابلاً للتنبؤ.

5. **راجع أي تعديلات مستقبلية على `OptimizedNonceMiddleware`** — خاصةً الفروع الثلاثة حيث يمكن تعيين nonce: فرع مسار التجاهل، وفرع التوليد الفارغ، ومعالج الاستثناءات.

### الاختبارات التي تُنفّذ هذا الإصلاح

| الاختبار | ما يرصده |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | يفشل إذا أُعيد إدخال `"bootstrap-nonce-placeholder"` في فرع الطلب الأول |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | يفشل إذا أُعيد إدخال `"fallback-nonce"` في فرع التوليد الفارغ |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | يفشل إذا أُعيد إدخال `"error-fallback-nonce"` في معالج الاستثناءات |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | يفشل إذا أنتج أي احتياطي نفس nonce مرتين في 50 استدعاء متتالياً (وهو ما ستفعله أي سلسلة ثابتة) |
