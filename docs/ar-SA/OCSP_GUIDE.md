# دليل تنفيذ OCSP (بروتوكول حالة الشهادة عبر الإنترنت)

## نظرة عامة

يتضمن هذا المشروع **دعم قالب** للتحقق من شهادات OCSP. يتيح OCSP التحقق الفوري من حالة إلغاء الشهادة قبل معالجة طلبات الويب.

## ما هو OCSP؟

يوفر OCSP بديلاً لقوائم إلغاء الشهادات (CRL) للتحقق مما إذا كانت شهادة قد أُلغيت:

- **التحقق الفوري**: يفحص حالة الشهادة فوراً
- **الكفاءة**: يستعلم فقط عن حالة الشهادات المحددة
- **خفة الحجم**: استجابات أصغر من تنزيلات CRL الكاملة
- **التحديث المستمر**: يمتلك دائماً معلومات إلغاء حديثة

## التكوين

### 1. علم الميزة

تفعيل التحقق من OCSP في `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. إعدادات OCSP

تكوين سلوك OCSP في `appsettings.json`:

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.yourcompany.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

### خيارات التكوين

| الإعداد | النوع | الافتراضي | الوصف |
|---------|------|---------|-------------|
| `EnableOcspValidation` | bool | `false` | تفعيل/تعطيل التحقق من OCSP |
| `OcspServerUrl` | string | `null` | URL خادم OCSP الخاص بك |
| `RequestTimeoutSeconds` | int | `30` | مهلة انتظار طلبات OCSP |
| `MaxRetryAttempts` | int | `3` | عدد المحاولات عند الفشل |
| `CacheDurationMinutes` | int | `60` | مدة تخزين استجابات OCSP مؤقتاً |
| `ServerUnavailableBehavior` | string | `"Warn"` | السلوك عند توقف الخادم: `"Fail"` أو `"Allow"` أو `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | تفعيل التسجيل التفصيلي |
| `SkipValidationInDevelopment` | bool | `true` | تخطي OCSP في وضع التطوير |

---

## تنفيذ القالب

التنفيذ الحالي هو **قالب** يُوضح البنية وتصميم API. لاستخدام OCSP في الإنتاج، يجب عليك:

### 1. تنفيذ بروتوكول OCSP

استبدل طريقة `PerformOcspValidationAsync` في `OcspValidationService.cs` بتنفيذ بروتوكول OCSP الفعلي:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO: تنفيذ بروتوكول OCSP الفعلي
    // 1. بناء طلب OCSP
    // 2. إرساله إلى خادم OCSP
    // 3. تحليل استجابة OCSP
    // 4. التحقق من توقيع الاستجابة
    // 5. إعادة حالة الشهادة
}
```

### 2. بناء خادم OCSP

تحتاج إلى خادم OCSP منفصل يقوم بـ:
- قبول طلبات OCSP (بتنسيق RFC 6960)
- التحقق من حالة الشهادة في قاعدة بيانات CA الخاصة بك
- إعادة استجابات OCSP موقّعة

**الخيارات:**
- استخدام خدمة OCSP تجارية (مثل DigiCert وLet's Encrypt)
- بناء خادم OCSP مخصص باستخدام مكتبات:
  - **OpenSSL** - مكتبة C/C++ مع دعم OCSP
  - **BouncyCastle** - مكتبة .NET لـ OCSP
  - **Python** - مكتبة `cryptography` مع دعم OCSP

---

## مثال الاستخدام

### التحقق الأساسي من الشهادة

```csharp
public class MyCertificateHandler
{
    private readonly IOcspValidationService _ocspService;

    public MyCertificateHandler(IOcspValidationService ocspService)
    {
        _ocspService = ocspService;
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
    {
        // فحص منطقي بسيط
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### التحقق التفصيلي من الحالة

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    // فحص الحالة
    switch (result.Status)
    {
        case OcspStatus.Good:
            logger.LogInformation("Certificate is valid");
            return result;

        case OcspStatus.Revoked:
            logger.LogError("Certificate has been revoked!");
            throw new SecurityException("Certificate revoked");

        case OcspStatus.Unknown:
            logger.LogWarning("Certificate status unknown");
            // المعالجة بناءً على السياسة
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP server unavailable");
            // السلوك الاحتياطي بناءً على إعداد ServerUnavailableBehavior
            break;
    }

    return result;
}
```

---

## التكامل مع mTLS

يعمل OCSP بسلاسة مع مصادقة شهادات mTLS:

```csharp
// في ServiceCollectionExtensions.cs
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// في حدث التحقق من الشهادة
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        // إجراء التحقق من OCSP
        var ocspService = context.HttpContext.RequestServices
            .GetRequiredService<IOcspValidationService>();

        var isValid = await ocspService.ValidateCertificateAsync(
            context.ClientCertificate);

        if (!isValid)
        {
            context.Fail("Certificate validation failed via OCSP");
        }
    }
};
```

---

## سلوكيات عدم توفر الخادم

### "Fail" - أمان صارم

```json
"ServerUnavailableBehavior": "Fail"
```

- يرفض الطلبات عند توقف خادم OCSP
- الخيار الأكثر أماناً
- قد يتسبب في مشاكل التوفر

**استخدم عند:** الأمان القصوى مطلوب، التحقق من الشهادة حرج

### "Allow" - التوفر العالي

```json
"ServerUnavailableBehavior": "Allow"
```

- يسمح بالطلبات عند توقف خادم OCSP
- يُعطي الأولوية للتوفر على الأمان
- يُسجّل تحذيرات

**استخدم عند:** توفر الخدمة أهم من التحقق الفوري

### "Warn" - متوازن (الافتراضي)

```json
"ServerUnavailableBehavior": "Warn"
```

- يسمح بالطلبات لكن يُسجّل تحذيرات
- نهج متوازن
- يُمكّن المراقبة/التنبيه

**استخدم عند:** تريد مراقبة مشاكل OCSP دون حظر حركة المرور

---

## التخزين المؤقت

تُخزَّن استجابات OCSP مؤقتاً لتقليل الحمل على الخادم:

```json
"CacheDurationMinutes": 60
```

**الفوائد:**
- يُقلل استعلامات خادم OCSP
- يُحسّن الأداء
- يوفر مرونة خلال الانقطاعات القصيرة

**إبطال التخزين المؤقت:**
- تلقائي بعد انتهاء مدة التخزين
- يدوي: إعادة تشغيل التطبيق

---

## اعتبارات الأمان

### ✅ افعل:

- استخدم HTTPS لـ URL خادم OCSP
- تحقق من توقيعات استجابة OCSP
- اضبط مدة التخزين المؤقت المناسبة (وازن بين الحداثة والأداء)
- استخدم سلوك "Fail" في البيئات عالية الأمان
- راقب توفر خادم OCSP
- نفّذ منطق إعادة المحاولة للأعطال العابرة
- سجّل جميع إخفاقات التحقق من OCSP

### ❌ لا تفعل:

- استخدام HTTP لـ OCSP في الإنتاج
- تخطي التحقق من توقيع استجابة OCSP
- تخزين الاستجابات لفترة طويلة (أكثر من 24 ساعة)
- تجاهل إخفاقات خادم OCSP بصمت
- تعطيل OCSP في الإنتاج بدون مبرر

---

## تنفيذ خادم OCSP

### الخيار 1: OpenSSL OCSP Responder

```bash
# تشغيل OpenSSL OCSP responder
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### الخيار 2: BouncyCastle (.NET)

```csharp
// مثال باستخدام مكتبة BouncyCastle
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. تحليل الطلب
        // 2. التحقق من حالة الشهادة في قاعدة البيانات
        // 3. بناء الاستجابة
        // 4. توقيع الاستجابة
        // 5. إعادة الاستجابة الموقّعة
    }
}
```

### الخيار 3: خدمة OCSP التجارية

- **DigiCert**: خدمة OCSP مُدارة
- **Let's Encrypt**: OCSP مجاني لشهاداتهم
- **GlobalSign**: حلول OCSP للمؤسسات

---

## المراقبة والتسجيل

### تفعيل التسجيل التفصيلي

```json
{
  "OcspSettings": {
    "EnableDetailedLogging": true
  },
  "Logging": {
    "LogLevel": {
      "WebAppExperimental266.Services.OcspValidationService": "Debug"
    }
  }
}
```

### رسائل السجل

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## الاختبار

### اختبارات الوحدة

تشغيل اختبارات OCSP:

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### الاختبار اليدوي

1. **تعطيل OCSP** - التحقق من عمل التطبيق بدون OCSP
2. **URL غير صالح** - اختبار إعدادات ServerUnavailableBehavior
3. **شهادة صالحة** - يجب أن تُعيد `OcspStatus.Good`
4. **الاستجابة المُخزَّنة مؤقتاً** - التحقق من عمل التخزين المؤقت

---

## اعتبارات الأداء

### تكوين التخزين المؤقت

```json
"CacheDurationMinutes": 60  // تخزين مؤقت لساعة واحدة
```

**المقايضات:**
- **مدة قصيرة (5-15 دقيقة)**: بيانات أحدث، حمل OCSP أعلى
- **مدة طويلة (60-120 دقيقة)**: أداء أفضل، خطر بيانات قديمة

### إعدادات المهلة

```json
"RequestTimeoutSeconds": 30
"MaxRetryAttempts": 3
```

**التوصيات:**
- المهلة: 10-30 ثانية للإنتاج
- المحاولات: 2-3 محاولات للأعطال العابرة

---

## استكشاف الأخطاء وإصلاحها

### المشكلة: خادم OCSP غير متاح دائماً

**الحلول:**
1. تحقق من صحة `OcspServerUrl`
2. تحقق من أن جدار الحماية يسمح بـ HTTPS الصادر
3. تحقق من تشغيل خادم OCSP
4. راجع السجلات لأخطاء المهلة

### المشكلة: جميع الشهادات تفشل في التحقق

**الحلول:**
1. تحقق من أن خادم OCSP يمتلك بيانات حالة الشهادة
2. تحقق من اكتمال سلسلة الشهادات
3. تأكد من صحة توقيع استجابة OCSP
4. راجع سجلات خادم OCSP

### المشكلة: التخزين المؤقت لا يعمل

**الحلول:**
1. تحقق من `CacheDurationMinutes > 0`
2. تحقق من استخدام نفس بصمة الشهادة
3. أعد تشغيل التطبيق لمسح التخزين المؤقت

---

## الخطوات التالية

لجعل OCSP يعمل بالكامل:

1. ✅ **التكوين مكتمل** - الإعدادات جاهزة
2. ✅ **واجهة الخدمة مكتملة** - API مُعرَّف
3. ✅ **الاختبارات مكتملة** - أكثر من 30 اختبار وحدوي مُضمَّن
4. ⏳ **بروتوكول OCSP** - يحتاج تنفيذ RFC 6960
5. ⏳ **خادم OCSP** - يحتاج نشر OCSP responder
6. ⏳ **التكامل** - الربط بمصادقة mTLS

---

## المراجع

- [RFC 6960](https://tools.ietf.org/html/rfc6960) - مواصفة OCSP
- [توثيق BouncyCastle](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [مصادقة شهادات Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**الحالة:** ✅ القالب جاهز  
**بروتوكول OCSP:** ⏳ قيد التنفيذ  
**خادم OCSP:** ⏳ قيد النشر  
**الاختبارات:** ✅ أكثر من 30 اختبار مُضمَّن
