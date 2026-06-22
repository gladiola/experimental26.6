# የAzure Key Vault PFX ሰርተፊኬት መመሪያ

**ቀን:** 2024-12-20

## አጠቃላይ

ይህ መመሪያ ሙሉ PFX ሰርተፊኬት (private key ጨምሮ) በAzure Key Vault ውስጥ በትክክል እንዴት እንደሚከማች እና እንደሚመለስ ያብራራል።

---

## ሊቀርቡ የሚገቡ የተለመዱ ስህተቶች

### ❌ የተሳሳተ: PFX እንደ Base64 Secret ማከማቸት

```powershell
# ይህን አታድርጉ
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**ለምን ይወድቃል:**
1. **መጠን ገደብ:** Key Vault secret 25KB ገደብ አለው
2. **encoding ችግኝ:** Base64 ላይ line breaks/ማበላሸት ሊኖር ይችላል
3. **የውሂብ አይነት ልዩነት:** secret ጽሑፍ ነው፣ cert ግን binary ነው
4. **metadata ጉድለት:** expiration/subject ያሉ መረጃዎች ይጠፋሉ

---

## ✅ ትክክለኛ መንገድ: Certificate API ይጠቀሙ

### መንገድ 1: certificate በቀጥታ ማስገባት (የሚመከር)

#### certificate መጫን (PowerShell)

```powershell
# ቅንብሮች
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**ጥቅሞች:**
- ✅ ትልቅ cert ፋይሎችን ይደግፋል
- ✅ metadata ሙሉ በሙሉ ይጠብቃል
- ✅ private key ያለው secret በራስ-ሰር ይፈጥራል
- ✅ certificate rotation ይደግፋል
- ✅ Azure RBAC እና access policies ጋር ይሰራል

#### certificate መመለስ (C#)

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
    try
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        KeyVaultCertificateWithPolicy certificate =
            await certificateClient.GetCertificateAsync(certificateName);

        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "PFX ከKey Vault ሲጫን የክሪፕቶ ስህተት");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "certificate ሲመለስ ያልተጠበቀ ስህተት");
        return null;
    }
}
```

---

### መንገድ 2: Managed Identity መጠቀም (production)

production ላይ client secret ከመጠቀም ይልቅ **Managed Identity** ይጠቀሙ።

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        var credential = new DefaultAzureCredential();

        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        var certificate = await certificateClient.GetCertificateAsync(certificateName);

        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        var secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Managed Identity በመጠቀም certificate ሲመለስ ስህተት");
        return null;
    }
}
```

---

## በWebAppExperimental266 ውስጥ መተግበር

### የአሁኑ ሁኔታ

**ቦታ:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`  
**ሁኔታ:** template ነው፣ production ኮድ ይፈልጋል

### የሚመከሩ ማሻሻያዎች

- template ን `CertificateClient` + `SecretClient` በመጠቀም በሙሉ implementation ተካ
- production ላይ `DefaultAzureCredential` ተጠቀም
- `CryptographicException` እና `Azure.RequestFailedException` ያዝ
- የተመለሰው cert private key እንዳለው አረጋግጥ

---

## የሚያስፈልጉ NuGet packages

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**ማስታወሻ:** በWebAppExperimental266 ውስጥ አስቀድሞ ተጨምረዋል።

---

## ቅንብር

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
# client secret auth
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Managed Identity (production)
# ተጨማሪ secret አያስፈልግም
```

---

## የAzure Key Vault ፈቃዶች

### የሚያስፈልጉ ፈቃዶች

ለapp identity (Service Principal ወይም Managed Identity):

**Certificate permissions:**
- Get
- List

**Secret permissions:**
- Get
- List

**ለምን ሁለቱም?**
- Certificate permissions ለmetadata
- Secret permissions ለprivate key

---

## ሙከራ

- certificate ከKey Vault ይመለስ እንደሚችል ሞክር
- `HasPrivateKey == true` አረጋግጥ
- ከእውነተኛ Azure ሀብቶች ጋር integration test አድርግ

---

## ከmTLS ጋር አቀናብር

`EnableMtls` እና `EnableKeyVault` ሲበሩ:
- server cert ከKey Vault ይመለሳል
- cert በKestrel HTTPS defaults ይተገበራል
- የመጫን ስኬት በlogs ይመዘገባል

---

## ንፅፅር: በSecret ማከማቸት vs በCertificate ማከማቸት

| ባህሪ | Secret | Certificate |
|---|---|---|
| መጠን ገደብ | 25 KB | በጣም ከፍተኛ |
| Private key | በእጅ አስተዳደር | በራስ-ሰር |
| Metadata | የለም | ሙሉ |
| Rotation | በእጅ | built-in |
| ምክር | አትጠቀሙ | **ይህን ይጠቀሙ** |

---

## Certificate rotation

Azure Key Vault certificates auto-rotation እና policy ይደግፋሉ።
`GetCertificateAsync(certificateName)` በሚጠራ ጊዜ አዲሱን version በራስ-ሰር ማግኘት ይቻላል።

---

## ችግኝ ፍቺ

### “Certificate not found”
- የcertificate ስም ይፈትሹ
- cert በKey Vault ውስጥ እንዳለ ያረጋግጡ
- access policies ይፈትሹ

### “Access denied”
- Service Principal/Managed Identity permissions ይፈትሹ
- Certificate + Secret permissions ሁለቱም እንደተሰጡ ያረጋግጡ

### “Certificate has no private key”
- `GetCertificateAsync()` ብቻ አትጠቀሙ
- `GetSecretAsync(certificate.SecretId.Name)` ይጠቀሙ

### “CryptographicException”
- PFX data እንዳልተበላሸ ያረጋግጡ
- certificate format ይፈትሹ

---

## የሽግግር checklist

- [ ] NuGet packages ያረጋግጡ
- [ ] `AzureKeyVaultCertificateOperations.cs` ያዘምኑ
- [ ] certificate በ`Import-AzKeyVaultCertificate` ያስገቡ
- [ ] access policies ያቀናብሩ (Certificate: Get/List, Secret: Get/List)
- [ ] appsettings ያዘምኑ
- [ ] Managed Identity ወይም client secret ያቀናብሩ
- [ ] certificate retrieval ይሞክሩ
- [ ] private key እንዳለ ያረጋግጡ
- [ ] retrieved cert ጋር mTLS ይሞክሩ
- [ ] rotation policy ያስቀምጡ

---

## ማጠቃለያ

### ✅ ያድርጉ
- `Import-AzKeyVaultCertificate` ይጠቀሙ
- `CertificateClient` + `SecretClient` ይጠቀሙ
- production ላይ Managed Identity ይጠቀሙ
- Certificate + Secret permissions ሁለቱንም ይስጡ
- cert ውስጥ private key እንዳለ ይፈትሹ

### ❌ አታድርጉ
- PFXን እንደ Base64 secret ማከማቸት
- certificate data አላስፈላጊ በእጅ መስራት
- Managed Identity ሲኖር production ላይ client secrets መጠቀም
- Secret permissions መርሳት

---

## ማጣቀሻዎች

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**ሁኔታ:** መመሪያው ተጠናቋል  
**መጨረሻ ዝማኔ:** 2024-12-20  
**ስሪት:** 1.0  
**ፕሮጀክት:** WebAppExperimental266
