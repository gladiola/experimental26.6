# WebAppExperimental266

تطبيق ويب ASP.NET Core 9 Razor Pages يتضمن مصادقة Azure AD، وبروتوكول TLS المتبادل (mTLS)، وإدارة الشهادات عبر Azure Key Vault، وAzure Cosmos DB، وAzure Blob Storage، وAWS Secrets Manager، وAmazon DynamoDB، وGCP Secret Manager، وGCP Firestore، وطبقة أمان HTTP مُعززة مع سياسة أمان المحتوى المستندة إلى nonce.

---

## جدول المحتويات

- [الميزات](#الميزات)
- [أعلام الميزات](#أعلام-الميزات)
- [المتطلبات الأساسية](#المتطلبات-الأساسية)
- [التثبيت – Windows Azure (App Service)](#التثبيت--windows-azure-app-service)
- [التثبيت – خادم OpenBSD مع خدمات Azure](#التثبيت--خادم-openbsd-مع-خدمات-azure)
- [مرجع التكوين](#مرجع-التكوين)
- [النصوص البرمجية المساعدة](#النصوص-البرمجية-المساعدة)
- [ملاحظات الأمان](#ملاحظات-الأمان)

---

## الميزات

### مصادقة Azure AD (OpenID Connect)
يُصادق التطبيق المستخدمين عبر **منصة هوية Microsoft** باستخدام بروتوكول OpenID Connect (عبر `Microsoft.Identity.Web`). تتطلب جميع المسارات ضمن `/Experimental` هوية Azure AD مُصادقاً عليها. صفحات `/Privacy` و`/Error` و`/About` متاحة للعموم.

### مصادقة mTLS بشهادة العميل
عند التفعيل، يجب على العملاء تقديم شهادة X.509 صالحة. تتحكم الإعدادات في `MtlsSettings` في أنواع الشهادات المسموح بها (المتسلسلة أو الموقّعة ذاتياً أو كلتيهما)، والتحقق من إلغاء الشهادات، والجهات المُصدِرة المسموح بها.

### تكامل Azure Key Vault
يستعيد التطبيق **شهادة الخادم** TLS من Azure Key Vault عند بدء التشغيل. يُحقن `X509Certificate2` المُحمَّل مباشرةً في إعدادات HTTPS الافتراضية لـ Kestrel، دون الحاجة إلى ملف PFX على القرص.

### سياسة أمان المحتوى مع nonce لكل طلب
عند التفعيل، تحمل كل استجابة HTTP رأساً `Content-Security-Policy` يتضمن توجيه `script-src` قيمة **nonce عشوائية مشفرة** لكل طلب. كما تدعم سياسة أمان المحتوى قوائم السماح المستندة إلى تجزئة SHA-256 للنصوص البرمجية المضمّنة.

### رؤوس أمان HTTP القياسية
يُضيف `UseStandardSecurityHeaders` إلى كل استجابة: `X-Frame-Options`، و`X-Content-Type-Options`، و`Strict-Transport-Security`، و`Referrer-Policy`، و`Cross-Origin-Opener-Policy`، و`Cross-Origin-Resource-Policy`، و`Permissions-Policy`، ويزيل رؤوس `Server` و`X-Powered-By` و`X-AspNetMvc-Version`.

### Azure Blob Storage
عند التفعيل، يوفر `BlobSettingsService` خدمة Scoped مدعومة بسلسلة اتصال وحد أقصى قابل للتهيئة لعدد المرفقات.

### Azure Cosmos DB
عند التفعيل، يتحقق التطبيق من اتصال Cosmos DB عند بدء التشغيل باستدعاء `database.ReadAsync()`.

### AWS Secrets Manager
عند التفعيل، يستعيد `AwsSecretsManagerOperationsService` الأسرار والشهادات من AWS Secrets Manager. التكوين في قسم `AwsSecretsManager` مع المعاملات `Region` و`CertificateSecretName` و`IVSecretName` و`NonceKeySecretName` وبيانات الاعتماد `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
عند التفعيل، يتحقق `AwsDynamoDbService` من اتصال جدول DynamoDB عند بدء التشغيل. التكوين في قسم `AwsDynamoDb` مع المعاملات `Region` و`TableName` وبيانات الاعتماد `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
عند التفعيل، يستعيد `GcpSecretManagerOperationsService` الأسرار من Google Cloud Secret Manager. التكوين في قسم `GcpSecretManager` مع المعاملات `ProjectId` و`CertificateSecretId` و`IVSecretId` و`NonceKeySecretId` و`CredentialFilePath` (اختياري، يستخدم ADC إذا كان فارغاً).

### GCP Firestore
عند التفعيل، يُنشئ `GcpFirestoreService` عميل Firestore عند بدء التشغيل. التكوين في قسم `GcpFirestore` مع المعاملات `ProjectId` و`DatabaseId` (الافتراضي: "(default)") و`CollectionName` و`CredentialFilePath` (اختياري).

### إدارة هوية AWS Cognito
عند التفعيل، يُهيئ `AddAwsCognitoAuthentication` مصادقة OpenID Connect مقابل **مجموعة مستخدمي Amazon Cognito** — المعادل لـ Microsoft Entra ID / Azure AD في AWS. نقطة اكتشاف OIDC هي:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
التكوين في قسم `AwsCognito`: `Region` و`UserPoolId` و`AppClientId` و`AppClientSecret` (تخزين في Secrets المستخدم) و`Domain` (نطاق واجهة Cognito المستضافة).

### منصة هوية GCP
عند التفعيل، يُهيئ `AddGcpIdentityAuthentication` مصادقة OpenID Connect باستخدام **Google OAuth 2.0 / OIDC** — المعادل لـ Microsoft Entra ID / Azure AD في GCP. نقطة اكتشاف OIDC هي:
`https://accounts.google.com/.well-known/openid-configuration`
التكوين في قسم `GcpIdentity`: `ClientId` و`ClientSecret` (تخزين في Secrets المستخدم) و`ProjectId` اختياري. احصل على بيانات الاعتماد من **Google Cloud Console ← APIs والخدمات ← بيانات الاعتماد**.

### إدارة الجلسات الآمنة
تستخدم الجلسات ذاكرة تخزين مؤقت موزعة داخل العملية مع **مهلة خمول مدتها 30 دقيقة**. تُضبط ملفات تعريف ارتباط الجلسة على `HttpOnly` و`Secure = Always` و`SameSite = Strict`.

### التوطين
يدعم التطبيق **11 لغة**: en-US، وde-DE، وes-ES، وfr-FR، وpt-PT، وit-IT، وzh-HK، وko-KR، وhi-IN، وru-RU، وar-SA. يتضمن العربي تبديلاً تلقائياً لتخطيط RTL.

### تسجيل آمن لمعلومات PII
يُجزّئ `LoggingHelper` معلومات التعريف الشخصية في مخرجات التسجيل باستخدام HMAC-SHA256. يمكن توفير مفتاح ثابت من 32 بايت عبر `Logging:PiiHmacKey`.

---

## أعلام الميزات

تتحكم أعلام ميزات منطقية في `appsettings.json` في جميع الأنظمة الفرعية الرئيسية.

| العلم | القيمة الافتراضية | الوصف |
|---|---|---|
| `EnableSession` | `true` | جلسة جانب الخادم وملف تعريف ارتباط الجلسة |
| `EnableLocalization` | `true` | دعم متعدد اللغات (11 لغة) |
| `EnableAzureAd` | `true` | مصادقة Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | سياسات التخويل على مستوى المسار |
| `EnableKeyVault` | `false` | تحميل شهادة خادم TLS من Azure Key Vault |
| `EnableNonceServices` | `false` | توليد nonce لـ CSP لكل طلب |
| `EnableCSP` | `false` | إرفاق رأس `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | إرفاق رؤوس أمان HTTP القياسية |
| `EnableBlobStorage` | `false` | خدمة Azure Blob Storage |
| `EnableCosmosDb` | `false` | خدمة Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | نموذج أولي لـ AWS Secrets Manager |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | إدارة هوية AWS Cognito (OpenID Connect) |
| `EnableGcpSecretManager` | `false` | نموذج أولي لـ GCP Secret Manager |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | منصة هوية GCP (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | اشتراط شهادات TLS للعميل |
| `EnableOcspValidation` | `false` | فحص إلغاء شهادة OCSP (نموذج أولي) |

---

## المتطلبات الأساسية

1. **تسجيل تطبيق Azure AD** – بعنوان URI لإعادة التوجيه وسر العميل أو بيانات اعتماد الشهادة.
2. **Azure Key Vault** – يحتوي على شهادة PFX للخادم كسر.
3. **حساب Azure Cosmos DB** (اختياري).
4. **حساب Azure Blob Storage** (اختياري).
5. **.NET 9 SDK / Runtime** – الإصدار 9.0 أو أحدث.
6. **بيانات اعتماد AWS** (مستخدم/دور IAM مع صلاحيات `secretsmanager` و`dynamodb`) – مطلوبة عند تفعيل `EnableAwsSecretsManager` أو `EnableAwsDynamoDb`.
7. **حساب خدمة GCP أو ADC** (مع صلاحيات `secretmanager` و`datastore`) – مطلوب عند تفعيل `EnableGcpSecretManager` أو `EnableGcpFirestore`.

---

## التثبيت – Windows Azure (App Service)

### 1. إنشاء موارد Azure

```powershell
# Log in
az login

# Create a resource group
az group create --name MyResourceGroup --location eastus

# Create an App Service plan (Linux or Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Create the web app (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. تسجيل تطبيق Azure AD

في [بوابة Azure](https://portal.azure.com):
1. انتقل إلى **Microsoft Entra ID → تسجيلات التطبيقات → تسجيل جديد**.
2. اضبط URI لإعادة التوجيه على `https://<your-app>.azurewebsites.net/signin-oidc`.
3. ضمن **الشهادات والأسرار**، أنشئ سر عميل وانسخ القيمة.
4. دوّن **معرّف المستأجر** و**معرّف العميل** من لوحة النظرة العامة.

### 3. إنشاء Azure Key Vault وتحميل شهادة الخادم

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# Upload your PFX as a Key Vault secret (base64-encoded)
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# Grant the App Service Managed Identity access
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. تكوين إعدادات التطبيق

انسخ `appsettings.template.json` إلى `appsettings.json` واملأ قيم العناصر النائبة. يجب **عدم** تخزين الأسرار في التحكم بالمصدر — اضبطها كإعدادات تطبيق App Service أو عبر User Secrets محلياً:

```powershell
# In Azure App Service, set secrets as app settings:
az webapp config appsettings set --name MyWebApp26 --resource-group MyResourceGroup --settings \
  "AzureAd__TenantId=<TENANT_ID>" \
  "AzureAd__ClientId=<CLIENT_ID>" \
  "AzureAd__ClientSecret=<CLIENT_SECRET>" \
  "AzureKeyVault__KeyVaultURL=https://MyKeyVault26.vault.azure.net/" \
  "AzureKeyVault__KeyVaultSecret=<KV_SECRET>" \
  "AzureKeyVault__KeyVaultPassName=ServerCert" \
  "FeatureFlags__EnableKeyVault=true" \
  "FeatureFlags__EnableAzureAd=true"
```

### 5. نشر التطبيق

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. تمكين HTTPS والنطاق المخصص (مُوصى به)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. تمكين mTLS على Azure App Service (اختياري)

يدعم Azure App Service شهادات العميل عبر البوابة:
1. انتقل إلى **App Service → إعدادات TLS/SSL → شهادات العميل**.
2. اضبط **شهادات العميل الواردة** على **مطلوبة**.

ثم اضبط `FeatureFlags__EnableMtls=true` في إعدادات التطبيق.

---

## التثبيت – خادم OpenBSD مع خدمات Azure

> **مهم:** لا يملك .NET 9 بنية Microsoft رسمية لـ OpenBSD. تستخدم الإرشادات أدناه **حاوية متوافقة مع Linux** (عبر [Podman](https://podman.io/)، المتاح في شجرة حزم OpenBSD) لتشغيل تطبيق ASP.NET Core 9 على OpenBSD مع التواصل مع خدمات Azure عبر HTTPS.

### 1. تثبيت المتطلبات الأساسية على OpenBSD

```sh
# As root
pkg_add podman
pkg_add curl git
```

إذا لم يكن Podman أو Docker متاحاً لإصدار OpenBSD الخاص بك، ففكر في تشغيل التطبيق في **جهاز ظاهري Linux** (مثل vmm(4) مع ضيف Debian/Ubuntu) واتبع مسار نشر Linux القياسي من داخل ذلك الضيف.

### 2. سحب صورة وقت تشغيل ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. بناء التطبيق (على جهاز بناء Linux أو Windows)

على جهاز مثبّت عليه .NET 9 SDK، انشر بنية مستقلة تستهدف Linux x64:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

انقل مجلد `publish/` إلى مضيف OpenBSD (مثلاً عبر `scp` أو وحدة تخزين مشتركة).

### 4. إنشاء ملف التكوين

على مضيف OpenBSD، أنشئ `/etc/webappexp26/appsettings.json` بقيم الإنتاج الخاصة بك (بدون أسرار في الملف؛ استخدم متغيرات البيئة بدلاً من ذلك):

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": {
    "EnableAzureAd": true,
    "EnableKeyVault": true,
    "EnableSecurityHeaders": true,
    "EnableMtls": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/",
    "KeyVaultPassName": "ServerCert"
  }
}
```

تُحقن الأسرار كمتغيرات بيئة في الخطوة التالية.

### 5. تشغيل الحاوية

```sh
podman run -d \
  --name webappexp26 \
  -p 443:8443 \
  -v /etc/webappexp26:/app/config:ro \
  -v /path/to/publish:/app:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="https://+:8443" \
  -e AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
  -e AzureKeyVault__KeyVaultSecret="YOUR_KV_SECRET" \
  -e Logging__PiiHmacKey="YOUR_32_BYTE_BASE64_KEY" \
  mcr.microsoft.com/dotnet/aspnet:9.0 \
  dotnet /app/WebAppExperimental266.dll \
    --contentRoot /app \
    --configDir /app/config
```

### 6. تكوين جدار حماية OpenBSD Packet Filter (pf)

أضف إلى `/etc/pf.conf` للسماح بـ HTTPS الوارد والاتصالات الصادرة إلى نقاط نهاية Azure:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

أعد تحميل مجموعة القواعد:

```sh
pfctl -f /etc/pf.conf
```

### 7. تكوين DNS وشهادات TLS

تأكد من أن اسم المضيف في `AllowedHosts` يُحلّل إلى IP العام لخادم OpenBSD. يشترط Azure AD إمكانية الوصول إلى URI إعادة التوجيه (`/signin-oidc`) عبر HTTPS، لذا يجب أن تكون شهادة الخادم موثوقة. استخدم شهادة من CA عام (مثل Let's Encrypt عبر `acme-client(1)`) أو حمّل شهادة موقّعة من CA إلى Azure Key Vault وفعّل `EnableKeyVault`.

### 8. الاتصال الصادر بخدمات Azure

يجب أن تكون نقاط نهاية خدمة Azure التالية قابلة للوصول من مضيف OpenBSD عبر TCP 443:

| Service | Endpoint |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

اختبر الاتصال قبل بدء الحاوية:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## مرجع التكوين

انسخ `appsettings.template.json` إلى `appsettings.json` واستبدل جميع قيم `{{PLACEHOLDER}}`.

| القسم | المفتاح | الوصف |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | تسجيل تطبيق Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault واسم الشهادة |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | سياسة شهادة العميل mTLS |
| `NonceEncryption` | `Key`, `IV` | مفتاح 32 بايت وIV بحجم 16 بايت لتشفير nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | اتصال Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | اتصال Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | التحقق من OCSP (نموذج أولي) |
| `Logging` | `PiiHmacKey` | مفتاح HMAC بصيغة base64 بحجم 32 بايت لتجزئة PII في السجلات |

أنشئ مفاتيح التشفير وقيم IV باستخدام البرنامج النصي PowerShell المضمّن:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

خزّن جميع الأسرار في **.NET User Secrets** للتطوير المحلي:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
```

> بالنسبة لـ GCP، قم بتعيين متغير البيئة `GOOGLE_APPLICATION_CREDENTIALS` على مسار ملف JSON لحساب الخدمة، أو قم بتشغيل `gcloud auth application-default login` للتطوير المحلي.

---

## النصوص البرمجية المساعدة

يحتوي مجلد `SupportingScripts/` على أدوات PowerShell:

| النص البرمجي | الغرض |
|---|---|
| `IVandKeySampleGenerator.ps1` | توليد مفتاح AES عشوائي بحجم 32 بايت وIV بحجم 16 بايت (base64) |
| `HashInlineScriptPowerShell.ps1` | حساب تجزئات SHA-256 للنصوص البرمجية المضمّنة (لقوائم السماح في CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | كما هو أعلاه، يُخرج التجزئات بصيغة base64 |
| `CertificateUploaderToAzureExample.ps1` | تحميل شهادة PFX إلى Azure Key Vault |
| `CheckRoles.ps1` | التحقق من تعيينات أدوار RBAC في Azure للتطبيق |
| `ExportResourceGroups.ps1` | تصدير إعدادات مجموعات موارد Azure |
| `TroubleshootingCosmosDBInfo.ps1` | تشخيص اتصال Cosmos DB |
| `SetupFromTemplate.ps1` | أتمتة التكوين الأولي من `appsettings.template.json` |

---

## ملاحظات الأمان

- **لا تلتزم أبداً بالأسرار في نظام التحكم في الإصدار.**
- تُعدّ تنفيذ التحقق من OCSP **نموذجاً أولياً** يرفض جميع الشهادات. استبدل `PerformOcspValidationAsync` قبل تفعيل `EnableOcspValidation` في الإنتاج.
- قيم nonce **لا تُسجَّل أبداً**.
- رأس الاستجابة `Server` مُقنَّع بالقيمة `webserver`.
- **لا تخزن أبداً بيانات اعتماد AWS أو GCP في نظام التحكم في الإصدار.** استخدم متغيرات البيئة أو مدير الأسرار.
- تُعدّ تنفيذات AWS وGCP **نماذج أولية** تتطلب تنفيذاً كاملاً قبل الاستخدام في الإنتاج.
- بالنسبة لـ AWS، يُفضَّل استخدام أدوار IAM على مفاتيح الوصول المُضمَّنة في الكود حيثما أمكن.
- بالنسبة لـ GCP، يُفضَّل استخدام Application Default Credentials (ADC) على ملفات حساب الخدمة الصريحة.
