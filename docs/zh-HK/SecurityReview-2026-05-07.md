# 安全審查 — WebAppExperimental266

**日期：** 2026-05-07
**範圍：** 完整程式碼庫靜態分析（2026-05-06 審查的後續追蹤）
**審查人員：** 自動化安全審查

---

## 執行摘要

本次後續審查確認，2026-05-06 安全審查中識別的 5 個漏洞中，有 3 個已完全修復，1 個仍部分修復。本次審查亦發現 4 項新問題。應用程式的整體安全狀況持續改善。

---

## 先前發現事項的狀態（2026-05-06）

| # | 發現事項 | 嚴重程度 | 狀態 |
|---|---------|----------|--------|
| 20 | NonceRefresherService 保留未使用的 Key Vault 建構函式依賴項 | 🟠 高 | ✅ 已修復 |
| 21 | OcspValidationService 內部快取使用非執行緒安全的 Dictionary | 🟡 中 | ✅ 已修復 |
| 22 | OCSP 驗證 stub 仍然存在 — 以關閉模式失敗但尚未實作 | 🔵 低 | ⚠️ 已接受（依設計） |
| 23 | mTLS 配合空的 AllowedIssuers 拒絕所有憑證（fail-closed，未記錄） | 🔵 低 | ✅ 已修復 |
| 24 | OcspSettings.ServerUnavailableBehavior 預設為 "Warn"（允許在錯誤時直接通過） | 🔵 低 | ⚠️ 部分修復 |

---

## 先前發現事項的詳細狀態

### ✅ 20. NonceRefresherService 未使用的 DI 依賴項 — 已修復

**檔案：** `Services/NonceRefresherService.cs`

`NonceRefresherService` 的建構函式現在只宣告 `ILogger<NonceRefresherService>`、`ILoggerFactory` 和 `INonceCatalogService`。四個先前未使用的依賴項（`IKeyVaultSettingsService`、`INonceEncryptionSettingsService`、`IAzureADSettingsService`、`IAzureKeyVaultOperationsService`）已被移除。這解決了當 `EnableKeyVault = false`（預設值）且 `EnableNonceServices = true`（預設值）時，阻止應用程式啟動的服務拒絕風險。

---

### ✅ 21. OcspValidationService 非執行緒安全的快取 — 已修復

**檔案：** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` 已被替換為 `ConcurrentDictionary<string, CachedOcspResponse>`。`_cache.Remove` 呼叫已更新為 `_cache.TryRemove`。快取現在可安全地進行並發存取。

---

### ⚠️ 22. OCSP 驗證 Stub — 已接受（依設計）

**檔案：** `Services/OcspValidationService.cs`

Stub 仍然存在，但能正確地以關閉模式失敗。由於 `EnableOcspValidation` 預設為 `false`，這對生產環境沒有影響。在完整的 OCSP 實作完成之前，此問題作為資訊性發現被接受。

---

### ✅ 23. mTLS 空的 AllowedIssuers — 已修復

**檔案：** `Extensions/ServiceCollectionExtensions.cs`

當 `ValidateClientCertificateIssuer = true` 且 `AllowedIssuers` 為空時，現在會記錄啟動警告：

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

這為遇到 fail-closed 行為的操作人員提供了明確的指引。

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — 部分修復

**檔案：** `appsettings.template.json`（已修復），`Models/Settings/OcspSettings.cs`（尚未修復）

模板現在正確指定 `"ServerUnavailableBehavior": "Fail"`。但是，`OcspSettings.cs`（第 39 行）中的 C# 類別預設值仍為 `"Warn"`。如果操作人員啟用 OCSP 並從配置檔案中省略 `ServerUnavailableBehavior`，類別預設值 `"Warn"` 將靜默應用，允許在 OCSP 伺服器中斷時直接通過。應更改類別預設值以符合模板建議。

---

## 新發現事項

| # | 區域 | 嚴重程度 |
|---|------|----------|
| 25 | OcspSettings 類別預設值（"Warn"）與模板（"Fail"）不一致 | 🔵 低 |
| 26 | NonceCatalogService 單一共享 nonce 金鑰允許跨請求 nonce 碰撞 | 🟡 中 |
| 27 | OptimizedNonceMiddleware 靜態計數器使用有符號 32 位元整數（溢位風險） | 🔵 低 |
| 28 | Program.cs 註冊空的 ILoggerFactory 單例，遮蔽框架日誌記錄器 | 🟡 中 |

---

## 🟡 中

### 26. NonceCatalogService 共享 Nonce 金鑰允許跨請求 Nonce 碰撞

**檔案：** `Services/NonceCatalogService.cs`、`Services/NonceMiddleware.cs`、`Services/OptimizedNonceMiddleware.cs`

Nonce 目錄將所有 nonce 儲存在單一共享金鑰 `"CSPNonce"` 下。在並發負載下，可能發生以下競態條件：

1. 請求 A 呼叫 `RefreshNonceAsync()` — nonce A1 被儲存為 `_nonceCollection["CSPNonce"]`。
2. 請求 B 呼叫 `RefreshNonceAsync()` — nonce B1 覆蓋 `_nonceCollection["CSPNonce"]`。
3. 請求 A 呼叫 `GetANonce("CSPNonce")` — 收到 B1，而非 A1。
4. 請求 A 的 CSP 標頭和版面配置 nonce 都包含 B1。
5. 請求 B 也包含 B1。

兩個並發回應共享相同的 nonce。雖然兩個值仍然是加密隨機且不可預測的（沒有硬編碼字串），但相同的 nonce 值出現在多個同時回應中，削弱了 CSP 規範要求的每個請求唯一性保證。能夠觀察到一個回應 nonce 的攻擊者，對至少一個其他並發回應擁有有效的 nonce。

**建議：** 直接在每個請求的中介軟體內生成 nonce（例如 `Nonce.GenerateSecureNonce()`），並僅將其儲存在 `HttpContext.Items["Nonce"]` 中，繞過共享目錄以用於每個請求的 nonce。只有當 nonce 需要在單個請求的多個中介軟體層之間共享時，才需要共享目錄，而 `HttpContext.Items` 已原生處理此情況。

---

### 28. Program.cs 註冊空的 ILoggerFactory 單例

**檔案：** `Program.cs`（第 85 行）

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core 在 `WebApplication.CreateBuilder` 期間，自動註冊一個完全配置的 `ILoggerFactory`（包含來自 `builder.Logging` 配置的所有日誌提供者）。這個明確的 `AddSingleton` 註冊新增了第二個未配置的 `LoggerFactory` 實例，沒有任何提供者。由於 `GetRequiredService<ILoggerFactory>()` 傳回最近註冊的實作，透過依賴注入接收 `ILoggerFactory` 的服務（如 `NonceRefresherService`）將使用這個空的工廠，並且不會透過 `_loggerFactory.CreateLogger<T>()` 產生任何日誌輸出。

**風險：** `NonceRefresherService` 中的靜默日誌記錄 — nonce 生成的成功和失敗不會輸出到任何已配置的日誌接收器。這在不影響功能的情況下，降低了應用程式在安全敏感操作期間的可觀察性。

**建議：** 移除明確的 `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` 註冊。框架已配置的 `ILoggerFactory`（包含主控台和任何其他提供者）將被依賴它的服務正確解析。

---

## 🔵 低 / 資訊性

### 25. OcspSettings 類別預設值與模板不一致

**檔案：** `Models/Settings/OcspSettings.cs`（第 39 行）

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

模板（`appsettings.template.json`）指定 `"ServerUnavailableBehavior": "Fail"`，但 C# 類別預設值為 `"Warn"`。如果 `ServerUnavailableBehavior` 在作用中的配置檔案中缺失，類別預設值將靜默應用，而非模板建議。這是發現事項 #24 的殘留問題。

**建議：** 將類別預設值從 `"Warn"` 更改為 `"Fail"`，以符合模板和最小權限原則。

---

### 27. OptimizedNonceMiddleware 靜態計數器可能溢位

**檔案：** `Services/OptimizedNonceMiddleware.cs`（第 25–26 行）

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

這些有符號 32 位元計數器透過 `Interlocked.Increment` 以原子方式遞增。在大約 21 億次遞增後，它們將迴繞到 `int.MinValue`（−2,147,483,648），導致效率計算 `(total - generated) * 100.0 / total` 產生錯誤或無意義的結果。在每秒 1,000 個請求的情況下，溢位將在大約 24.8 天的連續運行後發生。

**建議：** 將計數器欄位類型從 `int` 更改為 `long`，並使用 `Interlocked.Increment` 的 `long` 多載以防止溢位。

---

## 安全標頭評估（目前狀態）

以下標頭透過 `UseStandardSecurityHeaders` 套用 — 與先前審查相比未變更：

| 標頭 | 值 | 評估 |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ 良好 |
| `X-XSS-Protection` | `0` | ✅ 良好（停用已棄用的稽核器） |
| `X-Content-Type-Options` | `nosniff` | ✅ 良好 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 良好 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 良好 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 良好 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 良好 |
| `Permissions-Policy` | 地理位置、相機、麥克風、interest-cohort 已停用 | ✅ 良好 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 良好 |
| `Content-Security-Policy` | 基於 nonce，在啟用 CSP 時套用 | ✅ 良好 |
| `Server` | 遮蔽為 `"webserver"` | ✅ 良好 |
| `X-Powered-By` | 已移除 | ✅ 良好 |

---

## 整體評估

先前審查中的所有高嚴重性發現事項均已修復。目前的發現事項僅限於兩個中等嚴重性問題（#26 共享 nonce 金鑰，#28 空的 ILoggerFactory）和兩個低嚴重性資訊項目（#25 類別預設值不一致，#27 計數器中的整數溢位）。建議立即關注發現事項 #28（空的 ILoggerFactory 單例），因為它在 nonce 操作期間靜默地抑制了與安全相關的診斷日誌記錄。應解決發現事項 #26（共享 nonce 金鑰），以恢復 CSP 規範要求的每個請求 nonce 唯一性保證。
