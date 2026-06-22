# mTLS（Mutual TLS）用戶端憑證驗證指南

## 概覽

本專案支援 **雙向 TLS（mTLS）**。除伺服器憑證外，連線用戶端亦必須提供有效憑證，以達成雙向身份驗證。

## 什麼是 mTLS？

mTLS 在一般 TLS 之上再加入「用戶端憑證」驗證：
1. **伺服器憑證**：證明伺服器身份（一般 HTTPS）
2. **用戶端憑證**：證明用戶端身份（mTLS）

## 設定

### 1) 功能旗標

在 `appsettings.json` 啟用：

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2) mTLS 參數

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

| 設定 | 型別 | 預設值 | 說明 |
|---|---|---|---|
| `RequireClientCertificate` | bool | `true` | 是否必須提供用戶端憑證 |
| `AllowCertificateChains` | bool | `true` | 是否允許 CA 鏈式憑證 |
| `AllowSelfSignedCertificates` | bool | `false` | 是否允許自簽憑證（建議僅開發） |
| `CheckCertificateRevocation` | bool | `false` | 是否進行撤銷檢查 |
| `ClientCertificateName` | string | null | Key Vault 中的憑證名稱 |
| `ValidateClientCertificateIssuer` | bool | `true` | 是否驗證簽發者 |

### 3) 伺服器憑證（Azure Key Vault）

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## 安裝流程

### 前置條件
1. 已建立 Azure Key Vault 並授權
2. 伺服器 PFX 憑證已放入 Key Vault
3. 已準備用戶端憑證（自簽或 CA）

### 步驟 1：上傳伺服器憑證

```bash
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### 步驟 2：產生用戶端憑證

#### A. 自簽（開發）

```powershell
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### B. CA 簽發（生產）
請向正式憑證機構申請。

### 步驟 3：更新應用程式設定

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

### 步驟 4：測試

#### cURL
```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### PowerShell
```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### 瀏覽器
1. 匯入用戶端憑證
2. 開啟網站
3. 選擇要送出的憑證

## 環境行為

### 開發環境
- 伺服器憑證可由 Key Vault 讀取
- 用戶端憑證可設為非必要（`AllowCertificate`）
- 可允許自簽憑證

### 生產環境
- 伺服器憑證由 Key Vault 讀取
- `EnableMtls=true` 時應要求用戶端憑證
- 建議僅接受 CA 鏈式憑證

## 安全最佳實務

### 建議
- 生產環境使用 CA 簽發憑證
- 憑證儲存在 Azure Key Vault
- 生產環境啟用撤銷檢查
- 驗證憑證簽發者
- 定期輪替憑證

### 避免
- 生產環境使用自簽憑證
- 把憑證提交到版本控制
- 多人共用同一用戶端憑證
- 在生產關閉驗證

## 疑難排解

### 「No client certificate provided」
- 確認客戶端已安裝憑證
- 檢查 `RequireClientCertificate`
- 檢查系統信任鏈

### 「Certificate chain validation failed」
- 安裝根 CA
- 測試時可暫設 `AllowSelfSignedCertificates=true`
- 檢查憑證是否過期

### 「Server certificate not retrieved from Key Vault」
- 檢查 Key Vault 權限
- 檢查 Azure AD 憑證資訊
- 檢查 Managed Identity

## 記錄

mTLS 事件會寫入日誌，例如：

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## 與現有認證整合

mTLS 可與 Azure AD 同時啟用：
1. 先做用戶端憑證驗證（傳輸層）
2. 再做 Azure AD 認證（應用層）

## 參考

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## 實作檔案

- `Models/Settings/MtlsSettings.cs`
- `Models/Settings/FeatureFlags.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Program.cs`

## 其他資源

可參考：`SupportingScripts/CertificateUploaderToAzureExample.ps1`
