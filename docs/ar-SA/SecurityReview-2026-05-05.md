# مراجعة أمنية — WebAppExperimental266

**التاريخ:** 2026-05-05  
**النطاق:** تحليل ثابت لقاعدة الكود الكاملة  

---

## جدول الملخص

| # | المجال | الخطورة |
|---|------|----------|
| 1 | إعادة استخدام AES-GCM IV في توليد nonce | 🔴 حرج ✅ |
| 2 | تسجيل nonce بنص صريح | 🔴 حرج ✅ |
| 3 | سلاسل nonce احتياطية ثابتة في الكود | 🔴 حرج ✅ |
| 4 | قاموس nonce عمومي غير آمن للخيوط | 🟠 عالٍ |
| 5 | التحقق من جهة إصدار شهادة mTLS معطّل | 🟠 عالٍ |
| 6 | التحقق من إلغاء شهادة mTLS مُعطَّل افتراضياً | 🟠 عالٍ |
| 7 | OCSP تُعيد دائماً "صالحة" (محاكاة) | 🟠 عالٍ |
| 8 | المصادقة/التفويض معطّلان افتراضياً في التكوين | 🟠 عالٍ |
| 9 | رؤوس الأمان تُطبَّق متأخرة في المسار | 🟠 عالٍ |
| 10 | ملف تعريف ارتباط الجلسة يفتقر إلى Secure وSameSite | 🟡 متوسط |
| 11 | رأس Set-Cookie عمومي مشوّه | 🟡 متوسط |
| 12 | Content-Type مُجبَر على text/html في كل مكان | 🟡 متوسط |
| 13 | AllowedHosts هو حرف بدل | 🟡 متوسط |
| 14 | Nonce غير مُطبَّق على وسوم `<script>` في التخطيط | 🟡 متوسط |
| 15 | رأس Referrer-Policy مفقود | 🟡 متوسط |
| 16 | تسجيل PII بنص صريح | 🔵 منخفض |
| 17 | جزء من سلسلة الاتصال في السجلات | 🔵 منخفض |
| 18 | عمليات Key Vault هي محاكاة | 🔵 منخفض |
| 19 | رأس X-XSS-Protection المهجور | 🔵 منخفض |

---

## 🔴 حرج

### 1. إعادة استخدام AES-GCM IV — توليد nonce مكسور تشفيرياً ✅ تم الإصلاح في الإيداع 45ae31b

**الملفات:** `Models/Main_Objects/Nonce.cs`، `Services/NonceRefresherService.cs`

يستخدم تشفير AES-GCM الذي يُولّد nonces لـ CSP **IV ثابتاً يُجلب من Key Vault في كل استدعاء**. ينهار AES-GCM عند إعادة استخدام IV مع نفس المفتاح: يستطيع مهاجم يرصد نصين مشفرَين إجراء XOR عليهما لاسترداد XOR للنصين الأصليين، ويمكن تزوير علامات المصادقة.

الإصلاح بسيط — CSP nonces لا تحتاج تشفيراً بالكامل. يحتاج CSP nonce فقط أن يكون **غير قابل للتنبؤ وفريداً لكل طلب**؛ استدعاء `RandomNumberGenerator.GetBytes(16)` محوَّلاً إلى Base64 كافٍ وصحيح.

---

### 2. تسجيل قيم nonce بنص صريح ✅ تم الإصلاح في الإيداع bb6f27a

**الملفات:** `Services/NonceMiddleware.cs` (السطر 31)، `Services/NonceRefresherService.cs` (السطر 82)

يُسجَّل CSP nonce المُولَّد حرفياً في سجلات التطبيق:

```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info, $"Nonce: {nonce}");
```

أي شخص لديه وصول للسجلات يحصل على nonce صالح ويستطيع تجاوز CSP بسهولة لحقن سكريبت مضمّن.

---

### 3. nonces احتياطية ثابتة في الكود ✅ تم الإصلاح في الإيداع 11cc9f7

**الملف:** `Services/OptimizedNonceMiddleware.cs` (الأسطر 53، 78، 92)

إذا فشل توليد nonce أو كان كتالوج nonce فارغاً، يلجأ middleware إلى النصوص الحرفية `"bootstrap-nonce-placeholder"` و`"fallback-nonce"` و`"error-fallback-nonce"`. هذه السلاسل مُدرجة في كود المصدر ومعروفة للمهاجمين. حالة خطأ (مثل تعذّر الوصول إلى Key Vault) ستضع nonce متوقعاً وقابلاً للاستغلال في رأس CSP.

---

## 🟠 عالٍ

### 4. NonceCatalogService يستخدم Dictionary ثابتاً غير آمن للخيوط ✅ تم الإصلاح في الإيداع ae2b6c9

**الملف:** `Services/NonceCatalogService.cs` (السطر 20)

```csharp
private static readonly Dictionary<string, Nonce> _nonceCollection = new Dictionary<string, Nonce>();
```

`Dictionary<TKey, TValue>` ليس آمناً للخيوط في القراءة والكتابة المتزامنة. تحت الضغط، يمكن لطلبين يتسابقان على تحديث نفس مفتاح nonce أن يتسببا في تلف البيانات أو رمي استثناءات. كتالوج nonce هو أيضاً singleton (قاموس عمومي فعلياً)، مما يعني أن nonce أحد الطلبات يمكن الكتابة فوقه من طلب آخر في منتصف المعالجة — تصادم nonce بين الطلبات. استخدم `ConcurrentDictionary` وخزّن nonces لكل طلب في `HttpContext.Items` بدلاً من القاموس العمومي المشترك.

---

### 5. التحقق من جهة إصدار شهادة العميل في mTLS معطّل ✅ تم الإصلاح في الإيداع fd3d4fb

**الملف:** `Extensions/ServiceCollectionExtensions.cs` (الأسطر 305–313)

إعداد `ValidateClientCertificateIssuer` موجود ومُفعَّل افتراضياً، لكن كود التحقق الفعلي مُعلَّق:

```csharp
// if (!context.ClientCertificate.Issuer.Contains("Expected Issuer"))
// {
//     context.Fail("Certificate issuer not trusted");
// }
```

عند تفعيل mTLS، يمكن لأي شهادة عميل من أي جهة إصدار (تتسلسل إلى جذر موثوق) المصادقة — لا يُطبَّق أي قيد على المستأجر/الجهة المُصدِرة.

---

### 6. التحقق من إلغاء شهادة mTLS مُعطَّل افتراضياً ✅ تم الإصلاح في الإيداع fd3d7b3

**الملفات:** `Models/Settings/MtlsSettings.cs` (السطر 26)، `appsettings.template.json`

`CheckCertificateRevocation` يُعيَّن افتراضياً على `false` في كل من النموذج والقالب. يمكن استخدام شهادات العملاء الملغاة للمصادقة إلى أجل غير مسمى. في mTLS الإنتاجي، يجب أن يُفعَّل التحقق من الإلغاء افتراضياً.

---

### 7. التحقق من OCSP هو محاكاة تُعيد دائماً "صالحة" ✅ تم الإصلاح في الإيداع b4c3807

**الملف:** `Services/OcspValidationService.cs` (الأسطر 149–163)

طريقة `PerformOcspValidationAsync` هي صراحةً "تنفيذ قالب" تُعيد دائماً `IsValid = true` بعد `Task.Delay(100)`. إذا فُعّل التحقق من OCSP في التكوين، فسيمرّر بصمت جميع الشهادات — بما في ذلك الملغاة — على أنها صالحة، مع تسجيل تحذير يسهل تفويته.

---

### 8. المصادقة والتفويض مُعطَّلان افتراضياً ✅ تم الإصلاح في الإيداع b392c47

**الملف:** `appsettings.json` (الأسطر 16–17)

```json
"EnableAzureAd": false,
"EnableAuthorization": false
```

يُشحَن التكوين الافتراضي بدون مصادقة أو تفويض. المطوّر الذي ينسخ `appsettings.template.json` (الذي يحتوي أيضاً على هذه القيم معطّلة) دون قراءة التوثيق بعناية سينشر تطبيقاً مفتوحاً. يجب أن تتطلب الإعدادات الافتراضية في القالب إلغاء تفعيلاً متعمداً، وليس تفعيلاً متعمداً.

---

### 9. رؤوس الأمان تُطبَّق بعد التوجيه/المصادقة ✅ تم الإصلاح في الإيداع 016e57c

**الملف:** `Program.cs` (الأسطر 130–152)

تُستدعى `UseNonceAndSecurityHeadersAsync` و`UseStandardSecurityHeaders` بعد `UseRouting` و`UseAuthentication` و`UseAuthorization`. الاستجابات التي تُقصر المسار قبل الوصول إلى هذه middleware (مثل إعادة توجيه 401، أو رفض 403) قد لا تحصل على رؤوس الأمان. يجب أن تكون رؤوس الأمان في أقرب نقطة ممكنة في المسار.

---

## 🟡 متوسط

### 10. ملف تعريف ارتباط الجلسة يفتقر إلى سمات `Secure` و`SameSite` ✅ تم الإصلاح في الإيداع 8f2223c

**الملف:** `Extensions/ServiceCollectionExtensions.cs` (الأسطر 41–46)

يُعيَّن ملف تعريف ارتباط الجلسة على `HttpOnly = true` و`IsEssential = true`، لكنه يُغفل `Cookie.SecurePolicy = CookieSecurePolicy.Always` و`Cookie.SameSite = SameSiteMode.Strict`. يمكن إرسال ملف تعريف الارتباط عبر HTTP العادي (إذا لم يُطلَق إعادة التوجيه بعد) أو إرساله عبر المواقع.

---

### 11. رأس `Set-Cookie` عمومي مشوّه ✅ تم الإصلاح في الإيداع 8f2223c

**الملف:** `Extensions/ApplicationBuilderExtensions.cs` (السطر 73)

```csharp
context.Response.Headers.Append("Set-Cookie", "path=/; Secure; HttpOnly; SameSite=Strict");
```

يُضيف هذا رأس `Set-Cookie` بلا اسم ولا قيمة لكل استجابة. هذا غير صالح وسيُتجاهل (أو يُرفض) من المتصفحات، لكنه يُنتج آثاراً غريبة في جميع الاستجابات بما في ذلك الملفات الثابتة واستجابات JSON API وفحوصات الصحة. يجب تعيين أمان ملف تعريف الارتباط في خيارات ملف تعريف الارتباط المحدد المُكوَّن، وليس كرأس خام عمومي.

---

### 12. `Content-Type` مُجبَر على `text/html` لجميع الاستجابات ✅ تم الإصلاح في الإيداع 8f2223c

**الملف:** `Extensions/ApplicationBuilderExtensions.cs` (السطر 72)

```csharp
context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");
```

يُكتب هذا فوق Content-Type لكل استجابة — نقاط نهاية API وJSON والتنزيلات الثنائية والملفات الثابتة ستُدّعي جميعها أنها `text/html`. يتعارض هذا مع `X-Content-Type-Options: nosniff`، الذي يمنع المتصفحات من تجاوز نوع المحتوى المُعلَن.

---

### 13. `AllowedHosts` مُعيَّن على حرف بدل ✅ تم الإصلاح في الإيداع 8f2223c

**الملفات:** `appsettings.json` (السطر 11)، `appsettings.template.json` (السطر 36)

```json
"AllowedHosts": "*"
```

يُعطّل هذا التحقق المدمج من رأس المضيف في ASP.NET Core. تتيح هجمات حقن رأس المضيف تسميم ذاكرة التخزين المؤقت وتسميم روابط إعادة تعيين كلمة المرور وإعادة التوجيه المفتوحة. يجب تعيين هذا على النطاق/النطاقات المحددة التي يُخدَم منها التطبيق.

---

### 14. التخطيط لا يُطبّق nonce على وسوم `<script>` ✅ تم الإصلاح في الإيداع 8f2223c

**الملف:** `Views/Shared/_Layout.cshtml`

يُحمّل التخطيط عدة ملفات JavaScript (`jquery.min.js` و`bootstrap.bundle.min.js` و`site.js`) لكن لا يتضمن أي وسم `<script>` قيمة `nonce="@Context.Items["Nonce"]"`. إذا فُعّل CSP مع nonces، ستُحظر هذه السكريبتات من المتصفح. تنفيذ nonce مُوصَّل في middleware لكنه غير مُستهلَك في العروض، مما يجعل نظام CSP nonce غير فعّال.

---

### 15. رأس Referrer-Policy مفقود ✅ تم الإصلاح في الإيداع 8f2223c

**الملف:** `Extensions/ApplicationBuilderExtensions.cs`

لا تتضمن رؤوس الأمان القياسية `Referrer-Policy`. بدون هذا، يُرسل المتصفح URL الكامل في رأس `Referer` للموارد الخارجية (مثل CDN ArcGIS المضمّن في CSP)، مما قد يُسرّب مسارات الجلسات المُصادَق عليها.

---

## 🔵 منخفض / إعلامي

### 16. تسجيل PII بنص صريح ✅ تم الإصلاح في الإيداع 93bb4e9

**الملف:** `Services/LoggingHelper.cs` (الأسطر 85، 105)

يُسجَّل OID المستخدم والبريد الإلكتروني والاسم ومعرف الجلسة والأدوار حرفياً مع كل طلب مُصادَق:

```csharp
_logger.LogInformation("{0} {1} called in Session {2} User-Oid {3} Email {4} Name {5}",
    DateTime.UtcNow, methodName, userClaims.Sid, userClaims.Oid, userClaims.Email, userClaims.Name);

_logger.LogInformation("{0} Oid carries the following permissions: {1}", userClaims.Oid, sb.ToString());
```

بناءً على لوائح الخصوصية المعمول بها (GDPR وCCPA وHIPAA)، قد يكون هذا مشكلة امتثال. فكّر في إخفاء المعرّفات أو تجزئتها في مخرجات السجل وتوجيه السجلات التي تحتوي على PII إلى مستودع مُتحكَّم فيه بشكل مناسب. يمكن الحفاظ على هدف إعادة بناء الجلسة الجنائية بتسجيل تجزئات HMAC-SHA256 متسقة للمعرّفات بدلاً من قيمها الصريحة.

---

### 17. جزء من سلسلة الاتصال في السجلات ✅ تم الإصلاح في الإيداع 93bb4e9

**الملف:** `Extensions/ServiceCollectionExtensions.cs` (السطر 404)

```csharp
logger.LogInformation("Cosmos connection string sample (last 5): {Sample}",
    cosmosSettings.CosmosConnectionString[^5..]);
```

حتى جزء صغير من السر في السجلات ليس ممارسةً جيدة. يجب على عبارة السجل بدلاً من ذلك تأكيد وجود سلسلة الاتصال (غير فارغة) دون تسجيل أي جزء منها.

---

### 18. عمليات Key Vault هي محاكاة ✅ تم الإصلاح في الإيداع 93bb4e9

**الملف:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

كلتا `GetCertificateFromKeyVault` و`GetSecretFromKeyVault` هما محاكاتا قالب تُعيدان `null`/قيماً وهمية. مع تفعيل Key Vault، تُعيد `GetCertificateFromKeyVault` قيمة `null`، مما يتسبب في `InvalidOperationException` عند بدء التشغيل — فشل سريع جيد، لكنه يعني أيضاً أنه لا يوجد تكامل فعلي مع Key Vault لمراجعة معالجة الأسرار.

---

### 19. رأس `X-XSS-Protection: 1; mode=block` مهجور ✅ تم الإصلاح في الإيداع 93bb4e9

**الملف:** `Extensions/ApplicationBuilderExtensions.cs` (السطر 70)

أزالت المتصفحات الحديثة الدعم لـ `X-XSS-Protection`. الرأس ليس ضاراً، لكنه يُعطي شعوراً زائفاً بالأمان. النهج الموصى به هو الاعتماد على CSP قوي بدلاً من ذلك. تُعدّ قيمة `0` (تعطيل مدقق XSS) أحياناً أأمن من `1; mode=block` للمتصفحات القديمة لأن المدقق ذاته كان يحتوي على سلوكيات قابلة للاستغلال.
