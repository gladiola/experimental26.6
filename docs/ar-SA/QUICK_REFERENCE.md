# بطاقة المرجع السريع - قالب Razor Pages

## 🚀 البدء السريع (5 دقائق)

```powershell
# 1. تشغيل سكريبت الإعداد
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. البناء والتشغيل
dotnet build
dotnet run
```

## 📁 ملفات التكوين

| الملف | الغرض | مُدرَج في Git؟ |
|------|---------|------------|
| `appsettings.template.json` | قالب مع عناصر نائبة | ✅ نعم |
| `appsettings.json` | تكوينك الفعلي | ❌ لا (مُستثنى من git) |
| أسرار المستخدم | القيم الحساسة | ❌ لا (محلي فقط) |

## 🔧 أعلام الميزات (تفعيل/تعطيل سريع)

عدّل `appsettings.json` ← قسم `FeatureFlags`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // 🔐 فعّل للمصادقة
  "EnableNonceServices": false,  // 🔒 فعّل لـ CSP
  "EnableCosmosDb": false,       // 🗄️ فعّل لقاعدة البيانات
  "EnableBlobStorage": false     // 📦 فعّل للملفات
}
```

## 🔑 أوامر أسرار المستخدم

```powershell
# تهيئة
dotnet user-secrets init

# تعيين سر
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# سرد جميع الأسرار
dotnet user-secrets list

# إزالة سر
dotnet user-secrets remove "AzureAd:ClientSecret"

# مسح جميع الأسرار
dotnet user-secrets clear
```

## 📋 الأسرار المطلوبة حسب الميزة

### مصادقة Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# ولّد أولاً: .\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
dotnet user-secrets set "CosmosDb:AccountKey" "your-account-key"
```

### Blob Storage
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## 🛠️ السكريبتات المفيدة

| السكريبت | الغرض | الاستخدام |
|--------|---------|-------|
| `SetupFromTemplate.ps1` | الإعداد الأولي | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | تغيير مساحة الاسم | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | توليد المفاتيح | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | حساب تجزئات CSP | `.\HashInlineScriptPowerShell.ps1` |

## 🗺️ مراحل التطوير

### المرحلة 1: أساسية (إعداد 5 دقائق)
- ✅ الجلسة
- ✅ التوطين
- ✅ رؤوس الأمان
- ❌ بدون مصادقة
- ❌ بدون قاعدة بيانات

**التكوين**: جميع الأعلام `false` باستثناء `EnableSession` و`EnableLocalization` و`EnableSecurityHeaders`

### المرحلة 2: + المصادقة (إعداد 30 دقيقة)
- ✅ ميزات المرحلة 1
- ✅ Azure AD
- ✅ التفويض
- ✅ CSP + Nonce
- ❌ بدون قاعدة بيانات

**التكوين**: تفعيل `EnableAzureAd` و`EnableAuthorization` و`EnableNonceServices` و`EnableCSP`

**المتطلبات**:
- تسجيل تطبيق Azure AD
- مفاتيح تشفير مُولَّدة

### المرحلة 3: + خدمات Azure (إعداد ساعة إلى ساعتين)
- ✅ ميزات المرحلة 2
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**التكوين**: تفعيل `EnableCosmosDb` و`EnableBlobStorage` و`EnableKeyVault`

**المتطلبات**:
- موارد Azure مُنشأة
- سلاسل اتصال في أسرار المستخدم

## 🔍 استكشاف الأخطاء السريع

### أخطاء البناء
```powershell
# تنظيف وإعادة البناء
dotnet clean
dotnet build

# التحقق من الحزم المفقودة
dotnet restore
```

### "Configuration not found"
```powershell
# التحقق من وجود الملف
Test-Path appsettings.json

# إذا كان مفقوداً، انسخ من القالب
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
# سرد الأسرار
dotnet user-secrets list

# إعادة تشغيل الإعداد
.\SupportingScripts\SetupFromTemplate.ps1
```

### حلقة المصادقة / أخطاء 401
1. تحقق من أن URI إعادة التوجيه في Azure AD يتطابق
2. تحقق من `EnableAzureAd: true` في appsettings.json
3. تحقق من سر العميل في أسرار المستخدم
4. امسح كعكات المتصفح

### انتهاكات CSP
1. تحقق من `EnableNonceServices: true`
2. تحقق من تعيين مفاتيح التشفير
3. راجع وحدة تحكم المتصفح لأخطاء CSP
4. عطّل CSP مؤقتاً للاختبار: `EnableCSP: false`

## 📖 التوثيق

- **التوثيق الكامل**: `TEMPLATE_README.md`
- **التكوين**: `appsettings.template.json`
- **مساحة الاسم**: شغّل `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ✅ قائمة تحقق الأمان

قبل النشر للإنتاج:

- [ ] جميع الأسرار في Azure Key Vault أو أسرار المستخدم
- [ ] `appsettings.json` مُستثنى من git
- [ ] `.gitignore` يتضمن الاستثناءات الخاصة بالقالب
- [ ] رؤوس الأمان مُفعَّلة
- [ ] CSP مُكوَّن مع nonces
- [ ] HTTPS مُفرَض
- [ ] المصادقة مُفعَّلة للصفحات المحمية
- [ ] الأسرار مُدوَّرة من القيم الافتراضية

## 💡 نصائح

- **ابدأ بسيطاً**: ابدأ بالمرحلة 1، أضف الميزات تدريجياً
- **استخدم WhatIf**: اختبر السكريبتات بـ `-WhatIf` قبل التطبيق
- **تحقق من السجلات**: فعّل `"Default": "Debug"` في `Logging:LogLevel` لاستكشاف الأخطاء
- **تحقق من الأسرار**: شغّل `dotnet user-secrets list` لرؤية ما هو مُكوَّن
- **بنيات نظيفة**: إذا ظهرت أخطاء غريبة، جرّب `dotnet clean && dotnet build`

## ❓ المساعدة

1. اقرأ `TEMPLATE_README.md`
2. تحقق من تعليقات `appsettings.template.json`
3. شغّل `dotnet user-secrets list`
4. فعّل تسجيل التصحيح
5. تحقق من Azure Portal لحالة الموارد

---

**إصدار القالب**: 1.0  
**ASP.NET Core**: 9.0  
**آخر تحديث**: 2024-12-20
