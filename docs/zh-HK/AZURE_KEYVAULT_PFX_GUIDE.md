# Azure Key Vault PFX 憑證指南

## 日期：2024-12-20

## 概覽

本指南總結在生產實作中得到的經驗，說明如何在 Azure Key Vault **正確儲存與擷取完整 PFX 憑證（包含私鑰）**。

---

## 常見錯誤（請避免）

### 錯誤做法：把 PFX 當成 Base64 Secret 儲存

```powershell
# 不建議：實務上容易失敗
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**原因：**
1. **大小限制**：Key Vault Secret 約有 25 KB 限制，PFX 常超過。
2. **編碼風險**：Base64 轉換可能引入換行或資料損毀。
3. **型別不對**：Secret 適合字串，不適合完整憑證二進位資料。
4. **中繼資料遺失**：有效期限、主體等憑證資訊不完整。

---

## 正確做法：使用憑證專用 API

### 方法一：直接匯入憑證（建議）

這是目前最穩定、最建議的做法。

#### 上傳憑證（PowerShell）

```powershell
# 變數
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\path\to\your\certificate.pfx"
$plainPassword = "your-pfx-password"

# 轉為 SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# 匯入 Key Vault 憑證
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**優點：**
- 可處理大型憑證
- 保留完整憑證中繼資料
- 會自動建立含私鑰的 Secret 版本
- 支援憑證輪替
- 可配合 Azure RBAC 與存取原則

#### 擷取憑證（C#）

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

### 方法二：Managed Identity（生產環境）

生產環境建議使用 **Managed Identity**，避免保存 client secret。

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

## WebAppExperimental266 內的實作狀態

**位置：** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`  
**狀態：** 範本（template）實作，仍需補上正式生產版邏輯。

目前方法會記錄警告並回傳 `null`，建議替換成可用於生產環境的實作（參考上方範例）。

---

## 必要 NuGet 套件

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

> 註：本專案已包含這些套件。

---

## 設定範例

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
# Client secret 驗證
 dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Managed Identity（生產）
# 不需設定 secret
```

---

## Azure Key Vault 權限

應用程式身分（Service Principal / Managed Identity）需至少具備：

- **Certificate 權限**：`Get`, `List`
- **Secret 權限**：`Get`, `List`

**為什麼兩者都要？**
- Certificate API 取憑證中繼資料
- Secret API 取私鑰內容（PFX）

### Azure CLI 設定

```bash
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 與 mTLS 整合

在 `EnableMtls` 與 `EnableKeyVault` 皆啟用時，啟動階段從 Key Vault 讀取伺服器憑證，並注入 Kestrel HTTPS 設定。

---

## Secret vs Certificate 儲存方式比較

| 項目 | 以 Secret 儲存 | 以 Certificate 儲存 |
|---|---|---|
| 大小限制 | 約 25 KB | 實務上更適合憑證管理 |
| 私鑰處理 | 需自行處理 | 自動流程較完整 |
| 中繼資料 | 幾乎沒有 | 完整 |
| 輪替 | 手動 | 內建機制較佳 |
| 複雜度 | 高 | 低 |
| 建議 | 不建議 | **建議** |

---

## 輪替（Rotation）

可透過 Key Vault 憑證政策設定自動續期，例如到期前 30 天自動更新。

---

## 疑難排解

### 錯誤：「Certificate not found」
- 檢查憑證名稱是否正確
- 檢查憑證是否存在 Key Vault
- 檢查存取原則是否已授權

### 錯誤：「Access denied」
- 檢查身分是否有 Certificate + Secret 權限
- 檢查 Managed Identity 是否啟用

### 錯誤：「Certificate has no private key」
- 只呼叫 `GetCertificateAsync()` 只會拿到公鑰
- 必須再用 `GetSecretAsync()` 取得含私鑰的 PFX

### 錯誤：「CryptographicException」
- PFX 資料損毀
- 憑證格式不正確
- 匯入方式不正確

---

## 遷移檢查清單

- [ ] 安裝必要 NuGet 套件
- [ ] 更新 `AzureKeyVaultCertificateOperations.cs` 為正式實作
- [ ] 使用 `Import-AzKeyVaultCertificate` 匯入憑證
- [ ] 設定 Certificate/Secret 存取權限
- [ ] 更新 `appsettings.json`
- [ ] 生產環境改用 Managed Identity
- [ ] 驗證可成功取得憑證與私鑰
- [ ] 驗證 mTLS 正常
- [ ] 設定憑證輪替政策

---

## 總結

### 建議做法
- 使用 `Import-AzKeyVaultCertificate` 上傳 PFX
- 使用 `CertificateClient` + `SecretClient` 讀取
- 生產環境使用 Managed Identity
- 同時授權 Certificate 與 Secret 權限

### 不建議做法
- 不要把 PFX 直接當 Base64 Secret 存
- 不要手動拼裝憑證資料流程
- 不要在生產環境依賴長期 client secret

---

## 參考

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**狀態：** 指南完成  
**最後更新：** 2024-12-20  
**版本：** 1.0  
**專案：** WebAppExperimental266
