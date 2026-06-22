# Aratohu Tiwhikete PFX mō Azure Key Vault

## Rā: 2024-12-20

## Tirohanga Whānui

Ka whakamārama tēnei aratohu i te huarahi **tika** mō te penapena me te tiki tiwhikete PFX katoa (me te private key) i Azure Key Vault, i takea mai i ngā akoranga o te whakatinanatanga production.

---

## ⚠️ Ngā Hē Noa hei Karo

### ❌ HĒ: Penapena PFX hei Base64 Secret

```powershell
# DON'T DO THIS - It doesn't work!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**He aha i hē ai:**
1. **Rahinga here**: he 25 KB te rohe mō ngā secret; he nui ake te nuinga o ngā PFX
2. **Raru encoding**: ka taea e Base64 te whakaputa line-break/corruption
3. **Type mismatch**: mō ngā aho māmā ngā secret, ehara mō te raraunga cert pūmau
4. **Kāore he metadata cert**: ka ngaro te expiry, te subject, me ētahi atu

---

## ✅ TIKA: Whakamahia ngā API Tiwhikete Motuhake

### Tikanga 1: Import tika te Tiwhikete (Tūtohu)

Koinei te huarahi pai rawa atu, ā, koinei hoki te mea e pai ana i te codebase.

#### Tuku Tiwhikete (PowerShell)

```powershell
# Define variables
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# Convert password to SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Import certificate to Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Ngā painga:**
- ✅ Ka kawe i ngā tiwhikete rahi katoa
- ✅ Ka tiaki i te metadata katoa
- ✅ Ka waihanga aunoa i te secret version me te private key
- ✅ Ka tautoko i te certificate rotation
- ✅ Ka hono ki Azure RBAC me ngā access policy

#### Tiki Tiwhikete (C#)

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
        // Create credential
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // Initialize certificate client
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);

        // Get certificate (this gets public key and metadata)
        KeyVaultCertificateWithPolicy certificate =
            await certificateClient.GetCertificateAsync(certificateName);

        // Get the secret that contains the private key
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        // The secret value is the Base64-encoded PKCS12 (PFX) with private key
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        // Create X509Certificate2 with private key
        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Error loading PFX certificate from Key Vault");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error retrieving certificate");
        return null;
    }
}
```

---

### Tikanga 2: Whakamahia te Managed Identity (Production)

Mō production, whakamahia te **Managed Identity** hei utu mō ngā client secret.

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
        _logger.LogError(ex, "Error retrieving certificate with Managed Identity");
        return null;
    }
}
```

---

## 🧩 Whakatinanatanga i WebAppExperimental266

### Āhua o nāianei

**Wāhi:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Āhua:** ⚠️ Template implementation — me whakakapi ki te waehere production.

**Waehere o nāianei (Template):**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    _logger.LogWarning("GetCertificateFromKeyVault called - implement this method for production use");
    return await Task.FromResult<X509Certificate2?>(null);
}
```

### Whakahou e tūtohu ana

Whakakapia ki tētahi implementation production-ready e whakamahi ana i `CertificateClient` + `SecretClient` me `DefaultAzureCredential` i production.

> Tuhipoka: Me pupuri tonu ngā rautaki hopu hapa (`CryptographicException`, `RequestFailedException`, `Exception`) me ngā logs haumaru.

---

## 📦 Ngā Package NuGet e hiahiatia ana

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Tuhipoka:** Kua tāutahia kē ēnei ki te kaupapa WebAppExperimental266.

---

## ⚙️ Whirihoranga

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
# For client secret authentication
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# For Managed Identity (production)
# No secrets needed - identity is handled by Azure
```

---

## 🔐 Azure Key Vault Access Policies

### Whakaaetanga e hiahiatia ana

Mō te identity o te taupānga (Service Principal rānei Managed Identity):

**Certificate Permissions:**
- ✅ Get
- ✅ List

**Secret Permissions:**
- ✅ Get
- ✅ List

**He aha i hiahia ai i ngā mea e rua (Certificate + Secret)?**
- Ka tiki metadata mā ngā whakaaetanga Certificate
- Ka tiki private key mā ngā whakaaetanga Secret

### Tatūnga mā Azure Portal

1. Haere ki Key Vault → Access policies
2. Pāwhiri "Add Access Policy"
3. Kōwhiri Certificate permissions: Get, List
4. Kōwhiri Secret permissions: Get, List
5. Kōwhiri principal (tō app / managed identity)
6. Tiaki

### Tatūnga mā Azure CLI

```bash
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 Whakamātautau i te whakatinanatanga

### Unit Test tauira

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);

    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");

    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### Integration Test

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    var keyVaultUrl = TestConfiguration["AzureKeyVault:KeyVaultURL"];
    var certName = TestConfiguration["AzureKeyVault:CertificateName"];

    var operations = new AzureKeyVaultCertificateOperations(_logger);

    var cert = await operations.GetCertificateFromKeyVault(
        tenantId: TestConfiguration["AzureAd:TenantId"],
        clientId: TestConfiguration["AzureAd:ClientId"],
        keyVaultURL: keyVaultUrl,
        certificateName: certName,
        certPasswordName: "");

    Assert.NotNull(cert);
    Assert.True(cert.HasPrivateKey, "Certificate must have private key");
}
```

---

## 🔗 Whakamahi i te mTLS

```csharp
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();

    if (serverCertificate != null)
    {
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = serverCertificate;
            });
        });

        logger.LogInformation("mTLS enabled with Key Vault certificate");
    }
}
```

---

## 📊 Whakataurite: Secret vs Certificate storage

| Āhuatanga | Penapena hei Secret | Penapena hei Certificate |
|---------|----------------------|--------------------------|
| **Size Limit** | 25 KB | Unlimited |
| **Private Key** | ❌ ā-ringa | ✅ aunoa |
| **Metadata** | ❌ kāore | ✅ katoa |
| **Rotation** | ❌ ā-ringa | ✅ built-in |
| **Expiration** | ❌ ā-ringa | ✅ auto-tracked |
| **RBAC** | Basic | Certificate-specific |
| **Complexity** | Teitei | Iti |
| **Tohutohu** | ❌ Kaua e whakamahi | ✅ **Whakamahia tēnei** |

---

## 🔁 Certificate Rotation

### Aunoa

```powershell
az keyvault certificate set-policy `
    --vault-name your-keyvault `
    --name server-cert `
    --policy @policy.json
```

`policy.json`:
```json
{
  "lifetimeActions": [
    {
      "trigger": {
        "daysBeforeExpiry": 30
      },
      "action": {
        "actionType": "AutoRenew"
      }
    }
  ]
}
```

### Waehere taupānga

```csharp
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Mō tētahi version motuhake:

```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName,
    version: "specific-version-id");
```

---

## 🩺 Rapurongoā

### Hapa: "Certificate not found"

**Tirohia:**
1. Tika te ingoa tiwhikete
2. Kei te noho te cert i Key Vault
3. Kua whirihora tika ngā access policy

```bash
az keyvault certificate list --vault-name your-keyvault
```

### Hapa: "Access denied"

**Tirohia:**
1. Kei te tika ngā whakaaetanga a te Service Principal
2. Kua tukuna ngā whakaaetanga Certificate **me** Secret
3. Kua whakahohea te Managed Identity (mēnā e whakamahia ana)

```bash
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Hapa: "Certificate has no private key"

**Tirohia:**
1. Kei te whakamahi `.GetSecretAsync()` hoki, ehara i te `.GetCertificateAsync()` anake
2. I import te cert me te private key
3. Kei te tika te secret version

```csharp
// WRONG - No private key
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer;

// CORRECT - Has private key
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value);
```

### Hapa: "CryptographicException"

**Ngā take noa:**
1. Kua pirau te raraunga PFX
2. Hōputu cert hē
3. Kupuhipa hē (te nuinga kāore e hiahiatia mō KV)

```csharp
try
{
    var cert = new X509Certificate2(pfxBytes);
}
catch (CryptographicException ex)
{
    _logger.LogError("PFX data length: {Length}, First 20 chars: {Preview}",
        pfxBytes.Length,
        Convert.ToBase64String(pfxBytes.Take(20).ToArray()));
    throw;
}
```

---

## ✅ Rārangi Arowhai Hekenga

- [ ] Tāuta ngā NuGet package e hiahiatia ana
- [ ] Whakahōu `AzureKeyVaultCertificateOperations.cs` ki te waehere production
- [ ] Import cert ki Key Vault mā `Import-AzKeyVaultCertificate`
- [ ] Whirihora access policy (Certificate: Get/List, Secret: Get/List)
- [ ] Whakahōu `appsettings.json`
- [ ] Tatūnga Managed Identity (production) rānei client secret (dev)
- [ ] Whakamātautau i te tiki tiwhikete
- [ ] Whakau kei te noho te private key
- [ ] Whakamātautau mTLS me te cert i tikina
- [ ] Tatūnga policy certificate rotation
- [ ] Tuhia ngā tukanga whakahaere tiwhikete

---

## 🧾 Whakarāpopototanga

### ✅ MAHIA:
- Whakamahia `Import-AzKeyVaultCertificate` ki te tuku PFX
- Whakamahia `CertificateClient` + `SecretClient` ki te tiki
- Whakamahia Managed Identity i production
- Tukuna ngā whakaaetanga Certificate me Secret e rua
- Whakamātautau kia whai private key te cert

### ❌ KAUA E MAHI:
- Kaua e penapena PFX hei Base64 secret
- Kaua e whakahaere ā-ringa i te raraunga cert
- Kaua e whakamahi client secret i production
- Kaua e wareware ki ngā whakaaetanga Secret
- Kaua e whakangāwareware i te expiry o te cert

---

## 📚 Tohutoro

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Āhua:** ✅ Aratohu Kua Oti  
**Whakahoutanga Whakamutunga:** 2024-12-20  
**Putanga:** 1.0  
**Kaupapa:** WebAppExperimental266
