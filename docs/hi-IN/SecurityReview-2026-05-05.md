# सुरक्षा समीक्षा — WebAppExperimental266

**दिनांक:** 2026-05-05  
**स्कोप:** संपूर्ण कोडबेस स्थिर विश्लेषण

---

## सारांश तालिका

| # | क्षेत्र | गंभीरता |
|---|---|---|
| 1 | Nonce generation में AES-GCM IV reuse | 🔴 Critical ✅ |
| 2 | Nonce plaintext logging | 🔴 Critical ✅ |
| 3 | Hardcoded fallback nonce strings | 🔴 Critical ✅ |
| 4 | Non-thread-safe global nonce dictionary | 🟠 High |
| 5 | mTLS issuer validation commented out | 🟠 High |
| 6 | mTLS revocation checking default off | 🟠 High |
| 7 | OCSP stub always valid | 🟠 High |
| 8 | Auth/AuthZ defaults off | 🟠 High |
| 9 | Security headers pipeline में देर से | 🟠 High |
| 10 | Session cookie में Secure + SameSite गायब | 🟡 Medium |
| 11 | Malformed global Set-Cookie header | 🟡 Medium |
| 12 | Global Content-Type forced to text/html | 🟡 Medium |
| 13 | AllowedHosts wildcard | 🟡 Medium |
| 14 | Layout script tags में nonce लागू नहीं | 🟡 Medium |
| 15 | Referrer-Policy header missing | 🟡 Medium |
| 16 | PII plaintext logs | 🔵 Low |
| 17 | Partial connection string logs | 🔵 Low |
| 18 | Key Vault ops stubs | 🔵 Low |
| 19 | Deprecated X-XSS-Protection header | 🔵 Low |

---

## 🔴 Critical

### 1) AES-GCM IV Reuse — Nonce generation cryptographically broken ✅

`Nonce` generation fixed IV reuse कर रही थी। यह AES-GCM के लिए असुरक्षित है और integrity compromise कर सकता है।
सही समाधान: `RandomNumberGenerator.GetBytes(16)` आधारित random Base64 nonce generation।

### 2) Plaintext Nonce Logging ✅

nonce values सीधे logs में जाने से CSP bypass संभव था। status-only logging अपनाकर सुधार किया गया।

### 3) Hardcoded Fallback Nonces ✅

fallback branches में deterministic strings थीं; इन्हें `Nonce.GenerateSecureNonce()` से प्रतिस्थापित किया गया।

---

## 🟠 High (संक्षेप)

4. nonce catalog के लिए non-thread-safe shared dictionary का जोखिम (concurrency/integrity)।  
5. mTLS issuer validation logic टिप्पणी में था (enforcement अनुपस्थित)।  
6. revocation checking default off था।  
7. OCSP validation template stub था।  
8. authentication/authorization defaults secure-by-default नहीं थे।  
9. security headers middleware pipeline में देर से लागू थे।

---

## 🟡 Medium (संक्षेप)

10. session cookie hardening incomplete थी।  
11. malformed global `Set-Cookie` header था।  
12. `Content-Type` गलत तरीके से global override हो रहा था।  
13. `AllowedHosts="*"` host-header सुरक्षा कमजोर करता था।  
14. script tags में nonce attributes लागू नहीं थे।  
15. `Referrer-Policy` header अनुपस्थित था।

---

## 🔵 Low / Informational (संक्षेप)

16. PII plaintext logging risk।  
17. connection string snippet logging best-practice के विरुद्ध।  
18. Key Vault operations template/stub स्थिति।  
19. `X-XSS-Protection` header obsolete।

---

## निष्कर्ष

रिपोर्ट में दर्शाए गए critical मुद्दों के सुधार से nonce-संबंधित CSP सुरक्षा में महत्वपूर्ण मजबूती आई है। उच्च/मध्यम श्रेणी के बचे मुद्दों के लिए secure-by-default configuration, middleware ordering, और certificate validation path की production-grade hardening पर ध्यान देना चाहिए।
