# Nonce 產生效能優化指南

## 目前問題

Nonce 現在幾乎對每個 HTTP 請求都會產生，包括：
- 靜態檔（CSS/JS/圖片）
- API 呼叫
- 健康檢查
- 負載平衡器探測

造成：
- Key Vault 呼叫過多
- 不必要的密碼學運算
- 效能下降
- 成本增加

## 方案：只在需要 HTML 回應時產生 Nonce

僅對會輸出 HTML 且需要 CSP 的回應產生新 nonce。

---

## 實作重點

### 1) Response-Only Nonce Middleware

核心概念：在送出回應前、確認是 HTML 時才產生 nonce。

```csharp
public class NonceResponseMiddleware
{
    // ...省略建構式

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            if (ShouldGenerateNonce(context))
            {
                await _nonceRefresherService.RefreshNonceAsync();
                var nonce = _nonceCatalogService.GetANonce("CSPNonce");
                context.Items["Nonce"] = nonce;
            }

            context.Response.Body = originalBodyStream;
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldGenerateNonce(HttpContext context)
    {
        if (context.Response.StatusCode != 200) return false;
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType)) return false;
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2) 請求期中介層維持現有 nonce

保留既有 nonce，不在每次請求都重新生成。

---

## 可選策略

### 選項 1：按路徑過濾（最簡單）
- 跳過 `/css`、`/js`、`/lib`、`/api` 與副檔名路徑
- 只對頁面請求生成 nonce

### 選項 2：每個 HTML 回應一個 nonce（建議）
- 在 `OnStarting` 事件、送標頭前判斷 `text/html`

### 選項 3：Lazy 生成（最省）
- 只有在構建 CSP 標頭時才生成
- 可搭配短時效快取

---

## 效能改善（示例）

### 優化前
- 每分鐘 1000 請求
- nonce 生成 1000 次
- Key Vault 呼叫 2000 次

### 優化後
- 每分鐘 1000 請求
- 只有 100 個頁面請求生成 nonce
- Key Vault 呼叫 200 次（約降 90%）

---

## 建議方案

採用 **路徑過濾 + 快取**：
- 明確忽略靜態資源路徑
- 頁面請求才生成 nonce
- 錯誤路徑仍回傳安全可用 nonce

---

## 驗證方式

```powershell
dotnet run
Invoke-WebRequest "https://localhost:5001/"                # 應生成 nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"    # 不應生成 nonce
Invoke-WebRequest "https://localhost:5001/Privacy"         # 應生成 nonce
```

並可加入計數型日誌觀察生成頻率。

---

## 遷移步驟

1. 備份目前 `NonceMiddleware.cs`
2. 建立 `OptimizedNonceMiddleware.cs`
3. 更新 `Program.cs` 註冊順序
4. 驗證靜態檔請求
5. 驗證頁面請求
6. 觀察 Key Vault 指標
7. 確認後移除舊中介層

---

## 預期結果

- nonce 生成次數下降約 90%
- Key Vault 呼叫下降約 90%
- 靜態內容回應更快
- 雲端成本降低
- HTML 頁面安全性維持不變

---

## 可加入設定

```json
{
  "NonceGeneration": {
    "GenerateForStaticFiles": false,
    "GenerateForApiCalls": false,
    "NonceLifetimeMinutes": 5,
    "EnableOptimization": true
  }
}
```
