# सुरक्षा समीक्षा — WebAppExperimental266

**दिनांक:** 2026-05-07
**दायरा:** संपूर्ण कोडबेस स्थैतिक विश्लेषण (2026-05-06 समीक्षा का अनुवर्ती)
**समीक्षक:** स्वचालित सुरक्षा समीक्षा

---

## कार्यकारी सारांश

यह अनुवर्ती समीक्षा पुष्टि करती है कि 2026-05-06 की सुरक्षा समीक्षा में पहचानी गई 5 कमजोरियों में से 3 को पूरी तरह से ठीक किया जा चुका है, जबकि 1 आंशिक रूप से ठीक की गई है। समीक्षा में 4 नई खोज भी सामने आई हैं। एप्लिकेशन की समग्र सुरक्षा स्थिति में सुधार जारी है।

---

## पूर्व निष्कर्षों की स्थिति (2026-05-06)

| # | निष्कर्ष | गंभीरता | स्थिति |
|---|---------|----------|--------|
| 20 | NonceRefresherService अप्रयुक्त Key Vault कंस्ट्रक्टर निर्भरताएँ बनाए रखता है | 🟠 उच्च | ✅ ठीक किया गया |
| 21 | OcspValidationService का आंतरिक कैश गैर-थ्रेड-सुरक्षित Dictionary का उपयोग करता है | 🟡 मध्यम | ✅ ठीक किया गया |
| 22 | OCSP सत्यापन स्टब अभी भी मौजूद है — बंद अवस्था में विफल होता है लेकिन अनिम्प्लीमेंटेड | 🔵 निम्न | ⚠️ स्वीकृत (डिज़ाइन के अनुसार) |
| 23 | खाली AllowedIssuers के साथ mTLS सभी प्रमाणपत्रों को अस्वीकार करता है (fail-closed, अदस्तावेज़ीकृत) | 🔵 निम्न | ✅ ठीक किया गया |
| 24 | OcspSettings.ServerUnavailableBehavior का डिफ़ॉल्ट "Warn" है (त्रुटि पर पास-थ्रू की अनुमति देता है) | 🔵 निम्न | ⚠️ आंशिक रूप से ठीक किया गया |

---

## पूर्व निष्कर्षों की विस्तृत स्थिति

### ✅ 20. NonceRefresherService अप्रयुक्त DI निर्भरताएँ — ठीक किया गया

**फ़ाइल:** `Services/NonceRefresherService.cs`

`NonceRefresherService` कंस्ट्रक्टर अब केवल `ILogger<NonceRefresherService>`, `ILoggerFactory` और `INonceCatalogService` घोषित करता है। चार पहले अप्रयुक्त निर्भरताएँ (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) हटा दी गई हैं। इससे सेवा-अस्वीकृति जोखिम हल हो जाता है जो `EnableKeyVault = false` (डिफ़ॉल्ट) और `EnableNonceServices = true` (डिफ़ॉल्ट) होने पर एप्लिकेशन को शुरू होने से रोकता था।

---

### ✅ 21. OcspValidationService गैर-थ्रेड-सुरक्षित कैश — ठीक किया गया

**फ़ाइल:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` को `ConcurrentDictionary<string, CachedOcspResponse>` से बदल दिया गया है। `_cache.Remove` कॉल को `_cache.TryRemove` में अपडेट किया गया है। कैश अब समवर्ती पहुँच के लिए सुरक्षित है।

---

### ⚠️ 22. OCSP सत्यापन स्टब — स्वीकृत (डिज़ाइन के अनुसार)

**फ़ाइल:** `Services/OcspValidationService.cs`

स्टब अभी भी मौजूद है लेकिन सही तरीके से बंद अवस्था में विफल होता है। चूँकि `EnableOcspValidation` का डिफ़ॉल्ट `false` है, इसका उत्पादन पर कोई प्रभाव नहीं है। पूर्ण OCSP कार्यान्वयन होने तक इसे एक सूचनात्मक निष्कर्ष के रूप में स्वीकार किया जाता है।

---

### ✅ 23. mTLS खाली AllowedIssuers — ठीक किया गया

**फ़ाइल:** `Extensions/ServiceCollectionExtensions.cs`

जब `ValidateClientCertificateIssuer = true` और `AllowedIssuers` खाली हो, तब अब स्टार्टअप चेतावनी लॉग की जाती है:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

यह fail-closed व्यवहार का सामना करने वाले ऑपरेटरों को स्पष्ट मार्गदर्शन प्रदान करता है।

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — आंशिक रूप से ठीक किया गया

**फ़ाइलें:** `appsettings.template.json` (ठीक किया गया), `Models/Settings/OcspSettings.cs` (अभी ठीक नहीं किया गया)

टेम्पलेट अब सही तरीके से `"ServerUnavailableBehavior": "Fail"` निर्दिष्ट करता है। हालांकि, `OcspSettings.cs` (पंक्ति 39) में C# क्लास डिफ़ॉल्ट `"Warn"` बना हुआ है। यदि कोई ऑपरेटर OCSP सक्षम करता है और अपनी कॉन्फ़िगरेशन फ़ाइल से `ServerUnavailableBehavior` छोड़ देता है, तो क्लास डिफ़ॉल्ट `"Warn"` चुपचाप लागू हो जाता है, जिससे OCSP सर्वर आउटेज पर पास-थ्रू की अनुमति मिलती है। टेम्पलेट अनुशंसा के अनुसार क्लास डिफ़ॉल्ट बदला जाना चाहिए।

---

## नए निष्कर्ष

| # | क्षेत्र | गंभीरता |
|---|------|----------|
| 25 | OcspSettings क्लास डिफ़ॉल्ट ("Warn") टेम्पलेट ("Fail") से भिन्न है | 🔵 निम्न |
| 26 | NonceCatalogService एकल साझा nonce कुंजी अनुरोधों के बीच nonce टकराव की अनुमति देती है | 🟡 मध्यम |
| 27 | OptimizedNonceMiddleware स्थैतिक काउंटर हस्ताक्षरित 32-बिट पूर्णांक का उपयोग करते हैं (ओवरफ़्लो जोखिम) | 🔵 निम्न |
| 28 | Program.cs खाली ILoggerFactory सिंगलटन पंजीकृत करता है, फ्रेमवर्क लॉगर को छायांकित करता है | 🟡 मध्यम |

---

## 🟡 मध्यम

### 26. NonceCatalogService साझा Nonce कुंजी अनुरोधों के बीच Nonce टकराव की अनुमति देती है

**फ़ाइलें:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

nonce कैटलॉग सभी nonces को एकल साझा कुंजी `"CSPNonce"` के तहत संग्रहीत करता है। समवर्ती लोड के तहत, निम्नलिखित रेस कंडीशन संभव है:

1. अनुरोध A, `RefreshNonceAsync()` कॉल करता है — nonce A1 को `_nonceCollection["CSPNonce"]` के रूप में संग्रहीत किया जाता है।
2. अनुरोध B, `RefreshNonceAsync()` कॉल करता है — nonce B1, `_nonceCollection["CSPNonce"]` को अधिलेखित कर देता है।
3. अनुरोध A, `GetANonce("CSPNonce")` कॉल करता है — A1 की बजाय B1 प्राप्त होता है।
4. अनुरोध A का CSP हेडर और लेआउट nonce दोनों B1 में हैं।
5. अनुरोध B में भी B1 है।

दो समवर्ती प्रतिक्रियाएँ एक ही nonce साझा करती हैं। जबकि दोनों मान अभी भी क्रिप्टोग्राफिक रूप से यादृच्छिक और अनुमानित नहीं हैं (कोई हार्डकोडेड स्ट्रिंग नहीं), वही nonce मान कई एक साथ प्रतिक्रियाओं में दिखाई देता है, जो CSP विनिर्देश द्वारा आवश्यक प्रति-अनुरोध विशिष्टता गारंटी को कमजोर करता है। जो हमलावर एक प्रतिक्रिया के nonce का अवलोकन कर सकता है, उसके पास कम से कम एक अन्य समवर्ती प्रतिक्रिया के लिए एक वैध nonce है।

**अनुशंसा:** प्रत्येक अनुरोध के लिए सीधे मिडलवेयर के अंदर nonce उत्पन्न करें (उदाहरण के लिए, `Nonce.GenerateSecureNonce()`) और इसे केवल `HttpContext.Items["Nonce"]` में संग्रहीत करें, प्रति-अनुरोध nonces के लिए साझा कैटलॉग को बाईपास करते हुए। साझा कैटलॉग तब केवल तभी आवश्यक होगा जब एक nonce को एकल अनुरोध के भीतर मिडलवेयर परतों में साझा किया जाना हो, जिसे `HttpContext.Items` पहले से ही मूल रूप से संभालता है।

---

### 28. Program.cs खाली ILoggerFactory सिंगलटन पंजीकृत करता है

**फ़ाइल:** `Program.cs` (पंक्ति 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core, `WebApplication.CreateBuilder` के दौरान स्वचालित रूप से एक पूरी तरह से कॉन्फ़िगर की गई `ILoggerFactory` (`builder.Logging` कॉन्फ़िगरेशन से सभी लॉगिंग प्रदाताओं के साथ) पंजीकृत करता है। यह स्पष्ट `AddSingleton` पंजीकरण बिना किसी प्रदाता के एक दूसरी, अनकॉन्फ़िगर्ड `LoggerFactory` इंस्टेंस जोड़ता है। चूँकि `GetRequiredService<ILoggerFactory>()` सबसे हाल ही में पंजीकृत कार्यान्वयन लौटाता है, इसलिए जो सेवाएँ निर्भरता इंजेक्शन के माध्यम से `ILoggerFactory` प्राप्त करती हैं (जैसे `NonceRefresherService`) इस खाली फ़ैक्टरी का उपयोग करेंगी और `_loggerFactory.CreateLogger<T>()` के माध्यम से कोई लॉग आउटपुट नहीं देंगी।

**जोखिम:** `NonceRefresherService` में मूक लॉगिंग — nonce जनरेशन सफलताओं और विफलताओं को किसी भी कॉन्फ़िगर किए गए लॉगिंग सिंक को नहीं भेजा जाता। यह कार्यक्षमता को प्रभावित किए बिना सुरक्षा-संवेदनशील संचालन के दौरान एप्लिकेशन की अवलोकनीयता को कम करता है।

**अनुशंसा:** स्पष्ट `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` पंजीकरण हटाएँ। फ्रेमवर्क की कॉन्फ़िगर `ILoggerFactory` (Console और किसी भी अन्य प्रदाता के साथ) तब उन सेवाओं द्वारा सही ढंग से हल की जाएगी जो इस पर निर्भर हैं।

---

## 🔵 निम्न / सूचनात्मक

### 25. OcspSettings क्लास डिफ़ॉल्ट टेम्पलेट से भिन्न है

**फ़ाइल:** `Models/Settings/OcspSettings.cs` (पंक्ति 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

टेम्पलेट (`appsettings.template.json`) `"ServerUnavailableBehavior": "Fail"` निर्दिष्ट करता है, लेकिन C# क्लास डिफ़ॉल्ट `"Warn"` है। यदि `ServerUnavailableBehavior` सक्रिय कॉन्फ़िगरेशन फ़ाइल से अनुपस्थित है, तो टेम्पलेट अनुशंसा के बजाय क्लास डिफ़ॉल्ट चुपचाप लागू हो जाता है। यह निष्कर्ष #24 का अवशेष है।

**अनुशंसा:** टेम्पलेट और न्यूनतम विशेषाधिकार के सिद्धांत के अनुरूप क्लास डिफ़ॉल्ट को `"Warn"` से `"Fail"` में बदलें।

---

### 27. OptimizedNonceMiddleware स्थैतिक काउंटर ओवरफ़्लो हो सकते हैं

**फ़ाइल:** `Services/OptimizedNonceMiddleware.cs` (पंक्तियाँ 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

इन हस्ताक्षरित 32-बिट काउंटरों को `Interlocked.Increment` के माध्यम से परमाणु रूप से बढ़ाया जाता है। लगभग 2.1 बिलियन वृद्धि के बाद, वे `int.MinValue` (−2,147,483,648) पर रैप होंगे, जिससे दक्षता गणना `(total - generated) * 100.0 / total` गलत या अर्थहीन परिणाम देगी। 1,000 अनुरोध प्रति सेकंड पर, लगभग 24.8 दिनों के निरंतर संचालन के बाद ओवरफ़्लो होता है।

**अनुशंसा:** काउंटर फ़ील्ड प्रकारों को `int` से `long` में बदलें और ओवरफ़्लो को रोकने के लिए `Interlocked.Increment` के `long` ओवरलोड का उपयोग करें।

---

## सुरक्षा हेडर मूल्यांकन (वर्तमान स्थिति)

निम्नलिखित हेडर `UseStandardSecurityHeaders` के माध्यम से लागू किए जाते हैं — पिछली समीक्षा से अपरिवर्तित:

| हेडर | मूल्य | मूल्यांकन |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ अच्छा |
| `X-XSS-Protection` | `0` | ✅ अच्छा (पुराने ऑडिटर को अक्षम करता है) |
| `X-Content-Type-Options` | `nosniff` | ✅ अच्छा |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ अच्छा |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ अच्छा |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ अच्छा |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ अच्छा |
| `Permissions-Policy` | जियोलोकेशन, कैमरा, माइक्रोफोन, interest-cohort अक्षम | ✅ अच्छा |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ अच्छा |
| `Content-Security-Policy` | Nonce-आधारित, CSP सक्षम होने पर लागू | ✅ अच्छा |
| `Server` | `"webserver"` तक मास्क किया गया | ✅ अच्छा |
| `X-Powered-By` | हटाया गया | ✅ अच्छा |

---

## समग्र मूल्यांकन

पिछली समीक्षाओं के सभी उच्च-गंभीरता निष्कर्षों को ठीक कर दिया गया है। वर्तमान निष्कर्ष दो मध्यम-गंभीरता मुद्दों (#26 साझा nonce कुंजी, #28 खाली ILoggerFactory) और दो निम्न-गंभीरता सूचनात्मक आइटम (#25 क्लास डिफ़ॉल्ट बेमेल, #27 काउंटर में पूर्णांक ओवरफ़्लो) तक सीमित हैं। निष्कर्ष #28 (खाली ILoggerFactory सिंगलटन) के लिए तत्काल ध्यान देने की सिफारिश की जाती है क्योंकि यह nonce संचालन के दौरान सुरक्षा-संबंधित डायग्नोस्टिक लॉगिंग को चुपचाप दबा देता है। CSP विनिर्देश द्वारा आवश्यक प्रति-अनुरोध nonce विशिष्टता गारंटी को बहाल करने के लिए निष्कर्ष #26 (साझा nonce कुंजी) को संबोधित किया जाना चाहिए।
