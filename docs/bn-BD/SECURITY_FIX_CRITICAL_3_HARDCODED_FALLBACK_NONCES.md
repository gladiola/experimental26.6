# নিরাপত্তা সংশোধন: হার্ডকোডেড ফলব্যাক নন্স (সংকটজনক #৩)

**সংশোধিত হয়েছে:** `Services/OptimizedNonceMiddleware.cs`  
**টেস্ট:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## কি ভুল ছিল

`OptimizedNonceMiddleware`-এ তিনটি হার্ডকোডেড স্ট্রিং লিটারেল ছিল যা স্বাভাবিক নন্স জেনারেশন ব্যর্থ হলে বা এখনো চালানো না হলে ফলব্যাক নন্স মান হিসেবে ব্যবহৃত হত:

| অবস্থান | হার্ডকোডেড মান |
|----------|-----------------|
| `InvokeAsync` — প্রথম অনুরোধ, ক্যাটালগ খালি | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — জেনারেশন খালি স্ট্রিং রিটার্ন করেছে | `"fallback-nonce"` |
| `InvokeAsync` — ব্যতিক্রম পথ | `"error-fallback-nonce"` |

### কেন এটি সংকটজনক

**একটি নন্স শুধুমাত্র তখনই নিরাপদ যখন একজন আক্রমণকারী এটি পূর্বাভাস দিতে পারে না।** হার্ডকোডেড লিটারেলগুলি সোর্স কন্ট্রোলে কমিট করা হয় এবং তাই রিপোজিটরি অ্যাক্সেস সহ যে কেউ জানতে পারে (যেকোনো আক্রমণকারী যিনি সোর্স অ্যাক্সেস পেয়েছেন বা বাইনারি ডিকম্পাইল করেছেন তা সহ)।

নির্দিষ্ট বিপদ হল এই ফলব্যাক পথগুলি **ত্রুটির অবস্থা** দ্বারা সক্রিয় হয় — ঠিক সেই পরিস্থিতি যা একজন আক্রমণকারী সবচেয়ে বেশি ইঞ্জিনিয়ার করার সম্ভাবনা রাখে (যেমন, রেট-লিমিটিং বা নেটওয়ার্ক ব্যাঘাতের মাধ্যমে Key Vault সাময়িকভাবে অনুপলব্ধ করা)। যখন অ্যাপ্লিকেশন একটি পূর্বাভাসযোগ্য নন্সে গ্রেসফুলি ডিগ্রেড করে, CSP হেডার সাজসজ্জামূলক হয়ে ওঠে: আক্রমণকারী কেবল `<script nonce="fallback-nonce">` ইনজেক্ট করে এবং ব্রাউজার এটি কার্যকর করে।

### মূল কারণের কোড (সংশোধনের আগে)

```csharp
// কোনো নন্স তৈরির আগে প্রথম অনুরোধ
existingNonce = "bootstrap-nonce-placeholder";

// নন্স জেনারেশন খালি রিটার্ন করেছে
nonce = "fallback-nonce";

// ব্যতিক্রম পথ
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## কি সংশোধন করা হয়েছে

সমস্ত তিনটি ফলব্যাক পথ এখন রানটাইমে একটি তাজা, অপ্রত্যাশিত 16-বাইট র্যান্ডম নন্স তৈরি করতে `Nonce.GenerateSecureNonce()` কল করে:

```csharp
// আগে (ঝুঁকিপূর্ণ):
existingNonce = "bootstrap-nonce-placeholder";
// পরে (নিরাপদ):
existingNonce = Nonce.GenerateSecureNonce();

// আগে (ঝুঁকিপূর্ণ):
nonce = "fallback-nonce";
// পরে (নিরাপদ):
nonce = Nonce.GenerateSecureNonce();

// আগে (ঝুঁকিপূর্ণ):
context.Items["Nonce"] = "error-fallback-nonce";
// পরে (নিরাপদ):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` ক্রিপ্টোগ্রাফিকভাবে র্যান্ডম 16 বাইট তৈরি করতে `RandomNumberGenerator.Fill` (একটি CSPRNG) ব্যবহার করে যা Base64 হিসেবে এনকোড করা হয়। যেহেতু এটি Key Vault নির্ভরতা ছাড়া একটি স্ট্যাটিক পদ্ধতি, তাই এটি Key Vault অনুপলব্ধ থাকলেও কল করা নিরাপদ — ঠিক সেই ত্রুটির অবস্থা যা আগে হার্ডকোডেড ফলব্যাক প্রকাশ করত।

---

## কিভাবে এটি সংশোধিত রাখবেন

1. **কোডবেসে কোথাও হার্ডকোডেড নন্স লিটারেল প্রবর্তন করবেন না**, প্রেক্ষাপট নির্বিশেষে (ফলব্যাক, পরীক্ষা, প্লেসহোল্ডার, মন্তব্যের উদাহরণ যা কপি-পেস্ট হতে পারে, ইত্যাদি)।

2. **`context.Items["Nonce"]` সেট করে প্রতিটি কোড পথকে ক্রিপ্টোগ্রাফিকভাবে র্যান্ডম মান ব্যবহার করতে হবে।** `Nonce.GenerateSecureNonce()` বা `RandomNumberGenerator.GetBytes(16)` + Base64 কল করুন।

3. **অনুরোধ জুড়ে একটি একক নন্স ক্যাশ করবেন না।** প্রতিটি অনুরোধকে তার নিজস্ব তাজা নন্স পেতে হবে।

4. **ত্রুটি পথগুলি সবচেয়ে বিপজ্জনক।** যদি কোনো কারণে নন্স জেনারেশন ব্যর্থ হয়, রেসপন্সটি এখনো একটি র্যান্ডম নন্স পাওয়া উচিত, কখনো একটি পূর্বাভাসযোগ্য ফলব্যাক নয়।

5. **`OptimizedNonceMiddleware`-তে যেকোনো ভবিষ্যত পরিবর্তন পর্যালোচনা করুন** — বিশেষ করে তিনটি শাখা যেখানে নন্স সেট করা যেতে পারে: ignore-path শাখা, empty-generation শাখা, এবং exception-handler শাখা।

### এই সংশোধন বলবৎকারী টেস্টসমূহ

| টেস্ট | কি ধরে |
|------|-----------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | প্রথম-অনুরোধ শাখায় `"bootstrap-nonce-placeholder"` পুনরায় প্রবর্তন করা হলে ব্যর্থ হয় |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | empty-generation শাখায় `"fallback-nonce"` পুনরায় প্রবর্তন করা হলে ব্যর্থ হয় |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | exception handler-এ `"error-fallback-nonce"` পুনরায় প্রবর্তন করা হলে ব্যর্থ হয় |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | কোনো ফলব্যাক ৫০ টি পরপর কলে একই নন্স দুইবার তৈরি করলে ব্যর্থ হয় (যা যেকোনো হার্ডকোডেড স্ট্রিং করবে) |
