# Security Review — WebAppExperimental266

**日期：** 2026-05-05  
**範圍：** 全程式碼靜態分析

---

## 摘要表

| # | 領域 | 嚴重度 |
|---|---|---|
| 1 | Nonce 產生中 AES-GCM IV 重用 | 🔴 Critical ✅ |
| 2 | Nonce 明文寫入日誌 | 🔴 Critical ✅ |
| 3 | 硬編碼後備 nonce 字串 | 🔴 Critical ✅ |
| 4 | 全域 nonce 字典非執行緒安全 | 🟠 High |
| 5 | mTLS issuer 驗證被註解掉 | 🟠 High |
| 6 | mTLS 撤銷檢查預設關閉 | 🟠 High |
| 7 | OCSP stub 一律通過 | 🟠 High |
| 8 | 設定中 auth/authz 預設關閉 | 🟠 High |
| 9 | 安全標頭在管線過後才套用 | 🟠 High |
| 10 | Session cookie 缺 Secure + SameSite | 🟡 Medium |
| 11 | 全域 Set-Cookie 標頭格式錯誤 | 🟡 Medium |
| 12 | 全回應強制 Content-Type=text/html | 🟡 Medium |
| 13 | AllowedHosts 為萬用字元 | 🟡 Medium |
| 14 | Layout `<script>` 未套 nonce | 🟡 Medium |
| 15 | 缺少 Referrer-Policy | 🟡 Medium |
| 16 | PII 明文寫入日誌 | 🔵 Low |
| 17 | 日誌輸出部分連線字串 | 🔵 Low |
| 18 | Key Vault 操作為 stub | 🔵 Low |
| 19 | X-XSS-Protection 已過時 | 🔵 Low |

---

## 🔴 Critical

### 1. AES-GCM IV 重用（Nonce 生成）✅ 已修復（commit 45ae31b）

**檔案：** `Models/Main_Objects/Nonce.cs`、`Services/NonceRefresherService.cs`

原實作以固定 IV + 同 key 反覆做 AES-GCM，屬於重大密碼學錯誤。CSP nonce 不需要加密，只需不可預測且每請求唯一，使用 `RandomNumberGenerator.GetBytes(16)` 後 Base64 即可。

---

### 2. Nonce 明文記錄 ✅ 已修復（commit bb6f27a）

**檔案：** `Services/NonceMiddleware.cs`、`Services/NonceRefresherService.cs`

nonce 一旦寫入日誌，能讀日誌者即可取得有效 nonce，進而繞過 CSP 注入 inline script。

---

### 3. 硬編碼 fallback nonce ✅ 已修復（commit 11cc9f7）

**檔案：** `Services/OptimizedNonceMiddleware.cs`

錯誤路徑使用固定字串 nonce，等同可預測值；攻擊者可刻意觸發退化流程進行利用。

---

## 🟠 High

### 4. NonceCatalogService 使用非執行緒安全 Dictionary ✅ 已修復（commit ae2b6c9）

**檔案：** `Services/NonceCatalogService.cs`

全域 `Dictionary` 在併發讀寫下可出錯，並可能導致請求間 nonce 互相覆寫。應使用 `ConcurrentDictionary`，且 nonce 優先放在 `HttpContext.Items`。

---

### 5. mTLS issuer 驗證被註解 ✅ 已修復（commit fd3d4fb）

**檔案：** `Extensions/ServiceCollectionExtensions.cs`

`ValidateClientCertificateIssuer` 雖預設為 `true`，但實際驗證程式碼被註解，導致憑證簽發者限制失效。

---

### 6. mTLS 撤銷檢查預設關閉 ✅ 已修復（commit fd3d7b3）

**檔案：** `Models/Settings/MtlsSettings.cs`、`appsettings.template.json`

撤銷檢查預設關閉會讓已撤銷憑證長期可用。

---

### 7. OCSP 永遠回傳有效（stub）✅ 已修復（commit b4c3807）

**檔案：** `Services/OcspValidationService.cs`

若啟用 OCSP 但仍為 stub，實際上不會攔阻任何撤銷憑證。

---

### 8. Auth/Authz 預設關閉 ✅ 已修復（commit b392c47）

**檔案：** `appsettings.json`

預設配置可能導致未保護部署。安全預設應改為需明確停用，而非明確啟用。

---

### 9. 安全標頭套用太晚 ✅ 已修復（commit 016e57c）

**檔案：** `Program.cs`

若在 routing/auth 之後才套標頭，提早中止的回應可能拿不到安全標頭。

---

## 🟡 Medium

### 10. Session cookie 缺 `Secure` / `SameSite` ✅ 已修復（commit 8f2223c）

**檔案：** `Extensions/ServiceCollectionExtensions.cs`

Cookie 安全屬性不完整，可能增加傳輸與跨站風險。

---

### 11. 全域 `Set-Cookie` 標頭格式錯誤 ✅ 已修復（commit 8f2223c）

**檔案：** `Extensions/ApplicationBuilderExtensions.cs`

全域追加無名稱值的 `Set-Cookie` 屬不正確做法，應在特定 cookie options 設定。

---

### 12. 全回應強制 `Content-Type=text/html` ✅ 已修復（commit 8f2223c）

**檔案：** `Extensions/ApplicationBuilderExtensions.cs`

會破壞 API/靜態檔/二進位下載回應型別。

---

### 13. `AllowedHosts` 使用 `*` ✅ 已修復（commit 8f2223c）

**檔案：** `appsettings.json`、`appsettings.template.json`

等同關閉 Host Header 驗證，可能引發 cache poisoning、重設連結污染等攻擊面。

---

### 14. Layout 的 `<script>` 未套 nonce ✅ 已修復（commit 8f2223c）

**檔案：** `Views/Shared/_Layout.cshtml`

啟用 CSP nonce 時，未加 `nonce` 屬性的 script 會被瀏覽器阻擋。

---

### 15. 缺少 Referrer-Policy ✅ 已修復（commit 8f2223c）

**檔案：** `Extensions/ApplicationBuilderExtensions.cs`

缺少此標頭可能導致 URL 資訊外洩至第三方資源。

---

## 🔵 Low / 資訊性

### 16. PII 明文寫入日誌 ✅ 已修復（commit 93bb4e9）

**檔案：** `Services/LoggingHelper.cs`

OID、Email、Name 等個資明文輸出，可能有合規風險。建議以 HMAC 後的識別值取代。

---

### 17. 日誌輸出部分連線字串 ✅ 已修復（commit 93bb4e9）

**檔案：** `Extensions/ServiceCollectionExtensions.cs`

即使只輸出片段也非最佳實務，應改為僅記錄是否存在。

---

### 18. Key Vault 操作為 stub ✅ 已修復（commit 93bb4e9）

**檔案：** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

stub 在啟用後會 fail-fast，雖安全但代表尚未有可審計的正式資料流。

---

### 19. `X-XSS-Protection` 已過時 ✅ 已修復（commit 93bb4e9）

**檔案：** `Extensions/ApplicationBuilderExtensions.cs`

現代瀏覽器多已不支援，應以強 CSP 為主。
