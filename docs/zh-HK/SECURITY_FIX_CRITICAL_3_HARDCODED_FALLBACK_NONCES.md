# 安全修復：硬編碼後備 Nonce（Critical #3）

**修復檔案：** `Services/OptimizedNonceMiddleware.cs`  
**測試檔案：** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## 問題說明

`OptimizedNonceMiddleware` 曾在例外/退化流程中使用固定字串作為後備 nonce：

| 位置 | 固定值 |
|---|---|
| 首次請求、catalog 為空 | `"bootstrap-nonce-placeholder"` |
| 生成結果為空字串 | `"fallback-nonce"` |
| 例外流程 | `"error-fallback-nonce"` |

硬編碼 nonce 可被預測。若攻擊者觸發錯誤路徑（例如限流、網路干擾導致依賴失效），就可能取得可預測 nonce 並繞過 CSP。

---

## 修復內容

三條後備路徑全部改成執行期隨機生成：

```csharp
// BEFORE
existingNonce = "bootstrap-nonce-placeholder";
// AFTER
existingNonce = Nonce.GenerateSecureNonce();

// BEFORE
nonce = "fallback-nonce";
// AFTER
nonce = Nonce.GenerateSecureNonce();

// BEFORE
context.Items["Nonce"] = "error-fallback-nonce";
// AFTER
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` 使用 `RandomNumberGenerator.Fill` 生成 16-byte 隨機值，並轉 Base64，可在依賴不可用時仍安全運作。

---

## 持續安全守則

1. 不要在任何程式路徑硬編碼 nonce。
2. 所有 `context.Items["Nonce"]` 寫入都必須是不可預測隨機值。
3. 不要跨請求重用同一 nonce。
4. 錯誤路徑更不能使用固定後備值。
5. 修改 `OptimizedNonceMiddleware` 時重點檢查三個分支：
   - ignore-path
   - empty-generation
   - exception-handler

---

## 對應測試

| 測試 | 能防止什麼回歸 |
|---|---|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | 防止回復 `bootstrap-nonce-placeholder` |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | 防止回復 `fallback-nonce` |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | 防止回復 `error-fallback-nonce` |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | 防止後備 nonce 固定不變 |
