# mTLS (মিউচুয়াল TLS) ক্লায়েন্ট সার্টিফিকেট প্রমাণীকরণ গাইড

## সংক্ষিপ্ত বিবরণ

এই প্রজেক্ট এখন **মিউচুয়াল TLS (mTLS)** প্রমাণীকরণ সমর্থন করে, যা সার্ভার এবং ক্লায়েন্ট উভয়কেই বৈধ সার্টিফিকেট উপস্থাপন করতে প্রয়োজন। এটি দ্বি-মুখী প্রমাণীকরণের মাধ্যমে উন্নত নিরাপত্তা প্রদান করে।

## mTLS কি?

mTLS স্ট্যান্ডার্ড TLS প্রসারিত করে প্রয়োজন করে:
1. **সার্ভার সার্টিফিকেট**: সার্ভার তার পরিচয় প্রমাণ করতে একটি সার্টিফিকেট উপস্থাপন করে (স্ট্যান্ডার্ড HTTPS)
2. **ক্লায়েন্ট সার্টিফিকেট**: ক্লায়েন্টও তার পরিচয় প্রমাণ করতে একটি সার্টিফিকেট উপস্থাপন করে (mTLS সংযোজন)

## কনফিগারেশন

### ১. ফিচার ফ্ল্যাগ

`appsettings.json`-এ mTLS সক্ষম করুন:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### ২. mTLS সেটিংস

`appsettings.json`-এ mTLS আচরণ কনফিগার করুন:

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "my-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

#### কনফিগারেশন বিকল্পসমূহ

| সেটিং | টাইপ | ডিফল্ট | বিবরণ |
|---------|------|---------|-------------|
| `RequireClientCertificate` | bool | `true` | সত্য হলে, ক্লায়েন্ট সার্টিফিকেট বাধ্যতামূলক |
| `AllowCertificateChains` | bool | `true` | চেইনড (CA-স্বাক্ষরিত) সার্টিফিকেট অনুমতি দিন |
| `AllowSelfSignedCertificates` | bool | `false` | স্ব-স্বাক্ষরিত সার্টিফিকেট অনুমতি দিন (শুধুমাত্র ডেভ) |
| `CheckCertificateRevocation` | bool | `false` | অনলাইন প্রত্যাহার পরীক্ষা করুন |
| `ClientCertificateName` | string | null | Azure Key Vault-এ সার্টিফিকেটের নাম |
| `ValidateClientCertificateIssuer` | bool | `true` | সার্টিফিকেট ইস্যুয়ার যাচাই করুন |

### ৩. সার্ভার সার্টিফিকেট (Azure Key Vault)

সার্ভার সার্টিফিকেট Azure Key Vault থেকে পুনরুদ্ধার করা হয়:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## সেটআপ নির্দেশাবলী

### পূর্বশর্তসমূহ

1. উপযুক্ত অনুমতি সহ Azure Key Vault
2. Azure Key Vault-এ সংরক্ষিত সার্ভার সার্টিফিকেট সিক্রেট হিসেবে (PFX ফরম্যাট)
3. ক্লায়েন্ট সার্টিফিকেট (তৈরি করা বা CA থেকে প্রাপ্ত)

### ধাপ ১: Key Vault-এ সার্ভার সার্টিফিকেট আপলোড করুন

```bash
# প্রয়োজনে সার্টিফিকেটকে PFX-এ রূপান্তর করুন
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Azure CLI ব্যবহার করে Key Vault-এ আপলোড করুন
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# পাসওয়ার্ড আলাদা সিক্রেট হিসেবে সংরক্ষণ করুন
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### ধাপ ২: ক্লায়েন্ট সার্টিফিকেট তৈরি করুন

#### বিকল্প ক: স্ব-স্বাক্ষরিত (শুধুমাত্র ডেভেলপমেন্ট)

```powershell
# ক্লায়েন্ট সার্টিফিকেট তৈরি করুন
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# PFX-এ এক্সপোর্ট করুন
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### বিকল্প খ: CA-স্বাক্ষরিত (প্রোডাকশন)

ক্লায়েন্ট সার্টিফিকেট পেতে আপনার সার্টিফিকেট অথরিটির সাথে কাজ করুন।

### ধাপ ৩: অ্যাপ্লিকেশন কনফিগার করুন

`appsettings.json` আপডেট করুন:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert",
    "KeyVaultPassName": "server-cert-password"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false
  }
}
```

### ধাপ ৪: ক্লায়েন্ট সার্টিফিকেট দিয়ে পরীক্ষা করুন

#### cURL ব্যবহার করে:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### PowerShell ব্যবহার করে:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### ব্রাউজার ব্যবহার করে:

1. ব্রাউজার সার্টিফিকেট স্টোরে ক্লায়েন্ট সার্টিফিকেট আমদানি করুন
2. আপনার অ্যাপ্লিকেশনে নেভিগেট করুন
3. ব্রাউজার ক্লায়েন্ট সার্টিফিকেট নির্বাচন করতে অনুরোধ করবে

## পরিবেশ-নির্দিষ্ট আচরণ

### ডেভেলপমেন্ট
- সার্ভার সার্টিফিকেট Key Vault থেকে লোড করা হয় (যদি পাওয়া যায়)
- ক্লায়েন্ট সার্টিফিকেট **ঐচ্ছিক** (`AllowCertificate` মোড)
- স্ব-স্বাক্ষরিত সার্টিফিকেট অনুমতি দেওয়া যেতে পারে

### প্রোডাকশন
- সার্ভার সার্টিফিকেট Key Vault থেকে লোড করা হয়
- ক্লায়েন্ট সার্টিফিকেট `EnableMtls = true` হলে **প্রয়োজনীয়**
- শুধুমাত্র চেইনড সার্টিফিকেট প্রস্তাবিত

## নিরাপত্তা সর্বোত্তম অনুশীলন

### ? করুন:
- প্রোডাকশনে CA-স্বাক্ষরিত সার্টিফিকেট ব্যবহার করুন
- Azure Key Vault-এ সার্টিফিকেট সংরক্ষণ করুন
- প্রোডাকশনে সার্টিফিকেট প্রত্যাহার পরীক্ষা সক্ষম করুন
- সার্টিফিকেট ইস্যুয়ার যাচাই করুন
- PFX ফাইলের জন্য শক্তিশালী পাসওয়ার্ড ব্যবহার করুন
- নিয়মিত সার্টিফিকেট পরিবর্তন করুন

### ? করবেন না:
- প্রোডাকশনে স্ব-স্বাক্ষরিত সার্টিফিকেট ব্যবহার করবেন না
- সোর্স কন্ট্রোলে সার্টিফিকেট কমিট করবেন না
- ব্যবহারকারীদের মধ্যে ক্লায়েন্ট সার্টিফিকেট ভাগ করবেন না
- প্রোডাকশনে সার্টিফিকেট যাচাই অক্ষম করবেন না

## সমস্যা সমাধান

### ত্রুটি: "No client certificate provided"

**কারণ**: ক্লায়েন্ট সার্টিফিকেট পাঠায়নি
**সমাধান**: 
- ক্লায়েন্ট সার্টিফিকেট ইনস্টল করা আছে কিনা যাচাই করুন
- `RequireClientCertificate` সেটিং পরীক্ষা করুন
- সার্টিফিকেট সিস্টেম দ্বারা বিশ্বাসযোগ্য কিনা নিশ্চিত করুন

### ত্রুটি: "Certificate chain validation failed"

**কারণ**: সার্টিফিকেট বিশ্বাসযোগ্য নয়
**সমাধান**:
- CA রুট সার্টিফিকেট ইনস্টল করুন
- পরীক্ষার জন্য `AllowSelfSignedCertificates = true` সেট করুন
- সার্টিফিকেটের মেয়াদ শেষ হয়নি কিনা যাচাই করুন

### ত্রুটি: "Server certificate not retrieved from Key Vault"

**কারণ**: Azure Key Vault অ্যাক্সেস সমস্যা
**সমাধান**:
- Key Vault অনুমতি যাচাই করুন
- Azure AD ক্রেডেনশিয়াল পরীক্ষা করুন
- ম্যানেজড আইডেন্টিটি কনফিগার করা আছে কিনা নিশ্চিত করুন

## লগিং

mTLS প্রমাণীকরণ ইভেন্টগুলি লগ করা হয়:

```
[Information] mTLS সক্ষম - ক্লায়েন্ট সার্টিফিকেট প্রয়োজন
[Information] CN=MyClient সার্টিফিকেটের জন্য mTLS প্রমাণীকরণ সফল হয়েছে
[Error] mTLS প্রমাণীকরণ ব্যর্থ হয়েছে: সার্টিফিকেট যাচাই ব্যর্থ হয়েছে
```

## বিদ্যমান প্রমাণীকরণের সাথে ইন্টিগ্রেশন

mTLS Azure AD প্রমাণীকরণের পাশাপাশি কাজ করে:

1. **ক্লায়েন্ট সার্টিফিকেট যাচাইকরণ** প্রথমে হয় (ট্রান্সপোর্ট স্তর)
2. **Azure AD প্রমাণীকরণ** পরবর্তীতে হয় (অ্যাপ্লিকেশন স্তর)

গভীরতায় প্রতিরক্ষা নিরাপত্তার জন্য উভয়ই একসাথে সক্ষম করা যেতে পারে।

## রেফারেন্সসমূহ

- [Microsoft ডকস: সার্টিফিকেট প্রমাণীকরণ](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault ইন্টিগ্রেশন](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## উদাহরণ কোড

বাস্তবায়ন নিম্নলিখিতগুলিতে পাওয়া যাবে:
- `Models/Settings/MtlsSettings.cs` - কনফিগারেশন মডেল
- `Models/Settings/FeatureFlags.cs` - ফিচার ফ্ল্যাগ
- `Extensions/ServiceCollectionExtensions.cs` - সার্ভিস নিবন্ধন
- `Program.cs` - অ্যাপ্লিকেশন স্টার্টআপ

## অতিরিক্ত রিসোর্সসমূহ

সার্টিফিকেট আপলোডের উদাহরণের জন্য `SupportingScripts/CertificateUploaderToAzureExample.ps1` দেখুন।
