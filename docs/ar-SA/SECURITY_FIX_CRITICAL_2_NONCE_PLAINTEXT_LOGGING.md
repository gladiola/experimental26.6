# إصلاح أمني: تسجيل قيم nonce بنص صريح (حرج #2)

**تم الإصلاح في:** `Services/NonceMiddleware.cs`، `Services/NonceRefresherService.cs`  
**الاختبارات:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## ما كان خاطئاً

سجّل موقعان قيمة nonce الخاصة بـ CSP حرفياً في تدفق سجل التطبيق:

**`Services/NonceMiddleware.cs` (السطر 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (السطر 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### لماذا هذا أمر حرج

nonce الخاص بـ CSP هو *الآلية الوحيدة* التي تمنع حقن السكريبت المضمّن بمجرد تفعيل CSP. أمانه يعتمد كلياً على كونه **سرياً طوال فترة استجابة واحدة**.

في بيئة سحابية/مؤسسية، يمكن عادةً قراءة سجلات التطبيق من قِبل:
* فرق العمليات
* خدمات تجميع السجلات (مثل Azure Monitor وSplunk وELK)
* أي حساب يملك صلاحية القراءة على مستودع السجلات

يستطيع أي شخص يمكنه قراءة سطر سجل يحتوي على `Nonce: <القيمة>` حقن وسم `<script>` مضمّن بهذه القيمة وجعل المتصفح ينفّذه، متجاوزاً CSP بالكامل. حتى لو دار nonce مع كل طلب، يستطيع المهاجم الذي يملك وصولاً حياً للسجلات التصرف ضمن نافذة نفس الطلب.

---

## ما تم إصلاحه

استُبدلت عبارتا التسجيل برسائل تؤكد *حالة* توليد nonce دون الكشف عن قيمته:

**`NonceMiddleware.cs`:**
```csharp
// قبل (قابل للاستغلال):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// بعد (آمن):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// قبل (قابل للاستغلال):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// بعد (آمن):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## كيفية الحفاظ على الإصلاح

1. **لا تسجّل قيمة nonce أبداً.** يمكن لرسائل السجل تأكيد توليد nonce أو استرداده (حالة نجاح/فشل)، لكن سلسلة nonce ذاتها يجب ألا تظهر في أي معامل سجل أو حقل تسجيل منظّم أو تضمين سلسلة.

2. **راجع أي عبارة تسجيل جديدة في الكود المرتبط بـ nonce** (`NonceMiddleware`، `OptimizedNonceMiddleware`، `NonceRefresherService`، `NonceCatalogService`) للتأكد من عدم تضمين قيمة nonce.

3. **لا تكشف عن nonce في بيانات القياس أو المقاييس أو التتبع الموزع** للأسباب ذاتها. غالباً ما تُعاد توجيه سمات التتبع ووسوم الامتداد إلى خلفيات تجميع السجلات.

4. **يجب التعامل مع nonce باعتباره سراً لكل طلب.** يمكن تخزينه في `HttpContext.Items` للاستخدام ضمن مسار عرض طلب واحد، لكن يجب ألا يغادر العملية عبر أي قناة قابلة للرصد باستثناء رأس استجابة HTTP وسمة `nonce="..."` في HTML التي يؤمّنها.

### الاختبارات التي تُنفّذ هذا الإصلاح

| الاختبار | ما يرصده |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | يفشل إذا أُعيدت إضافة سلسلة nonce إلى أي رسالة سجل في `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | يفشل إذا أُعيدت إضافة سلسلة nonce إلى أي رسالة سجل في `NonceMiddleware` |
