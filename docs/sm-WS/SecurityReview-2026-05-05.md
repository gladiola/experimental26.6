# Iloiloga Saogalemu — WebAppExperimental266

**Aso:** 2026-05-05  
**Avanoa:** Suʻesuʻega static atoa o le codebase

---

## Lisi Aotelega

| # | Vaega | Lavelave |
|---|---|---|
| 1 | Toe faaaogaina AES-GCM IV i nonce generation | 🔴 Mataʻutia ✅ |
| 2 | Nonce tusitusia plaintext i logs | 🔴 Mataʻutia ✅ |
| 3 | Hardcoded fallback nonce strings | 🔴 Mataʻutia ✅ |
| 4 | Non-thread-safe nonce dictionary | 🟠 Maualuga |
| 5 | mTLS issuer validation commented out | 🟠 Maualuga |
| 6 | mTLS revocation check off by default | 🟠 Maualuga |
| 7 | OCSP stub e toe faafoʻi valid | 🟠 Maualuga |
| 8 | Auth/AuthZ off by default i config | 🟠 Maualuga |
| 9 | Security headers tuai i pipeline | 🟠 Maualuga |
| 10 | Session cookie e leai Secure + SameSite | 🟡 Fafati |
| 11 | Malformed global Set-Cookie header | 🟡 Fafati |
| 12 | Content-Type forced i tali uma | 🟡 Fafati |
| 13 | AllowedHosts wildcard | 🟡 Fafati |
| 14 | Nonce e lē faʻaaogaina i layout script tags | 🟡 Fafati |
| 15 | Referrer-Policy missing | 🟡 Fafati |
| 16 | PII tusia plaintext | 🔵 Maualalo |
| 17 | Connection-string fragment i logs | 🔵 Maualalo |
| 18 | Key Vault ops o templates/stubs | 🔵 Maualalo |
| 19 | X-XSS-Protection deprecated | 🔵 Maualalo |

---

## Mataʻutia (Critical)

### 1) AES-GCM IV Reuse i Nonce Generation ✅

O le toe faaaogaina o IV ma ki tutusa i AES-GCM e solia ai puipuiga cryptographic. Na fautuaina le suiga i random nonce generation saʻo (`RandomNumberGenerator`) e aunoa ma encryption.

### 2) Nonce i Logs ✅

O le tusitusia nonce moni i logs e mafai ai CSP bypass. E tatau ona logs status-only.

### 3) Hardcoded Fallback Nonces ✅

Fallback literals tumau e valoia; e tatau ona random fallback i branch taitasi.

---

## Maualuga (High)

### 4) Nonce catalog global e le thread-safe ✅

Faaaoga `ConcurrentDictionary` ma retrieval atomic.

### 5) mTLS issuer validation disabled/commented ✅

Faamalosia issuer validation pe a ON `ValidateClientCertificateIssuer`.

### 6) Revocation check off by default ✅

Fautuaina default `true` mo gaosiga mTLS.

### 7) OCSP stub returns valid ✅

Aua le fail-open; pe a le atoatoa implementation, fail-closed pe tape feature.

### 8) Auth/AuthZ off by default ✅

Fautuaina defaults saogalemu ma opt-out manino.

### 9) Security headers tuai i middleware order ✅

Tuʻu headers i luma i pipeline ina ia aofia 401/403 ma short-circuit responses.

---

## Fafati (Medium)

Mea 10–15 e aofia ai cookie flags, malformed headers, forced content type, wildcard hosts, nonce omission i layout scripts, ma missing referrer policy.

---

## Maualalo / Faamatalaga

Mea 16–19 e aofia ai PII logging, partial secret logging, Key Vault stubs, ma deprecated X-XSS-Protection.

---

## Iʻuga

Na faamauina e le iloiloga le tele o vaivaiga saogalemu i vaega nonce, mTLS, OCSP, ma defaults/configuration. O faaleleiga mulimuli ane na taulai i fail-closed behavior, logging hygiene, ma thread safety.
