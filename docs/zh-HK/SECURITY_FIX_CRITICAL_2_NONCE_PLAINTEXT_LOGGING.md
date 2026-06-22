# 安全修復：Nonce 明文寫入日誌（Critical #2）

**修復檔案：** `Services/NonceMiddleware.cs`、`Services/NonceRefresherService.cs`  
**測試檔案：** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## 問題說明

原本有兩處把 CSP nonce 值直接寫入日誌：

- `Services/NonceMiddleware.cs`
- `Services/NonceRefresherService.cs`

CSP nonce 屬於「單次回應期間的秘密值」。若日志可被營運、監控平台或其他讀者存取，攻擊者可利用該 nonce 注入可執行 inline script，直接繞過 CSP。

---

## 修復內容

把「輸出 nonce 值」改為「只記錄狀態」，不輸出敏感內容：

```csharp
// BEFORE
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// AFTER
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

```csharp
// BEFORE
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// AFTER
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## 持續安全守則

1. 永遠不要記錄 nonce 值。
2. nonce 相關程式碼新增日誌時，僅記錄成功/失敗狀態。
3. 不要把 nonce 放進 telemetry、metrics、trace tags。
4. nonce 僅應存在於單一請求生命週期（例如 `HttpContext.Items`）。

---

## 對應測試

| 測試 | 能防止什麼回歸 |
|---|---|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | 防止 refresher 再次輸出 nonce 值 |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | 防止 middleware 再次輸出 nonce 值 |
