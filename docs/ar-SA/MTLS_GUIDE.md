# دليل مصادقة شهادة عميل mTLS (TLS المتبادل)

## نظرة عامة

يدعم هذا المشروع الآن مصادقة **TLS المتبادل (mTLS)**، التي تتطلب من كل من الخادم والعميل تقديم شهادات صالحة. يوفر هذا أماناً مُعزَّزاً من خلال المصادقة ثنائية الاتجاه.

## ما هو mTLS؟

يُوسّع mTLS بروتوكول TLS القياسي بإضافة:
1. **شهادة الخادم**: يُقدّم الخادم شهادة لإثبات هويته (HTTPS القياسي)
2. **شهادة العميل**: يُقدّم العميل أيضاً شهادة لإثبات هويته (إضافة mTLS)

## التكوين

### 1. علم الميزة

تفعيل mTLS في `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. إعدادات mTLS

تكوين سلوك mTLS في `appsettings.json`:

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "my-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

#### خيارات التكوين

| الإعداد | النوع | الافتراضي | الوصف |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | إذا كان true، شهادة العميل إلزامية |
| `AllowCertificateChains` | bool | `true` | السماح بالشهادات المتسلسلة (الموقّعة من CA) |
| `AllowSelfSignedCertificates` | bool | `false` | السماح بالشهادات الموقّعة ذاتياً (للتطوير فقط) |
| `CheckCertificateRevocation` | bool | `false` | إجراء فحص إلغاء عبر الإنترنت |
| `ClientCertificateName` | string | null | اسم الشهادة في Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | التحقق من جهة إصدار الشهادة |

### 3. شهادة الخادم (Azure Key Vault)

تُسترد شهادة الخادم من Azure Key Vault:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## تعليمات الإعداد

### المتطلبات الأساسية

1. Azure Key Vault مع الأذونات المناسبة
2. شهادة الخادم مُخزَّنة في Azure Key Vault كسر (بتنسيق PFX)
3. شهادات العميل (يمكن توليدها أو الحصول عليها من CA)

### الخطوة 1: تحميل شهادة الخادم إلى Key Vault

```bash
# تحويل الشهادة إلى PFX إذا لزم الأمر
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# التحميل إلى Key Vault باستخدام Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# تخزين كلمة المرور كسر منفصل
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### الخطوة 2: توليد شهادات العميل

#### الخيار أ: شهادة موقّعة ذاتياً (للتطوير فقط)

```powershell
# توليد شهادة العميل
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# التصدير إلى PFX
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### الخيار ب: موقّعة من CA (للإنتاج)

تعاون مع مرجعيتك الشهادة للحصول على شهادات العميل.

### الخطوة 3: تكوين التطبيق

تحديث `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert",
    "KeyVaultPassName": "server-cert-password"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false
  }
}
```

### الخطوة 4: الاختبار بشهادة عميل

#### باستخدام cURL:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### باستخدام PowerShell:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### باستخدام المتصفح:

1. استيراد شهادة العميل في مخزن شهادات المتصفح
2. الانتقال إلى تطبيقك
3. سيطلب المتصفح تحديد شهادة العميل

## السلوك الخاص بالبيئة

### بيئة التطوير
- تُحمَّل شهادة الخادم من Key Vault (إذا كانت متاحة)
- شهادات العميل **اختيارية** (وضع `AllowCertificate`)
- يمكن السماح بالشهادات الموقّعة ذاتياً

### بيئة الإنتاج
- تُحمَّل شهادة الخادم من Key Vault
- شهادات العميل **إلزامية** إذا كان `EnableMtls = true`
- يُوصى بالشهادات المتسلسلة فقط

## أفضل ممارسات الأمان

### ✅ افعل:
- استخدم شهادات موقّعة من CA في الإنتاج
- خزّن الشهادات في Azure Key Vault
- فعّل التحقق من إلغاء الشهادات في الإنتاج
- تحقق من جهة إصدار الشهادة
- استخدم كلمات مرور قوية لملفات PFX
- دوّر الشهادات بانتظام

### ❌ لا تفعل:
- استخدام شهادات موقّعة ذاتياً في الإنتاج
- إيداع الشهادات في التحكم بالمصدر
- مشاركة شهادات العميل بين المستخدمين
- تعطيل التحقق من الشهادات في الإنتاج

## استكشاف الأخطاء وإصلاحها

### الخطأ: "No client certificate provided"

**السبب**: العميل لم يُرسل شهادة  
**الحل**: 
- تحقق من تثبيت شهادة العميل
- تحقق من إعداد `RequireClientCertificate`
- تأكد من أن الشهادة موثوقة من النظام

### الخطأ: "Certificate chain validation failed"

**السبب**: الشهادة غير موثوقة  
**الحل**:
- ثبّت شهادة CA الجذر
- عيّن `AllowSelfSignedCertificates = true` للاختبار
- تحقق من أن الشهادة لم تنتهِ صلاحيتها

### الخطأ: "Server certificate not retrieved from Key Vault"

**السبب**: مشكلة وصول إلى Azure Key Vault  
**الحل**:
- تحقق من أذونات Key Vault
- تحقق من بيانات اعتماد Azure AD
- تأكد من تكوين Managed Identity

## التسجيل

تُسجَّل أحداث مصادقة mTLS:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## التكامل مع المصادقة الموجودة

يعمل mTLS جنباً إلى جنب مع مصادقة Azure AD:

1. **التحقق من شهادة العميل** يحدث أولاً (طبقة النقل)
2. **مصادقة Azure AD** تحدث بعد ذلك (طبقة التطبيق)

يمكن تفعيل كليهما في آن واحد للحصول على أمان متعمق الدفاع.

## المراجع

- [توثيق Microsoft: مصادقة الشهادات](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [تكامل Azure Key Vault](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## مثال الكود

يمكن العثور على التنفيذ في:
- `Models/Settings/MtlsSettings.cs` - نموذج التكوين
- `Models/Settings/FeatureFlags.cs` - علم الميزة
- `Extensions/ServiceCollectionExtensions.cs` - تسجيل الخدمة
- `Program.cs` - بدء تشغيل التطبيق

## موارد إضافية

راجع `SupportingScripts/CertificateUploaderToAzureExample.ps1` لأمثلة تحميل الشهادات.
