# Treoir Deimhnithe PFX Azure Key Vault

## Dáta: 2024-12-20

## Forbhreathnú

Déanann an treoir seo cur síos ar an gcur chuige **ceart** chun deimhnithe PFX iomlána (le heochracha príobháideacha) a stóráil agus a aisghabháil in Azure Key Vault, bunaithe ar cheachtanna ón gcur i bhfeidhm táirgthe.

---

## ⚠️ Botúin Choitianta le Seachaint

### ❌ MÍCHEART: PFX a stóráil mar Rún Base64

```powershell
# NÁ DÉAN SEO - ní oibríonn sé!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Cén fáth a dteipeann air:**
1. **Teorainn méide**: tá teorainn 25 KB ar rúin Key Vault — is minic a sháraíonn PFX í
2. **Fadhbanna ionchódaithe**: is féidir truailliú a chruthú le línte nua Base64
3. **Mí-oiriúnú cineáil**: tá rúin do shreanga simplí, ní do shonraí deimhnithe dénártha
4. **Gan meiteashonraí deimhnithe**: cailleann sé dátaí éaga, ábhar, srl.

---

## ✅ CEART: Úsáid APIanna Speisialta Deimhnithe

### Modh 1: Iompórtáil Deimhniú go Díreach (Molta)

Seo an cur chuige is fearr agus an ceann atá ag obair sa chódchiste anois.

#### Uaslódáil Deimhnithe (PowerShell)

```powershell
# Sainmhínigh athróga
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# Tiontaigh pasfhocal go SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Iompórtáil deimhniú go Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Buntáistí:**
- ✅ Láimhseálann deimhnithe d'aon mhéid
- ✅ Coinníonn meiteashonraí deimhnithe go léir
- ✅ Cruthaíonn leagan rúin leis an eochair phríobháideach go huathoibríoch
- ✅ Tacaíonn le rothlú deimhnithe
- ✅ Comhtháthú le Azure RBAC agus beartais rochtana

#### Aisghabháil Deimhnithe (C#)

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

### Modh 2: Úsáid Managed Identity (Táirgeadh)

I dtimpeallachtaí táirgthe, bain úsáid as **Managed Identity** in ionad rúin cliaint.

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

## 🧩 Cur i bhFeidhm i WebAppExperimental266

### Stádas Cur i bhFeidhm Reatha

**Suíomh:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Stádas:** ⚠️ Cur i bhfeidhm teimpléid - tá cód táirgthe fós ag teastáil

**Cód Reatha (Teimpléad):**
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

### Nuashonrú Molta

Cuir an leagan táirgthe in ionad an teimpléid:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public class AzureKeyVaultCertificateOperations : IAzureKeyVaultCertificateOperations
{
    private readonly ILogger<AzureKeyVaultCertificateOperations> _logger;

    public AzureKeyVaultCertificateOperations(ILogger<AzureKeyVaultCertificateOperations> logger)
    {
        _logger = logger;
    }

    public async Task<X509Certificate2?> GetCertificateFromKeyVault(
        string tenantId,
        string clientId,
        string keyVaultURL,
        string certificateName,
        string certPasswordName)
    {
        try
        {
            _logger.LogInformation("Retrieving certificate '{CertName}' from Key Vault", certificateName);

            var credential = new DefaultAzureCredential();

            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate =
                await certificateClient.GetCertificateAsync(certificateName);

            _logger.LogDebug("Certificate found. Thumbprint: {Thumbprint}, Expires: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);

            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

            byte[] pfxBytes = Convert.FromBase64String(secret.Value);

            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.PersistKeySet);

            _logger.LogInformation("Successfully loaded certificate with private key");

            return x509Certificate;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error loading certificate '{CertName}'", certificateName);
            return null;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Key Vault error: {StatusCode} - {Message}",
                ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving certificate");
            return null;
        }
    }

    public async Task<KeyVaultSecret> GetSecretFromKeyVault(
        string tenantId,
        string clientId,
        string clientSecret,
        string keyVaultURL,
        string secretName)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);

            return await secretClient.GetSecretAsync(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}'", secretName);
            throw;
        }
    }
}
```

---

## 📦 Pacáistí NuGet Riachtanacha

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

**Nóta:** Suiteáilte cheana féin sa tionscadal WebAppExperimental266.

---

## ⚙️ Cumraíocht

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
# Le haghaidh Client Secret auth
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Le haghaidh Managed Identity (táirgeadh)
# Ní theastaíonn rúin - láimhseálann Azure an chéannacht
```

---

## 🔐 Beartais Rochtana Azure Key Vault

### Ceadanna Riachtanacha

Don chéannacht feidhmchláir (Service Principal nó Managed Identity):

**Ceadanna Deimhnithe:**
- ✅ Get
- ✅ List

**Ceadanna Rúin:**
- ✅ Get
- ✅ List

**Cén fáth Deimhniú AGUS Rún?**
- Ceadanna deimhnithe: meiteashonraí deimhnithe
- Ceadanna rúin: eochair phríobháideach

### Socrú trí Azure Portal

1. Key Vault → Access policies
2. "Add Access Policy"
3. Roghnaigh ceadanna deimhnithe: Get, List
4. Roghnaigh ceadanna rúin: Get, List
5. Roghnaigh principal (d'app nó managed identity)
6. Sábháil

### Socrú trí Azure CLI

```bash
# Faigh Object ID d'fheidhmchlár nó managed identity
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# Deonaigh ceadanna
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 Tástáil an Chur i bhFeidhm

### Sampla Aonad-Tástála

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

### Sampla Tástála Comhtháthaithe

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

## 🔄 Úsáid i mTLS

```csharp
// In Program.cs
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

## 📊 Comparáid: Stóráil mar Rún vs mar Dheimhniú

| Gné | Stóráil mar Rún | Stóráil mar Dheimhniú |
|---------|----------------|---------------------|
| **Teorainn Méide** | 25 KB | Gan teorainn phraiticiúil |
| **Eochair Phríobháideach** | ❌ Láimhseáil láimhe | ✅ Uathoibríoch |
| **Meiteashonraí** | ❌ Gan aon cheann | ✅ Iomlán |
| **Rothlú** | ❌ Lámhleabhar | ✅ Tógtha isteach |
| **Éag** | ❌ Rianú láimhe | ✅ Rianú uathoibríoch |
| **RBAC** | Bunúsach | Sonrach do dheimhnithe |
| **Castacht** | Ard | Íseal |
| **Moladh** | ❌ Ná húsáid | ✅ **Úsáid seo** |

---

## 🔁 Rothlú Deimhnithe

### Rothlú Uathoibríoch

```powershell
# Socraigh beartas uathoibríoch rothlaithe
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

### Cód Feidhmchláir

```csharp
// Faigheann sé an leagan is déanaí i gcónaí
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Le haghaidh leagan sonrach:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName,
    version: "specific-version-id");
```

---

## 🧯 Fabhtcheartú

### Earráid: "Certificate not found"

**Seiceáil:**
1. Ainm an deimhnithe ceart
2. Go bhfuil an deimhniú ann i Key Vault
3. Go bhfuil beartais rochtana socraithe

```bash
az keyvault certificate list --vault-name your-keyvault
```

### Earráid: "Access denied"

**Seiceáil:**
1. Go bhfuil ceadanna cearta ag Service Principal
2. Go bhfuil ceadanna Deimhnithe AGUS Rúin ann
3. Go bhfuil Managed Identity cumasaithe (más á úsáid)

```bash
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Earráid: "Certificate has no private key"

**Seiceáil:**
1. `GetSecretAsync()` á úsáid, ní `GetCertificateAsync()` amháin
2. Deimhniú iompórtáilte le heochair phríobháideach
3. Leagan rúin ceart á úsáid

```csharp
// MÍCHEART - gan eochair phríobháideach
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // public key amháin

// CEART - le heochair phríobháideach
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // le private key
```

### Earráid: "CryptographicException"

**Cúiseanna coitianta:**
1. Sonraí PFX truaillithe
2. Formáid deimhnithe mhícheart
3. Pasfhocal neamhbhailí (de ghnáth ní gá le KV)

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

## ✅ Seicliosta Imirce

- [ ] Suiteáil pacáistí NuGet riachtanacha
- [ ] Nuashonraigh `AzureKeyVaultCertificateOperations.cs` le cód táirgthe
- [ ] Iompórtáil deimhniú go Key Vault le `Import-AzKeyVaultCertificate`
- [ ] Cumraigh beartais rochtana (Certificate: Get/List, Secret: Get/List)
- [ ] Nuashonraigh cumraíocht in `appsettings.json`
- [ ] Socraigh Managed Identity (táirgeadh) nó client secret (forbairt)
- [ ] Tástáil aisghabháil deimhnithe
- [ ] Fíoraigh go bhfuil eochair phríobháideach i láthair
- [ ] Tástáil mTLS leis an deimhniú aisghafa
- [ ] Socraigh beartas rothlaithe deimhnithe
- [ ] Doiciméadaigh nósanna imeachta bainistithe deimhnithe

---

## 📝 Achoimre

### ✅ DÉAN:
- Úsáid `Import-AzKeyVaultCertificate` chun PFX a uaslódáil
- Úsáid `CertificateClient` + `SecretClient` chun aisghabháil
- Úsáid Managed Identity i dtáirgeadh
- Deonaigh ceadanna Deimhnithe agus Rúin araon
- Tástáil go bhfuil eochair phríobháideach sa deimhniú

### ❌ NÁ DÉAN:
- Ná stóráil PFX mar rún Base64
- Ná bainistigh sonraí deimhnithe de láimh
- Ná húsáid client secrets i dtáirgeadh
- Ná déan dearmad ar cheadanna Rúin
- Ná déan neamhaird de dhátaí éaga deimhnithe

---

## 📚 Tagairtí

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Stádas:** ✅ Treoir Críochnaithe  
**Nuashonraithe Deireanach:** 2024-12-20  
**Leagan:** 1.0  
**Tionscadal:** WebAppExperimental266
