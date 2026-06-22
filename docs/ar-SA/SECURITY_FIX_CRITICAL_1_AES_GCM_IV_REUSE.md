# إصلاح أمني: إعادة استخدام IV في AES-GCM أثناء توليد nonce (حرج #1)

**تم الإصلاح في:** `Models/Main_Objects/Nonce.cs`، `Services/NonceRefresherService.cs`،
`Services/NonceCatalogService.cs`  
**الاختبارات:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`،
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## ما كان خاطئاً

كانت فئة `Nonce` تستخدم **تشفير AES-GCM مع IV ثابت** يُجلب من Azure Key Vault في كل استدعاء. إعادة استخدام نفس IV مع نفس مفتاح AES-GCM يُعدّ خطأً تشفيرياً كارثياً:

* يستطيع المهاجم الذي يرصد نصين مشفرَين باستخدام نفس IV والمفتاح أن يُجري عملية XOR عليهما لاسترداد XOR للنصين الأصليين.
* الأخطر في حالة علامات مصادقة nonce، تُتيح إعادة استخدام IV تزوير علامات المصادقة، مما يُبطل ضمانة سلامة AES-GCM بالكامل.

فضلاً عن الفشل التشفيري، لم يُضف التشفير **أي فائدة أمنية** في هذه الحالة. يحتاج nonce في سياسة أمان المحتوى (CSP) إلى خاصيتين فقط: يجب أن يكون **غير قابل للتنبؤ** و**فريداً لكل طلب**. توفر هاتين الخاصيتين مباشرةً مولّدات الأرقام العشوائية المُشفَّرة آمنياً (`RandomNumberGenerator`). أضاف التشفير تعقيداً دون إضافة أمان.

### الكود الجذري للمشكلة (قبل الإصلاح)

```csharp
// Nonce.cs — نفس IV يُجلب من Key Vault في كل استدعاء
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — يُجلب مرة واحدة ويُعاد استخدامه عبر جميع الطلبات
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## ما تم إصلاحه

تستدعي `Nonce.GenerateSecureNonce()` الآن `RandomNumberGenerator.Fill(byte[])` مباشرةً لإنتاج 16 بايت من البيانات العشوائية المُشفَّرة آمنياً، ثم تُرمّزها بـ Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* لا حاجة إلى استدعاءات Key Vault للحصول على IV أو مفتاح التشفير.
* لا يتضمن AES-GCM أو أي تشفير آخر.
* لا يقبل مُنشئ `Nonce` بعد الآن معاملات `KeyVaultSecret`.

تم أيضاً إصلاح خلل ثانوي في `NonceCatalogService.GetANonce`: كان الأسلوب يستخدم سابقاً فحصاً ثنائي الخطوة (`TryGetValue` يليه المُفهرس `[]`)، وهو غير ذري وقد يُلقي `KeyNotFoundException` عندما يُزيل خيط آخر المفتاح بين الاستدعاءين. يستخدم الإصلاح `TryGetValue` مع المعامل `out` لاسترداد القيمة في عملية ذرية واحدة.

---

## كيفية الحفاظ على الإصلاح

1. **لا تُدخل IV أو مفتاحاً من Key Vault لتوليد nonce.** إذا كان Key Vault مستخدماً لأسرار أخرى فهذا مقبول — لكن توليد nonce يجب ألا يعتمد على IV ثابت.

2. **لا تستبدل `GenerateSecureNonce` بمخطط AES-GCM أو CBC/CTR** يُعيد استخدام IV أو عدّاداً عبر الطلبات.

3. **احتفظ بـ nonce بطول 16 بايت على الأقل (128 بت).** تقليل طول البايت يزيد احتمال التصادم ويُقلل الانتروبيا المتاحة لـ CSP.

4. **لا تُغيّر `RandomNumberGenerator.Fill` إلى `new Random()`** أو أي مولّد غير CSPRNG.

5. **احتفظ بـ `NonceCatalogService.GetANonce` باستخدام `TryGetValue` مع المعامل `out`.** نمط الفحص ثم البحث في خطوتين (`TryGetValue` + المُفهرس) غير آمن للخيوط حتى مع `ConcurrentDictionary`.

### الاختبارات التي تُنفّذ هذا الإصلاح

| الاختبار | ما يرصده |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | يفشل في التجميع إذا عاد المُنشئ لقبول معاملات `KeyVaultSecret` IV + key |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | يفشل إذا توقف توليد nonce أو أعاد قيمة غير Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | يفشل إذا قلّ طول البايت عن 16 |
| `Nonce_SuccessiveGenerations_AreUnique` | يفشل إذا أدت إعادة استخدام IV إلى إنتاج نفس nonce بشكل متكرر |
| `Nonce_HasSufficientEntropy` | يفشل إذا كان مصدر الانتروبيا غير عشوائي |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | يفشل إذا عاد `ConcurrentDictionary` إلى `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | يفشل إذا أُعيد إدخال سباق TOCTOU في `GetANonce` |
