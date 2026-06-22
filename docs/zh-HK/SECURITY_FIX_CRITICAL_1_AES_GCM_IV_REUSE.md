# 安全修復：Nonce 產生中的 AES-GCM IV 重用（Critical #1）

**修復檔案：** `Models/Main_Objects/Nonce.cs`、`Services/NonceRefresherService.cs`、`Services/NonceCatalogService.cs`  
**測試檔案：** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`、`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## 問題說明

原本 `Nonce` 類別在每次呼叫都使用從 Key Vault 取出的固定 IV 進行 AES-GCM 加密。對 AES-GCM 而言，**同一把 key 重用 IV 是嚴重密碼學錯誤**：

- 可讓攻擊者比較密文推得明文關係。
- 更嚴重是可破壞驗證標籤安全性，影響完整性保證。

另外，此處加密本身沒有實際安全收益。CSP nonce 的核心需求只是：
1. 不可預測
2. 每請求唯一

使用 CSPRNG（`RandomNumberGenerator`）即可滿足。

---

## 修復內容

`Nonce.GenerateSecureNonce()` 改為直接產生 16 位元組安全隨機資料並 Base64 編碼：

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

修復結果：
- 不再依賴 Key Vault IV / key 來生成 nonce
- 不再使用 AES-GCM
- `Nonce` 建構式不再接收 `KeyVaultSecret`

同時修復 `NonceCatalogService.GetANonce` 的非原子讀取問題（避免 `TryGetValue` + 索引器競態）。

---

## 持續安全守則

1. 不要再引入固定 IV + nonce 生成流程。
2. 不要改回會跨請求重用 IV/counter 的加密模式。
3. nonce 至少維持 16 bytes（128 bits）。
4. 不要用 `new Random()` 取代 CSPRNG。
5. `GetANonce` 維持使用 `TryGetValue(out ...)` 原子讀取。

---

## 對應測試

| 測試 | 能防止什麼回歸 |
|---|---|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | 防止建構式回復成需 Key Vault 參數 |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | 防止產生失敗或非 Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | 防止長度被改小 |
| `Nonce_SuccessiveGenerations_AreUnique` | 防止重複 nonce |
| `Nonce_HasSufficientEntropy` | 防止隨機性不足 |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | 防止退回非執行緒安全字典 |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | 防止競態異常回歸 |
