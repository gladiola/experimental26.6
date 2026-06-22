# নিরাপত্তা পর্যালোচনা — WebAppExperimental266

**তারিখ:** 2026-05-06
**পরিসর:** কোড রিপোজিটরির সম্পূর্ণ অডিট (2026-05-05 পর্যালোচনার অনুসরণ)
**পর্যালোচক:** স্বয়ংক্রিয় নিরাপত্তা পর্যালোচনা

---

## নির্বাহী সারসংক্ষেপ

এই অনুসরণ পর্যালোচনা নিশ্চিত করে যে 2026-05-05 নিরাপত্তা পর্যালোচনায় চিহ্নিত উনিশটি (19) নিরাপত্তা ত্রুটি সফলভাবে সংশোধন করা হয়েছে। এই পর্যালোচনাটি এই সময়ে আবিষ্কৃত পাঁচটি (5) নতুন বা অবশিষ্ট ত্রুটিও চিহ্নিত করে। আগের পর্যালোচনা থেকে অ্যাপ্লিকেশনের সামগ্রিক নিরাপত্তা অবস্থান উল্লেখযোগ্যভাবে উন্নত হয়েছে।

---

## পূর্ববর্তী ত্রুটিগুলির অবস্থা (2026-05-05)

সমস্ত উনিশটি (19) পূর্ববর্তী ত্রুটি **সংশোধিত হিসেবে নিশ্চিত করা হয়েছে**:

| # | ত্রুটি | গুরুত্ব | অবস্থা |
|---|--------|---------|--------|
| 1 | নন্স তৈরিতে AES-GCM IV পুনরায় ব্যবহার | 🔴 সংকটজনক | ✅ সংশোধিত |
| 2 | প্লেইনটেক্সট লগে নন্স লেখা | 🔴 সংকটজনক | ✅ সংশোধিত |
| 3 | হার্ড-কোড করা নন্স সেটআপ স্ট্রিং | 🔴 সংকটজনক | ✅ সংশোধিত |
| 4 | গ্লোবাল নন্স স্টোর থ্রেড-নিরাপদ নয় | 🟠 উচ্চ | ✅ সংশোধিত |
| 5 | mTLS ক্লায়েন্ট সার্টিফিকেট ইস্যুয়ার যাচাইকরণ প্রয়োগ করা হয়নি | 🟠 উচ্চ | ✅ সংশোধিত |
| 6 | mTLS সার্টিফিকেট প্রত্যাহার পরীক্ষা ডিফল্টে অক্ষম | 🟠 উচ্চ | ✅ সংশোধিত |
| 7 | OCSP সর্বদা বৈধ ফেরত দেয় (stub) | 🟠 উচ্চ | ✅ সংশোধিত |
| 8 | ফিচার ফ্ল্যাগে প্রমাণীকরণ/অনুমোদন ডিফল্টে অক্ষম | 🟠 উচ্চ | ✅ সংশোধিত |
| 9 | পাইপলাইনের শেষে নিরাপত্তা হেডার যোগ করা হয়েছে | 🟠 উচ্চ | ✅ সংশোধিত |
| 10 | সেশন কুকিতে `Secure` + `SameSite` নেই | 🟡 মাঝারি | ✅ সংশোধিত |
| 11 | গ্লোবাল `Set-Cookie` হেডার ইনজেক্ট করা হয়েছে | 🟡 মাঝারি | ✅ সংশোধিত |
| 12 | সর্বত্র `Content-Type` হার্ড-কোড করে `text/html` করা হয়েছে | 🟡 মাঝারি | ✅ সংশোধিত |
| 13 | `AllowedHosts` ওয়াইল্ডকার্ড টোকেনে সেট করা হয়েছে | 🟡 মাঝারি | ✅ সংশোধিত |
| 14 | টেমপ্লেটে `<script>` ট্যাগে নন্স সংযুক্ত নয় | 🟡 মাঝারি | ✅ সংশোধিত |
| 15 | `Referrer-Policy` হেডার অনুপস্থিত | 🟡 মাঝারি | ✅ সংশোধিত |
| 16 | প্লেইনটেক্সট লগে PII লেখা | 🔵 নিম্ন | ✅ সংশোধিত |
| 17 | বার্তায় স্ট্রিং সংযোজন | 🔵 নিম্ন | ✅ সংশোধিত |
| 18 | Key Vault অপারেশন হলো stubs | 🔵 নিম্ন | ✅ সংশোধিত |
| 19 | `X-XSS-Protection: 1; mode=block` পুরনো | 🔵 নিম্ন | ✅ সংশোধিত |

---

## নতুন / অবশিষ্ট ত্রুটি

| # | অবস্থান | গুরুত্ব |
|---|---------|---------|
| 20 | NonceRefresherService অব্যবহৃত Key Vault কনস্ট্রাক্টর নির্ভরতা ধরে রেখেছে | 🟠 উচ্চ |
| 21 | OcspValidationService মেমরি ক্যাশে থ্রেড-নিরাপদ নয় এমন Dictionary ব্যবহার করছে | 🟡 মাঝারি |
| 22 | OCSP যাচাইকরণ stub এখনও আছে — fail-closed কিন্তু বাস্তবায়িত নয় | 🔵 নিম্ন |
| 23 | mTLS খালি AllowedIssuers-সহ সমস্ত ক্লায়েন্ট সার্টিফিকেট প্রত্যাখ্যান করে (fail-closed, ডকুমেন্টেড নয়) | 🔵 নিম্ন |
| 24 | OcspSettings.ServerUnavailableBehavior ডিফল্ট "Warn" (ত্রুটির সময় পাস-থ্রু অনুমতি দেয়) | 🔵 নিম্ন |

---

## বিস্তারিত ত্রুটি

### ✅ 2026-05-05 থেকে নিশ্চিত সংশোধন

#### 1. AES-GCM IV পুনরায় ব্যবহার — সংশোধিত

**ফাইল:** `Models/Main_Objects/Nonce.cs`

AES-GCM থেকে নন্স তৈরি সম্পূর্ণরূপে পুনর্লিখিত হয়েছে। `Nonce.GenerateSecureNonce()` এখন 16 র্যান্ডম বাইটের জন্য `RandomNumberGenerator.Fill(randomBytes)` কল করে এবং একটি Base64 স্ট্রিং ফেরত দেয়। কোনো Key Vault নির্ভরতা নেই, কোনো IV নেই, কোনো এনক্রিপশন নেই — CSP নন্সের জন্য এটি সঠিক পদ্ধতি।

---

#### 2. নন্স মান লগে লেখা হয়নি — সংশোধিত

**ফাইলসমূহ:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

উভয় ফাইল শুধুমাত্র স্ট্যাটাস বার্তা লেখে (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) এবং কখনই নন্স মান নিজেই লেখে না।

---

#### 3. হার্ড-কোড করা সেটআপ নন্স সরানো হয়েছে — সংশোধিত

**ফাইল:** `Services/OptimizedNonceMiddleware.cs`

তিনটি হার্ড-কোড করা স্ট্রিং (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) স্বাভাবিক পথে `Nonce.GenerateSecureNonce()`-তে কল এবং দুটি সেটআপ ত্রুটি নির্দেশক দ্বারা প্রতিস্থাপিত হয়েছে।

---

#### 4. থ্রেড-নিরাপদ নন্স স্টোর — সংশোধিত

**ফাইল:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` কে `ConcurrentDictionary<string, Nonce>`-তে পরিবর্তন করা হয়েছে। `GetANonce` এখন দুই-ধাপ চেকের পরিবর্তে একটি পারমাণবিক `TryGetValue` কল ব্যবহার করে।

---

#### 5. mTLS ক্লায়েন্ট সার্টিফিকেট ইস্যুয়ার যাচাইকরণ এখন কাজ করছে — সংশোধিত

**ফাইলসমূহ:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

হার্ড-কোড করা ইস্যুয়ার যাচাইকরণ প্রসঙ্গ একটি `mtlsSettings.IsIssuerAllowed(issuer)` কলে প্রতিস্থাপিত হয়েছে, যা `AllowedIssuers`-এর বিপরীতে কেস-ইনসেনসিটিভ স্ট্রিং তুলনা করে। তালিকা খালি থাকলে (কনফিগার করা নেই), পদ্ধতিটি `false` ফেরত দেয়, সমস্ত সার্টিফিকেট প্রত্যাখ্যান করে (fail-closed)।

---

#### 6. mTLS সার্টিফিকেট প্রত্যাহার পরীক্ষা এখন ডিফল্টে সক্ষম — সংশোধিত

**ফাইল:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` এখন ডিফল্টে `true`। `appsettings.template.json`-ও `"CheckCertificateRevocation": true` সেট করে।

---

#### 7. OCSP Stub এখন Fail-Closed — সংশোধিত

**ফাইল:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` এখন `IsValid = false` এবং `OcspStatus.Error` ফেরত দেয় এবং ত্রুটি লগ করে, নীরবে `IsValid = true` ফেরত দেওয়ার পরিবর্তে। OCSP সক্ষম করা প্রকৃত বাস্তবায়ন না হওয়া পর্যন্ত সমস্ত সার্টিফিকেট প্রত্যাখ্যান করবে।

---

#### 8. প্রমাণীকরণ ও অনুমোদন এখন ডিফল্টে সক্ষম — সংশোধিত

**ফাইল:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` এবং `EnableAuthorization` উভয়ই এখন `FeatureFlags` ক্লাসে ডিফল্টে `true`। `appsettings.json`-ও উভয়কে `true`-তে সেট করে।

---

#### 9. পাইপলাইনে নিরাপত্তা হেডার আগে যোগ করা হয়েছে — সংশোধিত

**ফাইল:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` এবং `UseStandardSecurityHeaders` এখন `UseRouting`, `UseAuthentication`, এবং `UseAuthorization`-এর আগে কল করা হয়। সমস্ত রেসপন্স, short-circuit 401/403 রেসপন্স সহ, তাদের নিরাপত্তা হেডার পাবে।

---

#### 10–15. কুকি, Content-Type, AllowedHosts, টেমপ্লেটে Nonce, Referrer-Policy — সংশোধিত

**ফাইলসমূহ:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- সেশন কুকি এখন `CookieSecurePolicy.Always` এবং `SameSiteMode.Strict`-এ সেট করা হয়েছে।
- অকাঙ্ক্ষিত গ্লোবাল `Set-Cookie` হেডার সরানো হয়েছে।
- গ্লোবাল `Content-Type: text/html` ভেরিয়েবল সরানো হয়েছে।
- `appsettings.json`-এ `AllowedHosts` এখন `"localhost;127.0.0.1"`; টেমপ্লেট `"{{YOUR_HOSTNAME}}"` ব্যবহার করে।
- `_Layout.cshtml`-এ তিনটি `<script>` ট্যাগেই এখন `nonce="@Context.Items["Nonce"]"` আছে।
- `Referrer-Policy: strict-origin-when-cross-origin` `UseStandardSecurityHeaders` দ্বারা যোগ করা হয়েছে।

---

#### 16–19. লগে PII, স্ট্রিং সংযোজন, Key Vault Stubs, X-XSS-Protection — সংশোধিত

**ফাইলসমূহ:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- সমস্ত PII (OID, ইমেইল, নাম, SID, ভূমিকা) লগে লেখার আগে `LoggingHelper.HashPii()`-এর মাধ্যমে HMAC-SHA256 হ্যাশ করা হয়। কনফিগারেশনে `Logging:PiiHmacKey`-এর মাধ্যমে স্থিতিশীল HMAC কী প্রদান করা যেতে পারে; কনফিগার না করা হলে প্রতি-প্রক্রিয়া কী ব্যবহার করা হয়।
- Cosmos DB লগ বার্তা শুধুমাত্র যাচাই করে যে সংযোজন স্ট্রিং বিদ্যমান (`!string.IsNullOrEmpty`), বিষয়বস্তু নয়।
- `AzureKeyVaultCertificateOperations` এখন স্টার্টআপের সময় সার্টিফিকেট null হলে `InvalidOperationException` ছুঁড়ে দেয়, নীরবে মেটাডেটা মান ফেরত দেওয়ার পরিবর্তে।
- `X-XSS-Protection` এখন `"0"`-এ সেট করা (পুরনো XSS ফিল্টার নিষ্ক্রিয় করে), আধুনিক ব্রাউজার নির্দেশিকার সাথে সামঞ্জস্যপূর্ণ।

---

## 🟠 উচ্চ

### 20. NonceRefresherService অব্যবহৃত Key Vault কনস্ট্রাক্টর নির্ভরতা ধরে রেখেছে

**ফাইল:** `Services/NonceRefresherService.cs`

`NonceRefresherService` `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, এবং `IAzureKeyVaultOperationsService`-এর জন্য কনস্ট্রাক্টর প্যারামিটার ইনজেক্ট করে। যেহেতু নন্স তৈরি সরাসরি `RandomNumberGenerator` ব্যবহার করতে সরলীকৃত হয়েছে, এই নির্ভরতাগুলির কোনোটিই ব্যবহার করা হয় না।

**সমস্যা:** যখন `EnableNonceServices = true` এবং `EnableKeyVault = false` (ডিফল্ট), এই সার্ভিসগুলি DI কন্টেইনারে নিবন্ধিত নয়, নন্স সার্ভিস প্রথম রিসলভ হওয়ার সময় স্টার্টআপে `InvalidOperationException` ঘটায়। এটি ডিফল্ট কনফিগারেশন দ্বারা সৃষ্ট একটি সার্ভিস-অনুপলব্ধ অবস্থা। `FeatureFlags` ক্লাস ডিফল্টে `EnableNonceServices = true` সেট করে, তাই শুধুমাত্র ক্লাস ডিফল্টের উপর নির্ভরশীল যেকোনো পরিবেশ (`appsettings.json` পরিবর্তন ছাড়া) ব্যর্থ হবে।

**সুপারিশ:** `NonceRefresherService` থেকে চারটি অব্যবহৃত কনস্ট্রাক্টর প্যারামিটার এবং তাদের সংশ্লিষ্ট প্রাইভেট ফিল্ড সরান। সার্ভিসের শুধুমাত্র `ILogger<NonceRefresherService>`, `ILoggerFactory`, এবং `INonceCatalogService` প্রয়োজন।

---

## 🟡 মাঝারি

### 21. OcspValidationService মেমরি ক্যাশে থ্রেড-নিরাপদ নয় এমন Dictionary ব্যবহার করছে

**ফাইল:** `Services/OcspValidationService.cs` (লাইন 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` সমকালীন পাঠ এবং লেখার জন্য নিরাপদ নয়। যদি `OcspValidationService` singleton হিসেবে নিবন্ধিত হয় (বা একই ইনস্ট্যান্স রিকোয়েস্ট জুড়ে শেয়ার করা হয়), সমকালীন OCSP যাচাইকরণ ক্যাশ নষ্ট করতে পারে, যা হারানো এন্ট্রি, অপ্রত্যাশিত এক্সেপশন বা পুরনো ডেটা ফেরত দিতে পারে।

**সুপারিশ:** `Dictionary<string, CachedOcspResponse>` কে `ConcurrentDictionary<string, CachedOcspResponse>`-তে পরিবর্তন করুন। `_cache.Remove` কল (লাইন 103) `_cache.TryRemove`-এ আপডেট করুন।

---

## 🔵 নিম্ন / তথ্যমূলক

### 22. OCSP যাচাইকরণ Stub — Fail-Closed কিন্তু বাস্তবায়িত নয়

**ফাইল:** `Services/OcspValidationService.cs` (লাইন 157–173)

`PerformOcspValidationAsync` এখনও একটি stub। ত্রুটি #7 সংশোধন আচরণ "সর্বদা বৈধ" থেকে "সর্বদা অবৈধ (fail-closed)"-এ পরিবর্তন করেছে। তবে, এটি প্রকৃত OCSP বাস্তবায়ন নয়। `EnableOcspValidation = false` (ডিফল্ট) হলে, ব্যবসায়িক প্রভাব নেই। যেকোনো পরিবেশে OCSP সক্ষম করার আগে, একটি প্রকৃত OCSP স্ট্যাটাস বাস্তবায়ন করতে হবে।

---

### 23. খালি AllowedIssuers সহ mTLS সমস্ত ক্লায়েন্ট সার্টিফিকেট প্রত্যাখ্যান করে

**ফাইল:** `Models/Settings/MtlsSettings.cs`

যখন `ValidateClientCertificateIssuer = true` (ডিফল্ট) এবং `AllowedIssuers` খালি (কনফিগার না করা হলেও ডিফল্ট), `IsIssuerAllowed()` `false` ফেরত দেয়, সমস্ত ক্লায়েন্ট সার্টিফিকেট প্রত্যাখ্যান করে। এটি পছন্দসই fail-closed আচরণ, কিন্তু স্পষ্টভাবে ডকুমেন্ট করা নয়। টেমপ্লেট সাবধানে না পড়ে mTLS সক্ষম করা অপারেটররা সমস্ত ক্লায়েন্ট সার্টিফিকেট স্পষ্ট ব্যাখ্যা ছাড়াই প্রত্যাখ্যাত হতে দেখতে পারেন।

**সুপারিশ:** `ValidateClientCertificateIssuer = true` এবং `AllowedIssuers` খালি থাকলে স্টার্টআপে একটি লগ সতর্কতা যোগ করুন।

---

### 24. OcspSettings.ServerUnavailableBehavior ডিফল্ট "Warn"

**ফাইল:** `appsettings.template.json` (লাইন 134), `Services/OcspValidationService.cs`

`ServerUnavailableBehavior`-এর ডিফল্ট মান টেমপ্লেটে `"Warn"`, যা OCSP সার্ভিস অ্যাক্সেসযোগ্য না হলে রিকোয়েস্টকে পাস-থ্রু করতে অনুমতি দেয়। উচ্চ-নিরাপত্তা পরিবেশের জন্য, এটি `"Fail"` হওয়া উচিত যাতে OCSP সার্ভিস ব্যর্থতা সার্টিফিকেট প্রত্যাহার পরীক্ষা নীরবে এড়িয়ে না যায়।

**সুপারিশ:** টেমপ্লেটে তিনটি বিকল্পই (`Fail`, `Allow`, `Warn`) স্পষ্টভাবে ডকুমেন্ট করুন এবং ন্যূনতম বিশেষাধিকারের নীতি অনুযায়ী ডিফল্ট `"Fail"`-এ পরিবর্তন করার কথা বিবেচনা করুন।

---

## নিরাপত্তা হেডার মূল্যায়ন (বর্তমান অবস্থা)

এই হেডারগুলি `UseStandardSecurityHeaders` দ্বারা যোগ করা হয়:

| হেডার | মান | মূল্যায়ন |
|-------|-----|---------|
| `X-Frame-Options` | `DENY` | ✅ ভালো |
| `X-XSS-Protection` | `0` | ✅ ভালো (পুরনো XSS ফিল্টার নিষ্ক্রিয় করে) |
| `X-Content-Type-Options` | `nosniff` | ✅ ভালো |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ ভালো |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ ভালো |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ ভালো |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ ভালো |
| `Permissions-Policy` | geolocation, camera, microphone, interest-cohort নিষ্ক্রিয় | ✅ ভালো |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ ভালো |
| `Content-Security-Policy` | Nonce-ভিত্তিক, CSP সক্ষম হলে অন্তর্ভুক্ত | ✅ ভালো |
| `Server` | `"webserver"`-এ লুকানো | ✅ ভালো |
| `X-Powered-By` | সরানো হয়েছে | ✅ ভালো |

---

## সামগ্রিক মূল্যায়ন

অ্যাপ্লিকেশনটি আগের পর্যালোচনার সমস্ত সংকটজনক ও উচ্চ-গুরুত্বের দুর্বলতা সংশোধন করেছে। বর্তমান ত্রুটিগুলি একটি উচ্চ-গুরুত্বের কনফিগারেশন/DI সমস্যায় (ত্রুটি #20) এবং নিম্ন-গুরুত্বের তথ্যমূলক আইটেমে সীমাবদ্ধ। নিরাপত্তা অবস্থান উল্লেখযোগ্যভাবে উন্নত হয়েছে। ত্রুটি #20-এর জন্য জরুরি পদক্ষেপ সুপারিশ করা হচ্ছে (NonceRefresherService-এ অব্যবহৃত DI নির্ভরতা) কারণ এটি ডিফল্ট কনফিগারেশনে অ্যাপ্লিকেশনকে শুরু হতে বাধা দিতে পারে।
