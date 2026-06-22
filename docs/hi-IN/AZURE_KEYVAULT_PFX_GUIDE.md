# Azure Key Vault PFX प्रमाणपत्र मार्गदर्शिका

## अवलोकन

यह मार्गदर्शिका Azure Key Vault में पूर्ण PFX प्रमाणपत्र (private key सहित) को सुरक्षित रूप से स्टोर और रिट्रीव करने का **सही तरीका** बताती है।

---

## सामान्य गलतियाँ जिनसे बचना चाहिए

### ❌ गलत: PFX को Base64 secret के रूप में स्टोर करना

```powershell
# ऐसा न करें
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**क्यों विफल होता है:**
1. Key Vault secret का आकार सीमा (25 KB) PFX से छोटी हो सकती है
2. Base64 एन्कोडिंग में corruption/line break जोखिम
3. Secret साधारण string के लिए है, binary cert data के लिए नहीं
4. Cert metadata (expiry/subject आदि) खो जाती है

---

## ✅ सही: Certificate-विशिष्ट APIs का उपयोग करें

### तरीका 1: प्रमाणपत्र को सीधे Import करें (अनुशंसित)

#### Upload (PowerShell)

```powershell
$v = "your-keyvault-name"
$n = "server-cert"
$pfx = "C:\path\to\certificate.pfx"
$pwd = ConvertTo-SecureString "your-pfx-password" -AsPlainText -Force

Import-AzKeyVaultCertificate -VaultName $v -Name $n -FilePath $pfx -Password $pwd
```

**लाभ:**
- बड़े प्रमाणपत्रों के लिए बेहतर
- metadata सुरक्षित रहती है
- private key secret version स्वतः बनता है
- rotation-friendly

#### Retrieve (C#)

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public async Task<X509Certificate2?> GetCertificateFromKeyVaultAsync(
    string tenantId,
    string clientId,
    string clientSecret,
    string keyVaultUrl,
    string certificateName)
{
    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
    var certificate = await certificateClient.GetCertificateAsync(certificateName);

    var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
    var secret = await secretClient.GetSecretAsync(certificate.Value.SecretId.Name);

    byte[] pfxBytes = Convert.FromBase64String(secret.Value.Value);
    return new X509Certificate2(
        pfxBytes,
        (string?)null,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
}
```

### तरीका 2: Managed Identity (Production)

प्रोडक्शन में client secret के बजाय `DefaultAzureCredential`/Managed Identity का उपयोग करें।

---

## WebAppExperimental266 में कार्यान्वयन

- स्थान: `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`
- वर्तमान स्थिति: टेम्पलेट/स्टब आधारित; production-grade implementation की आवश्यकता

---

## आवश्यक NuGet पैकेज

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

---

## कॉन्फ़िगरेशन

### appsettings.json

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "{{USE_USER_SECRETS}}",
    "KeyVaultPassName": "server-cert"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "ClientCertificateName": "client-cert"
  }
}
```

### User Secrets

```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"
```

---

## Azure Key Vault अनुमतियाँ (Access Policies)

ऐप identity (Service Principal या Managed Identity) को:

- Certificate: `Get`, `List`
- Secret: `Get`, `List`

दोनों आवश्यक हैं: metadata cert API से मिलती है, private key secret API से।

---

## परीक्षण

- यूनिट टेस्ट: प्रमाणपत्र null न हो, private key मौजूद हो
- इंटीग्रेशन टेस्ट: वास्तविक Key Vault से प्रमाणपत्र लोड सत्यापित करें

---

## Secret बनाम Certificate स्टोरेज तुलना

| फ़ीचर | Secret | Certificate |
|---|---|---|
| आकार सीमा | 25 KB | बेहतर/उपयुक्त |
| Private key handling | मैनुअल | स्वचालित |
| Metadata | सीमित | पूर्ण |
| Rotation | मैनुअल | बिल्ट-इन |
| अनुशंसा | ❌ | ✅ |

---

## Troubleshooting

### "Certificate not found"
- नाम सत्यापित करें
- Vault में प्रमाणपत्र मौजूद है या नहीं देखें
- access policy जांचें

```bash
az keyvault certificate list --vault-name your-keyvault
```

### "Access denied"
- identity permissions जांचें
- Certificate + Secret दोनों अनुमतियाँ दें

### "Certificate has no private key"
- केवल `GetCertificateAsync()` पर्याप्त नहीं
- `GetSecretAsync()` से private key payload लें

---

## माइग्रेशन चेकलिस्ट

- [ ] आवश्यक NuGet पैकेज सत्यापित करें
- [ ] `AzureKeyVaultCertificateOperations.cs` को production logic से अपडेट करें
- [ ] प्रमाणपत्र को `Import-AzKeyVaultCertificate` से import करें
- [ ] Certificate/Secret permissions कॉन्फ़िगर करें
- [ ] कॉन्फ़िगरेशन और identity सेटअप पूर्ण करें
- [ ] private key उपलब्धता सत्यापित करें
- [ ] mTLS एकीकरण परीक्षण करें

---

## सारांश

### करें
- `Import-AzKeyVaultCertificate` उपयोग करें
- `CertificateClient` + `SecretClient` के संयोजन से पढ़ें
- production में Managed Identity अपनाएँ
- private key उपस्थिति अवश्य सत्यापित करें

### न करें
- PFX को Base64 secret के रूप में स्टोर न करें
- production में client secrets पर निर्भर न रहें

---

**स्थिति:** मार्गदर्शिका पूर्ण  
**अंतिम अद्यतन:** 2024-12-20  
**संस्करण:** 1.0  
**परियोजना:** WebAppExperimental266
