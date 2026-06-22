# OCSP（Online Certificate Status Protocol）實作指南

## 概覽

本專案包含 OCSP 憑證撤銷檢查的**範本支援**。OCSP 可在處理請求前，即時檢查憑證是否被撤銷。

## 什麼是 OCSP？

OCSP 是 CRL（憑證撤銷清單）的替代方式，具備：
- 即時查驗
- 僅查特定憑證狀態
- 回應較輕量
- 狀態更新更即時

## 設定

### 1) 功能旗標

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2) OCSP 設定

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.yourcompany.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

| 設定 | 型別 | 預設值 | 說明 |
|---|---|---|---|
| `EnableOcspValidation` | bool | `false` | 啟用/停用 OCSP |
| `OcspServerUrl` | string | `null` | OCSP 回應器 URL |
| `RequestTimeoutSeconds` | int | `30` | OCSP 請求逾時秒數 |
| `MaxRetryAttempts` | int | `3` | 失敗重試次數 |
| `CacheDurationMinutes` | int | `60` | 快取 OCSP 回應時長 |
| `ServerUnavailableBehavior` | string | `"Warn"` | 伺服器不可用時行為：`Fail` / `Allow` / `Warn` |
| `EnableDetailedLogging` | bool | `false` | 詳細日誌 |
| `SkipValidationInDevelopment` | bool | `true` | 開發環境略過驗證 |

---

## 範本實作狀態

目前 `OcspValidationService.cs` 的 `PerformOcspValidationAsync` 仍為範本。投入生產前請完成：
1. 建立 RFC 6960 格式 OCSP 請求
2. 發送至 OCSP 伺服器
3. 解析回應
4. 驗證回應簽章
5. 回傳正確狀態

同時需要可用的 OCSP 回應器（自建或商用）。

---

## 使用範例

### 基本驗證

```csharp
public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
{
    return await _ocspService.ValidateCertificateAsync(clientCert);
}
```

### 詳細驗證狀態

```csharp
var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);
switch (result.Status)
{
    case OcspStatus.Good:
        logger.LogInformation("Certificate is valid");
        break;
    case OcspStatus.Revoked:
        logger.LogError("Certificate has been revoked!");
        throw new SecurityException("Certificate revoked");
    case OcspStatus.Unknown:
        logger.LogWarning("Certificate status unknown");
        break;
    case OcspStatus.ServerUnavailable:
        logger.LogWarning("OCSP server unavailable");
        break;
}
```

---

## 與 mTLS 整合

可在憑證驗證事件中呼叫 OCSP 服務；若驗證失敗則 `context.Fail(...)` 拒絕請求。

---

## ServerUnavailableBehavior

### `Fail`（最嚴格）
- OCSP 不可用就拒絕
- 安全最高、可用性較低

### `Allow`（可用性優先）
- OCSP 不可用仍允許
- 需配合警示監控

### `Warn`（預設平衡）
- 允許請求但記錄警告
- 便於逐步上線

---

## 快取

建議快取 OCSP 回應（例如 60 分鐘）以降低負載並提升效能。快取到期後自動更新。

---

## 安全建議

### 建議
- OCSP URL 一律使用 HTTPS
- 驗證 OCSP 回應簽章
- 根據環境選擇合理快取時間
- 高安全場景優先 `Fail`
- 監控 OCSP 服務可用性
- 實作重試機制

### 避免
- 生產環境使用 HTTP OCSP
- 略過回應簽章驗證
- 快取過久（>24 小時）
- 無警示地忽略 OCSP 失敗

---

## 測試

### 單元測試

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### 手動測試
1. 關閉 OCSP：確認系統仍可運作
2. 設定無效 URL：驗證 `ServerUnavailableBehavior`
3. 有效憑證：應回傳 `OcspStatus.Good`
4. 重複請求：確認快取生效

---

## 疑難排解

### OCSP 伺服器持續不可用
- 檢查 `OcspServerUrl`
- 檢查防火牆是否允許出站 HTTPS
- 檢查 OCSP 服務是否運行
- 查看逾時相關日誌

### 全部憑證都驗證失敗
- 檢查 OCSP 端是否有憑證狀態資料
- 檢查完整憑證鏈
- 檢查回應簽章是否正確

### 快取無效
- 檢查 `CacheDurationMinutes > 0`
- 確認是同一張憑證指紋
- 重啟應用程式清空快取

---

## 下一步

1. 設定完成
2. 服務介面完成
3. 測試完成（30+）
4. 待完成：OCSP 協議實作（RFC 6960）
5. 待完成：OCSP 回應器部署
6. 待完成：與 mTLS 生產整合

---

## 參考

- [RFC 6960](https://tools.ietf.org/html/rfc6960)
- [BouncyCastle Documentation](https://www.bouncycastle.org/csharp/)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**狀態：** 範本可用  
**OCSP 協議：** 待實作  
**OCSP 伺服器：** 待部署  
**測試：** 30+ 已包含
