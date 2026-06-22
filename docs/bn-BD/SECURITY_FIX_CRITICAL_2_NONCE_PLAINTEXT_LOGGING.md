# নিরাপত্তা সংশোধন: নন্স মান প্লেইনটেক্সটে লগ করা (সংকটজনক #২)

**সংশোধিত হয়েছে:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**টেস্ট:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## কি ভুল ছিল

দুটি স্থান অ্যাপ্লিকেশন লগ স্ট্রিমে প্রকৃত CSP নন্স মান হুবহু লগ করত:

**`Services/NonceMiddleware.cs` (লাইন ৩১):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (লাইন ৮২):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### কেন এটি সংকটজনক

একটি CSP নন্স হল *একমাত্র* প্রক্রিয়া যা CSP প্রয়োগ করা হলে ইনলাইন-স্ক্রিপ্ট ইনজেকশন প্রতিরোধ করে। এর নিরাপত্তা সম্পূর্ণরূপে এটি **একটি একক রেসপন্সের জীবনকালের জন্য গোপন** থাকার উপর নির্ভর করে।

ক্লাউড/এন্টারপ্রাইজ পরিবেশে অ্যাপ্লিকেশন লগ সাধারণত পড়তে পারে:
* অপারেশন টিম
* লগ অ্যাগ্রিগেশন সার্ভিস (যেমন Azure Monitor, Splunk, ELK)
* লগ সিঙ্কে রিডার অ্যাক্সেস সহ যেকোনো অ্যাকাউন্ট

যে কেউ `Nonce: <value>` ধারণকারী একটি লগ লাইন পড়তে পারে সে সেই নন্স মান সহ একটি ইনলাইন `<script>` ট্যাগ ইনজেক্ট করতে পারে এবং ব্রাউজারটি এটি কার্যকর করবে, CSP সম্পূর্ণরূপে বাইপাস করে। এমনকি যদি নন্স প্রতি অনুরোধে পরিবর্তন হয়, লাইভ লগ অ্যাক্সেস সহ একজন আক্রমণকারী একই অনুরোধের উইন্ডোর মধ্যে কাজ করতে পারে।

---

## কি সংশোধন করা হয়েছে

উভয় লগ স্টেটমেন্ট এমন বার্তা দিয়ে প্রতিস্থাপন করা হয়েছে যা মান না প্রকাশ করে নন্স জেনারেশনের *স্ট্যাটাস* নিশ্চিত করে:

**`NonceMiddleware.cs`:**
```csharp
// আগে (ঝুঁকিপূর্ণ):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// পরে (নিরাপদ):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// আগে (ঝুঁকিপূর্ণ):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// পরে (নিরাপদ):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## কিভাবে এটি সংশোধিত রাখবেন

1. **কখনো নন্স মান লগ করবেন না।** লগ বার্তাগুলি নিশ্চিত করতে পারে যে একটি নন্স তৈরি বা পুনরুদ্ধার করা হয়েছে (সাফল্য/ব্যর্থতার স্ট্যাটাস), কিন্তু নন্স স্ট্রিং নিজেই কোনো লগ প্যারামিটার, স্ট্রাকচার্ড-লগিং ফিল্ড, বা স্ট্রিং ইন্টারপোলেশনে কখনো উপস্থিত হওয়া উচিত নয়।

2. **নন্স-সম্পর্কিত কোডে যেকোনো নতুন লগ স্টেটমেন্ট পর্যালোচনা করুন** (`NonceMiddleware`, `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`) নিশ্চিত করতে যে নন্স মান অন্তর্ভুক্ত নেই।

3. **একই কারণে টেলিমেট্রি, মেট্রিক্স, বা ডিস্ট্রিবিউটেড ট্রেসে নন্স প্রকাশ করবেন না।** ট্রেস অ্যাট্রিবিউট এবং স্প্যান ট্যাগগুলি প্রায়ই লগ অ্যাগ্রিগেশন ব্যাকএন্ডে ফরওয়ার্ড করা হয়।

4. **নন্সকে একটি প্রতি-অনুরোধ গোপনীয়তা হিসেবে বিবেচনা করতে হবে।** এটি একটি একক অনুরোধের রেন্ডারিং পাইপলাইনের মধ্যে ব্যবহারের জন্য `HttpContext.Items`-এ সংরক্ষণ করা যেতে পারে, কিন্তু এটি HTTP রেসপন্স হেডার এবং এটি সুরক্ষিত HTML-এর `nonce="..."` অ্যাট্রিবিউট ছাড়া কোনো পর্যবেক্ষণযোগ্য চ্যানেলের মাধ্যমে প্রক্রিয়া ছেড়ে যাওয়া উচিত নয়।

### এই সংশোধন বলবৎকারী টেস্টসমূহ

| টেস্ট | কি ধরে |
|------|-----------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | `NonceRefresherService`-এ কোনো লগ বার্তায় নন্স স্ট্রিং পুনরায় প্রবর্তন করা হলে ব্যর্থ হয় |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | `NonceMiddleware`-এ কোনো লগ বার্তায় নন্স স্ট্রিং পুনরায় প্রবর্তন করা হলে ব্যর্থ হয় |
