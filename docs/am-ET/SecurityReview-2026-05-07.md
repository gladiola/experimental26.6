# የደህንነት ግምገማ — WebAppExperimental266

**ቀን:** 2026-05-07
**ወሰን:** ሙሉ የኮድ መሠረት ስታቲክ ትንታኔ (ከ2026-05-06 ግምገማ ክትትል)
**ገምጋሚ:** አውቶሜቲድ ደህንነት ግምገማ

---

## አጭር ማጠቃለያ

ይህ ክትትል ግምገማ ከ2026-05-06 ደህንነት ግምገማ ውስጥ ከተለዩት 5 ድክመቶች ውስጥ 3ቱ ሙሉ በሙሉ መስተካከላቸውን፣ 1ኛው ደግሞ በከፊል ተስተካክሎ እንደቀረ ያረጋግጣል። ግምገማው 4 አዲስ ግኝቶችንም ለይቷል። የትግበራው አጠቃላይ ደህንነት ሁኔታ ቀጥሎ እያሻሻሉ ነው።

---

## የቀደሙ ግኝቶች ሁኔታ (2026-05-06)

| # | ግኝት | ክብደት | ሁኔታ |
|---|---------|----------|--------|
| 20 | NonceRefresherService ጥቅም ያልተሰጣቸው Key Vault ኮንስትራክተር ጥገኛዎችን ይይዛል | 🟠 ከፍተኛ | ✅ ተስተካክሏል |
| 21 | OcspValidationService የውስጥ ካሽ ክር-ደህንነት ያልሆነ Dictionary ይጠቀማል | 🟡 መካከለኛ | ✅ ተስተካክሏል |
| 22 | OCSP ማረጋገጫ ስተብ አሁንም አለ — fail-closed ሆኗል ነገር ግን አልተተገበረም | 🔵 ዝቅተኛ | ⚠️ ተቀብሏል (በዲዛይን) |
| 23 | mTLS ከባዶ AllowedIssuers ጋር ሁሉንም ሰርተፊኬቶች ይቃወማል (fail-closed፣ ያልተዘጋጀ) | 🔵 ዝቅተኛ | ✅ ተስተካክሏል |
| 24 | OcspSettings.ServerUnavailableBehavior ወደ "Warn" ነባሪ ነው (ስህተት ሲኖር ማለፍ ያስችላል) | 🔵 ዝቅተኛ | ⚠️ በከፊል ተስተካክሏል |

---

## የቀደሙ ግኝቶች ዝርዝር ሁኔታ

### ✅ 20. NonceRefresherService ጥቅም ያልተሰጣቸው DI ጥገኛዎች — ተስተካክሏል

**ፋይል:** `Services/NonceRefresherService.cs`

`NonceRefresherService` ኮንስትራክተር አሁን `ILogger<NonceRefresherService>`፣ `ILoggerFactory` እና `INonceCatalogService` ብቻ ያወጃል። ቀደም ሲል ጥቅም ያልተሰጣቸው አራት ጥገኛዎች (`IKeyVaultSettingsService`፣ `INonceEncryptionSettingsService`፣ `IAzureADSettingsService`፣ `IAzureKeyVaultOperationsService`) ተወግደዋል። ይህ `EnableKeyVault = false` (ነባሪ) እና `EnableNonceServices = true` (ነባሪ) ሲሆን ትግበራው እንዳይጀምር ያደርግ የነበረውን የአገልግሎት ክልከላ አደጋ ፈቷል።

---

### ✅ 21. OcspValidationService ክር-ደህንነት ያልሆነ ካሽ — ተስተካክሏል

**ፋይል:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` በ`ConcurrentDictionary<string, CachedOcspResponse>` ተተክቷል። የ`_cache.Remove` ጥሪ ወደ `_cache.TryRemove` ተዘምኗል። ካሹ አሁን ለትይዩ ፕሮሰሲንግ ደህንነቱ የተጠበቀ ነው።

---

### ⚠️ 22. OCSP ማረጋገጫ ስተብ — ተቀብሏል (በዲዛይን)

**ፋይል:** `Services/OcspValidationService.cs`

ስተቡ አሁንም አለ ነገር ግን በትክክል fail-closed ሆኗል። `EnableOcspValidation` ወደ `false` ነባሪ ስለሆነ፣ ምንም ምርታዊ ተፅዕኖ የለውም። ሙሉ OCSP ትግበራ እስኪጠናቀቅ ድረስ እንደ መረጃ ግኝት ተቀብሏል።

---

### ✅ 23. mTLS ባዶ AllowedIssuers — ተስተካክሏል

**ፋይል:** `Extensions/ServiceCollectionExtensions.cs`

`ValidateClientCertificateIssuer = true` ሲሆን `AllowedIssuers` ባዶ ሲሆን አሁን የጀምሪያ ማስጠንቀቂያ ይመዘገባል:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

ይህ fail-closed ሁኔታ የሚያጋጥማቸውን ኦፕሬተሮች ግልጽ መመሪያ ይሰጣል።

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — በከፊል ተስተካክሏል

**ፋይሎች:** `appsettings.template.json` (ተስተካክሏል)፣ `Models/Settings/OcspSettings.cs` (ገና አልተስተካከለም)

ቴምፕሌቱ አሁን `"ServerUnavailableBehavior": "Fail"` ብሎ ትክክለኛ ትርጉም ይሰጣል። ሆኖም ግን `OcspSettings.cs` (መስመር 39) ውስጥ ያለው C# ክፍል ነባሪ `"Warn"` ሆኖ ቀጥሏል። ኦፕሬተር OCSP ካነቃ እና `ServerUnavailableBehavior`ን ከኮንፊጌሬሽን ፋይሉ ካቀረበ፣ ክፍሉ ነባሪ `"Warn"` በዝምታ ይተገበራል፣ ይህም OCSP ሰርቨር ቆሚ ጊዜ ማለፍ ያስችላል። ከቴምፕሌቱ ምክረ-ሃሳብ ጋር ለመዛመድ ክፍሉ ነባሪ መቀየር አለበት።

---

## አዲስ ግኝቶች

| # | አካባቢ | ክብደት |
|---|------|----------|
| 25 | OcspSettings ክፍሉ ነባሪ ("Warn") ከቴምፕሌቱ ("Fail") ይዘናጋል | 🔵 ዝቅተኛ |
| 26 | NonceCatalogService ነጠላ ጋራ nonce ቁልፍ ተጠያቂ nonce ግጭት ያስችላል | 🟡 መካከለኛ |
| 27 | OptimizedNonceMiddleware ስታቲክ ቆጠሪዎች ምልክት ያለው 32-bit integers ይጠቀማሉ (overflow አደጋ) | 🔵 ዝቅተኛ |
| 28 | Program.cs ባዶ ILoggerFactory singleton ይመዘግባል፣ ይህም የፍሬምወርኩን ሎገር ያጋርፋል | 🟡 መካከለኛ |

---

## 🟡 መካከለኛ

### 26. NonceCatalogService ጋራ Nonce ቁልፍ ተጠያቂ Nonce ግጭት ያስችላል

**ፋይሎች:** `Services/NonceCatalogService.cs`፣ `Services/NonceMiddleware.cs`፣ `Services/OptimizedNonceMiddleware.cs`

ዘ nonce ካታሎግ ሁሉንም nonces ነጠላ በሆነ ጋራ ቁልፍ `"CSPNonce"` ስር ያስቀምጣል። ትይዩ ጭነት ሲኖር፣ ይህ ዓይነቱ ሁኔታ ሊፈጠር ይችላል:

1. ጥያቄ A `RefreshNonceAsync()`ን ይጠራል — nonce A1 ​​እንደ `_nonceCollection["CSPNonce"]` ይቀመጣል።
2. ጥያቄ B `RefreshNonceAsync()`ን ይጠራል — nonce B1 `_nonceCollection["CSPNonce"]`ን ይሽርናል።
3. ጥያቄ A `GetANonce("CSPNonce")`ን ይጠራል — A1 ሳይሆን B1 ይቀበላል።
4. የጥያቄ A CSP ርዕስ እና ኢንላይን nonce ሁለቱም B1 ይይዛሉ።
5. ጥያቄ B ደግሞ B1 ይይዛል።

ሁለት ትይዩ ምላሾች አንድ nonce ያካፍላሉ። ሁለቱም ዋጋዎች አሁንም ክሪፕቶግራፊያዊ ዘፈቀደ እና ሊተነበዩ የማይችሉ ቢሆኑም (ምንም hardcoded string የለም)፣ አንድ አይነት nonce ዋጋ በብዙ ትይዩ ምላሾች ውስጥ ይታያል፣ ይህም CSP ዝርዝሩ የሚጠይቀውን በጥያቄ-ብቻ ልዩነት ዋስትና ያዳክማል። አንዱ ምላሽ ላይ nonce ሊከታተል የሚችል አጥቂ ለቢያንስ ሌላ ትይዩ ምላሽ ዋጋ ያለው nonce ይኖረዋል።

**ምክረ-ሃሳብ:** Nonce ቀጥታ ለእያንዳንዱ ጥያቄ በ middleware ውስጥ ያስፈልጋል (ለምሳሌ `Nonce.GenerateSecureNonce()`) እና `HttpContext.Items["Nonce"]` ብቻ ቀምጡ፣ ጋራ ካታሎጉን ለጥያቄ-ብቻ nonces ዙሪያ ያልፉ። ጋራ ካታሎጉ ያስፈልጋል ጥቅሉ nonce በ middleware ሽፋኖች ማጋራት ካስፈለገ ብቻ ሲሆን፣ ይህንን `HttpContext.Items` ተፈጥሯዊ ሁኔታ ይሠራዋል።

---

### 28. Program.cs ባዶ ILoggerFactory Singleton ይመዘግባል

**ፋይል:** `Program.cs` (መስመር 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core `WebApplication.CreateBuilder` ወቅት ሙሉ ኮንፊጌሬሽን `ILoggerFactory` (ከ`builder.Logging` ኮንፊጌሬሽን ያሉ ሁሉ ሎጊንግ አቅራቢዎች ጋር) ሲቀርብ ይመዘግባል። ይህ ግልጽ `AddSingleton` ምዝገባ ሁለተኛ፣ ያልተዋቀረ `LoggerFactory` ምሳሌ ያለ አቅራቢዎች ይጨምራል። `GetRequiredService<ILoggerFactory>()` በቅርቡ ለተመዘገበው ትግበራ ስለሚሰጥ፣ DI በኩል `ILoggerFactory` የሚቀበሉ አገልግሎቶች (እንደ `NonceRefresherService`) ይህ ባዶ ፋብሪካ ይጠቀማሉ እና `_loggerFactory.CreateLogger<T>()` በኩል ምንም ሎጊንግ ውጤት አያስገኙም።

**አደጋ:** `NonceRefresherService` ውስጥ ዝም ሎጊንግ — nonce ምስረታ ስኬቶች እና ሽፈቶች ወደ ኮንፊጌሬሽን ሎጊንግ ሲንኮች አይሄዱም። ይህ ተግባርን ሳይነካ ደህንነት-ተኮር ስራዎች ወቅት የትግበራ ታዛቢነትን ይቀንሳል።

**ምክረ-ሃሳብ:** ግልጽ `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` ምዝገባ ያስወግዱ። የፍሬምወርኩ ኮንፊጌሬሽን `ILoggerFactory` (Console እና ሌሎች አቅራቢዎች ጋር) ከዚያ ሲጠቀሙ ሲደርስ ትክክለኛ ምላሽ ይሰጣል።

---

## 🔵 ዝቅተኛ / መረጃ ሰጪ

### 25. OcspSettings ክፍሉ ነባሪ ከቴምፕሌቱ ይዘናጋል

**ፋይል:** `Models/Settings/OcspSettings.cs` (መስመር 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

ቴምፕሌቱ (`appsettings.template.json`) `"ServerUnavailableBehavior": "Fail"` ብሎ ይገልጻል፣ ነገር ግን C# ክፍሉ ነባሪ `"Warn"` ነው። `ServerUnavailableBehavior` ንቁ ኮንፊጌሬሽን ፋይል ውስጥ ከሌለ፣ ክፍሉ ነባሪ ከቴምፕሌቱ ምክረ-ሃሳብ ሳይሆን በዝምታ ይተገበራል። ይህ ከ#24 ግኝት ቅሪት ነው።

**ምክረ-ሃሳብ:** ክፍሉ ነባሪ ከ`"Warn"` ወደ `"Fail"` ቀይሩ ቴምፕሌቱ እና ዝቅተኛ ልዩ መብት መርሆ ጋር ለመዛመድ።

---

### 27. OptimizedNonceMiddleware ስታቲክ ቆጠሪዎች ሊሞሉ ይችላሉ

**ፋይል:** `Services/OptimizedNonceMiddleware.cs` (መስመሮች 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

እነዚህ ምልክት ያላቸው 32-bit ቆጠሪዎች `Interlocked.Increment` ተጠቅሞ አቶሚካሊ ይጨምራሉ። ወደ 2.1 ቢሊዮን ጭማሪዎች ካሉ በኋላ ወደ `int.MinValue` (−2,147,483,648) ይሽከረከራሉ፣ ይህም ቅልጥፍና ስሌቱ `(total - generated) * 100.0 / total` ስህተት ወይም ትርጉም አልባ ውጤቶች እንዲሰጥ ያደርጋል። በሰከንድ 1,000 ጥያቄዎች፣ overflow ከ24.8 ቀናት ቀጣይ ስራ በኋላ ይፈጠራል።

**ምክረ-ሃሳብ:** ቆጠሪ ፊልድ ዓይነቶችን ከ`int` ወደ `long` ቀይሩ እና overflow ለመከላከል `Interlocked.Increment`ን `long` overload ይጠቀሙ።

---

## የደህንነት ርዕስ ግምገማ (ወቅታዊ ሁኔታ)

ከዚህ በፊት ባለው ግምገማ ምንም ለውጥ ሳይደረግ `UseStandardSecurityHeaders` በኩል የሚከተሉት ርዕሶች ይተገበራሉ:

| ርዕስ | ዋጋ | ግምገማ |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ ጥሩ |
| `X-XSS-Protection` | `0` | ✅ ጥሩ (የዘለቄ አዲቲርን ያሰናክላል) |
| `X-Content-Type-Options` | `nosniff` | ✅ ጥሩ |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ ጥሩ |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ ጥሩ |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ ጥሩ |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ ጥሩ |
| `Permissions-Policy` | geolocation፣ camera፣ microphone፣ interest-cohort ተሰናክሏል | ✅ ጥሩ |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ ጥሩ |
| `Content-Security-Policy` | Nonce-ተኮር፣ CSP ሲነቃ ይተገበራል | ✅ ጥሩ |
| `Server` | ወደ `"webserver"` ተደብቋል | ✅ ጥሩ |
| `X-Powered-By` | ተወግዷል | ✅ ጥሩ |

---

## አጠቃላይ ግምገማ

ከቀደሙ ግምገማዎች ሁሉም ከፍተኛ ክብደት ያላቸው ግኝቶች ተስተካክለዋል። የአሁኑ ግኝቶች ወደ ሁለት የመካከለኛ ክብደት ጉዳዮች (#26 ጋራ nonce ቁልፍ፣ #28 ባዶ ILoggerFactory) እና ሁለት ዝቅተኛ ክብደት መረጃ ሰጪ ዕቃዎች (#25 ክፍሉ ነባሪ ልዩነት፣ #27 ቆጠሪዎቹ ውስጥ integer overflow) ይወሰናሉ። ለ#28 ግኝት (ባዶ ILoggerFactory singleton) ወቅታዊ ትኩረት ይመከራል ምክንያቱም ደህንነት-ተኮር ምርመራ ሎጊንግን nonce ስራዎች ወቅት በዝምታ ያዘጋዋልና። ሁሉንም ሁሉ CSP ዝርዝሩ የሚጠይቀውን ጥያቄ-ብቻ nonce ልዩነት ዋስትና ለማደስ ግኝት #26 (ጋራ nonce ቁልፍ) ማስተካከል አለበት።
