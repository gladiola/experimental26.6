# নিরাপত্তা সংশোধন: নন্স জেনারেশনে AES-GCM IV পুনরায় ব্যবহার (সংকটজনক #১)

**সংশোধিত হয়েছে:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**টেস্ট:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## কি ভুল ছিল

`Nonce` ক্লাস প্রতিটি কলে Azure Key Vault থেকে আনা **স্থির IV সহ AES-GCM এনক্রিপশন** ব্যবহার করত। একই AES-GCM কী দিয়ে একই IV পুনরায় ব্যবহার করা একটি মারাত্মক ক্রিপ্টোগ্রাফিক ত্রুটি:

* একজন আক্রমণকারী যে একই IV এবং কী দিয়ে এনক্রিপ্ট করা দুটি সাইফারটেক্সট পর্যবেক্ষণ করে সে দুটি প্লেইনটেক্সটের XOR পুনরুদ্ধার করতে XOR করতে পারে।
* আরও গুরুত্বপূর্ণভাবে নন্স-ভিত্তিক প্রমাণীকরণ ট্যাগের জন্য, IV পুনরায় ব্যবহার প্রমাণীকরণ ট্যাগ জাল করার অনুমতি দেয়, AES-GCM-এর অখণ্ডতার গ্যারান্টি সম্পূর্ণরূপে ভেঙে দেয়।

ক্রিপ্টোগ্রাফিক ব্যর্থতার বাইরে, এনক্রিপশন এই ব্যবহারের ক্ষেত্রে **কোনো নিরাপত্তা সুবিধা যোগ করেনি**। একটি CSP নন্সের শুধুমাত্র দুটি বৈশিষ্ট্য প্রয়োজন: এটি অবশ্যই **অপ্রত্যাশিত** এবং **প্রতি অনুরোধে অনন্য** হতে হবে। এই বৈশিষ্ট্যগুলি ইতিমধ্যে একটি ক্রিপ্টোগ্রাফিকভাবে নিরাপদ র্যান্ডম নম্বর জেনারেটর (`RandomNumberGenerator`) দ্বারা সরাসরি প্রদান করা হয়। মানটি এনক্রিপ্ট করা নিরাপত্তা না বাড়িয়ে জটিলতা যোগ করেছিল।

### মূল কারণের কোড (সংশোধনের আগে)

```csharp
// Nonce.cs — প্রতিটি কলে Key Vault থেকে একই IV আনা হত
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — একবার আনা এবং সমস্ত অনুরোধে পুনরায় ব্যবহার করা হত
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## কি সংশোধন করা হয়েছে

`Nonce.GenerateSecureNonce()` এখন সরাসরি `RandomNumberGenerator.Fill(byte[])` কল করে 16 বাইট ক্রিপ্টোগ্রাফিকভাবে র্যান্ডম ডেটা তৈরি করে, তারপর ফলাফল Base64-এনকোড করে:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* IV বা এনক্রিপশন কীয়ের জন্য কোনো Key Vault কল প্রয়োজন নেই বা করা হয় না।
* কোনো AES-GCM বা অন্য কোনো সাইফার জড়িত নেই।
* `Nonce` কনস্ট্রাক্টর আর `KeyVaultSecret` প্যারামিটার গ্রহণ করে না।

`NonceCatalogService.GetANonce`-এ একটি গৌণ বাগও সংশোধন করা হয়েছে: পদ্ধতিটি আগে একটি দুই-ধাপের check-then-lookup (`TryGetValue` তারপর indexer `[]`) ব্যবহার করত, যা পারমাণবিক নয় এবং যখন অন্য থ্রেড দুটি কলের মধ্যে কীটি সরিয়ে দেয় তখন `KeyNotFoundException` throw করতে পারত। সংশোধনটি একটি একক পারমাণবিক অপারেশনে মান পুনরুদ্ধার করতে `out` প্যারামিটার সহ `TryGetValue` ব্যবহার করে।

---

## কিভাবে এটি সংশোধিত রাখবেন

1. **নন্স জেনারেশনের জন্য কখনো Key Vault IV বা কী প্রবর্তন করবেন না।** Key Vault যদি অন্য সিক্রেটের জন্য ব্যবহার করা হয়, তা ঠিক আছে — কিন্তু নন্স জেনারেশন কখনো একটি স্থির IV-এর উপর নির্ভর করবে না।

2. **`GenerateSecureNonce`-কে কখনো AES-GCM বা CBC/CTR স্কিম দিয়ে প্রতিস্থাপন করবেন না** যা অনুরোধ জুড়ে একটি IV বা কাউন্টার পুনরায় ব্যবহার করে।

3. **নন্স কমপক্ষে 16 বাইট (128 বিট) রাখুন।** বাইটের দৈর্ঘ্য কমানো সংঘর্ষের সম্ভাবনা বাড়ায় এবং CSP-এর জন্য উপলব্ধ এন্ট্রপি কমায়।

4. **`RandomNumberGenerator.Fill`-কে `new Random()` বা অন্য কোনো non-CSPRNG-এ পরিবর্তন করবেন না।**

5. **`NonceCatalogService.GetANonce`-কে `out` প্যারামিটার সহ `TryGetValue` ব্যবহার করতে রাখুন।** দুই-ধাপের check-then-lookup প্যাটার্ন (`TryGetValue` + indexer) `ConcurrentDictionary` দিয়েও থ্রেড-নিরাপদ নয়।

### এই সংশোধন বলবৎকারী টেস্টসমূহ

| টেস্ট | কি ধরে |
|------|-----------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | কনস্ট্রাক্টরটি `KeyVaultSecret` IV + কী গ্রহণ করতে পরিবর্তিত হলে কম্পাইল করতে ব্যর্থ হয় |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | নন্স জেনারেশন ভেঙে গেলে বা non-Base64 মান রিটার্ন করলে ব্যর্থ হয় |
| `GenerateSecureNonce_Returns16ByteBase64` | বাইটের দৈর্ঘ্য 16-এর নিচে কমানো হলে ব্যর্থ হয় |
| `Nonce_SuccessiveGenerations_AreUnique` | IV পুনরায় ব্যবহারের কারণে একই নন্স বারবার তৈরি হলে ব্যর্থ হয় |
| `Nonce_HasSufficientEntropy` | এন্ট্রপি উৎস অ-র্যান্ডম হলে ব্যর্থ হয় |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | `ConcurrentDictionary`-কে `Dictionary`-তে পরিবর্তন করা হলে ব্যর্থ হয় |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | `GetANonce`-এ TOCTOU রেস পুনরায় প্রবর্তন করা হলে ব্যর্থ হয় |
