# 安全修复：nonce 生成中的 AES-GCM IV 重用（严重问题 #1）

**修复于：** `Models/Main_Objects/Nonce.cs`、`Services/NonceRefresherService.cs`、
`Services/NonceCatalogService.cs`
**测试：** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`、
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## 问题描述

`Nonce` 类在每次调用时使用**从 Azure Key Vault 获取的固定 IV** 进行 **AES-GCM 加密**。在使用相同 AES-GCM 密钥的情况下重用同一 IV 是灾难性的密码学错误：

* 攻击者若观察到使用相同 IV 和密钥加密的两段密文，可通过异或运算恢复两段明文的异或值。
* 更关键的是，对于基于 nonce 的认证标签，IV 重用允许伪造认证标签，从而完全破坏 AES-GCM 的完整性保证。

除密码学缺陷外，该加密操作对此用例**毫无安全价值**。CSP nonce 只需满足两个属性：**不可预测**且**每次请求唯一**。这两个属性可直接由密码学安全随机数生成器（`RandomNumberGenerator`）提供。加密操作增加了复杂性，却未带来任何安全提升。

### 修复前的根因代码

```csharp
// Nonce.cs — 每次调用从 Key Vault 获取相同 IV
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — 获取一次后在所有请求间重用
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## 修复内容

`Nonce.GenerateSecureNonce()` 现在直接调用 `RandomNumberGenerator.Fill(byte[])` 生成 16 字节密码学随机数据，然后进行 Base64 编码：

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* 不再需要也不再进行任何 Key Vault 调用以获取 IV 或加密密钥。
* 不涉及 AES-GCM 或任何其他密码。
* `Nonce` 构造函数不再接受 `KeyVaultSecret` 参数。

同时修复了 `NonceCatalogService.GetANonce` 中的一个次要 bug：该方法之前使用两步"检查后查找"（`TryGetValue` 后跟索引器 `[]`），这不是原子操作，当另一线程在两次调用之间删除键时可能抛出 `KeyNotFoundException`。修复后使用带 `out` 参数的 `TryGetValue` 在单个原子操作中完成值的检索。

---

## 如何保持修复状态

1. **永远不要为 nonce 生成引入 Key Vault IV 或密钥。** 如果 Key Vault 用于其他机密，这没问题——但 nonce 生成绝不能依赖固定 IV。

2. **永远不要用重用 IV 或计数器的 AES-GCM 或 CBC/CTR 方案替换 `GenerateSecureNonce`。**

3. **保持 nonce 至少为 16 字节（128 位）。** 减少字节长度会增加碰撞概率并降低 CSP 可用的熵。

4. **不要将 `RandomNumberGenerator.Fill` 替换为 `new Random()`** 或任何其他非密码学安全伪随机数生成器。

5. **保持 `NonceCatalogService.GetANonce` 使用带 `out` 参数的 `TryGetValue`。** 即使使用 `ConcurrentDictionary`，两步"检查后查找"模式（`TryGetValue` + 索引器）也不是线程安全的。

### 执行此修复的测试

| 测试 | 捕获的问题 |
|------|-----------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | 若构造函数恢复为接受 `KeyVaultSecret` IV + 密钥参数则编译失败 |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | 若 nonce 生成损坏或返回非 Base64 值则失败 |
| `GenerateSecureNonce_Returns16ByteBase64` | 若字节长度降至 16 以下则失败 |
| `Nonce_SuccessiveGenerations_AreUnique` | 若 IV 重用导致重复生成相同 nonce 则失败 |
| `Nonce_HasSufficientEntropy` | 若熵源不是随机的则失败 |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | 若 `ConcurrentDictionary` 被恢复为 `Dictionary` 则失败 |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | 若 `GetANonce` 中的检查时间/使用时间竞争条件被重新引入则失败 |
