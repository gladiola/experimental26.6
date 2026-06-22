# দ্রুত রেফারেন্স কার্ড - Razor Pages টেমপ্লেট

## ?? শুরু করুন (৫ মিনিট)

```powershell
# ১. সেটআপ স্ক্রিপ্ট চালান
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# ২. বিল্ড ও রান করুন
dotnet build
dotnet run
```

## ?? কনফিগারেশন ফাইলসমূহ

| ফাইল | উদ্দেশ্য | কমিটেড? |
|------|---------|------------|
| `appsettings.template.json` | প্লেসহোল্ডার সহ টেমপ্লেট | ? হ্যাঁ |
| `appsettings.json` | আপনার প্রকৃত কনফিগ | ? না (git-ignored) |
| ইউজার সিক্রেটস | সংবেদনশীল মান | ? না (শুধুমাত্র স্থানীয়) |

## ?? ফিচার ফ্ল্যাগ (দ্রুত সক্ষম/অক্ষম)

`appsettings.json` → `FeatureFlags` বিভাগ সম্পাদনা করুন:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // ?? প্রমাণীকরণের জন্য চালু করুন
  "EnableNonceServices": false,  // ??? CSP-এর জন্য চালু করুন
  "EnableCosmosDb": false,       // ?? ডেটাবেসের জন্য চালু করুন
  "EnableBlobStorage": false     // ?? ফাইলের জন্য চালু করুন
}
```

## ?? ইউজার সিক্রেটস কমান্ড

```powershell
# শুরু করুন
dotnet user-secrets init

# একটি সিক্রেট সেট করুন
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# সমস্ত সিক্রেট তালিকা করুন
dotnet user-secrets list

# একটি সিক্রেট মুছুন
dotnet user-secrets remove "AzureAd:ClientSecret"

# সমস্ত সিক্রেট পরিষ্কার করুন
dotnet user-secrets clear
```

## ?? ফিচার অনুযায়ী প্রয়োজনীয় সিক্রেটস

### Azure AD প্রমাণীকরণ
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# প্রথমে তৈরি করুন: .\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
dotnet user-secrets set "CosmosDb:AccountKey" "your-account-key"
```

### ব্লব স্টোরেজ
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## ?? দরকারী স্ক্রিপ্টসমূহ

| স্ক্রিপ্ট | উদ্দেশ্য | ব্যবহার |
|--------|---------|-------|
| `SetupFromTemplate.ps1` | প্রাথমিক সেটআপ | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | নেমস্পেস পরিবর্তন | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | কী তৈরি করুন | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | CSP হ্যাশ গণনা করুন | `.\HashInlineScriptPowerShell.ps1` |

## ??? ডেভেলপমেন্ট ফেজসমূহ

### ফেজ ১: মৌলিক (৫ মিনিট সেটআপ)
- ? সেশন
- ? স্থানীয়করণ
- ? নিরাপত্তা হেডার
- ? কোনো প্রমাণীকরণ নেই
- ? কোনো ডেটাবেস নেই

**কনফিগ**: `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders` ছাড়া সমস্ত ফ্ল্যাগ `false`

### ফেজ ২: + প্রমাণীকরণ (৩০ মিনিট সেটআপ)
- ? ফেজ ১ ফিচার
- ? Azure AD
- ? অনুমোদন
- ? CSP + নন্স
- ? কোনো ডেটাবেস নেই

**কনফিগ**: `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP` সক্ষম করুন

**প্রয়োজনীয়**:
- Azure AD অ্যাপ রেজিস্ট্রেশন
- তৈরি এনক্রিপশন কী

### ফেজ ৩: + Azure সার্ভিস (১-২ ঘণ্টা সেটআপ)
- ? ফেজ ২ ফিচার
- ? Cosmos DB
- ? ব্লব স্টোরেজ
- ? Key Vault

**কনফিগ**: `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault` সক্ষম করুন

**প্রয়োজনীয়**:
- Azure রিসোর্স তৈরি
- ইউজার সিক্রেটসে সংযোগ স্ট্রিং

## ?? দ্রুত সমস্যা সমাধান

### বিল্ড ত্রুটি
```powershell
# পরিষ্কার করুন এবং পুনরায় বিল্ড করুন
dotnet clean
dotnet build

# মিসিং প্যাকেজ পরীক্ষা করুন
dotnet restore
```

### "Configuration not found"
```powershell
# ফাইল বিদ্যমান কিনা যাচাই করুন
Test-Path appsettings.json

# যদি মিসিং হয়, টেমপ্লেট থেকে কপি করুন
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
# সিক্রেট তালিকা করুন
dotnet user-secrets list

# সেটআপ পুনরায় চালান
.\SupportingScripts\SetupFromTemplate.ps1
```

### অথ লুপ / ৪০১ ত্রুটি
1. Azure AD রিডাইরেক্ট URI মিলছে কিনা পরীক্ষা করুন
2. appsettings.json-এ `EnableAzureAd: true` যাচাই করুন
3. ইউজার সিক্রেটসে ক্লায়েন্ট সিক্রেট পরীক্ষা করুন
4. ব্রাউজার কুকি পরিষ্কার করুন

### CSP লঙ্ঘন
1. `EnableNonceServices: true` যাচাই করুন
2. এনক্রিপশন কী সেট করা আছে কিনা পরীক্ষা করুন
3. CSP ত্রুটির জন্য ব্রাউজার কনসোল পর্যালোচনা করুন
4. পরীক্ষার জন্য সাময়িকভাবে CSP অক্ষম করুন: `EnableCSP: false`

## ?? ডকুমেন্টেশন

- **পূর্ণ ডকস**: `TEMPLATE_README.md`
- **কনফিগারেশন**: `appsettings.template.json`
- **নেমস্পেস**: `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"` চালান

## ?? নিরাপত্তা চেকলিস্ট

প্রোডাকশনে ডিপ্লয় করার আগে:

- [ ] Azure Key Vault বা ইউজার সিক্রেটসে সমস্ত সিক্রেট
- [ ] `appsettings.json` গিট-ইগনোর করা আছে
- [ ] `.gitignore`-এ টেমপ্লেট-নির্দিষ্ট ইগনোর অন্তর্ভুক্ত
- [ ] নিরাপত্তা হেডার সক্ষম
- [ ] নন্স সহ CSP কনফিগার করা
- [ ] HTTPS প্রয়োগ করা
- [ ] সুরক্ষিত পেজের জন্য প্রমাণীকরণ সক্ষম
- [ ] ডিফল্ট থেকে সিক্রেট পরিবর্তন করা

## ?? টিপস

- **সহজ দিয়ে শুরু করুন**: ফেজ ১ দিয়ে শুরু করুন, ধীরে ধীরে ফিচার যোগ করুন
- **WhatIf ব্যবহার করুন**: প্রয়োগ করার আগে `-WhatIf` দিয়ে স্ক্রিপ্ট পরীক্ষা করুন
- **লগ পরীক্ষা করুন**: সমস্যা সমাধানের জন্য `Logging:LogLevel`-এ `"Default": "Debug"` সক্ষম করুন
- **সিক্রেট যাচাই করুন**: কী কনফিগার করা আছে দেখতে `dotnet user-secrets list` চালান
- **পরিষ্কার বিল্ড**: অদ্ভুত ত্রুটি হলে `dotnet clean && dotnet build` চেষ্টা করুন

## ?? সাহায্য

1. `TEMPLATE_README.md` পড়ুন
2. `appsettings.template.json` মন্তব্য পরীক্ষা করুন
3. `dotnet user-secrets list` চালান
4. ডিবাগ লগিং সক্ষম করুন
5. রিসোর্সের স্ট্যাটাসের জন্য Azure পোর্টাল পরীক্ষা করুন

---

**টেমপ্লেট ভার্সন**: 1.0  
**ASP.NET Core**: 9.0  
**সর্বশেষ আপডেট**: 2024-12-20
