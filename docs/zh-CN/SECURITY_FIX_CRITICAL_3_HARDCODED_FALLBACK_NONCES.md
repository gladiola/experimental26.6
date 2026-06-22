# 安全修复：硬编码的后备 nonce（严重问题 #3）

**修复于：** `Services/OptimizedNonceMiddleware.cs`
**测试：** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## 问题描述

`OptimizedNonceMiddleware` 包含三个硬编码的字符串字面量，当正常 nonce 生成失败或尚未运行时被用作后备 nonce 值：

| 位置 | 硬编码值 |
|------|---------|
| `InvokeAsync` — 首次请求，目录为空 | `"bootstrap-nonce-placeholder"` |
| `InvokeAsync` — 生成返回空字符串 | `"fallback-nonce"` |
| `InvokeAsync` — 异常路径 | `"error-fallback-nonce"` |

### 为何严重

**nonce 只有在攻击者无法预测的情况下才是安全的。** 硬编码的字面量已提交到源代码控制，因此任何能访问仓库的人（包括获得源代码访问权限或反编译二进制文件的攻击者）都知晓这些值。

具体的危险在于，这些后备路径由**错误条件**触发——正是攻击者最可能精心制造的情况（例如，通过速率限制或网络中断使 Key Vault 暂时不可用）。当应用程序优雅降级到可预测的 nonce 时，CSP 标头形同虚设：攻击者只需注入 `<script nonce="fallback-nonce">` 即可让浏览器执行它。

### 修复前的根因代码

```csharp
// 首次请求前尚未生成任何 nonce
existingNonce = "bootstrap-nonce-placeholder";

// nonce 生成返回空值
nonce = "fallback-nonce";

// 异常路径
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## 修复内容

所有三个后备路径现在均调用 `Nonce.GenerateSecureNonce()` 在运行时生成全新的、不可预测的 16 字节随机 nonce：

```csharp
// 修复前（存在漏洞）：
existingNonce = "bootstrap-nonce-placeholder";
// 修复后（安全）：
existingNonce = Nonce.GenerateSecureNonce();

// 修复前（存在漏洞）：
nonce = "fallback-nonce";
// 修复后（安全）：
nonce = Nonce.GenerateSecureNonce();

// 修复前（存在漏洞）：
context.Items["Nonce"] = "error-fallback-nonce";
// 修复后（安全）：
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` 使用 `RandomNumberGenerator.Fill`（密码学安全伪随机数生成器）生成 16 个密码学随机字节并编码为 Base64。由于它是没有 Key Vault 依赖的静态方法，即使在 Key Vault 不可用时也可安全调用——而 Key Vault 不可用正是之前触发硬编码后备的错误条件。

---

## 如何保持修复状态

1. **永远不要在代码库的任何位置引入硬编码的 nonce 字面量**，无论上下文如何（后备、测试、占位符、可能被复制粘贴的注释示例等）。

2. **设置 `context.Items["Nonce"]` 的每条代码路径都必须使用密码学随机值。** 调用 `Nonce.GenerateSecureNonce()` 或 `RandomNumberGenerator.GetBytes(16)` + Base64。

3. **不要在请求间缓存单个 nonce。** 每个请求都必须接收自己的全新 nonce。

4. **错误路径是最危险的。** 如果 nonce 生成因任何原因失败，响应仍应接收随机 nonce，而永远不是可预测的后备值。

5. **审查 `OptimizedNonceMiddleware` 的任何未来更改**——尤其是可以设置 nonce 的三个分支：忽略路径分支、空生成分支和异常处理分支。

### 执行此修复的测试

| 测试 | 捕获的问题 |
|------|-----------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | 若 `"bootstrap-nonce-placeholder"` 被重新引入首次请求分支则失败 |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | 若 `"fallback-nonce"` 被重新引入空生成分支则失败 |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | 若 `"error-fallback-nonce"` 被重新引入异常处理程序则失败 |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | 若任何后备在 50 次连续调用中生成相同的 nonce（任何硬编码字符串都会如此）则失败 |
