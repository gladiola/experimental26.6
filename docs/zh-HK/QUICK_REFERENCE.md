# 快速參考卡（Razor Pages 範本）

## 5 分鐘快速開始

```powershell
# 1. 執行初始化腳本
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Build + Run
dotnet build
dotnet run
```

## 設定檔一覽

| 檔案 | 用途 | 會提交？ |
|---|---|---|
| `appsettings.template.json` | 範本（含占位符） | 會 |
| `appsettings.json` | 實際環境設定 | 不會（git ignore） |
| User Secrets | 敏感資料 | 不會（本機） |

## 功能旗標（快速啟用/停用）

在 `appsettings.json` → `FeatureFlags`：

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## User Secrets 常用命令

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## 各功能所需 Secrets

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce / CSP
```powershell
# 先執行 .\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
dotnet user-secrets set "CosmosDb:AccountKey" "your-account-key"
```

### Blob Storage
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## 常用腳本

| 腳本 | 用途 | 用法 |
|---|---|---|
| `SetupFromTemplate.ps1` | 初始設定 | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | 更改命名空間 | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | 產生金鑰 | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | 計算 CSP Hash | `.\HashInlineScriptPowerShell.ps1` |

## 開發階段建議

### Phase 1（約 5 分鐘）
- Session
- Localization
- Security Headers
- 不開啟 Auth
- 不開啟 DB

### Phase 2（約 30 分鐘）
- 包含 Phase 1
- 啟用 Azure AD + Authorization
- 啟用 CSP + Nonce

### Phase 3（約 1–2 小時）
- 包含 Phase 2
- 啟用 Cosmos DB / Blob Storage / Key Vault

## 快速排錯

### Build 錯誤
```powershell
dotnet clean
dotnet build
dotnet restore
```

### 找不到設定
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### 找不到 Secret
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Auth 迴圈 / 401
1. 檢查 Azure AD redirect URI
2. 檢查 `EnableAzureAd: true`
3. 檢查 client secret
4. 清除瀏覽器 cookies

### CSP 違規
1. 檢查 `EnableNonceServices: true`
2. 檢查 Key / IV 是否正確
3. 檢查瀏覽器 Console
4. 可暫時 `EnableCSP: false` 做比對

## 文件入口

- 完整文件：`TEMPLATE_README.md`
- 設定：`appsettings.template.json`
- 更改命名空間：`RenameNamespace.ps1`

## 上線前安全清單

- [ ] 所有 secrets 放在 Key Vault 或 User Secrets
- [ ] `appsettings.json` 已被 git ignore
- [ ] Security headers 已啟用
- [ ] CSP + nonce 已設定
- [ ] 強制 HTTPS
- [ ] 受保護頁面已啟用認證
- [ ] 已輪替預設密鑰

---

**Template Version**: 1.0  
**ASP.NET Core**: 9.0  
**Last Updated**: 2024-12-20
