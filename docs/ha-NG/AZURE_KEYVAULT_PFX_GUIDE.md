# Jagorar Takardar Shaida ta PFX a Azure Key Vault

## Kwanan wata: 2024-12-20

## Bayani

Wannan jagorar tana bayyana **hanyar da ta dace** don adanawa da dawo da cikakkun takardun shaida na PFX (tare da private key) a Azure Key Vault, bisa darussan da aka koya daga aiwatarwa na ainihi.

---

## Kurakurai na gama-gari da ya kamata a guje musu

### ❌ BA DAIDAI BA: Ajiye PFX a matsayin Base64 Secret

```powershell
# KADA KA YI HAKA - ba ya aiki yadda ya kamata!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Dalilin gazawa:**
1. **Iyakar girma**: Key Vault secrets suna da iyakar 25 KB, kuma PFX yawanci ya fi haka
2. **Matsalar encoding**: Base64 na iya kawo line breaks ko lalacewar bayanai
3. **Rashin dacewar nau'i**: Secrets na rubutu ne, ba binary certificate data ba
4. **Babu metadata**: Ana rasa expiration, subject, da sauran bayanan takardar shaida

---

## ✅ DAIDAI: Yi amfani da API na takardar shaida

### Hanya ta 1: Shigo da takardar shaida kai tsaye (an fi ba da shawara)

Wannan ita ce hanya mafi kyau kuma ita ce wadda ta fi dacewa da wannan codebase.

#### Loda takardar shaida (PowerShell)

```powershell
# Saita canje-canje
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# Mayar da kalmar sirri zuwa SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Shigo da takardar shaida zuwa Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Fa'idodi:**
- ✅ Yana sarrafa takardu masu kowanne girma
- ✅ Yana kiyaye duk metadata
- ✅ Yana ƙirƙirar secret mai ɗauke da private key ta atomatik
- ✅ Yana tallafa wa juyawa (rotation) na takardar shaida
- ✅ Yana haɗuwa da Azure RBAC da access policies

#### Dawo da takardar shaida (C#)

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
        _logger.LogError(ex, "Kuskure wajen loda PFX daga Key Vault");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Kuskuren da ba a zata ba wajen dawo da takardar shaida");
        return null;
    }
}
```

---

### Hanya ta 2: Yi amfani da Managed Identity (production)

A samarwa, yi amfani da **Managed Identity** maimakon client secrets.

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
        _logger.LogError(ex, "Kuskure wajen dawo da takardar shaida ta Managed Identity");
        return null;
    }
}
```

---

## Aiwatarwa a WebAppExperimental266

### Matsayin aiwatarwa yanzu

**Wuri:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Matsayi:** samfurin template ne; yana buƙatar production code.

### Sabuntawar da ake ba da shawara

- Sauya template da cikakken aiwatarwa da ke amfani da `CertificateClient` + `SecretClient`
- Yi amfani da `DefaultAzureCredential` a production
- Tabbatar an kama `CryptographicException` da `Azure.RequestFailedException`
- Tabbatar certificate da aka dawo yana ɗauke da private key

---

## NuGet Packages da ake buƙata

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Lura:** an riga an saka su a aikin WebAppExperimental266.

---

## Saiti

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
# Don client secret auth
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Don Managed Identity (production)
# Babu buƙatar secret na musamman
```

---

## Izinin Azure Key Vault

### Izinin da ake buƙata

Ga app identity (Service Principal ko Managed Identity):

**Certificate permissions:**
- Get
- List

**Secret permissions:**
- Get
- List

**Me yasa duka biyu?**
- Certificate permissions don metadata
- Secret permissions don private key

---

## Gwaji

- Gwada dawo da takardar shaida daga Key Vault
- Tabbatar `HasPrivateKey == true`
- Yi integration test tare da albarkatun Azure na gaske

---

## Amfani da mTLS

Idan `EnableMtls` da `EnableKeyVault` suna kunne:
- Dawo da server certificate daga Key Vault
- Saita takardar shaida a Kestrel HTTPS defaults
- Tabbatar ana rubuta nasarar lodawa a logs

---

## Kwatanta: Ajiye a Secret vs Ajiye a Certificate

| Fasali | Ajiye a Secret | Ajiye a Certificate |
|---|---|---|
| Iyakar girma | 25 KB | Babba sosai |
| Private key | Kulawar hannu | Ta atomatik |
| Metadata | Babu | Cikakke |
| Rotation | Hanyar hannu | Built-in |
| Shawara | Kada a yi | **A yi wannan** |

---

## Rotation na takardar shaida

Azure Key Vault certificates suna tallafa wa auto-rotation da policy.
Aikace-aikacen zai iya samun sabuwar sigar certificate ta atomatik idan ana kiran `GetCertificateAsync(certificateName)`.

---

## Warware matsala

### “Certificate not found”
- Duba sunan certificate
- Duba cewa yana cikin Key Vault
- Duba access policies

### “Access denied”
- Duba izinin Service Principal/Managed Identity
- Tabbatar an bada duka Certificate + Secret permissions

### “Certificate has no private key”
- Kada a tsaya ga `GetCertificateAsync()` kawai
- Yi amfani da `GetSecretAsync(certificate.SecretId.Name)`

### “CryptographicException”
- Duba lalacewar PFX data
- Duba format ɗin takardar shaida

---

## Checklist na hijira

- [ ] Shigar da NuGet packages
- [ ] Sabunta `AzureKeyVaultCertificateOperations.cs`
- [ ] Shigo da certificate da `Import-AzKeyVaultCertificate`
- [ ] Saita access policies (Certificate: Get/List, Secret: Get/List)
- [ ] Sabunta appsettings
- [ ] Saita Managed Identity ko client secret
- [ ] Gwada dawo da certificate
- [ ] Tabbatar private key yana nan
- [ ] Gwada mTLS da certificate da aka dawo
- [ ] Saita rotation policy

---

## Takaitawa

### ✅ A YI:
- Yi amfani da `Import-AzKeyVaultCertificate`
- Yi amfani da `CertificateClient` + `SecretClient`
- Yi amfani da Managed Identity a production
- Bada Certificate da Secret permissions duka
- Gwada cewa certificate na dauke da private key

### ❌ KADA A YI:
- Ajiye PFX a matsayin Base64 secret
- Sarrafa bayanan certificate da hannu ba tare da buƙata ba
- Amfani da client secrets a production idan akwai Managed Identity
- Manta da Secret permissions

---

## Manazarta

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Matsayi:** Jagora ya kammala
**An sabunta na ƙarshe:** 2024-12-20
**Siga:** 1.0
**Aiki:** WebAppExperimental266
