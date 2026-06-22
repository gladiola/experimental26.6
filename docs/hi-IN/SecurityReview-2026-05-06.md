# सुरक्षा समीक्षा — WebAppExperimental266

**तिथि:** 2026-05-06
**दायरा:** संपूर्ण कोडबेस स्थैतिक विश्लेषण (2026-05-05 समीक्षा की अनुवर्ती कार्रवाई)
**समीक्षक:** स्वचालित सुरक्षा समीक्षा

---

## कार्यकारी सारांश

यह अनुवर्ती समीक्षा पुष्टि करती है कि 2026-05-05 की सुरक्षा समीक्षा में पहचानी गई सभी 19 कमजोरियों को ठीक कर दिया गया है। समीक्षा में इस सत्र के दौरान खोजे गए 5 नए या शेष निष्कर्ष भी पहचाने गए हैं। पिछली समीक्षा के बाद से एप्लिकेशन की समग्र सुरक्षा स्थिति में उल्लेखनीय सुधार हुआ है।

---

## पिछले निष्कर्षों की स्थिति (2026-05-05)

सभी 19 पिछले निष्कर्ष **पुष्टित रूप से ठीक** हैं:

| # | निष्कर्ष | गंभीरता | स्थिति |
|---|----------|---------|--------|
| 1 | nonce निर्माण में AES-GCM IV का पुन: उपयोग | 🔴 गंभीर | ✅ ठीक किया गया |
| 2 | Nonce को सादे पाठ में लॉग किया गया | 🔴 गंभीर | ✅ ठीक किया गया |
| 3 | हार्डकोडेड फ़ॉलबैक nonce स्ट्रिंग | 🔴 गंभीर | ✅ ठीक किया गया |
| 4 | गैर-थ्रेड-सेफ ग्लोबल nonce डिक्शनरी | 🟠 उच्च | ✅ ठीक किया गया |
| 5 | mTLS जारीकर्ता सत्यापन टिप्पणी में | 🟠 उच्च | ✅ ठीक किया गया |
| 6 | mTLS निरसन जाँच डिफ़ॉल्ट रूप से बंद | 🟠 उच्च | ✅ ठीक किया गया |
| 7 | OCSP हमेशा वैध लौटाता है (stub) | 🟠 उच्च | ✅ ठीक किया गया |
| 8 | कॉन्फ़िगरेशन में प्रमाणीकरण/प्राधिकरण डिफ़ॉल्ट रूप से बंद | 🟠 उच्च | ✅ ठीक किया गया |
| 9 | सुरक्षा हेडर पाइपलाइन में बहुत देर से लागू | 🟠 उच्च | ✅ ठीक किया गया |
| 10 | सत्र कुकी में `Secure` + `SameSite` गायब | 🟡 मध्यम | ✅ ठीक किया गया |
| 11 | विकृत वैश्विक `Set-Cookie` हेडर | 🟡 मध्यम | ✅ ठीक किया गया |
| 12 | `Content-Type` हर जगह `text/html` पर बाध्य | 🟡 मध्यम | ✅ ठीक किया गया |
| 13 | `AllowedHosts` वाइल्डकार्ड पर सेट | 🟡 मध्यम | ✅ ठीक किया गया |
| 14 | लेआउट में `<script>` टैग पर Nonce लागू नहीं | 🟡 मध्यम | ✅ ठीक किया गया |
| 15 | `Referrer-Policy` हेडर गायब | 🟡 मध्यम | ✅ ठीक किया गया |
| 16 | PII को सादे पाठ में लॉग किया गया | 🔵 निम्न | ✅ ठीक किया गया |
| 17 | लॉग में आंशिक कनेक्शन स्ट्रिंग | 🔵 निम्न | ✅ ठीक किया गया |
| 18 | Key Vault संचालन stub हैं | 🔵 निम्न | ✅ ठीक किया गया |
| 19 | पुराना `X-XSS-Protection: 1; mode=block` | 🔵 निम्न | ✅ ठीक किया गया |

---

## नए / शेष निष्कर्ष

| # | क्षेत्र | गंभीरता |
|---|---------|---------|
| 20 | NonceRefresherService अप्रयुक्त Key Vault कंस्ट्रक्टर निर्भरताएँ बनाए रखता है | 🟠 उच्च |
| 21 | OcspValidationService आंतरिक कैश गैर-थ्रेड-सेफ Dictionary का उपयोग करता है | 🟡 मध्यम |
| 22 | OCSP सत्यापन stub अभी भी मौजूद है — बंद विफल होता है लेकिन अकार्यान्वित | 🔵 निम्न |
| 23 | खाली AllowedIssuers के साथ mTLS सभी प्रमाणपत्रों को अस्वीकार करता है (fail-closed, अप्रलेखित) | 🔵 निम्न |
| 24 | OcspSettings.ServerUnavailableBehavior डिफ़ॉल्ट "Warn" है (त्रुटि पर पास-थ्रू की अनुमति) | 🔵 निम्न |

---

## विस्तृत निष्कर्ष

### ✅ 2026-05-05 के पुष्टित सुधार

#### 1. AES-GCM IV पुन: उपयोग — ठीक किया गया

**फ़ाइल:** `Models/Main_Objects/Nonce.cs`

AES-GCM-आधारित nonce निर्माण को पूरी तरह से बदल दिया गया है। `Nonce.GenerateSecureNonce()` अब 16 यादृच्छिक बाइट्स पर `RandomNumberGenerator.Fill(randomBytes)` को कॉल करता है और Base64 स्ट्रिंग लौटाता है। कोई Key Vault निर्भरता नहीं, कोई IV नहीं, कोई एन्क्रिप्शन नहीं — CSP nonce के लिए बिल्कुल सही दृष्टिकोण।

---

#### 2. Nonce मान अब लॉग नहीं किए जाते — ठीक किया गया

**फ़ाइलें:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

दोनों फ़ाइलें अब केवल स्थिति संदेश (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) लॉग करती हैं और कभी भी nonce मान स्वयं नहीं।

---

#### 3. हार्डकोडेड फ़ॉलबैक Nonce हटाए गए — ठीक किया गया

**फ़ाइल:** `Services/OptimizedNonceMiddleware.cs`

सभी तीन हार्डकोडेड लिटरल स्ट्रिंग (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) को सामान्य और अपवाद फ़ॉलबैक दोनों पथों में `Nonce.GenerateSecureNonce()` के कॉल से बदल दिया गया है।

---

#### 4. थ्रेड-सेफ Nonce डिक्शनरी — ठीक किया गया

**फ़ाइल:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` को `ConcurrentDictionary<string, Nonce>` से बदल दिया गया है। `GetANonce` अब दो-चरणीय जाँच-फिर-खोज के बजाय एकल परमाणु `TryGetValue` कॉल का उपयोग करता है।

---

#### 5. mTLS जारीकर्ता सत्यापन अब कार्यात्मक — ठीक किया गया

**फ़ाइल:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

टिप्पणी किए गए जारीकर्ता सत्यापन ब्लॉक को `mtlsSettings.IsIssuerAllowed(issuer)` के कॉल से बदल दिया गया है, जो `AllowedIssuers` के विरुद्ध केस-इनसेंसिटिव सब-स्ट्रिंग मिलान करता है। जब सूची खाली हो (अनकॉन्फ़िगर्ड), तो मेथड `false` लौटाती है, सभी प्रमाणपत्रों को अस्वीकार करती है (fail-closed)।

---

#### 6. mTLS निरसन जाँच डिफ़ॉल्ट रूप से सक्षम — ठीक किया गया

**फ़ाइल:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` अब डिफ़ॉल्ट रूप से `true` है। `appsettings.template.json` भी `"CheckCertificateRevocation": true` निर्दिष्ट करता है।

---

#### 7. OCSP Stub अब बंद विफल होता है — ठीक किया गया

**फ़ाइल:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` अब चुपचाप `IsValid = true` लौटाने के बजाय `OcspStatus.Error` के साथ `IsValid = false` लौटाता है और एक त्रुटि लॉग करता है। कॉन्फ़िगरेशन में OCSP सक्षम करना अब वास्तविक कार्यान्वयन प्रदान होने तक सभी प्रमाणपत्रों को अस्वीकार कर देगा।

---

#### 8. प्रमाणीकरण और प्राधिकरण डिफ़ॉल्ट रूप से सक्षम — ठीक किया गया

**फ़ाइल:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` और `EnableAuthorization` अब `FeatureFlags` क्लास में डिफ़ॉल्ट रूप से `true` हैं। `appsettings.json` भी दोनों को `true` पर सेट करता है।

---

#### 9. सुरक्षा हेडर रूटिंग से पहले लागू — ठीक किया गया

**फ़ाइल:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` और `UseStandardSecurityHeaders` अब `UseRouting`, `UseAuthentication`, और `UseAuthorization` से पहले कॉल किए जाते हैं। 401/403 शॉर्ट-सर्किट सहित सभी प्रतिक्रियाएँ सुरक्षा हेडर प्राप्त करती हैं।

---

#### 10–15. कुकी, Content-Type, AllowedHosts, लेआउट में Nonce, Referrer-Policy — ठीक किया गया

**फ़ाइलें:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- सत्र कुकी अब `CookieSecurePolicy.Always` और `SameSiteMode.Strict` सेट करती है।
- विकृत नामरहित `Set-Cookie` हेडर हटा दिया गया है।
- वैश्विक `Content-Type: text/html` ओवरराइड हटा दिया गया है।
- `appsettings.json` में `AllowedHosts` अब `"localhost;127.0.0.1"` है; टेम्पलेट `"{{YOUR_HOSTNAME}}"` उपयोग करता है।
- `_Layout.cshtml` में सभी तीन `<script>` टैग अब `nonce="@Context.Items["Nonce"]"` शामिल करते हैं।
- `Referrer-Policy: strict-origin-when-cross-origin` अब `UseStandardSecurityHeaders` द्वारा जोड़ा जाता है।

---

#### 16–19. PII लॉगिंग, कनेक्शन स्ट्रिंग लॉग, Key Vault Stub, X-XSS-Protection — ठीक किया गया

**फ़ाइलें:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- सभी PII (OID, ईमेल, नाम, SID, भूमिकाएँ) अब लॉग में लिखने से पहले `LoggingHelper.HashPii()` के माध्यम से HMAC-SHA256 हैश किए जाते हैं। कॉन्फ़िगरेशन में `Logging:PiiHmacKey` के माध्यम से एक स्थिर HMAC कुंजी प्रदान की जा सकती है; कॉन्फ़िगर न होने पर एक यादृच्छिक प्रति-प्रक्रिया कुंजी उपयोग की जाती है।
- Cosmos DB लॉग स्टेटमेंट अब केवल पुष्टि करता है कि कनेक्शन स्ट्रिंग मौजूद है (`!string.IsNullOrEmpty`), उसकी सामग्री नहीं।
- `AzureKeyVaultCertificateOperations` अब प्रमाणपत्र null होने पर स्टार्टअप पर `InvalidOperationException` फेंकता है, न कि चुपचाप डमी मान लौटाता है।
- `X-XSS-Protection` अब `"0"` पर सेट है (पुराने XSS ऑडिटर को अक्षम करते हुए), आधुनिक ब्राउज़र मार्गदर्शन के अनुरूप।

---

## 🟠 उच्च

### 20. NonceRefresherService अप्रयुक्त Key Vault कंस्ट्रक्टर निर्भरताएँ बनाए रखता है

**फ़ाइल:** `Services/NonceRefresherService.cs`

`NonceRefresherService` अभी भी `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, और `IAzureKeyVaultOperationsService` के लिए कंस्ट्रक्टर पैरामीटर घोषित करता है। चूँकि nonce निर्माण को `RandomNumberGenerator` का सीधे उपयोग करने के लिए सरल बनाया गया है, इनमें से कोई भी निर्भरता उपयोग नहीं की जाती।

**जोखिम:** जब `EnableNonceServices = true` और `EnableKeyVault = false` (डिफ़ॉल्ट), ये सेवाएँ DI कंटेनर में पंजीकृत नहीं हैं, जिससे nonce सेवा पहली बार रिज़ॉल्व होने पर रनटाइम पर `InvalidOperationException` होती है। यह प्रभावी रूप से डिफ़ॉल्ट कॉन्फ़िगरेशन द्वारा ट्रिगर की गई सेवा-निषेध स्थिति है। `FeatureFlags` क्लास डिफ़ॉल्ट रूप से `EnableNonceServices = true` सेट करती है, इसलिए केवल क्लास डिफ़ॉल्ट पर निर्भर कोई भी वातावरण (`appsettings.json` ओवरराइड के बिना) प्रारंभ होने में विफल होगा।

**अनुशंसा:** `NonceRefresherService` से चार अप्रयुक्त कंस्ट्रक्टर पैरामीटर और उनके संबंधित प्राइवेट फ़ील्ड हटाएँ। सेवा को केवल `ILogger<NonceRefresherService>`, `ILoggerFactory`, और `INonceCatalogService` की आवश्यकता है।

---

## 🟡 मध्यम

### 21. OcspValidationService आंतरिक कैश गैर-थ्रेड-सेफ Dictionary का उपयोग करता है

**फ़ाइल:** `Services/OcspValidationService.cs` (पंक्ति 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` समवर्ती पढ़ने और लिखने के लिए थ्रेड-सेफ नहीं है। यदि `OcspValidationService` एक सिंगलटन के रूप में पंजीकृत है (या यदि वही इंस्टेंस किसी अन्य तंत्र द्वारा अनुरोधों में साझा की जाती है), तो समवर्ती OCSP सत्यापन कैश को भ्रष्ट कर सकते हैं, जिससे खोई प्रविष्टियाँ, फेंके गए अपवाद, या पुराना डेटा लौटाया जा सकता है।

**अनुशंसा:** `Dictionary<string, CachedOcspResponse>` को `ConcurrentDictionary<string, CachedOcspResponse>` से बदलें। `_cache.Remove` कॉल (पंक्ति 103) को `_cache.TryRemove` में अपडेट करें।

---

## 🔵 निम्न / सूचनात्मक

### 22. OCSP सत्यापन Stub — बंद विफल लेकिन अकार्यान्वित

**फ़ाइल:** `Services/OcspValidationService.cs` (पंक्तियाँ 157–173)

`PerformOcspValidationAsync` अभी भी एक stub है। निष्कर्ष #7 के सुधार ने व्यवहार को "हमेशा वैध" से "हमेशा अवैध (fail-closed)" में सही ढंग से बदल दिया। हालाँकि, मेथड अभी भी एक वास्तविक OCSP कार्यान्वयन नहीं है। जब तक `EnableOcspValidation = false` (डिफ़ॉल्ट), इसका उत्पादन पर कोई प्रभाव नहीं। किसी भी वातावरण में OCSP सक्षम करने से पहले, उत्पादन-गुणवत्ता OCSP क्लाइंट लागू किया जाना चाहिए।

---

### 23. खाली AllowedIssuers के साथ mTLS सभी क्लाइंट प्रमाणपत्रों को अस्वीकार करता है

**फ़ाइल:** `Models/Settings/MtlsSettings.cs`

जब `ValidateClientCertificateIssuer = true` (डिफ़ॉल्ट) और `AllowedIssuers` खाली है (कॉन्फ़िगर न होने पर भी डिफ़ॉल्ट), `IsIssuerAllowed()` `false` लौटाती है, जिससे सभी क्लाइंट प्रमाणपत्र अस्वीकार किए जाते हैं। यह सही fail-closed व्यवहार है, लेकिन यह प्रमुखता से प्रलेखित नहीं है। जो ऑपरेटर टेम्पलेट को सावधानी से पढ़े बिना mTLS सक्षम करते हैं, वे पा सकते हैं कि बिना किसी स्पष्ट स्पष्टीकरण के सभी क्लाइंट कनेक्शन अस्वीकार किए जा रहे हैं।

**अनुशंसा:** जब `ValidateClientCertificateIssuer = true` और `AllowedIssuers` खाली हो तो स्टार्टअप पर एक चेतावनी लॉग संदेश जोड़ें।

---

### 24. OcspSettings.ServerUnavailableBehavior डिफ़ॉल्ट "Warn" है

**फ़ाइल:** `appsettings.template.json` (पंक्ति 134), `Services/OcspValidationService.cs`

`ServerUnavailableBehavior` सेटिंग टेम्पलेट में डिफ़ॉल्ट रूप से `"Warn"` है, जो OCSP सर्वर तक नहीं पहुँचने पर अनुरोधों को पास होने देती है। उच्च-सुरक्षा वातावरण के लिए, यह `"Fail"` होना चाहिए ताकि OCSP सर्वर आउटेज चुपचाप प्रमाणपत्र निरसन जाँच को खराब न करें।

**अनुशंसा:** टेम्पलेट में तीन विकल्पों (`Fail`, `Allow`, `Warn`) को स्पष्ट रूप से दस्तावेज़ीकरण करें और न्यूनतम विशेषाधिकार के सिद्धांत से मेल खाने के लिए डिफ़ॉल्ट को `"Fail"` में बदलने पर विचार करें।

---

## सुरक्षा हेडर मूल्यांकन (वर्तमान स्थिति)

निम्नलिखित हेडर अब `UseStandardSecurityHeaders` के माध्यम से लागू किए जाते हैं:

| हेडर | मान | मूल्यांकन |
|------|-----|-----------|
| `X-Frame-Options` | `DENY` | ✅ अच्छा |
| `X-XSS-Protection` | `0` | ✅ अच्छा (पुराने ऑडिटर को अक्षम करता है) |
| `X-Content-Type-Options` | `nosniff` | ✅ अच्छा |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ अच्छा |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ अच्छा |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ अच्छा |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ अच्छा |
| `Permissions-Policy` | जियोलोकेशन, कैमरा, माइक्रोफ़ोन, interest-cohort अक्षम | ✅ अच्छा |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ अच्छा |
| `Content-Security-Policy` | Nonce-आधारित, CSP सक्षम होने पर लागू | ✅ अच्छा |
| `Server` | `"webserver"` के रूप में मास्क किया गया | ✅ अच्छा |
| `X-Powered-By` | हटाया गया | ✅ अच्छा |

---

## समग्र मूल्यांकन

एप्लिकेशन ने पिछली समीक्षा से सभी गंभीर और उच्च-गंभीरता वाली कमजोरियों को संबोधित किया है। वर्तमान निष्कर्ष एक उच्च-गंभीरता कॉन्फ़िगरेशन/DI समस्या (निष्कर्ष #20) और निम्न-गंभीरता सूचनात्मक मदों तक सीमित हैं। सुरक्षा स्थिति में पर्याप्त सुधार हुआ है। निष्कर्ष #20 (NonceRefresherService में अप्रयुक्त DI निर्भरताएँ) के लिए तत्काल कार्रवाई की अनुशंसा की जाती है क्योंकि यह डिफ़ॉल्ट कॉन्फ़िगरेशन के अंतर्गत एप्लिकेशन को प्रारंभ होने से रोक सकती है।
