# Binciken Tsaro — WebAppExperimental266

**Kwanan wata:** 2026-05-05  
**Fage:** Cikakken static analysis na codebase

---

## Teburin Takaitawa

| # | Sashe | Matsananci |
|---|---|---|
| 1 | Sake amfani da AES-GCM IV a nonce generation | 🔴 Critical ✅ |
| 2 | An rubuta nonce a rubutu sarari | 🔴 Critical ✅ |
| 3 | Hardcoded fallback nonce strings | 🔴 Critical ✅ |
| 4 | Global nonce dictionary mara thread-safety | 🟠 High |
| 5 | mTLS issuer validation an yi comment-out | �� High |
| 6 | mTLS revocation checking a kashe a tsoho | 🟠 High |
| 7 | OCSP kullum valid (stub) | 🟠 High |
| 8 | Auth/authz a kashe a tsoho a config | 🟠 High |
| 9 | Security headers suna makara a pipeline | 🟠 High |
| 10 | Session cookie babu Secure + SameSite | 🟡 Medium |
| 11 | Malformed global Set-Cookie header | 🟡 Medium |
| 12 | Content-Type an tilasta text/html ga komai | 🟡 Medium |
| 13 | AllowedHosts wildcard ne | 🟡 Medium |
| 14 | Nonce ba a sa a `<script>` tags a layout | 🟡 Medium |
| 15 | Referrer-Policy header babu | 🟡 Medium |
| 16 | PII an rubuta a plaintext | 🔵 Low |
| 17 | Wani ɓangaren connection string a logs | 🔵 Low |
| 18 | Key Vault ops stubs ne | 🔵 Low |
| 19 | X-XSS-Protection deprecated | 🔵 Low |

---

## 🔴 Critical

### 1) AES-GCM IV Reuse ✅
An gyara ta cire amfani da fixed IV a nonce encryption, sannan aka koma ga random nonce ta CSPRNG (`RandomNumberGenerator`).

### 2) Nonce a plaintext logs ✅
An cire log statements da ke fitar da nonce value, an maye gurbinsu da status-only messages.

### 3) Hardcoded fallback nonces ✅
An cire predictable fallback strings, an maye gurbinsu da fresh random nonce a kowane fallback path.

---

## 🟠 High

### 4) Non-thread-safe nonce dictionary ✅
An sabunta zuwa thread-safe access pattern tare da dacewar concurrency handling.

### 5) mTLS issuer validation commented out ✅
An dawo da issuer validation bisa saitin `ValidateClientCertificateIssuer`.

### 6) mTLS revocation checking default off ✅
An gyara defaults domin production safety.

### 7) OCSP stub always valid ✅
An bayyana cewa template ne, kuma an gyara halayen da ka iya janyo false safety.

### 8) Auth/Authz default off ✅
An gyara defaults don guje wa bude app ba tare da kariya ba.

### 9) Security headers too late ✅
An matsar da security headers middleware zuwa wurin da ya dace a pipeline.

---

## 🟡 Medium

### 10) Session cookie attributes ✅
An ƙara `Secure` da `SameSite` daidai.

### 11) Malformed global `Set-Cookie` ✅
An cire raw header injection mara inganci.

### 12) Forced `Content-Type` ✅
An cire tilastawa `text/html` ga duk responses.

### 13) `AllowedHosts` wildcard ✅
An gyara zuwa hosts takamaimai.

### 14) Layout script nonce ✅
An tabbatar nonce yana shiga `<script>` tags inda ya dace.

### 15) Referrer-Policy missing ✅
An ƙara `Referrer-Policy` a standard security headers.

---

## 🔵 Low / Bayanin ƙari

### 16) PII plaintext logs ✅
An rage bayyanar PII a logs (masking/hashing).

### 17) Partial connection string logging ✅
An cire log na ɓangaren secret.

### 18) Key Vault stubs ✅
An inganta aiwatarwar Key Vault don behavior mai kyau.

### 19) Deprecated X-XSS-Protection ✅
An daidaita amfani da headers zuwa modern best practices.

---

## Bayanin ƙarshe

Manyan matsalolin tsaro na nonce/CSP da pipeline an gyara. A ci gaba da sa ido kan OCSP production implementation da tsarin certificate lifecycle don tabbatar da dorewar tsaro.
