# 安全審查 — WebAppExperimental266

**日期：** 2026-05-06
**範圍：** 完整程式碼庫靜態分析（2026-05-05 審查之後續審查）
**審查人員：** 自動化安全審查

---

## 執行摘要

此後續審查確認，2026-05-05 安全審查中發現的所有 19 個漏洞均已修復。本次審查亦發現了 5 個在本次會話中發現的新問題或殘留問題。自上次審查以來，應用程式的整體安全態勢已大幅改善。

---

## 先前發現事項之狀態（2026-05-05）

所有 19 項先前發現事項均**確認已修復**：

| # | 發現事項 | 嚴重程度 | 狀態 |
|---|----------|----------|------|
| 1 | nonce 生成中的 AES-GCM IV 重複使用 | 🔴 嚴重 | ✅ 已修復 |
| 2 | Nonce 以明文記錄 | 🔴 嚴重 | ✅ 已修復 |
| 3 | 硬式編碼的備用 nonce 字串 | 🔴 嚴重 | ✅ 已修復 |
| 4 | 非執行緒安全的全域 nonce 字典 | 🟠 高 | ✅ 已修復 |
| 5 | mTLS 發行者驗證已被註解 | 🟠 高 | ✅ 已修復 |
| 6 | mTLS 撤銷檢查預設關閉 | 🟠 高 | ✅ 已修復 |
| 7 | OCSP 始終返回有效（stub） | 🟠 高 | ✅ 已修復 |
| 8 | 設定中的身份驗證/授權預設關閉 | 🟠 高 | ✅ 已修復 |
| 9 | 安全標頭在管線中套用過晚 | 🟠 高 | ✅ 已修復 |
| 10 | 工作階段 Cookie 缺少 `Secure` + `SameSite` | 🟡 中 | ✅ 已修復 |
| 11 | 格式錯誤的全域 `Set-Cookie` 標頭 | 🟡 中 | ✅ 已修復 |
| 12 | `Content-Type` 在所有地方強制設為 `text/html` | 🟡 中 | ✅ 已修復 |
| 13 | `AllowedHosts` 設定為萬用字元 | 🟡 中 | ✅ 已修復 |
| 14 | Nonce 未套用至版面配置中的 `<script>` 標籤 | 🟡 中 | ✅ 已修復 |
| 15 | `Referrer-Policy` 標頭缺失 | 🟡 中 | ✅ 已修復 |
| 16 | PII 以明文記錄 | 🔵 低 | ✅ 已修復 |
| 17 | 記錄中包含部分連接字串 | 🔵 低 | ✅ 已修復 |
| 18 | Key Vault 操作為 stub | 🔵 低 | ✅ 已修復 |
| 19 | 已棄用的 `X-XSS-Protection: 1; mode=block` | 🔵 低 | ✅ 已修復 |

---

## 新增 / 殘留發現事項

| # | 範疇 | 嚴重程度 |
|---|------|----------|
| 20 | NonceRefresherService 保留未使用的 Key Vault 建構函式依賴項 | 🟠 高 |
| 21 | OcspValidationService 內部快取使用非執行緒安全的 Dictionary | 🟡 中 |
| 22 | OCSP 驗證 stub 仍然存在——以關閉狀態失敗但未實作 | 🔵 低 |
| 23 | AllowedIssuers 為空的 mTLS 拒絕所有憑證（fail-closed，未記錄） | 🔵 低 |
| 24 | OcspSettings.ServerUnavailableBehavior 預設為「Warn」（發生錯誤時允許通過） | 🔵 低 |

---

## 詳細發現事項

### ✅ 來自 2026-05-05 的確認修復

#### 1. AES-GCM IV 重複使用——已修復

**檔案：** `Models/Main_Objects/Nonce.cs`

基於 AES-GCM 的 nonce 生成已完全替換。`Nonce.GenerateSecureNonce()` 現在對 16 個隨機位元組呼叫 `RandomNumberGenerator.Fill(randomBytes)`，並返回 Base64 字串。無 Key Vault 依賴、無 IV、無加密——這正是 CSP nonce 的正確做法。

---

#### 2. Nonce 值不再被記錄——已修復

**檔案：** `Services/NonceMiddleware.cs`、`Services/NonceRefresherService.cs`

兩個檔案現在只記錄狀態訊息（`"Nonce retrieved for request."`、`"Nonce generated successfully."`），絕不記錄 nonce 值本身。

---

#### 3. 硬式編碼的備用 Nonce 已移除——已修復

**檔案：** `Services/OptimizedNonceMiddleware.cs`

所有三個硬式編碼的常值字串（`"bootstrap-nonce-placeholder"`、`"fallback-nonce"`、`"error-fallback-nonce"`）在正常路徑和例外備用路徑中均已替換為對 `Nonce.GenerateSecureNonce()` 的呼叫。

---

#### 4. 執行緒安全的 Nonce 字典——已修復

**檔案：** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` 已替換為 `ConcurrentDictionary<string, Nonce>`。`GetANonce` 現在使用單一原子性 `TryGetValue` 呼叫，而不是兩步驟的檢查後查找。

---

#### 5. mTLS 發行者驗證現已正常運作——已修復

**檔案：** `Extensions/ServiceCollectionExtensions.cs`、`Models/Settings/MtlsSettings.cs`

被註解的發行者驗證區塊已替換為對 `mtlsSettings.IsIssuerAllowed(issuer)` 的呼叫，該方法對 `AllowedIssuers` 執行不區分大小寫的子字串比對。當清單為空（未設定）時，該方法返回 `false`，拒絕所有憑證（fail-closed）。

---

#### 6. mTLS 撤銷檢查預設啟用——已修復

**檔案：** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` 現在預設為 `true`。`appsettings.template.json` 亦指定 `"CheckCertificateRevocation": true`。

---

#### 7. OCSP Stub 現在以關閉狀態失敗——已修復

**檔案：** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` 現在返回 `IsValid = false` 並附帶 `OcspStatus.Error` 且記錄錯誤，而不是靜默地返回 `IsValid = true`。在設定中啟用 OCSP 現在將拒絕所有憑證，直到提供真正的實作，而不是靜默接受它們。

---

#### 8. 身份驗證和授權預設啟用——已修復

**檔案：** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` 和 `EnableAuthorization` 現在在 `FeatureFlags` 類別中均預設為 `true`。`appsettings.json` 亦將兩者設定為 `true`。

---

#### 9. 安全標頭在路由前套用——已修復

**檔案：** `Program.cs`

`UseNonceAndSecurityHeadersAsync` 和 `UseStandardSecurityHeaders` 現在在 `UseRouting`、`UseAuthentication` 和 `UseAuthorization` 之前呼叫。所有回應（包括 401/403 短路）均會收到安全標頭。

---

#### 10–15. Cookie、Content-Type、AllowedHosts、版面配置中的 Nonce、Referrer-Policy——已修復

**檔案：** `Extensions/ServiceCollectionExtensions.cs`、`Extensions/ApplicationBuilderExtensions.cs`、`Views/Shared/_Layout.cshtml`、`appsettings.json`

- 工作階段 Cookie 現在設定 `CookieSecurePolicy.Always` 和 `SameSiteMode.Strict`。
- 格式錯誤的無名 `Set-Cookie` 標頭已移除。
- 全域 `Content-Type: text/html` 覆寫已移除。
- `appsettings.json` 中的 `AllowedHosts` 現在為 `"localhost;127.0.0.1"`；範本使用 `"{{YOUR_HOSTNAME}}"`。
- `_Layout.cshtml` 中的所有三個 `<script>` 標籤現在均包含 `nonce="@Context.Items["Nonce"]"`。
- `Referrer-Policy: strict-origin-when-cross-origin` 現在由 `UseStandardSecurityHeaders` 新增。

---

#### 16–19. PII 記錄、連接字串記錄、Key Vault Stub、X-XSS-Protection——已修復

**檔案：** `Services/LoggingHelper.cs`、`Extensions/ServiceCollectionExtensions.cs`、`Extensions/ApplicationBuilderExtensions.cs`

- 所有 PII（OID、電子郵件、姓名、SID、角色）現在在寫入記錄之前均透過 `LoggingHelper.HashPii()` 進行 HMAC-SHA256 雜湊處理。可以透過設定中的 `Logging:PiiHmacKey` 提供穩定的 HMAC 金鑰；未設定時使用隨機的每程序金鑰。
- Cosmos DB 記錄語句現在只確認連接字串是否存在（`!string.IsNullOrEmpty`），而不是其內容。
- `AzureKeyVaultCertificateOperations` 現在在憑證為 null 時於啟動時拋出 `InvalidOperationException`，而不是靜默返回虛擬值。
- `X-XSS-Protection` 現在設定為 `"0"`（停用已棄用的 XSS 稽核器），與現代瀏覽器指南一致。

---

## 🟠 高

### 20. NonceRefresherService 保留未使用的 Key Vault 建構函式依賴項

**檔案：** `Services/NonceRefresherService.cs`

`NonceRefresherService` 仍然為 `IKeyVaultSettingsService`、`INonceEncryptionSettingsService`、`IAzureADSettingsService` 和 `IAzureKeyVaultOperationsService` 宣告建構函式參數。由於 nonce 生成已簡化為直接使用 `RandomNumberGenerator`，這些依賴項均未被使用。

**風險：** 當 `EnableNonceServices = true` 且 `EnableKeyVault = false`（預設值）時，這些服務未在 DI 容器中註冊，導致在首次解析 nonce 服務時於執行時期拋出 `InvalidOperationException`。這實際上是由預設設定觸發的拒絕服務狀況。`FeatureFlags` 類別預設將 `EnableNonceServices = true`，因此任何僅依賴類別預設值（沒有 `appsettings.json` 覆寫）的環境都將無法啟動。

**建議：** 從 `NonceRefresherService` 中移除四個未使用的建構函式參數及其對應的私有欄位。該服務只需要 `ILogger<NonceRefresherService>`、`ILoggerFactory` 和 `INonceCatalogService`。

---

## 🟡 中

### 21. OcspValidationService 內部快取使用非執行緒安全的 Dictionary

**檔案：** `Services/OcspValidationService.cs`（第 47 行）

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` 對並行讀寫不是執行緒安全的。如果 `OcspValidationService` 被登錄為單例（或者如果同一實例透過任何其他機制在請求之間共用），並行 OCSP 驗證可能會破壞快取，導致條目遺失、拋出例外或返回過時資料。

**建議：** 將 `Dictionary<string, CachedOcspResponse>` 替換為 `ConcurrentDictionary<string, CachedOcspResponse>`。將 `_cache.Remove` 呼叫（第 103 行）更新為 `_cache.TryRemove`。

---

## 🔵 低 / 資訊性

### 22. OCSP 驗證 Stub——以關閉狀態失敗但未實作

**檔案：** `Services/OcspValidationService.cs`（第 157–173 行）

`PerformOcspValidationAsync` 仍然是一個 stub。發現事項 #7 的修復正確地將行為從「始終有效」更改為「始終無效（fail-closed）」。但是，該方法仍然不是真正的 OCSP 實作。只要 `EnableOcspValidation = false`（預設值），這對生產環境沒有影響。在任何環境中啟用 OCSP 之前，必須實作生產品質的 OCSP 客戶端。

---

### 23. AllowedIssuers 為空的 mTLS 拒絕所有用戶端憑證

**檔案：** `Models/Settings/MtlsSettings.cs`

當 `ValidateClientCertificateIssuer = true`（預設值）且 `AllowedIssuers` 為空（未設定時亦為預設值）時，`IsIssuerAllowed()` 返回 `false`，導致所有用戶端憑證被拒絕。這是正確的 fail-closed 行為，但未被顯著記錄。在未仔細閱讀範本的情況下啟用 mTLS 的操作員可能會發現所有用戶端連接均被拒絕，而沒有明顯的解釋。

**建議：** 當 `ValidateClientCertificateIssuer = true` 且 `AllowedIssuers` 為空時，在啟動時新增警告記錄訊息。

---

### 24. OcspSettings.ServerUnavailableBehavior 預設為「Warn」

**檔案：** `appsettings.template.json`（第 134 行）、`Services/OcspValidationService.cs`

`ServerUnavailableBehavior` 設定在範本中預設為 `"Warn"`，當無法到達 OCSP 伺服器時允許請求通過。對於高安全性環境，這應設定為 `"Fail"`，以避免 OCSP 伺服器中斷時靜默地降低憑證撤銷檢查。

**建議：** 在範本中清楚記錄三個選項（`Fail`、`Allow`、`Warn`），並考慮將預設值更改為 `"Fail"` 以符合最小權限原則。

---

## 安全標頭評估（目前狀態）

以下標頭現在透過 `UseStandardSecurityHeaders` 套用：

| 標頭 | 值 | 評估 |
|------|----|------|
| `X-Frame-Options` | `DENY` | ✅ 良好 |
| `X-XSS-Protection` | `0` | ✅ 良好（停用已棄用的稽核器） |
| `X-Content-Type-Options` | `nosniff` | ✅ 良好 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 良好 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 良好 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 良好 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 良好 |
| `Permissions-Policy` | 地理定位、相機、麥克風、interest-cohort 已停用 | ✅ 良好 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 良好 |
| `Content-Security-Policy` | 基於 nonce，在啟用 CSP 時套用 | ✅ 良好 |
| `Server` | 已遮罩為 `"webserver"` | ✅ 良好 |
| `X-Powered-By` | 已移除 | ✅ 良好 |

---

## 整體評估

應用程式已解決上次審查中所有嚴重和高嚴重程度的漏洞。目前的發現事項僅限於一個高嚴重程度的設定/DI 問題（發現事項 #20）和較低嚴重程度的資訊性項目。安全態勢已大幅改善。建議立即針對發現事項 #20（NonceRefresherService 中未使用的 DI 依賴項）採取行動，因為它可能會阻止應用程式在預設設定下啟動。
